using LaundryServiesAbstraction.IServices.AI;
using LaundryShared.DTO.AI;
using LaundryShared.DTO.Analytics;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaundryServies.Services.AI
{
    public class DemandForecastService : IDemandForecastService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public DemandForecastService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["OpenRouter:ApiKey"]!;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<DemandForecastDto?> GetDemandForecastAsync(LaundryAnalyticsDto analytics)
        {
            // بنحضر الـ monthly data عشان نبعتها للموديل
            var monthlyData = string.Join("\n", analytics.MonthlyRevenue
                .Select(m => $"- {m.Month}: {m.Orders} orders, {m.Revenue} EGP revenue"));

            var prompt = "You are a demand forecasting expert for laundry services in Egypt.\n" +
                         "Based on this laundry's historical data, forecast demand for next week.\n\n" +
                         "Historical Data (last 6 months):\n" +
                         $"{monthlyData}\n\n" +
                         $"Current Stats:\n" +
                         $"- Total Orders: {analytics.TotalOrders}\n" +
                         $"- Today's Orders: {analytics.TodayOrders}\n" +
                         $"- Active Orders: {analytics.ActiveOrders}\n" +
                         $"- Average Rating: {analytics.AverageRating}\n" +
                         $"- Most Requested Service: {analytics.MostRequestedService}\n\n" +
                         "Respond ONLY with a JSON object (no extra text):\n" +
                         "{\n" +
                         "  \"expected_orders_next_week\": number,\n" +
                         "  \"expected_revenue_next_week\": number,\n" +
                         "  \"demand_level\": \"High\" or \"Medium\" or \"Low\",\n" +
                         "  \"insights\": \"2-3 sentences in Arabic about the forecast\",\n" +
                         "  \"daily_breakdown\": [\n" +
                         "    {\"day\": \"Saturday\", \"expected_orders\": number},\n" +
                         "    {\"day\": \"Sunday\", \"expected_orders\": number},\n" +
                         "    {\"day\": \"Monday\", \"expected_orders\": number},\n" +
                         "    {\"day\": \"Tuesday\", \"expected_orders\": number},\n" +
                         "    {\"day\": \"Wednesday\", \"expected_orders\": number},\n" +
                         "    {\"day\": \"Thursday\", \"expected_orders\": number},\n" +
                         "    {\"day\": \"Friday\", \"expected_orders\": number}\n" +
                         "  ]\n" +
                         "}";

            var requestBody = new
            {
                model = "deepseek/deepseek-r1",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "https://openrouter.ai/api/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseString);

            // OpenRouter نفس format بتاع OpenAI
            var text = responseJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(text))
                return null;

            // DeepSeek R1 بيحط reasoning جوه <think> tags - بنشيلهم
            var thinkStart = text.IndexOf("<think>");
            var thinkEnd = text.IndexOf("</think>");
            if (thinkStart >= 0 && thinkEnd >= 0)
                text = text.Substring(thinkEnd + 8).Trim();

            text = text.Replace("```json", "").Replace("```", "").Trim();

            var result = JsonSerializer.Deserialize<DeepSeekInnerResult>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null) return null;

            return new DemandForecastDto
            {
                ExpectedOrdersNextWeek = result.ExpectedOrdersNextWeek,
                ExpectedRevenueNextWeek = result.ExpectedRevenueNextWeek,
                DemandLevel = Enum.TryParse<DemandLevel>(result.DemandLevel, true, out var level)
                              ? level
                              : DemandLevel.Medium,
                Insights = result.Insights,
                DailyBreakdown = result.DailyBreakdown?.Select(d => new DailyForecastDto
                {
                    Day = d.Day,
                    ExpectedOrders = d.ExpectedOrders
                }).ToList() ?? new()
            };
        }

        private class DeepSeekInnerResult
        {
            [JsonPropertyName("expected_orders_next_week")]
            public int ExpectedOrdersNextWeek { get; set; }

            [JsonPropertyName("expected_revenue_next_week")]
            public decimal ExpectedRevenueNextWeek { get; set; }

            [JsonPropertyName("demand_level")]
            public string DemandLevel { get; set; } = "";

            public string? Insights { get; set; }

            [JsonPropertyName("daily_breakdown")]
            public List<DailyBreakdownItem>? DailyBreakdown { get; set; }
        }

        private class DailyBreakdownItem
        {
            public string Day { get; set; } = "";

            [JsonPropertyName("expected_orders")]
            public int ExpectedOrders { get; set; }
        }
    }
}
