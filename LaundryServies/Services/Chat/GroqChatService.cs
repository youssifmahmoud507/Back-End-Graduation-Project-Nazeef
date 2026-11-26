using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LaundryServiesAbstraction.IServices.Chat;
using LaundryShared.Common;
using LaundryShared.DTO.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LaundryServies.Services.Chat;

public class GroqChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IServiceProvider _serviceProvider;

    private const string SystemPromptTemplate = """
        You are "Nody", a friendly and empathetic customer support assistant for "Nadeef" — a smart laundry platform.

        LANGUAGE:
        - Always respond in Egyptian Arabic unless the user writes in English, then reply in English.
        - Output MUST be in only one language per reply (Arabic OR English), never mixed scripts.
        - Never use non-Arabic/non-English scripts (no Cyrillic, Japanese, Turkish transliteration, or mixed characters).
        - Brand name must be written only as: "Nadeef" in English or "نضيف" in Arabic.

        PERSONALITY:
        - Warm, empathetic, proactive, and solution-focused.
        - Never robotic or dismissive.
        - Use emojis sparingly.
        - Act as a human customer support representative and never say you are an AI model.
        - Do not introduce yourself in every message.
        - Avoid opening with repetitive phrases like "أكيد" or "أهلاً" every time.
        - Keep a lovable brand voice: polite, reassuring, and practical.
        - Use soft marketing naturally (no spam): suggest one useful next step or offer when relevant.

        EMPATHY & PROBLEM SOLVING (critical):
        - When a customer reports a problem, ALWAYS: (1) acknowledge their feeling first, (2) ask a clarifying question if needed to understand the issue better, (3) suggest a concrete next step or solution — do NOT just say "contact support".
        - Think through the problem WITH the customer step by step. If you need more info, ask ONE clear question at a time.
        - Examples of problems and how to handle them:
            * "طلبي اتأخر" → اعترف بالإزعاج، اسأل عن رقم الطلب لو مش موجود في الـ context، اعرض الحالة من ACTIVE_ORDERS، واقترح تواصل مع المغسلة أو رفع شكوى.
            * "الغسيل وصل وحش" → اعترف بالمشكلة، اشرح إزاي يبعت شكوى رسمية من الشات مباشرةً.
            * "مش قادر أدفع" → اسأل عن طريقة الدفع اللي بيحاول، واشرح الطرق المتاحة.
            * "مش عارف أعمل أوردر" → اشرح الخطوات ببساطة.
        - Never end a response with just "تواصل مع الدعم" without first trying to solve the problem yourself.
        - If you genuinely can't resolve it, offer to escalate via the in-app complaint system AND explain how.

        CONTEXT (injected at runtime — do NOT invent missing values):
        USER_NAME: {USER_NAME}
        ACTIVE_ORDERS: {ACTIVE_ORDERS}
        ORDER_HISTORY: {ORDER_HISTORY}
        NEAREST_LAUNDRIES: {NEAREST_LAUNDRIES}
        WALLET_BALANCE: {WALLET_BALANCE}
        SERVICE_PRICE_TABLE: {SERVICE_PRICE_TABLE}

        WHAT YOU CAN ANSWER FREELY (general knowledge — no context needed):
        - How the app works, how to place an order, how to track, how to cancel
        - Supported payment methods: cash on delivery, online payment via card
        - Laundry service types in general (washing, ironing, dry cleaning, folding)
        - How to add/change a delivery address
        - How bundles and subscriptions work
        - General delivery time estimates (depends on laundry, typically same-day or next-day)
        - How to submit a complaint via the chat
        - What "PendingConfirmation", "InProgress", "Delivered" statuses mean
        - Any general question about Nadeef's features or policies

        FORMATTING RULES:
        - Currency → always use numbers and "جم" (e.g., 25 جم)
        - ALWAYS USE NUMBERS (DIGITS) for prices, order IDs, and dates. NEVER write numbers as words.
        - Status icons:
            🟢 مكتمل / تم التوصيل
            🟡 جاري / قيد التنفيذ
            🔴 ملغي / مشكلة
        - If the user asks for "طلباتي النشطة" or "أوردراتي", YOU MUST RETURN THE EXACT MARKDOWN TABLE FROM 'ACTIVE_ORDERS'. Do not use bullet points!
        - "حالة طلبي" → ملخص آخر طلب في 4-6 نقاط
        - "أسعار الخدمات" → return the exact markdown table from SERVICE_PRICE_TABLE.
        - "أقرب مغسلة" → أقرب 3 من NEAREST_LAUNDRIES مع المسافة
        - لو ACTIVE_ORDERS = none → رد بشكل طبيعي وبشري، وقدم 2-3 خطوات مفيدة.

        TOOL: cancel_order
        - استخدمه فقط لو اليوزر طلب إلغاء طلب معين صراحةً
        - Parameters مطلوبة: { order_id: string }
        - ONLY permitted if the order is in "PendingConfirmation" status. If in washing, picked up, etc., refuse the cancellation gently and explain why.
        - لو طلب "إزاي أعمل إلغاء؟" → اشرح الخطوات واسأله عن رقم الطلب.

        STRICT RULES (user-specific data only):
        - لا تخترع أرقام طلبات، أسعار محددة، أو مغاسل مش موجودة في الـ context.
        - لو اليوزر سأل عن طلب بعينه ومش في الـ context → اطلب منه رقم الطلب أو اعرضله طلباته من ACTIVE_ORDERS.
        - لو سأل عن رصيد محفظته ومش في الـ context → قوله يدخل على صفحة "المحفظة" في التطبيق.
        - لا تقول "مش عندي التفاصيل دي" لأسئلة عامة عن التطبيق — أجاوب من معرفتك كـ Nody.
        - لا تنهي أي رد بجملة "تواصل مع الدعم" فقط — دايماً اعرض خطوة عملية الأول.
        - متبدأش ردك بـ "بالتأكيد!" أو "أهلاً وسهلاً!" في كل رسالة — خليها طبيعية.
        - If generated output contains mixed or corrupted scripts, regenerate cleanly in the target language only.
        - Never output words like: "ノディ", "Надиф", "burada".

        RAW_CONTEXT:
        {USER_CONTEXT}
        """;

    public GroqChatService(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_configuration["Groq:BaseUrl"]!)
        };
        var apiKey = _configuration["Groq:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<Result<ChatResponseDto>> SendMessageAsync(
        string userId, ChatRequestDto request)
    {
        try
        {
            var messages = BuildMessagesList(request);
            var payload = new
            {
                model = _configuration["Groq:Model"],
                messages,
                stream = false,
                tools = GetTools()
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _httpClient.PostAsync("/openai/v1/chat/completions", content);

            if (!resp.IsSuccessStatusCode)
            {
                var errBody = await resp.Content.ReadAsStringAsync();
                return Result<ChatResponseDto>.Failure($"Groq error: {resp.StatusCode} - {errBody}");
            }

            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array && toolCalls.GetArrayLength() > 0)
            {
                var tc = toolCalls[0];
                var toolCallId = tc.GetProperty("id").GetString();
                var fProp = tc.GetProperty("function");
                var toolName = fProp.GetProperty("name").GetString();
                var toolArgs = fProp.GetProperty("arguments").GetString();

                string toolResult = await ExecuteToolAsync(userId, toolName, toolArgs);

                messages.Add(new
                {
                    role = "assistant",
                    content = (string)null,
                    tool_calls = new[] {
                        new {
                            id = toolCallId,
                            type = "function",
                            function = new {
                                name = toolName,
                                arguments = toolArgs
                            }
                        }
                    }
                });

                messages.Add(new
                {
                    role = "tool",
                    tool_call_id = toolCallId,
                    name = toolName,
                    content = toolResult
                });

                var payload2 = new { model = _configuration["Groq:Model"], messages, stream = false, tools = GetTools() };
                using var content2 = new StringContent(JsonSerializer.Serialize(payload2), Encoding.UTF8, "application/json");
                using var resp2 = await _httpClient.PostAsync("/openai/v1/chat/completions", content2);
                var body2 = await resp2.Content.ReadAsStringAsync();

                using var doc2 = JsonDocument.Parse(body2);
                var replyText = doc2.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "No response";
                replyText = SanitizeAssistantOutput(replyText, IsLikelyEnglish(request.Message)).Trim();

                return Result<ChatResponseDto>.Success(new ChatResponseDto { Message = replyText, CreatedAt = DateTime.UtcNow });
            }

            var simpleReply = message.GetProperty("content").GetString() ?? "No response";
            simpleReply = SanitizeAssistantOutput(simpleReply, IsLikelyEnglish(request.Message)).Trim();
            return Result<ChatResponseDto>.Success(new ChatResponseDto { Message = simpleReply, CreatedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return Result<ChatResponseDto>.Failure($"Chatbot error: {ex.Message}");
        }
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string userId,
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = BuildMessagesList(request);
        var isEnglish = IsLikelyEnglish(request.Message);
        await foreach (var chunk in RunPipelineStreamAsync(userId, messages, isEnglish, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async IAsyncEnumerable<string> RunPipelineStreamAsync(
        string userId,
        List<object> messages,
        bool isEnglish,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = _configuration["Groq:Model"],
            messages,
            stream = true,
            tools = GetTools()
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await _httpClient.PostAsync("/openai/v1/chat/completions", content, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync(cancellationToken);
            yield return $"Groq streaming error: {(int)resp.StatusCode} - {errBody}";
            yield break;
        }

        using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        bool isToolCall = false;
        string toolCallId = null;
        string toolName = null;
        var toolArgs = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("data: ")) line = line["data: ".Length..];
            if (line == "[DONE]") break;

                string? activeChunk = null;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");

                    if (delta.TryGetProperty("tool_calls", out var tcProp) && tcProp.ValueKind == JsonValueKind.Array)
                    {
                        isToolCall = true;
                        var tc = tcProp[0];
                        if (tc.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                            toolCallId = idProp.GetString();

                        if (tc.TryGetProperty("function", out var fProp))
                        {
                            if (fProp.TryGetProperty("name", out var nProp) && nProp.ValueKind == JsonValueKind.String)
                                toolName = nProp.GetString();
                            if (fProp.TryGetProperty("arguments", out var aProp) && aProp.ValueKind == JsonValueKind.String)
                                toolArgs.Append(aProp.GetString());
                        }
                    }
                    else if (!isToolCall && delta.TryGetProperty("content", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                    {
                        var chunk = textProp.GetString();
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            activeChunk = SanitizeAssistantOutput(chunk, isEnglish);
                        }
                    }
                }
                catch { }

                if (activeChunk != null)
                {
                    yield return activeChunk;
                }
            }

            if (isToolCall && !string.IsNullOrEmpty(toolName))
        {
            var argsString = toolArgs.ToString();
            string toolResult = await ExecuteToolAsync(userId, toolName, argsString);

            messages.Add(new
            {
                role = "assistant",
                content = (string)null,
                tool_calls = new[] {
                    new {
                        id = toolCallId,
                        type = "function",
                        function = new {
                            name = toolName,
                            arguments = argsString
                        }
                    }
                }
            });

            messages.Add(new
            {
                role = "tool",
                tool_call_id = toolCallId,
                name = toolName,
                content = toolResult
            });

            await foreach (var chunk in RunPipelineStreamAsync(userId, messages, isEnglish, cancellationToken))
            {
                yield return chunk;
            }
        }
    }

    private static bool IsLikelyEnglish(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var letters = text.Where(char.IsLetter).ToList();
        if (letters.Count == 0) return false;
        var latin = letters.Count(c => c <= 127);
        return latin >= (letters.Count * 0.6);
    }

    private static string SanitizeAssistantOutput(string input, bool isEnglish)
    {
        if (string.IsNullOrWhiteSpace(input)) return input ?? string.Empty;

        var cleaned = input
            .Replace("Надيف", isEnglish ? "Nadeef" : "نضيف", StringComparison.OrdinalIgnoreCase)
            .Replace("ノدي", isEnglish ? "Nody" : "نودي", StringComparison.OrdinalIgnoreCase)
            .Replace("ノدي", isEnglish ? "Nody" : "نودي", StringComparison.OrdinalIgnoreCase)
            .Replace("burada", string.Empty, StringComparison.OrdinalIgnoreCase);

        var pattern = isEnglish
            ? @"[^\u0000-\u007F\u00A0-\u00FF]"
            : @"[^\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF0-9A-Za-z\s\p{P}|>+=<~`^*_#$]";

        cleaned = Regex.Replace(cleaned, pattern, string.Empty);
        cleaned = Regex.Replace(cleaned, "\\s{2,}", " ");

        return cleaned;
    }

    private List<object> BuildMessagesList(ChatRequestDto request)
    {
        var messages = new List<object>();
        var systemContextParts = new List<string>();
        var nonSystemHistory = new List<ChatMessageDto>();

        foreach (var m in request.History)
        {
            if (m == null) continue;
            var role = m.Role?.Trim().ToLowerInvariant();
            if (role == "system")
            {
                if (!string.IsNullOrWhiteSpace(m.Content))
                    systemContextParts.Add(m.Content);
            }
            else
            {
                nonSystemHistory.Add(m);
            }
        }

        var userContextStr = string.Join("\n", systemContextParts);

        var userName = ExtractContextValue(userContextStr, "USER_NAME")
                       ?? ExtractContextValue(userContextStr, "CUSTOMER_NAME")
                       ?? "unknown";

        var activeOrders = ExtractContextValue(userContextStr, "ACTIVE_ORDERS") ?? "none";
        var orderHistory = ExtractContextValue(userContextStr, "ORDER_HISTORY") ?? "unknown";
        var nearestLaundries = ExtractContextValue(userContextStr, "NEAREST_LAUNDRIES") ?? "unknown";
        var walletBalance = ExtractContextValue(userContextStr, "WALLET_BALANCE") ?? "unknown";
        var servicePriceTable = ExtractContextValue(userContextStr, "SERVICE_PRICE_TABLE")
                                ?? ExtractContextValue(userContextStr, "MARKET_AVERAGE_PRICES")
                                ?? "unknown";

        var finalSystemPromptStr = SystemPromptTemplate
            .Replace("{USER_NAME}", userName)
            .Replace("{ACTIVE_ORDERS}", activeOrders)
            .Replace("{ORDER_HISTORY}", orderHistory)
            .Replace("{NEAREST_LAUNDRIES}", nearestLaundries)
            .Replace("{WALLET_BALANCE}", walletBalance)
            .Replace("{SERVICE_PRICE_TABLE}", servicePriceTable)
            .Replace("{USER_CONTEXT}", string.IsNullOrWhiteSpace(userContextStr) ? "none" : userContextStr);

        messages.Add(new { role = "system", content = finalSystemPromptStr });

        foreach (var m in nonSystemHistory)
        {
            var rawRole = m.Role?.Trim().ToLowerInvariant();
            messages.Add(new { role = rawRole is "assistant" or "user" or "system" or "tool" ? rawRole : "user", content = m.Content ?? "" });
        }

        messages.Add(new { role = "user", content = request.Message });
        return messages;
    }

    private static object GetTools()
    {
        return new[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "cancel_order",
                    description = "Cancels a specific laundry order. Only use this directly when user asks to cancel an order explicitly.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            order_id = new { type = "string", description = "The ID of the order to cancel." }
                        },
                        required = new[] { "order_id" }
                    }
                }
            }
        };
    }

    private async Task<string> ExecuteToolAsync(string userId, string toolName, string argsJson)
    {
        if (toolName == "cancel_order")
        {
            try
            {
                using var doc = JsonDocument.Parse(argsJson);
                if (doc.RootElement.TryGetProperty("order_id", out var oidProp))
                {
                    int orderId;
                    if (oidProp.ValueKind == JsonValueKind.Number && oidProp.TryGetInt32(out var n))
                    {
                        orderId = n;
                    }
                    else if (oidProp.ValueKind == JsonValueKind.String && int.TryParse(oidProp.GetString(), out var s))
                    {
                        orderId = s;
                    }
                    else
                    {
                        return "Failure: order_id is missing or invalid.";
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var orderService = scope.ServiceProvider.GetRequiredService<LaundryServiesAbstraction.IServices.Order.IOrderService>();

                    var req = new LaundryShared.DTO.Order.CancelOrderRequest { CancellationReason = "Cancelled via Chatbot" };
                    var result = await orderService.CancelOrderAsync(userId, orderId, req);

                    if (result.IsSuccess) return "The order has been cancelled successfully.";
                    else return $"Failed to cancel order: {result.ErrorMessage}";
                }
                return "Failure: order_id is missing or invalid.";
            }
            catch (Exception ex)
            {
                return $"Error parsing arguments: {ex.Message}";
            }
        }
        return "Tool not found.";
    }

    private static string? ExtractContextValue(string context, string key)
    {
        if (string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(key))
            return null;

        var linePrefix = key + ":";
        var lines = context.Split('\n', StringSplitOptions.TrimEntries);
        var directLine = lines.FirstOrDefault(l => l.StartsWith(linePrefix, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(directLine))
            return directLine.Substring(linePrefix.Length).Trim();

        var markerIndex = context.IndexOf(linePrefix, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return null;

        var start = markerIndex + linePrefix.Length;
        var knownKeys = new[]
        {
            "USER_NAME:", "CUSTOMER_NAME:", "ACTIVE_ORDERS:", "ORDER_HISTORY:",
            "NEAREST_LAUNDRIES:", "WALLET_BALANCE:", "SERVICE_PRICE_TABLE:", "MARKET_AVERAGE_PRICES:"
        };

        var end = context.Length;
        foreach (var k in knownKeys)
        {
            var idx = context.IndexOf(k, start, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && idx < end)
                end = idx;
        }

        var value = context[start..end].Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
