using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaundryShared.Enums.CustomerEnums
{
    public class ServiceCategoryConverter : JsonConverter<ServiceCategory>
    {
        public override ServiceCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // يدعم الرقم (مثلاً في services.json: "Category": 3) والنص
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.TryGetInt32(out var number) && Enum.IsDefined(typeof(ServiceCategory), number)
                    ? (ServiceCategory)number
                    : ServiceCategory.Wash;
            }

            if (reader.TokenType == JsonTokenType.Null)
                return ServiceCategory.Wash;

            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return ServiceCategory.Wash;

            return value.Trim().ToLower() switch
            {
                "washing" => ServiceCategory.Wash,
                "wash" => ServiceCategory.Wash,
                "dry cleaning" => ServiceCategory.DryCleaning,
                "drycleaning" => ServiceCategory.DryCleaning,
                "ironing" => ServiceCategory.Iron,
                "iron" => ServiceCategory.Iron,
                "special care" => ServiceCategory.Specialty,
                "specialty" => ServiceCategory.Specialty,
                _ => Enum.TryParse<ServiceCategory>(value, true, out var parsed) ? parsed : ServiceCategory.Wash
            };
        }

        public override void Write(Utf8JsonWriter writer, ServiceCategory value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}