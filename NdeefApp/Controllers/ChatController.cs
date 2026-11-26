using LaundryServiesAbstraction.IServices.Chat;
using LaundryShared.DTO.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using LaundryPerstince.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using LaundryShared.Enums.CustomerEnums;
using LaundryShared.Enums;
using LaundryDomain.Models.SystemModules;
using static Betalgo.Ranul.OpenAI.ObjectModels.SharedModels.IOpenAIModels;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LaundryApi.Controllers
{

    /// <summary>
    /// البوابة بتاعة الـ Chatbot
    /// بيدعم Normal و Streaming Response
    /// </summary>
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
        };

        private readonly IChatService _chatService;
        private readonly LaundryServiesAbstraction.IServices.Order.IOrderService _orderService;
        private readonly LaundryServiesAbstraction.IServices.User.IUserService _userService;
        private readonly LaundryServiesAbstraction.IServices.Notifications.INotificationService _notificationService;
        private readonly NadeefDbContext _dbContext;

        public ChatController(IChatService chatService,
            LaundryServiesAbstraction.IServices.Order.IOrderService orderService,
            LaundryServiesAbstraction.IServices.User.IUserService userService,
            LaundryServiesAbstraction.IServices.Notifications.INotificationService notificationService,
            NadeefDbContext dbContext)
        {
            _chatService = chatService;
            _orderService = orderService;
            _userService = userService;
            _notificationService = notificationService;
            _dbContext = dbContext;
        }

        private string GetUserId() => 
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
            User.FindFirstValue("uid") ?? 
            string.Empty;

        private async Task<string> GetUserNameAsync(string userId)
        {
            var claimName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name");
            if (!string.IsNullOrWhiteSpace(claimName))
                return claimName;

            if (string.IsNullOrEmpty(userId))
                return "العميل";

            try
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Name))
                    return user.Name;
            }
            catch { }

            return "العميل";
        }

        // NOTE: non-streaming endpoint removed. The controller relies on streaming only.

        /// <summary>
        /// POST /api/chat
        /// رد حرف حرف عن طريق SSE (streaming only)
        /// </summary>
        [HttpPost]
        public async Task SendMessageStream([FromBody] ChatRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 400;
                var err = Encoding.UTF8.GetBytes($"data: {{\"error\": \"Invalid request\"}}\n\n");
                await Response.Body.WriteAsync(err);
                await Response.Body.FlushAsync();
                return;
            }
            var userId = GetUserId();
            var userName = await GetUserNameAsync(userId);
            var normalizedMessage = NormalizeArabic(request.Message);
            var isEnglish = IsLikelyEnglish(request.Message);

            // Direct deterministic replies for key intents (more organized output)
            if (!string.IsNullOrWhiteSpace(userId))
            {
                bool hasCancelIntent = normalizedMessage.Contains("الغي") || normalizedMessage.Contains("الغاء") || normalizedMessage.Contains("cancel");
                bool hasComplaintIntent = normalizedMessage.Contains("شكوي") || normalizedMessage.Contains("شكوى") || normalizedMessage.Contains("مشكله") || normalizedMessage.Contains("مشكلة") || normalizedMessage.Contains("complaint") || normalizedMessage.Contains("issue");
                bool hasNearestIntent = normalizedMessage.Contains("اقرب") || normalizedMessage.Contains("nearest");
                bool hasPriceIntent = normalizedMessage.Contains("سعر") || normalizedMessage.Contains("اسعار") || normalizedMessage.Contains("أسعار") || normalizedMessage.Contains("price");

                if ((normalizedMessage.Contains("طلباتي النشطه") || normalizedMessage.Contains("الطلبات النشطه") || ContainsAnyEnglish(request.Message, "active orders", "my orders", "show my orders"))
                    && !hasCancelIntent && !hasComplaintIntent)
                {
                    SetSseHeaders();

                    var activeRes = await _orderService.GetActiveOrdersAsync(userId);
                    var direct = BuildActiveOrdersMarkdown(activeRes, isEnglish);
                    await WriteSseAsync(direct);
                    await WriteDoneAsync();
                    return;
                }

                if ((normalizedMessage.Contains("حاله طلبي") || normalizedMessage.Contains("حالة طلبي") || normalizedMessage.Contains("تتبع") || ContainsAnyEnglish(request.Message, "order status", "track order", "track my order"))
                    && !hasCancelIntent && !hasComplaintIntent)
                {
                    SetSseHeaders();

                    var activeRes = await _orderService.GetActiveOrdersAsync(userId);
                    var direct = BuildOrderStatusMarkdown(activeRes, isEnglish);
                    await WriteSseAsync(direct);
                    await WriteDoneAsync();
                    return;
                }

                if ((normalizedMessage.Contains("الغي طلب") || normalizedMessage.Contains("الغي الطلب") || normalizedMessage.Contains("إلغي طلب") || normalizedMessage.Contains("ازاي الغي") || ContainsAnyEnglish(request.Message, "cancel order", "cancel my order", "how to cancel"))
                    && !hasComplaintIntent)
                {
                    SetSseHeaders();

                    var activeRes = await _orderService.GetActiveOrdersAsync(userId);
                    var direct = await BuildCancelFlowMarkdownAsync(userId, activeRes, request.Message, isEnglish);
                    await WriteSseAsync(direct);
                    await WriteDoneAsync();
                    return;
                }

                // ─── Service Prices intent ───────────────────────────────────────────────
                // Catches: اسعار / بكام / السعر / كم التكلفة / price / etc.
                bool isPriceQuery =
                    normalizedMessage.Contains("جدول باسعار") ||
                    normalizedMessage.Contains("اسعار الخدمات") ||
                    normalizedMessage.Contains("اسعار") ||
                    normalizedMessage.Contains("بكام") ||
                    normalizedMessage.Contains("كام الغسيل") ||
                    normalizedMessage.Contains("كام الكوي") ||
                    normalizedMessage.Contains("كم التكلفه") ||
                    normalizedMessage.Contains("كم السعر") ||
                    normalizedMessage.Contains("التكلفه كام") ||
                    normalizedMessage.Contains("غسيل بكام") ||
                    normalizedMessage.Contains("كوي بكام") ||
                    normalizedMessage.Contains("متوسط السعر") ||
                    normalizedMessage.Contains("متوسط الاسعار") ||
                    ContainsAnyEnglish(request.Message, "service prices", "price list", "pricing", "how much", "what is the price", "cost of");

                if (isPriceQuery && !hasCancelIntent && !hasComplaintIntent)
                {
                    SetSseHeaders();
                    var direct = await BuildServicePricesMarkdownAsync(isEnglish);
                    await WriteSseAsync(direct);
                    await WriteDoneAsync();
                    return;
                }

                // ─── Nearest Laundry intent ─────────────────────────────────────────────
                // Catches: اقرب مغسله / مغسله قريبه / فين مغسله / اقدر اغسل فين / nearest / etc.
                bool isNearestLaundryQuery =
                    normalizedMessage.Contains("اقرب مغسله") ||
                    normalizedMessage.Contains("اقرب لاندري") ||
                    normalizedMessage.Contains("مغسله قريبه") ||
                    normalizedMessage.Contains("مغسله قريبة") ||
                    normalizedMessage.Contains("اقرب ليا") ||
                    normalizedMessage.Contains("فين مغسله") ||
                    normalizedMessage.Contains("فين لاندري") ||
                    normalizedMessage.Contains("اقدر اغسل فين") ||
                    normalizedMessage.Contains("عايز مغسله") ||
                    normalizedMessage.Contains("ابحث عن مغسله") ||
                    normalizedMessage.Contains("ابغا مغسله") ||
                    normalizedMessage.Contains("اقرب مغاسل") ||
                    normalizedMessage.Contains("ارب مغسله") ||
                    normalizedMessage.Contains("ارب مغسلة") ||
                    normalizedMessage.Contains("ارب لاندري") ||
                    normalizedMessage.Contains("ارب ليا") ||
                    normalizedMessage.Contains("ارب مغاسل") ||
                    ContainsAnyEnglish(request.Message, "nearest laundry", "closest laundry", "find laundry", "laundry near me", "nearby laundry");

                if (isNearestLaundryQuery && !hasCancelIntent && !hasComplaintIntent)
                {
                    SetSseHeaders();
                    var direct = await BuildNearestLaundriesMarkdownAsync(userId, isEnglish);
                    await WriteSseAsync(direct);
                    await WriteDoneAsync();
                    return;
                }

                if (normalizedMessage.Contains("اقدر اعمل ايه") || normalizedMessage.Contains("ايه الخدمات") || normalizedMessage.Contains("help") || normalizedMessage.Contains("المتوقع") || ContainsAnyEnglish(request.Message, "what can you do", "what can i do", "support menu"))
                {
                    SetSseHeaders();

                    var direct = BuildCustomerSupportMenuMarkdown(isEnglish);
                    await WriteSseAsync(direct);
                    await WriteDoneAsync();
                    return;
                }

                if (normalizedMessage.Contains("شكوي") || normalizedMessage.Contains("شكوى") || normalizedMessage.Contains("مشكله") || ContainsAnyEnglish(request.Message, "complaint", "issue", "problem", "report issue"))
                {
                    SetSseHeaders();

                    var activeRes = await _orderService.GetActiveOrdersAsync(userId);
                    var direct = await BuildComplaintFlowMarkdownAsync(userId, request.Message, activeRes, isEnglish);
                    await WriteSseAsync(direct);
                    await WriteDoneAsync();
                    return;
                }
            }

            // احضر بيانات المستخدم من الـ backend عشان نخلي الـ AI يرد اعتماداً على داتا التطبيق
            try
            {
                
                string contextSummary = $"CUSTOMER_NAME: {userName}\n";

                if (!string.IsNullOrEmpty(userId))
                {
                    var activeRes = await _orderService.GetActiveOrdersAsync(userId);
                    
                    if (activeRes != null && activeRes.IsSuccess && activeRes.Value != null && activeRes.Value.Count > 0)
                    {
                        var parts = new List<string>();
                        parts.Add("| رقم الطلب | المغسلة | السعر (جم) | الحالة |");
                        parts.Add("|---|---|---|---|");
                        foreach (var o in activeRes.Value)
                        {
                            var totalCentsOrFills = (int)o.TotalPrice;
                            var orderIdStr = o.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            var priceStr = totalCentsOrFills.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            parts.Add($"| {orderIdStr} | {o.LaundryName ?? "غير محدد"} | {priceStr} | {o.Status} |");
                        }
                        contextSummary += "ACTIVE_ORDERS:\n" + string.Join("\n", parts) + "\n\n";
                    }
                    else
                    {
                        contextSummary += "ACTIVE_ORDERS: none\n\n";
                    }

                    // Order History Injection
                    try
                    {
                        var historyRes = await _orderService.GetOrderHistoryAsync(userId, new LaundryShared.DTO.Order.OrderParams { PageIndex = 1, PageSize = 10 });
                        if (historyRes != null && historyRes.IsSuccess && historyRes.Value != null && historyRes.Value.Data != null && historyRes.Value.Data.Count > 0)
                        {
                            var historyParts = new List<string>();
                            historyParts.Add("| رقم الطلب | المغسلة | السعر (جم) | الحالة |");
                            historyParts.Add("|---|---|---|---|");
                            foreach (var o in historyRes.Value.Data)
                            {
                                var totalCents = (int)o.TotalPrice;
                                var orderIdStr = o.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                var priceStr = totalCents.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                historyParts.Add($"| {orderIdStr} | {o.LaundryName ?? "غير محدد"} | {priceStr} | {o.Status} |");
                            }
                            contextSummary += "ORDER_HISTORY:\n" + string.Join("\n", historyParts) + "\n\n";
                        }
                        else
                        {
                            contextSummary += "ORDER_HISTORY: none\n\n";
                        }
                    }
                    catch
                    {
                        contextSummary += "ORDER_HISTORY: none\n\n";
                    }

                    request.History.Insert(0, new LaundryShared.DTO.Chat.ChatMessageDto
                    {
                        Role = "system",
                        Content = contextSummary
                    });
                }

                var nearestLaundriesContext = await BuildNearestLaundriesContextAsync(userId);
                if (!string.IsNullOrWhiteSpace(nearestLaundriesContext))
                {
                    request.History.Insert(0, new LaundryShared.DTO.Chat.ChatMessageDto
                    {
                        Role = "system",
                        Content = nearestLaundriesContext
                    });
                }

                var marketAvgContext = await BuildMarketAverageContextAsync();
                if (!string.IsNullOrWhiteSpace(marketAvgContext))
                {
                    request.History.Insert(0, new LaundryShared.DTO.Chat.ChatMessageDto
                    {
                        Role = "system",
                        Content = marketAvgContext
                    });
                }
            }
            catch
            {
                // تجاهل أخطاء الجلب، نتابع بدون سياق
            }
            // بنحدد إن الـ Response هيبقى SSE
            SetSseHeaders();

            await foreach (var chunk in _chatService.SendMessageStreamAsync(
                GetUserId(), request))
            {
                // بنبعت كل جزء فور ما يجي مسلسلاً كـ JSON
                var json = JsonSerializer.Serialize(new { content = chunk }, _jsonOptions);
                var data = $"data: {json}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                await Response.Body.WriteAsync(bytes);
                await Response.Body.FlushAsync();
            }

            // بنبعت علامة إن الـ Stream خلص
            var done = "data: [DONE]\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(done));
            await Response.Body.FlushAsync();
        }

        private static string NormalizeArabic(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Trim().ToLowerInvariant()
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ة", "ه")
                .Replace("ى", "ي");
        }

        private async Task WriteSseAsync(string message)
        {
            var json = JsonSerializer.Serialize(new { content = message }, _jsonOptions);
            var data = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(data);
            await Response.Body.WriteAsync(bytes);
            await Response.Body.FlushAsync();
        }

        private void SetSseHeaders()
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
        }

        private async Task WriteDoneAsync()
        {
            var done = "data: [DONE]\n\n";
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(done));
            await Response.Body.FlushAsync();
        }

        private static string BuildActiveOrdersMarkdown(LaundryShared.Common.Result<List<LaundryShared.DTO.Order.OrderDto>> activeRes, bool isEnglish)
        {
            if (activeRes == null || !activeRes.IsSuccess || activeRes.Value == null || activeRes.Value.Count == 0)
                return isEnglish
                    ? """
✅ I checked your account — there are no active orders right now.

I can still help you with any of these:
- Show nearest laundries
- Show service prices
- Help you place a new order

Tell me what you want and I’ll handle it with you step by step.
"""
                    : """
✅ بصيت على حسابك — حالياً مفيش طلبات نشطة.

بس أنا معاك، أقدر أساعدك فوراً في:
- أقرب مغاسل ليك
- جدول أسعار الخدمات
- تبدأ طلب جديد خطوة بخطوة

قولي تحب نبدأ بإيه وأنا هظبطه معاك.
""";

            var sb = new StringBuilder();
            sb.AppendLine(isEnglish ? "📌 **Your Active Orders**" : "📌 **طلباتك النشطة حالياً**");
            sb.AppendLine();
            sb.AppendLine(isEnglish
                ? "| Order # | Laundry | Status | Price | Pickup Time | Delivery Time |"
                : "| # | المغسلة | الحالة | السعر | موعد الاستلام | موعد التسليم |");
            sb.AppendLine("|---|---|---|---:|---|---|");

            foreach (var o in activeRes.Value.OrderByDescending(x => x.CreatedAt))
            {
                sb.AppendLine($"| {o.Id} | {o.LaundryName ?? "-"} | {o.Status} | {o.TotalPrice.ToString("0.##", CultureInfo.InvariantCulture)} جم | {o.PickupTime:yyyy-MM-dd HH:mm} | {(o.DeliveryTime.HasValue ? o.DeliveryTime.Value.ToString("yyyy-MM-dd HH:mm") : "-")} |");
            }

            return sb.ToString();
        }

        private static string BuildOrderStatusMarkdown(LaundryShared.Common.Result<List<LaundryShared.DTO.Order.OrderDto>> activeRes, bool isEnglish)
        {
            if (activeRes == null || !activeRes.IsSuccess || activeRes.Value == null || activeRes.Value.Count == 0)
                return isEnglish
                    ? "ℹ️ You don’t have an active order right now. If you want, I can help you start a new order or show nearby laundries first."
                    : "ℹ️ حالياً مفيش طلب نشط. لو تحب، أقدر أساعدك تبدأ طلب جديد أو أجيبلك أقرب مغاسل الأول.";

            var latest = activeRes.Value.OrderByDescending(x => x.CreatedAt).First();
            return isEnglish
                ? $"🧾 **Latest Active Order Status**\n\n- Order ID: **{latest.Id}**\n- Laundry: **{latest.LaundryName ?? "-"}**\n- Status: **{latest.Status}**\n- Price: **{latest.TotalPrice.ToString("0.##", CultureInfo.InvariantCulture)} جم**\n- Pickup: **{latest.PickupTime:yyyy-MM-dd HH:mm}**\n- Delivery: **{(latest.DeliveryTime.HasValue ? latest.DeliveryTime.Value.ToString("yyyy-MM-dd HH:mm") : "-" )}**"
                : $"🧾 **حالة آخر طلب نشط**\n\n- رقم الطلب: **{latest.Id}**\n- المغسلة: **{latest.LaundryName ?? "-"}**\n- الحالة: **{latest.Status}**\n- السعر: **{latest.TotalPrice.ToString("0.##", CultureInfo.InvariantCulture)} جم**\n- الاستلام: **{latest.PickupTime:yyyy-MM-dd HH:mm}**\n- التسليم: **{(latest.DeliveryTime.HasValue ? latest.DeliveryTime.Value.ToString("yyyy-MM-dd HH:mm") : "-" )}**";
        }

        private static string BuildCancelHelpMarkdown(LaundryShared.Common.Result<List<LaundryShared.DTO.Order.OrderDto>> activeRes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🛑 **إلغاء الطلب**");
            sb.AppendLine();
            sb.AppendLine("تقدري تلغي الطلب لو حالته لسه **PendingConfirmation**.");
            sb.AppendLine("ابعتلي رسالة بالشكل ده:");
            sb.AppendLine("`الغِ الطلب رقم 123`");
            sb.AppendLine();

            if (activeRes != null && activeRes.IsSuccess && activeRes.Value != null && activeRes.Value.Count > 0)
            {
                sb.AppendLine("**الطلبات النشطة الحالية:**");
                foreach (var o in activeRes.Value.OrderByDescending(x => x.CreatedAt).Take(5))
                {
                    sb.AppendLine($"- طلب **{o.Id}** | الحالة: **{o.Status}** | المغسلة: **{o.LaundryName ?? "-"}**");
                }
            }

            return sb.ToString();
        }

        private async Task<string> BuildCancelFlowMarkdownAsync(
            string userId,
            LaundryShared.Common.Result<List<LaundryShared.DTO.Order.OrderDto>> activeRes,
            string message,
            bool isEnglish)
        {
            var orderId = ExtractOrderId(message);

            if (orderId.HasValue)
            {
                var req = new LaundryShared.DTO.Order.CancelOrderRequest
                {
                    CancellationReason = "Cancelled via Chat"
                };

                var cancelRes = await _orderService.CancelOrderAsync(userId, orderId.Value, req);
                if (cancelRes.IsSuccess)
                {
                    return isEnglish
                        ? $"✅ Order **{orderId.Value}** has been cancelled successfully."
                        : $"✅ تم إلغاء الطلب رقم **{orderId.Value}** بنجاح.";
                }

                return isEnglish
                    ? $"❌ Couldn't cancel order **{orderId.Value}**.\n\nReason: {cancelRes.ErrorMessage}"
                    : $"❌ مقدرتش ألغي الطلب رقم **{orderId.Value}**.\n\nالسبب: {cancelRes.ErrorMessage}";
            }

            var cancellable = (activeRes?.IsSuccess == true && activeRes.Value != null)
                ? activeRes.Value.Where(o => o.Status == OrderStatus.PendingConfirmation).OrderByDescending(o => o.CreatedAt).ToList()
                : new List<LaundryShared.DTO.Order.OrderDto>();

            if (cancellable.Count == 0)
            {
                return isEnglish
                    ? "ℹ️ There are no cancellable orders right now (status must be PendingConfirmation)."
                    : "ℹ️ حالياً مفيش طلبات قابلة للإلغاء (لازم تكون الحالة PendingConfirmation).";
            }

            var sb = new StringBuilder();
            sb.AppendLine(isEnglish ? "🛑 **Choose which order to cancel**" : "🛑 **اختار الطلب اللي عايز تلغيه**");
            sb.AppendLine();
            sb.AppendLine(isEnglish
                ? "| Order ID | Laundry | Status | Price | Date |"
                : "| رقم الطلب | المغسلة | الحالة | السعر | التاريخ |");
            sb.AppendLine("|---:|---|---|---:|---|");
            foreach (var o in cancellable)
            {
                sb.AppendLine($"| {o.Id} | {o.LaundryName ?? "-"} | {o.Status} | {o.TotalPrice.ToString("0.##", CultureInfo.InvariantCulture)} جم | {o.CreatedAt:yyyy-MM-dd HH:mm} |");
            }

            sb.AppendLine();
            sb.AppendLine(isEnglish
                ? "Send: `cancel order <order_id>`"
                : "ابعتلي: `الغِ الطلب رقم <رقم الطلب>`");
            return sb.ToString();
        }

        private static int? ExtractOrderId(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Case 1: Standalone number
            if (Regex.IsMatch(text.Trim(), @"^\d+$") && int.TryParse(text.Trim(), out var id1))
            {
                return id1;
            }

            // Case 2: Number preceded by order keywords
            var match = Regex.Match(text, @"(?:طلب|أوردر|اوردر|order|رقم|id)\s*#?\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var id2))
            {
                return id2;
            }

            return null;
        }

        private async Task<string> BuildServicePricesMarkdownAsync(bool isEnglish)
        {
            var services = await _dbContext.Services
                .Where(s => s.IsAvailable)
                .Select(s => new { s.ServiceName, s.Price })
                .ToListAsync();

            if (services.Count == 0)
                return isEnglish ? "ℹ️ No service prices are available right now." : "ℹ️ لا توجد أسعار خدمات متاحة حالياً.";

            var grouped = services
                .GroupBy(s => s.ServiceName)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    Min = g.Min(x => x.Price),
                    Avg = g.Average(x => x.Price),
                    Max = g.Max(x => x.Price),
                    Count = g.Count()
                })
                .OrderBy(x => x.ServiceName)
                .Take(20)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine(isEnglish ? "💸 **Service Prices (Market Average)**" : "💸 **جدول أسعار الخدمات (متوسط السوق)**");
            sb.AppendLine();
            sb.AppendLine(isEnglish
                ? "| Service | Min Price | Avg Price | Max Price | Laundries Count |"
                : "| الخدمة | أقل سعر | متوسط السعر | أعلى سعر | عدد المغاسل |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            foreach (var g in grouped)
            {
                sb.AppendLine($"| {g.ServiceName} | {g.Min.ToString("0.##", CultureInfo.InvariantCulture)} | {g.Avg.ToString("0.##", CultureInfo.InvariantCulture)} | {g.Max.ToString("0.##", CultureInfo.InvariantCulture)} | {g.Count} |");
            }

            return sb.ToString();
        }

        private async Task<string> BuildNearestLaundriesMarkdownAsync(string userId, bool isEnglish)
        {
            var addressesResult = await _userService.GetAddressesAsync(userId);
            if (addressesResult == null || !addressesResult.IsSuccess || addressesResult.Value == null)
                return isEnglish
                    ? "ℹ️ I couldn't find a saved address. Please add an address first so I can get the nearest laundry."
                    : "ℹ️ مش لاقي عنوان محفوظ ليكي/ليك. أضف عنوان أولاً علشان أقدر أطلع أقرب مغسلة.";

            var defaultAddress = addressesResult.Value.FirstOrDefault(a => a.IsDefault && a.Latitude.HasValue && a.Longitude.HasValue)
                                 ?? addressesResult.Value.FirstOrDefault(a => a.Latitude.HasValue && a.Longitude.HasValue);

            if (defaultAddress == null || !defaultAddress.Latitude.HasValue || !defaultAddress.Longitude.HasValue)
                return isEnglish
                    ? "ℹ️ Your current address has no coordinates, so I can't calculate nearest laundries."
                    : "ℹ️ العنوان الحالي بدون إحداثيات، مش قادر أحسب الأقرب.";

            var userLat = (double)defaultAddress.Latitude.Value;
            var userLon = (double)defaultAddress.Longitude.Value;

            var laundries = await _dbContext.Laundries
                .Where(l => l.Status == LaundryStatus.Active && l.Availability == LaundryAvailability.Open)
                .Select(l => new { l.Id, l.Name, l.Address, l.Latitude, l.Longitude, l.AverageRating })
                .ToListAsync();

            if (laundries.Count == 0)
                return isEnglish ? "ℹ️ No laundries are available right now." : "ℹ️ لا توجد مغاسل متاحة حالياً.";

            var top = laundries
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Address,
                    l.AverageRating,
                    DistanceKm = CalculateDistanceKm(userLat, userLon, (double)l.Latitude, (double)l.Longitude)
                })
                .OrderBy(x => x.DistanceKm)
                .Take(3)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine(isEnglish ? "📍 **Nearest 3 Laundries**" : "📍 **أقرب 3 مغاسل ليك**");
            sb.AppendLine();
            sb.AppendLine(isEnglish
                ? "| Laundry | Distance (km) | Rating | Address |"
                : "| المغسلة | المسافة (كم) | التقييم | العنوان |");
            sb.AppendLine("|---|---:|---:|---|");
            foreach (var l in top)
            {
                sb.AppendLine($"| {l.Name} | {l.DistanceKm.ToString("0.00", CultureInfo.InvariantCulture)} | {l.AverageRating.ToString("0.0", CultureInfo.InvariantCulture)} | {l.Address} |");
            }

            // ── Fetch prices for the single nearest laundry and show them ────────────
            var nearestId = top.FirstOrDefault()?.Id;
            if (nearestId.HasValue)
            {
                var nearestPrices = await _dbContext.Services
                    .Where(s => s.LaundryId == nearestId.Value && s.IsAvailable)
                    .Select(s => new { s.ServiceName, s.Price })
                    .OrderBy(s => s.ServiceName)
                    .Take(15)
                    .ToListAsync();

                if (nearestPrices.Count > 0)
                {
                    var nearestName = top.First().Name;
                    sb.AppendLine();
                    sb.AppendLine(isEnglish
                        ? $"💸 **Prices at {nearestName} (closest)**"
                        : $"💸 **أسعار {nearestName} (الأقرب)**");
                    sb.AppendLine();
                    sb.AppendLine(isEnglish ? "| Service | Price (EGP) |" : "| الخدمة | السعر (جم) |");
                    sb.AppendLine("|---|---:|");
                    foreach (var p in nearestPrices)
                    {
                        sb.AppendLine($"| {p.ServiceName} | {p.Price.ToString("0.##", CultureInfo.InvariantCulture)} |");
                    }
                }
                else
                {
                    // Fallback: show market averages instead
                    var avgPrices = await _dbContext.Services
                        .Where(s => s.IsAvailable)
                        .GroupBy(s => s.ServiceName)
                        .Select(g => new { ServiceName = g.Key, Avg = g.Average(x => x.Price) })
                        .OrderBy(x => x.ServiceName)
                        .Take(10)
                        .ToListAsync();

                    if (avgPrices.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine(isEnglish ? "💸 **Market Average Prices**" : "💸 **متوسط أسعار السوق**");
                        sb.AppendLine();
                        sb.AppendLine(isEnglish ? "| Service | Avg Price (EGP) |" : "| الخدمة | متوسط السعر (جم) |");
                        sb.AppendLine("|---|---:|");
                        foreach (var p in avgPrices)
                        {
                            sb.AppendLine($"| {p.ServiceName} | {p.Avg.ToString("0.##", CultureInfo.InvariantCulture)} |");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static string BuildCustomerSupportMenuMarkdown(bool isEnglish)
        {
            return isEnglish ? """
🧠 **How can I help you?**

1. Show active orders: `show my active orders`
2. Track order status: `track my order`
3. Cancel order: `cancel order`
4. Service price table: `show service prices`
5. Nearest laundry: `nearest laundry`
6. Best price suggestion: `best price for this service`
7. Send complaint: `complaint order 123: <your issue>`
8. Any support question: type it directly
""" : """
🧠 **أقدر أساعدك في إيه؟**

1. عرض الطلبات النشطة: `اعرض طلباتي النشطة`
2. متابعة حالة الطلب: `عايز اعرف حالة طلبي`
3. إلغاء طلب: `عاوز الغي اوردر`
4. جدول أسعار الخدمات: `عايز جدول بأسعار الخدمات`
5. أقرب مغسلة ليك: `ايه اقرب مغسلة`
6. ترشيح أفضل سعر: `أفضل سعر لنفس الخدمة`
7. إرسال شكوى: `شكوى على الطلب 123: <تفاصيل المشكلة>`
8. أي استفسار خدمة عملاء: اكتب سؤالك بشكل مباشر
""";
        }

        private async Task<string> BuildComplaintFlowMarkdownAsync(
            string userId,
            string message,
            LaundryShared.Common.Result<List<LaundryShared.DTO.Order.OrderDto>> activeRes,
            bool isEnglish)
        {
            var orderId = ExtractOrderId(message);
            var complaintText = ExtractComplaintText(message);

            if (!orderId.HasValue || string.IsNullOrWhiteSpace(complaintText) || complaintText.Length < 8)
            {
                var sbGuide = new StringBuilder();
                sbGuide.AppendLine(isEnglish ? "📝 **To submit a complaint, send:**" : "📝 **علشان تبعتي/تبعت شكوى ابعتي بالشكل ده:**");
                sbGuide.AppendLine();
                sbGuide.AppendLine(isEnglish
                    ? "`complaint order 123: courier delayed and package was damaged`"
                    : "`شكوى على الطلب 123: المندوب اتأخر والملابس وصلت متبهدلة`");
                sbGuide.AppendLine();

                var orders = (activeRes?.IsSuccess == true && activeRes.Value != null)
                    ? activeRes.Value.OrderByDescending(x => x.CreatedAt).Take(5).ToList()
                    : new List<LaundryShared.DTO.Order.OrderDto>();

                if (orders.Count > 0)
                {
                    sbGuide.AppendLine(isEnglish ? "**Your recent active orders:**" : "**أحدث الطلبات النشطة عندك:**");
                    sbGuide.AppendLine(isEnglish
                        ? "| Order ID | Laundry | Status | Date |"
                        : "| رقم الطلب | المغسلة | الحالة | التاريخ |");
                    sbGuide.AppendLine("|---:|---|---|---|");
                    foreach (var o in orders)
                    {
                        sbGuide.AppendLine($"| {o.Id} | {o.LaundryName ?? "-"} | {o.Status} | {o.CreatedAt:yyyy-MM-dd HH:mm} |");
                    }
                }

                return sbGuide.ToString();
            }

            var order = await _dbContext.Orders
                .Where(o => o.Id == orderId.Value && o.CustomerId == userId)
                .Select(o => new { o.Id, o.LaundryId, o.Status })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return isEnglish
                    ? "❌ This order is not found in your account."
                    : "❌ الطلب ده مش موجود في حسابك.";
            }

            var laundry = await _dbContext.Laundries
                .Where(l => l.Id == order.LaundryId)
                .Select(l => new { l.Name, l.AdminId })
                .FirstOrDefaultAsync();

            var customer = await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Name, u.PhoneNumber, u.Email })
                .FirstOrDefaultAsync();

            var customerName = customer?.Name ?? "العميل";
            var customerPhone = customer?.PhoneNumber ?? "N/A";
            var customerEmail = customer?.Email ?? "N/A";
            var laundryName = laundry?.Name ?? "Unknown Laundry";

            if (string.IsNullOrWhiteSpace(laundry?.AdminId))
            {
                return isEnglish
                    ? "❌ Could not route this complaint to the laundry owner right now. Please contact support from the app."
                    : "❌ مش قادر أوصل الشكوى لصاحب المغسلة دلوقتي. تواصل مع الدعم من التطبيق.";
            }

            var adminMessage = $"Complaint from customer {customerName} on order #{order.Id}. Laundry: {laundryName}. Status: {order.Status}. Issue: {complaintText}. Customer phone: {customerPhone}. Customer email: {customerEmail}.";
            var laundryAdminMessage = $"Customer complaint on order #{order.Id}. Issue: {complaintText}. Customer: {customerName}, Phone: {customerPhone}, Email: {customerEmail}.";

            var complaint = new Complaint
            {
                CustomerId = userId,
                OrderId = order.Id,
                LaundryId = order.LaundryId,
                LaundryAdminId = laundry.AdminId,
                Details = complaintText,
                Status = ComplaintStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Complaints.AddAsync(complaint);
            await _dbContext.SaveChangesAsync();

            var superAdmins = await _dbContext.Users
                .Where(u => u.Role == UserRole.SuperAdmin && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var adminId in superAdmins)
            {
                await _notificationService.SendNotificationAsync(
                    adminId,
                    "New Customer Complaint",
                    adminMessage);
            }

            if (!string.IsNullOrWhiteSpace(laundry?.AdminId))
            {
                await _notificationService.SendNotificationAsync(
                    laundry.AdminId,
                    "New Complaint on Your Laundry Order",
                    laundryAdminMessage);
            }

            return isEnglish
                ? $"✅ Your complaint has been submitted (ID: **{complaint.Id}**) for order **{order.Id}**. Admin and laundry owner were notified and can contact you soon."
                : $"✅ تم تسجيل الشكوى (رقم: **{complaint.Id}**) على الطلب **{order.Id}**. تم إخطار الإدارة وصاحب المغسلة وهيتواصلوا معاك قريب.";
        }

        private static string ExtractComplaintText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var cleaned = text.Trim();
            var colonIdx = cleaned.IndexOf(':');
            if (colonIdx >= 0 && colonIdx < cleaned.Length - 1)
                return cleaned[(colonIdx + 1)..].Trim();

            var lower = cleaned.ToLowerInvariant();
            var keywords = new[] { "شكوى", "شكوي", "مشكله", "complaint", "issue", "problem", "report" };
            foreach (var keyword in keywords)
            {
                var idx = lower.IndexOf(keyword);
                if (idx >= 0)
                {
                    var value = cleaned[(idx + keyword.Length)..].Trim();
                    return value.Replace("على الطلب", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("order", "", StringComparison.OrdinalIgnoreCase)
                                .Trim();
                }
            }

            return cleaned;
        }

        private static bool IsLikelyEnglish(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var letters = text.Where(char.IsLetter).ToList();
            if (letters.Count == 0) return false;
            var latin = letters.Count(c => c <= 127);
            return latin >= (letters.Count * 0.6);
        }

        private static bool ContainsAnyEnglish(string? source, params string[] terms)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;
            var s = source.ToLowerInvariant();
            return terms.Any(t => s.Contains(t));
        }

        [HttpGet("market-prices")]
        public async Task<IActionResult> GetMarketPrices()
        {
            var services = await _dbContext.Services
                .Where(s => s.IsAvailable)
                .GroupBy(s => s.ServiceName)
                .Select(g => new
                {
                    serviceName = g.Key,
                    minPrice = g.Min(x => x.Price),
                    avgPrice = g.Average(x => x.Price),
                    maxPrice = g.Max(x => x.Price),
                    laundriesCount = g.Count()
                })
                .OrderBy(x => x.serviceName)
                .ToListAsync();

            return Ok(services);
        }

        private async Task<string> BuildNearestLaundriesContextAsync(string userId)
        {
            var addressesResult = await _userService.GetAddressesAsync(userId);
            if (addressesResult == null || !addressesResult.IsSuccess || addressesResult.Value == null)
                return string.Empty;

            var defaultAddress = addressesResult.Value.FirstOrDefault(a => a.IsDefault && a.Latitude.HasValue && a.Longitude.HasValue)
                                 ?? addressesResult.Value.FirstOrDefault(a => a.Latitude.HasValue && a.Longitude.HasValue);

            if (defaultAddress == null || !defaultAddress.Latitude.HasValue || !defaultAddress.Longitude.HasValue)
                return string.Empty;

            var userLat = (double)defaultAddress.Latitude.Value;
            var userLon = (double)defaultAddress.Longitude.Value;

            var cache = HttpContext.RequestServices.GetService<IMemoryCache>();
            List<ActiveLaundryDto> laundriesList = null;
            if (cache != null && cache.TryGetValue<List<ActiveLaundryDto>>("Voice_ActiveLaundries", out var cachedLaundries))
            {
                laundriesList = cachedLaundries;
            }
            else
            {
                var queryResult = await _dbContext.Laundries
                    .Where(l => l.Status == LaundryShared.Enums.CustomerEnums.LaundryStatus.Active
                             && l.Availability == LaundryShared.Enums.CustomerEnums.LaundryAvailability.Open)
                    .Select(l => new ActiveLaundryDto
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Address = l.Address,
                        Latitude = l.Latitude,
                        Longitude = l.Longitude,
                        AverageRating = (decimal)l.AverageRating
                    })
                    .ToListAsync();

                laundriesList = queryResult;
                if (cache != null)
                {
                    cache.Set("Voice_ActiveLaundries", queryResult, TimeSpan.FromMinutes(2));
                }
            }

            if (laundriesList == null || laundriesList.Count == 0)
                return string.Empty;

            var nearest = laundriesList
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Address,
                    l.AverageRating,
                    DistanceKm = CalculateDistanceKm(userLat, userLon, (double)l.Latitude, (double)l.Longitude)
                })
                .OrderBy(x => x.DistanceKm)
                .Take(5)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("NEAREST_LAUNDRIES:");
            foreach (var n in nearest)
            {
                sb.AppendLine($"- {n.Name} | {n.DistanceKm.ToString("0.00", CultureInfo.InvariantCulture)} km | rating: {n.AverageRating.ToString("0.0", CultureInfo.InvariantCulture)} | address: {n.Address}");
            }

            var firstNearest = nearest.FirstOrDefault();
            if (firstNearest != null)
            {
                var nearestPrices = await _dbContext.Services
                    .Where(s => s.LaundryId == firstNearest.Id && s.IsAvailable)
                    .Select(s => new { s.ServiceName, s.Price })
                    .OrderBy(s => s.ServiceName)
                    .Take(15)
                    .ToListAsync();

                if (nearestPrices.Count > 0)
                {
                    sb.AppendLine($"PRICES_FOR_NEAREST_LAUNDRY ({firstNearest.Name}):");
                    foreach (var p in nearestPrices)
                    {
                        sb.AppendLine($"* {p.ServiceName}: {p.Price.ToString("0.##", CultureInfo.InvariantCulture)} EGP");
                    }
                }
            }

            return sb.ToString();
        }

        private async Task<string> BuildMarketAverageContextAsync()
        {
            var cache = HttpContext.RequestServices.GetService<IMemoryCache>();
            if (cache != null && cache.TryGetValue<string>("Chat_MarketAveragePrices", out var cachedResult))
            {
                return cachedResult;
            }

            var services = await _dbContext.Services
                .Where(s => s.IsAvailable)
                .GroupBy(s => s.ServiceName)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    AvgPrice = g.Average(x => x.Price),
                    MinPrice = g.Min(x => x.Price),
                    MaxPrice = g.Max(x => x.Price)
                })
                .OrderBy(x => x.ServiceName)
                .Take(20)
                .ToListAsync();

            if (services.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("MARKET_AVERAGE_PRICES:");
            foreach (var s in services)
            {
                sb.AppendLine($"- {s.ServiceName}: avg={s.AvgPrice.ToString("0.##", CultureInfo.InvariantCulture)}, min={s.MinPrice.ToString("0.##", CultureInfo.InvariantCulture)}, max={s.MaxPrice.ToString("0.##", CultureInfo.InvariantCulture)}");
            }

            var result = sb.ToString();
            if (cache != null)
            {
                cache.Set("Chat_MarketAveragePrices", result, TimeSpan.FromMinutes(10));
            }
            return result;
        }

        private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double deg) => deg * Math.PI / 180.0;

        private class ActiveLaundryDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public decimal AverageRating { get; set; }
        }
    }
}
