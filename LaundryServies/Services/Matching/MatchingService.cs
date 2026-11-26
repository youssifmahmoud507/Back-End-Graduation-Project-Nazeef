using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Specifications.LaundrySpecs;
using LaundryServiesAbstraction.IServices.Matching;
using LaundryShared.Common;
using LaundryShared.DTO.Matching;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LaundryServies.Services.Matching
{
    /// <summary>
    /// بيستخدم Gemini 2.5 Flash عشان يختار
    /// أحسن مغسلة للعميل بناءً على موقعه وتفضيلاته
    /// </summary>
    public class MatchingService : IMatchingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public MatchingService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        // ============================================================
        // أحسن مغسلة واحدة
        // ============================================================
        public async Task<Result<MatchingResponseDto>> GetBestMatchAsync(
            string customerId, MatchingRequestDto request)
        {
            try
            {
                // بنجيب كل المغاسل الشغالة
                var laundries = await _unitOfWork.Repository<Laundry, int>()
                    .GetAllWithSpecAsync(new AllLaundriesSpec());

                // بنفلتر المغاسل القريبة بس
                var nearbyLaundries = laundries
                    .Where(l => l.Status == LaundryShared.Enums.CustomerEnums.LaundryStatus.Active)
                    .Select(l => new
                    {
                        l.Id,
                        l.Name,
                        l.Address,
                        l.AverageRating,
                        l.Availability,
                    Distance = CalculateDistance(
                            request.Latitude, request.Longitude,
                            (double)l.Latitude, (double)l.Longitude)
                    })
                    .Where(l => l.Distance <= (request.MaxDistanceKm ?? 10))
                    .OrderBy(l => l.Distance)
                    .Take(10)
                    .ToList();

                if (!nearbyLaundries.Any())
                    return Result<MatchingResponseDto>.Failure(
                        "No laundries found near your location");

                // بنبعت للـ Gemini عشان يختار الأحسن
                var sb = new StringBuilder();
                sb.AppendLine("أنت نظام ذكي لاختيار أفضل مغسلة.");
                sb.AppendLine("بناءً على البيانات دي اختار أحسن مغسلة للعميل:");
                sb.AppendLine();
                sb.AppendLine($"موقع العميل: {request.Latitude}, {request.Longitude}");
                sb.AppendLine($"نوع الخدمة المطلوبة: {request.ServiceType ?? "غسيل عادي"}");
                sb.AppendLine($"أقصى ميزانية: {request.MaxBudget ?? 0} جنيه");
                sb.AppendLine();
                sb.AppendLine("المغاسل المتاحة:");
                sb.AppendLine(JsonSerializer.Serialize(nearbyLaundries, new JsonSerializerOptions { WriteIndented = true }));
                sb.AppendLine();
                sb.AppendLine("ارجع الرد بالـ JSON format ده بس:");
                sb.AppendLine("{\"laundryId\": 0,\n\"laundryName\": \"\",\n\"address\": \"\",\n\"distanceKm\": 0.0,\n\"rating\": 0.0,\n\"reason\": \"\",\n\"estimatedDeliveryTime\": \"\"}");

                var prompt = sb.ToString();

                var result = await CallGeminiAsync(prompt);

                if (result is null)
                    return Result<MatchingResponseDto>.Failure(
                        "AI matching failed");

                return Result<MatchingResponseDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<MatchingResponseDto>.Failure(
                    $"Matching error: {ex.Message}");
            }
        }

        // ============================================================
        // أحسن 3 مغاسل
        // ============================================================
        public async Task<Result<List<MatchingResponseDto>>> GetTopMatchesAsync(
            string customerId, MatchingRequestDto request)
        {
            try
            {
                var laundries = await _unitOfWork.Repository<Laundry, int>()
                    .GetAllWithSpecAsync(new AllLaundriesSpec());

                var nearbyLaundries = laundries
                    .Where(l => l.Status == LaundryShared.Enums.CustomerEnums.LaundryStatus.Active)
                    .Select(l => new
                    {
                        l.Id,
                        l.Name,
                        l.Address,
                        l.AverageRating,
                        l.Availability,
                    Distance = CalculateDistance(
                            request.Latitude, request.Longitude,
                            (double)l.Latitude, (double)l.Longitude)
                    })
                    .Where(l => l.Distance <= (request.MaxDistanceKm ?? 10))
                    .OrderBy(l => l.Distance)
                    .Take(10)
                    .ToList();

                if (!nearbyLaundries.Any())
                    return Result<List<MatchingResponseDto>>.Failure(
                        "No laundries found near your location");

                var sb = new StringBuilder();
                sb.AppendLine("أنت نظام ذكي لاختيار أفضل المغاسل.");
                sb.AppendLine("بناءً على البيانات دي اختار أحسن 3 مغاسل للعميل ورتبهم:");
                sb.AppendLine();
                sb.AppendLine($"موقع العميل: {request.Latitude}, {request.Longitude}");
                sb.AppendLine($"نوع الخدمة: {request.ServiceType ?? "غسيل عادي"}");
                sb.AppendLine($"أقصى ميزانية: {request.MaxBudget ?? 0} جنيه");
                sb.AppendLine();
                sb.AppendLine("المغاسل المتاحة:");
                sb.AppendLine(JsonSerializer.Serialize(nearbyLaundries, new JsonSerializerOptions { WriteIndented = true }));
                sb.AppendLine();
                sb.AppendLine("ارجع الرد بالـ JSON format ده بس (array):");
                sb.AppendLine("[ {\"laundryId\": 0, \"laundryName\": \"\", \"address\": \"\", \"distanceKm\": 0.0, \"rating\": 0.0, \"reason\": \"\", \"estimatedDeliveryTime\": \"\" } ]");

                var prompt = sb.ToString();

                var jsonResponse = await CallGeminiRawAsync(prompt);

                if (string.IsNullOrEmpty(jsonResponse))
                    return Result<List<MatchingResponseDto>>.Failure(
                        "AI matching failed");

                // بنحاول نعمل Parse للـ JSON
                var clean = jsonResponse
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                var results = JsonSerializer.Deserialize<List<MatchingResponseDto>>(
                    clean, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return Result<List<MatchingResponseDto>>.Success(
                    results ?? new List<MatchingResponseDto>());
            }
            catch (Exception ex)
            {
                return Result<List<MatchingResponseDto>>.Failure(
                    $"Matching error: {ex.Message}");
            }
        }

        // ============================================================
        // Helper - بيكلم Gemini API
        // ============================================================
        private async Task<MatchingResponseDto?> CallGeminiAsync(string prompt)
        {
            var raw = await CallGeminiRawAsync(prompt);
            if (string.IsNullOrEmpty(raw)) return null;

            var clean = raw
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            return JsonSerializer.Deserialize<MatchingResponseDto>(
                clean, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        private async Task<string?> CallGeminiRawAsync(string prompt)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var model = _configuration["Gemini:Model"];
            var baseUrl = _configuration["Gemini:BaseUrl"];

            var url = $"{baseUrl}/v1beta/models/{model}:generateContent?key={apiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 1000
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Gemini Error: {body}");
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }

        // ============================================================
        // Helper - بيحسب المسافة بين نقطتين
        // Haversine Formula
        // ============================================================
        private double CalculateDistance(
            double lat1, double lon1,
            double lat2, double lon2)
        {
            const double R = 6371; // نصف قطر الأرض بالكيلومتر

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
