using LaundryServiesAbstraction.IServices.AI;
using LaundryShared.DTO.AI;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaundryServies.Services.AI
{
    public class SentimentService : ISentimentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public SentimentService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Cohere:ApiKey"]!;

            // ✅ بنحطهم مرة واحدة بس في الـ constructor
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<SentimentResultDto?> AnalyzeSentimentAsync(string comment, int rating)
        {



            // لو مفيش comment نرجع based on rating بس
            if (string.IsNullOrWhiteSpace(comment))
            {
                return new SentimentResultDto
                {
                    Sentiment = rating >= 4 ? SentimentType.Positive
                              : rating == 3 ? SentimentType.Neutral
                              : SentimentType.Negative,
                    ConfidenceScore = 1.0f,
                    Summary = null
                };
            }

            var prompt = $"Analyze the sentiment of this laundry service review.\n" +
                         $"Rating: {rating}/5\n" +
                         $"Comment: \"{comment}\"\n\n" +
                         "Respond ONLY with a JSON object like this (no extra text):\n" +
                         "{\n" +
                         "  \"sentiment\": \"Positive\" or \"Negative\" or \"Neutral\",\n" +
                         "  \"confidence_score\": 0.0 to 1.0,\n" +
                         "  \"summary\": \"one short sentence summary in the same language as the comment\"\n" +
                         "}";

            var requestBody = new
            {
                model = "command-r-plus-08-2024",
                message = prompt,
                temperature = 0.2
            };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.PostAsync("https://api.cohere.com/v1/chat", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseString);

            var text = responseJson.RootElement
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(text))
                return null;

            text = text.Replace("```json", "").Replace("```", "").Trim();

            var result = JsonSerializer.Deserialize<CohereInnerResult>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null) return null;

            // ✅ دلوقتي بنرجع نتيجة Cohere الفعلية
            return new SentimentResultDto
            {
                Sentiment = Enum.TryParse<SentimentType>(result.Sentiment, true, out var parsed)
                            ? parsed
                            : SentimentType.Neutral, // fallback لو Cohere رجع حاجة غريبة
                ConfidenceScore = result.ConfidenceScore,
                Summary = result.Summary
            };
        }

        private class CohereInnerResult
        {
            public string Sentiment { get; set; } = "";

            [JsonPropertyName("confidence_score")]
            public float ConfidenceScore { get; set; }

            public string? Summary { get; set; }
        }
    }
}
