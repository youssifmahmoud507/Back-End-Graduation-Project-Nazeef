using LaundryDomain.IRepositories;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Specifications.LaundryAdminSpec;
using LaundryServiesAbstraction.IServices.Fraud;
using LaundryShared.Common;
using LaundryShared.DTO.Fraud;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using LaundryDomain.Models.UserModules;
using LaundryDomain.Specifications.LaundryAdminSpec;
using Microsoft.AspNetCore.Identity;

namespace LaundryServies.Services.Fraud
{
    /// <summary>
    /// بيستخدم Cerebras Qwen 3 235B
    /// عشان يكشف العمليات المشبوهة
    /// </summary>
    public class FraudService : IFraudService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly UserManager<ApplicationUser> _userManager;

        public FraudService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            HttpClient httpClient,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpClient = httpClient;
            _userManager = userManager;
        }

        // ============================================================
        // تحليل عملية معينة
        // ============================================================
        public async Task<Result<FraudResultDto>> AnalyzeActionAsync(
            FraudDetectionDto request)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("أنت نظام ذكي لكشف الغش والعمليات المشبوهة.");
                sb.AppendLine("حلل العملية دي وقول هي مشبوهة ولا لا:");
                sb.AppendLine();
                sb.AppendLine($"نوع العملية: {request.ActionType}");
                sb.AppendLine($"الـ IP Address: {request.IpAddress}");
                sb.AppendLine($"المبلغ: {request.Amount ?? 0} جنيه");
                sb.AppendLine($"عدد المحاولات الفاشلة: {request.FailedAttempts}");
                sb.AppendLine($"وقت العملية: {request.ActionTime}");
                sb.AppendLine();
                sb.AppendLine("ارجع الرد بالـ JSON format ده بس:");
                sb.AppendLine("{\"isSuspicious\": false, \"riskScore\": 0, \"reason\": \"\", \"recommendedAction\": \"Allow\" }");
                sb.AppendLine();
                sb.AppendLine("قواعد:");
                sb.AppendLine("- riskScore من 0 لـ 100");
                sb.AppendLine("- recommendedAction: Allow / Warn / Block");
                sb.AppendLine("- لو riskScore > 70 يبقى isSuspicious = true");

                var prompt = sb.ToString();

                var result = await CallGeminiAsync(prompt);

                if (result is null)
                    return Result<FraudResultDto>.Failure(
                        "Fraud analysis failed");

                return Result<FraudResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<FraudResultDto>.Failure(
                    $"Fraud detection error: {ex.Message}");
            }
        }

        // ============================================================
        // تحليل سلوك يوزر معين
        // ============================================================
        public async Task<Result<FraudResultDto>> AnalyzeUserBehaviorAsync(
            string userId)
        {
            try
            {
                // بنجيب أوردرات اليوزر في آخر 24 ساعة
                var orders = await _unitOfWork.Repository<Order, int>()
                    .GetAllWithSpecAsync(new RecentOrdersByUserSpec(userId));

                var orderCount = orders.Count();
                var totalAmount = orders.Sum(o => o.TotalPrice);
                var cancelledCount = orders.Count(o =>
                    o.Status == LaundryShared.Enums.CustomerEnums.OrderStatus.Cancelled);

                var sb = new StringBuilder();
                sb.AppendLine("أنت نظام ذكي لكشف الغش.");
                sb.AppendLine("حلل سلوك اليوزر ده في آخر 24 ساعة:");
                sb.AppendLine();
                sb.AppendLine($"عدد الأوردرات: {orderCount}");
                sb.AppendLine($"إجمالي المبالغ: {totalAmount} جنيه");
                sb.AppendLine($"عدد الأوردرات الملغية: {cancelledCount}");
                sb.AppendLine($"نسبة الإلغاء: {(orderCount > 0 ? (cancelledCount * 100 / orderCount) : 0)}%");
                sb.AppendLine();
                sb.AppendLine("ارجع الرد بالـ JSON format ده بس:");
                sb.AppendLine("{\"isSuspicious\": false, \"riskScore\": 0, \"reason\": \"\", \"recommendedAction\": \"Allow\" }");
                sb.AppendLine();
                sb.AppendLine("قواعد:");
                sb.AppendLine("- لو عدد الأوردرات > 10 في 24 ساعة يبقى مشبوه");
                sb.AppendLine("- لو نسبة الإلغاء > 50% يبقى مشبوه");
                sb.AppendLine("- لو المبلغ > 5000 جنيه في 24 ساعة يبقى مشبوه");
                sb.AppendLine("- riskScore من 0 لـ 100");
                sb.AppendLine("- recommendedAction: Allow / Warn / Block");

                var prompt = sb.ToString();

                var result = await CallGeminiAsync(prompt);

                if (result is null)
                    return Result<FraudResultDto>.Failure(
                        "Behavior analysis failed");

                return Result<FraudResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<FraudResultDto>.Failure(
                    $"Behavior analysis error: {ex.Message}");
            }
        }

        // ============================================================
        // تحليل عملية دفع مشبوهة
        // ============================================================
        public async Task<Result<FraudResultDto>> AnalyzePaymentAsync(
            string userId, decimal amount, string ipAddress)
        {
            try
            {
                // بنجيب عدد محاولات الدفع الفاشلة
                var recentOrders = await _unitOfWork.Repository<Order, int>()
                    .GetAllWithSpecAsync(new RecentOrdersByUserSpec(userId));

                var failedPayments = recentOrders
                    .Count(o => o.Status ==
                        LaundryShared.Enums.CustomerEnums.OrderStatus.Cancelled);

                var sb = new StringBuilder();
                sb.AppendLine("أنت نظام ذكي لكشف غش المدفوعات.");
                sb.AppendLine("حلل عملية الدفع دي:");
                sb.AppendLine();
                sb.AppendLine($"المبلغ: {amount} جنيه");
                sb.AppendLine($"الـ IP Address: {ipAddress}");
                sb.AppendLine($"عدد المحاولات الفاشلة السابقة: {failedPayments}");
                sb.AppendLine($"وقت العملية: {DateTime.UtcNow}");
                sb.AppendLine();
                sb.AppendLine("ارجع الرد بالـ JSON format ده بس:");
                sb.AppendLine("{\"isSuspicious\": false, \"riskScore\": 0, \"reason\": \"\", \"recommendedAction\": \"Allow\" }");
                sb.AppendLine();
                sb.AppendLine("قواعد:");
                sb.AppendLine("- لو المبلغ > 2000 جنيه يبقى مشبوه");
                sb.AppendLine("- لو المحاولات الفاشلة > 3 يبقى مشبوه");
                sb.AppendLine("- riskScore من 0 لـ 100");
                sb.AppendLine("- recommendedAction: Allow / Warn / Block");

                var prompt = sb.ToString();

                var result = await CallGeminiAsync(prompt);

                if (result is null)
                    return Result<FraudResultDto>.Failure(
                        "Payment analysis failed");

                return Result<FraudResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<FraudResultDto>.Failure(
                    $"Payment analysis error: {ex.Message}");
            }
        }


        public async Task<Result<IEnumerable<SuspiciousActivityDto>>> GetPendingAlertsAsync()
        {
            try
            {
                var activities = await _unitOfWork.Repository<SuspiciousActivity, int>().GetAllAsync();
                
                var pending = activities
                    .Where(a => a.Status == LaundryShared.Enums.Fraud.SuspiciousActivityStatus.Open)
                    .OrderByDescending(a => a.RiskScore)
                    .Select(a => new SuspiciousActivityDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        ActionType = a.ActionType ?? "Unknown",
                        Reason = a.Reason ?? "",
                        RiskScore = a.RiskScore,
                        IpAddress = a.IpAddress ?? "Unknown",
                        AiRecommendedAction = a.AiRecommendedAction ?? "Allow",
                        Status = a.Status.ToString(),
                        CreatedAt = a.CreatedAt,
                        ResolvedAt = a.ResolvedAt
                    }).ToList();

                return Result<IEnumerable<SuspiciousActivityDto>>.Success(pending);
            }
            catch (Exception ex)
            {
                // By returning the actual exception message, we can see exactly what failed on the server
                return Result<IEnumerable<SuspiciousActivityDto>>.Failure($"Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }
        }

        public async Task<Result<bool>> ReviewAlertAsync(int alertId, bool approveBan)
        {
            var activity = await _unitOfWork.Repository<SuspiciousActivity, int>().GetByIdAsync(alertId);
            if (activity == null)
                return Result<bool>.Failure("Alert not found");

            if (approveBan)
            {
                var user = await _userManager.FindByIdAsync(activity.UserId);
                if (user != null)
                {
                    user.IsActive = false;
                    await _userManager.UpdateAsync(user);
                }
                activity.Status = LaundryShared.Enums.Fraud.SuspiciousActivityStatus.Reviewed;
            }
            else
            {
                activity.Status = LaundryShared.Enums.Fraud.SuspiciousActivityStatus.Dismissed;
            }

            activity.ResolvedAt = DateTime.UtcNow;
            _unitOfWork.Repository<SuspiciousActivity, int>().Update(activity);
            await _unitOfWork.CompleteAsync();
            
            return Result<bool>.Success(true);
        }

        // ============================================================
        // Helper - بيكلم Cerebras API
        // ============================================================
        private async Task<FraudResultDto?> CallGeminiAsync(string prompt)
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
                    temperature = 0.2,
                    maxOutputTokens = 1500
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Gemini Status: {response.StatusCode}");
            Console.WriteLine($"Gemini Body: {body}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var rawText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(rawText)) 
                return null;

            var clean = rawText.Replace("```json", "").Replace("```", "").Trim();
            int start = clean.IndexOf('{');
            int end = clean.LastIndexOf('}');
            if (start >= 0 && end >= start)
            {
                clean = clean.Substring(start, end - start + 1);
            }
            else if (start >= 0)
            {
                clean = clean.Substring(start);
                // محاولة ترقيع لو الرد اتقطع
                if (!clean.EndsWith("}")) clean += "}";
            }

            try
            {
                return JsonSerializer.Deserialize<FraudResultDto>(
                    clean, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception jsonEx)
            {
                Console.WriteLine($"JSON Parse Error: {jsonEx.Message}");
                Console.WriteLine($"Raw Text received: {clean}");
                throw new Exception($"JSON Parse Error: {jsonEx.Message}. Response was: {clean}");
            }
        }
    }
}
