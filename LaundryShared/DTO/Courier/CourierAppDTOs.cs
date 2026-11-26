using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LaundryShared.DTO.Courier
{
    public class CourierProfileResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("totalOrders")]
        public int TotalOrders { get; set; }

        [JsonPropertyName("completionRate")]
        public double CompletionRate { get; set; }

        [JsonPropertyName("activeHours")]
        public int ActiveHours { get; set; }

        [JsonPropertyName("isOnline")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("availability")]
        public string Availability { get; set; } = string.Empty;

        [JsonPropertyName("unreadNotifications")]
        public int UnreadNotifications { get; set; }

        [JsonPropertyName("assignedLaundry")]
        public CourierAssignedLaundryDto? AssignedLaundry { get; set; }

        [JsonPropertyName("performance")]
        public CourierPerformanceOverviewDto? Performance { get; set; }
    }

    public class CourierAssignedLaundryDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class CourierPerformanceOverviewDto
    {
        [JsonPropertyName("completed")]
        public int Completed { get; set; }

        [JsonPropertyName("cancelled")]
        public int Cancelled { get; set; }

        [JsonPropertyName("hours")]
        public int Hours { get; set; }

        [JsonPropertyName("earningsThisMonth")]
        public string EarningsThisMonth { get; set; } = string.Empty;
    }

    public class UpdateCourierStatusRequestDto
    {
        [JsonPropertyName("isOnline")]
        public bool IsOnline { get; set; }
    }

    public class CourierStatsResponseDto
    {
        [JsonPropertyName("todaysOrders")]
        public int TodaysOrders { get; set; }

        [JsonPropertyName("inTransit")]
        public int InTransit { get; set; }

        [JsonPropertyName("earnedToday")]
        public decimal EarnedToday { get; set; }

        [JsonPropertyName("vsYesterdayPercentage")]
        public double VsYesterdayPercentage { get; set; }
    }

    public class CourierOrderResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("customer")]
        public string Customer { get; set; } = string.Empty;

        [JsonPropertyName("service")]
        public string Service { get; set; } = string.Empty;

        [JsonPropertyName("itemsCount")]
        public int ItemsCount { get; set; }

        [JsonPropertyName("pickupArea")]
        public string PickupArea { get; set; } = string.Empty;

        [JsonPropertyName("dropArea")]
        public string DropArea { get; set; } = string.Empty;

        [JsonPropertyName("distance")]
        public string Distance { get; set; } = string.Empty;

        [JsonPropertyName("eta")]
        public string Eta { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // pending, accepted, picked_up, delivered

        [JsonPropertyName("isUrgent")]
        public bool IsUrgent { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("isPaid")]
        public bool IsPaid { get; set; }
    }

    public class CourierActiveRunResponseDto
    {
        [JsonPropertyName("runId")]
        public string RunId { get; set; } = string.Empty;

        [JsonPropertyName("totalDistance")]
        public string TotalDistance { get; set; } = string.Empty;

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("stopsDone")]
        public int StopsDone { get; set; }

        [JsonPropertyName("totalStops")]
        public int TotalStops { get; set; }

        [JsonPropertyName("earnedSoFar")]
        public decimal EarnedSoFar { get; set; }

        [JsonPropertyName("remainingAmount")]
        public decimal RemainingAmount { get; set; }

        [JsonPropertyName("laundryName")]
        public string LaundryName { get; set; } = string.Empty;

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("stops")]
        public List<CourierActiveRunStopDto> Stops { get; set; } = new();
    }

    public class CourierActiveRunStopDto
    {
        [JsonPropertyName("stopNum")]
        public int StopNum { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("customer")]
        public string Customer { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("area")]
        public string Area { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("items")]
        public int Items { get; set; }

        [JsonPropertyName("service")]
        public string Service { get; set; } = string.Empty;

        [JsonPropertyName("etaFromNow")]
        public string EtaFromNow { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // done, current, upcoming

        [JsonPropertyName("isUrgent")]
        public bool IsUrgent { get; set; }

        [JsonPropertyName("isPaid")]
        public bool IsPaid { get; set; }
    }

    public class DeliverStopRequestDto
    {
        [JsonPropertyName("cashCollected")]
        public bool CashCollected { get; set; }

        [JsonPropertyName("actualDeliveryTime")]
        public DateTime ActualDeliveryTime { get; set; }
    }

    public class LocationSyncRequestDto
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }

        [JsonPropertyName("heading")]
        public double Heading { get; set; }

        [JsonPropertyName("speed")]
        public double Speed { get; set; }
    }

    public class CourierEarningsResponseDto
    {
        [JsonPropertyName("totalEarned")]
        public decimal TotalEarned { get; set; }

        [JsonPropertyName("ordersDone")]
        public int OrdersDone { get; set; }

        [JsonPropertyName("hoursActive")]
        public int HoursActive { get; set; }

        [JsonPropertyName("avgPerOrder")]
        public decimal AvgPerOrder { get; set; }

        [JsonPropertyName("cancelled")]
        public int Cancelled { get; set; }

        [JsonPropertyName("completionRate")]
        public double CompletionRate { get; set; }

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("topPercent")]
        public int TopPercent { get; set; }

        [JsonPropertyName("dailyEarnings")]
        public List<DailyEarningDto> DailyEarnings { get; set; } = new();

        [JsonPropertyName("recentTransactions")]
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();

        [JsonPropertyName("nextPayoutAmount")]
        public decimal NextPayoutAmount { get; set; }

        [JsonPropertyName("nextPayoutDate")]
        public string NextPayoutDate { get; set; } = string.Empty;
    }

    public class DailyEarningDto
    {
        [JsonPropertyName("day")]
        public string Day { get; set; } = string.Empty; // "Mon", "Tue"

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }

    public class RecentTransactionDto
    {
        [JsonPropertyName("customer")]
        public string Customer { get; set; } = string.Empty;

        [JsonPropertyName("area")]
        public string Area { get; set; } = string.Empty;

        [JsonPropertyName("timeAgo")]
        public string TimeAgo { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty; // "+EGP 95", "-"
    }
}
