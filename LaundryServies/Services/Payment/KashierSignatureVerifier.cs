using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LaundryServies.Services.Payment
{
    /// <summary>
    /// التحقق من توقيعات Kashier (HMAC-SHA256).
    /// - Redirect callback: التوقيع في الـ query param اسمه "signature" ومحسوب على باقي الـ query (من غير signature و mode).
    /// - Server webhook: التوقيع في الـ header x-kashier-signature ومحسوب على الحقول المذكورة في data.signatureKeys (مرتبة أبجدياً).
    /// بنجرّب الـ secrets المتاحة (Payment API Key و Secret API Key) وكذلك نسخة الـ encoded والـ raw من النص الموقّع،
    /// وكلهم محتاجين السر فمفيش تساهل أمني في ده. أي حاجة غير كده بتتعامل كإنها مش موثقة (fail closed).
    /// </summary>
    public static class KashierSignatureVerifier
    {
        public static string ComputeHmacSha256Hex(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool SignatureMatches(string payload, string? providedSignature, IEnumerable<string?> secrets)
        {
            if (string.IsNullOrWhiteSpace(providedSignature)) return false;
            var provided = Encoding.UTF8.GetBytes(providedSignature.Trim().ToLowerInvariant());

            foreach (var secret in secrets.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var expected = Encoding.UTF8.GetBytes(ComputeHmacSha256Hex(payload, secret!));
                if (expected.Length == provided.Length && CryptographicOperations.FixedTimeEquals(expected, provided))
                    return true;
            }

            return false;
        }

        /// <summary>التحقق من توقيع الـ redirect callback (الـ query string).</summary>
        public static bool VerifyRedirect(IEnumerable<KeyValuePair<string, string>> query, IEnumerable<string?> secrets)
        {
            var list = query.ToList();
            var signature = list.FirstOrDefault(kv => string.Equals(kv.Key, "signature", StringComparison.OrdinalIgnoreCase)).Value;
            if (string.IsNullOrWhiteSpace(signature)) return false;

            var signed = list
                .Where(kv => !string.Equals(kv.Key, "signature", StringComparison.OrdinalIgnoreCase)
                          && !string.Equals(kv.Key, "mode", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var plain = string.Join("&", signed.Select(kv => $"{kv.Key}={kv.Value}"));
            if (SignatureMatches(plain, signature, secrets)) return true;

            var encoded = string.Join("&", signed.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            return SignatureMatches(encoded, signature, secrets);
        }

        /// <summary>التحقق من توقيع الـ server webhook من الـ raw body.</summary>
        public static bool VerifyWebhook(string rawBody, string? headerSignature, IEnumerable<string?> secrets)
        {
            if (string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(headerSignature)) return false;

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                    return false;

                if (!data.TryGetProperty("signatureKeys", out var keysElement) || keysElement.ValueKind != JsonValueKind.Array)
                    return false;

                var keys = keysElement.EnumerateArray()
                    .Where(k => k.ValueKind == JsonValueKind.String)
                    .Select(k => k.GetString()!)
                    .OrderBy(k => k, StringComparer.Ordinal)
                    .ToList();

                if (keys.Count == 0) return false;

                var pairs = new List<KeyValuePair<string, string>>();
                foreach (var key in keys)
                {
                    var value = data.TryGetProperty(key, out var element) ? ElementToString(element) : string.Empty;
                    pairs.Add(new KeyValuePair<string, string>(key, value));
                }

                var plain = string.Join("&", pairs.Select(kv => $"{kv.Key}={kv.Value}"));
                if (SignatureMatches(plain, headerSignature, secrets)) return true;

                var encoded = string.Join("&", pairs.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                return SignatureMatches(encoded, headerSignature, secrets);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string ElementToString(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
    }
}
