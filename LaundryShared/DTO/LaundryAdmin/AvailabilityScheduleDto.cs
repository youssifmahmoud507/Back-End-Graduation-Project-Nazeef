using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace LaundryShared.DTO.LaundryAdmin
{
    public class AvailabilityScheduleDto
    {
        public List<AvailabilityScheduleDayDto> Days { get; set; } = new();
    }

    public class AvailabilityScheduleDayDto
    {
        [JsonPropertyName("dayOfWeek")]
        public int DayOfWeek { get; set; }
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; }
    }

    public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return TimeSpan.Zero;

            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return TimeSpan.Zero;

            TimeSpan finalResult;
            if (TimeSpan.TryParse(value, out var result))
            {
                finalResult = result;
            }
            else if (DateTime.TryParse(value, out var dtResult))
            {
                finalResult = dtResult.TimeOfDay;
            }
            else
            {
                throw new JsonException($"Invalid TimeSpan format: {value}");
            }

            if (finalResult.TotalHours >= 24) return new TimeSpan(23, 59, 59);
            if (finalResult.TotalHours < 0) return TimeSpan.Zero;
            return finalResult;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(@"hh\:mm\:ss"));
        }
    }
}
