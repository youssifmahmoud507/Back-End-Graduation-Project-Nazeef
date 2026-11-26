using LaundryServiesAbstraction.IServices.AI;
using LaundryShared.DTO.AI;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaundryServies.Services.AI
{
    public class PriceRecommendationService : IPriceRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly ILogger<PriceRecommendationService> _logger;
        private readonly IMemoryCache _cache;

        public PriceRecommendationService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<PriceRecommendationService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _apiKey = config["Mistral:ApiKey"];
            _logger = logger;
            _cache = cache;

            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            _httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<PriceRecommendationDto?> GetPriceRecommendationAsync(string serviceName, ServiceCategory category)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return BuildFallbackRecommendation(
                    serviceName,
                    category,
                    "Fallback pricing was used because the service name was empty.");
            }

            var cacheKey = $"PriceRec_{serviceName.Trim().ToLowerInvariant()}_{category}";
            if (_cache.TryGetValue(cacheKey, out PriceRecommendationDto? cachedRecommendation) && cachedRecommendation != null)
            {
                _logger.LogInformation("Returning cached price recommendation for service: {ServiceName}", serviceName);
                return cachedRecommendation;
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Mistral API key is missing. Using fallback price recommendation.");
                var fallback = BuildFallbackRecommendation(
                    serviceName,
                    category,
                    "Fallback pricing was used because the AI service is not configured.");
                
                // Cache fallback recommendations too to prevent repeating expensive fallback logic or calls
                _cache.Set(cacheKey, fallback, TimeSpan.FromHours(4));
                return fallback;
            }

            try
            {
                var prompt = "You are a pricing expert for laundry services in Egypt.\n" +
                             $"Service Name: {serviceName}\n" +
                             $"Category: {category}\n\n" +
                             "Based on the Egyptian laundry market (prices in EGP), suggest a fair price range.\n" +
                             "Respond ONLY with a valid JSON object (no extra text, no markdown, no Chinese characters):\n" +
                             "{\n" +
                             "  \"suggested_min_price\": number,\n" +
                             "  \"suggested_max_price\": number,\n" +
                             "  \"recommended_price\": number,\n" +
                             "  \"reasoning\": \"short explanation in Arabic language only\"\n" +
                             "}";

                var requestBody = new
                {
                    model = "mistral-small-latest",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://api.mistral.ai/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Mistral price recommendation failed with status {StatusCode}. Response: {Response}",
                        response.StatusCode,
                        errorBody);

                    return BuildFallbackRecommendation(
                        serviceName,
                        category,
                        "Fallback pricing was used because the AI provider returned an error.");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseString);

                var text = responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Mistral returned an empty response for price recommendation.");
                    return BuildFallbackRecommendation(
                        serviceName,
                        category,
                        "Fallback pricing was used because the AI response was empty.");
                }

                text = text.Replace("```json", "").Replace("```", "").Trim();

                var result = JsonSerializer.Deserialize<MistralInnerResult>(text, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    _logger.LogWarning("Mistral response could not be parsed for service {ServiceName}.", serviceName);
                    return BuildFallbackRecommendation(
                        serviceName,
                        category,
                        "Fallback pricing was used because the AI response could not be parsed.");
                }

                var recommendation = new PriceRecommendationDto
                {
                    SuggestedMinPrice = result.SuggestedMinPrice,
                    SuggestedMaxPrice = result.SuggestedMaxPrice,
                    RecommendedPrice = result.RecommendedPrice,
                    Reasoning = result.Reasoning
                };

                _cache.Set(cacheKey, recommendation, TimeSpan.FromHours(24));
                return recommendation;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Price recommendation failed for service {ServiceName} in category {Category}. Using fallback.",
                    serviceName,
                    category);

                return BuildFallbackRecommendation(
                    serviceName,
                    category,
                    "Fallback pricing was used because the AI service is temporarily unavailable.");
            }
        }

        private static PriceRecommendationDto BuildFallbackRecommendation(
            string serviceName,
            ServiceCategory category,
            string reasoning)
        {
            var normalizedName = serviceName.Trim().ToLowerInvariant();

            var basePrice = category switch
            {
                ServiceCategory.Wash => 45m,
                ServiceCategory.DryCleaning => 90m,
                ServiceCategory.Iron => 30m,
                ServiceCategory.Specialty => 120m,
                _ => 50m
            };

            if (normalizedName.Contains("blanket") || normalizedName.Contains("comforter"))
                basePrice += 60m;
            else if (normalizedName.Contains("suit") || normalizedName.Contains("dress"))
                basePrice += 35m;
            else if (normalizedName.Contains("shirt") || normalizedName.Contains("t-shirt"))
                basePrice -= 10m;
            else if (normalizedName.Contains("curtain"))
                basePrice += 40m;

            var minPrice = Math.Max(10m, Math.Round(basePrice * 0.8m, 0));
            var maxPrice = Math.Round(basePrice * 1.2m, 0);
            var recommendedPrice = Math.Round((minPrice + maxPrice) / 2m, 0);

            return new PriceRecommendationDto
            {
                SuggestedMinPrice = minPrice,
                SuggestedMaxPrice = maxPrice,
                RecommendedPrice = recommendedPrice,
                Reasoning = reasoning
            };
        }

        private class MistralInnerResult
        {
            [JsonPropertyName("suggested_min_price")]
            public decimal SuggestedMinPrice { get; set; }

            [JsonPropertyName("suggested_max_price")]
            public decimal SuggestedMaxPrice { get; set; }

            [JsonPropertyName("recommended_price")]
            public decimal RecommendedPrice { get; set; }

            public string? Reasoning { get; set; }
        }
    }
}
