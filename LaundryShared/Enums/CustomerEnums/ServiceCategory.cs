using System.Text.Json.Serialization;

namespace LaundryShared.Enums.CustomerEnums
{
    [JsonConverter(typeof(ServiceCategoryConverter))]
    public enum ServiceCategory
    {
        Wash = 1,
        DryCleaning = 2,
        Iron = 3,
        Specialty = 4
    }
}

