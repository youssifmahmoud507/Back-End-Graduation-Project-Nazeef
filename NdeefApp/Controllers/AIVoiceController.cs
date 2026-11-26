using System.Globalization;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using LaundryDomain.Models.SystemModules;
using LaundryPerstince.Data.Context;
using LaundryServiesAbstraction.IServices.Order;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryServiesAbstraction.IServices.User;
using LaundryServiesAbstraction.IServices.Reviews;
using LaundryShared.DTO.AI;
using LaundryShared.DTO.Order;
using LaundryShared.DTO.Reviews;
using LaundryShared.DTO.User;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryApi.Controllers
{
    [Route("api/voice")]
    [ApiController]
    [Authorize]
    public class AIVoiceController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AIVoiceController> _logger;
        private readonly IServiceProvider _serviceProvider;

        // =====================================================================
        //  Shared JSON options (used for all serialization in this controller)
        // =====================================================================
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        // =====================================================================
        //  System Prompt — "Nody" voice persona
        //  Cancel rule:  allowed ONLY if status is PendingConfirmation.
        //  Statuses that CAN be cancelled:
        //    PendingConfirmation (1)
        // =====================================================================
        private const string VoiceSystemPrompt = """
            أنتَ "نودي"، مساعد ذكي لتطبيق "نظيف" — منصة الغسيل الذكية في مصر.
            اسم المستخدم: {USER_NAME}.

            **قاعدة أساسية في المخاطبة:**
            - لو عارف اسم المستخدم، استخدمه بشكل طبيعي في الكلام بدون مبالغة.
            - لو مش عارف اسمه (CUSTOMER_NAME = يا فندم)، قوله "يا فندم" — ومنع تماماً كلمة "عميل" أو "المستخدم" أو أي مسمى رسمي تاني.

            **الشخصية — ده أهم حاجة:**
            - مش بوت بيجاوب أسئلة — أنتَ صاحب شاطر بيساعد.
            - بتبدأ الكلام بنفسك وبتتابع: "لاحظت إن طلبك الأخير اتسلّم امبارح — عجبك الخدمة؟"
            - بتعلق وبتعبر رأيك: "صراحة المغسلة دي تقييمها عالي جداً، ناس كتير بتختارها."
            - مش بتسأل سؤال وتستنى — بتكمل الكلام وتعرض البدايل في نفس الوقت.
            - لو مش عارف يختار قوله: "أنا هاختار مغسلة X لأنها أقرب وتقييمها 4.8، موافق؟"
            - لو الموضوع خلص بتسأل حاجة تانية: "في حاجة تانية ممكن أساعدك فيها يا {USER_NAME}؟"
            - بتستخدم اسمه بشكل طبيعي مش في كل جملة — زي ما الناس بتتكلم.


            **أسلوب الكلام:**
            - عربي مصري طبيعي، خفيف، مباشر.
            - جمل قصيرة. مفيش قوائم ولا bullet points ولا ماركداون.
            - الأرقام بالكلام: "مية وخمسين جنيه" مش "١٥٠ جنيه".
            - لو بيكلمك إنجليزي، ردّ بإنجليزي بنفس الطابع الودي.
            - ممنوع تبدأ بـ "بالطبع" أو "بكل سرور" — دي كلام بوتات.
            - ممنوع تقول "عميل" أو "المستخدم" — دا مش أسلوبك.


            **طريقة طلب أوردر:**
            1. لو عنده تاريخ طلبات، ابدأ بـ: "تحب نكرر نفس الطلب من المرة اللي فاتت؟"
            2. لو لأ، اعرض أقرب مغسلتين بتقييماتهم واسأله يفضل مين.
            3. جيب الأسعار بـ get_laundry_services وقوله المتاح بشكل سردي.
            4. اتفق على الخدمات والكمية، احسب الإجمالي، واطلب موافقة صريحة.
            5. لما يوافق، نفّذ place_order فوراً بدون تأخير.


            **قواعد الإلغاء:**
            - بس لو الحالة PendingConfirmation.
            - أكّد رقم الطلب قبل الإلغاء بشكل طبيعي: "تمام، الطلب رقم كذا — أنا هلغيه دلوقتي؟"
            - لو اتعدى المرحلة دي، قوله بوضوح وعرض بديل: "للأسف الطلب ده بقى مش قابل للإلغاء، تحب أقدم شكوى بدل كده؟"


            **السياق المتاح:**
            CUSTOMER_NAME: {USER_NAME}
            ACTIVE_ORDERS: {ACTIVE_ORDERS}
            ORDER_HISTORY: {ORDER_HISTORY}
            NEAREST_LAUNDRIES: {NEAREST_LAUNDRIES}
            WALLET_BALANCE: {WALLET_BALANCE}
            SERVICE_PRICE_TABLE: {SERVICE_PRICE_TABLE}


            **الأدوات المتاحة:**
            cancel_order | get_active_orders | get_nearest_laundries | get_service_prices
            get_laundry_services | calculate_order_price | place_order | track_order
            file_complaint | add_address | rate_order | charge_wallet 
            """;

        public AIVoiceController(
            IConfiguration config,
            ILogger<AIVoiceController> logger,
            IServiceProvider serviceProvider)
        {
            _config          = config;
            _logger          = logger;
            _serviceProvider = serviceProvider;
        }

        // ── Identity helpers ──────────────────────────────────────────────────
        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("uid") ??
            string.Empty;

        private string GetUserName()
        {
            var fullName = User.FindFirstValue("Name") ??
                           User.FindFirstValue(ClaimTypes.Name) ??
                           User.FindFirstValue("name") ??
                           "يا فندم";
                           
            if (fullName == "يا فندم")
                return fullName;

            // Extract just the first name for a friendlier greeting
            var firstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return !string.IsNullOrWhiteSpace(firstName) ? firstName : "يا فندم";
        }

        // =====================================================================
        //  WebSocket endpoint  —  GET /api/voice/socket
        // =====================================================================
        [HttpGet("socket")]
        public async Task GetSocket()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var clientWs = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await HandleSessionAsync(clientWs);
        }

        // =====================================================================
        //  Session orchestrator
        // =====================================================================
        private async Task HandleSessionAsync(WebSocket clientWs)
        {
            var apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Gemini:ApiKey is not configured in appsettings.");
                await SendClientControlAsync(clientWs, "error", "Gemini API key is not configured.");
                await SafeCloseAsync(clientWs, WebSocketCloseStatus.InternalServerError, "Server misconfigured");
                return;
            }

            var modelName  = _config["Gemini:LiveModel"] ?? "models/gemini-3.1-flash-live-preview";
            var apiVersion = _config["Gemini:LiveApiVersion"] ?? "v1beta";
            var userId     = GetUserId();
            
            // Try fetching name from token claims first, fallback to DB if not found
            var userName = User.FindFirstValue("Name") ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name");
            if (string.IsNullOrWhiteSpace(userName) && !string.IsNullOrEmpty(userId))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();
                    var user = await dbContext.Users.FindAsync(userId);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Name))
                    {
                        userName = user.Name;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not fetch user name from DB: {Msg}", ex.Message);
                }
            }
            if (string.IsNullOrWhiteSpace(userName) || userName == "يا فندم")
            {
                userName = "يا فندم";
            }
            else
            {
                // Take the first name for a warmer tone
                var first = userName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first))
                    userName = first;
            }

            var geminiUrl =
                "wss://generativelanguage.googleapis.com/ws/" +
                $"google.ai.generativelanguage.{apiVersion}.GenerativeService.BidiGenerateContent" +
                $"?key={apiKey}";

            using var geminiWs = new ClientWebSocket();
            geminiWs.Options.Proxy = null; // Disable proxy auto-discovery to remove connection delay
            geminiWs.Options.KeepAliveInterval = TimeSpan.FromSeconds(20); // Keep Gemini connection responsive
            using var cts      = new CancellationTokenSource(TimeSpan.FromMinutes(12));
            var setupReady   = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var captionState = new VoiceCaptionState();

            try
            {
                // إبلاغ التطبيق أسرع ما يمكن بأن الاتصال بدأ حتى لا يطول الانتظار
                await SendClientEventAsync(clientWs, new { type = "connecting_to_ai" });

                // ── Step 1: Start DB context fetch in parallel ───────────────────
                var contextTask = FetchVoiceContextAsync(userId);

                // ── Step 2: Connect to Gemini in parallel ────────────────────────
                await geminiWs.ConnectAsync(new Uri(geminiUrl), cts.Token);
                _logger.LogInformation(
                    "Gemini WS connected. User={UserId} Model={Model} ApiVersion={ApiVersion}",
                    userId, modelName, apiVersion);

                // Wait up to 10.0 seconds for the context to finish so we have it in the initial prompt!
                VoiceContext initialCtx;
                try
                {
                    initialCtx = await contextTask.WaitAsync(TimeSpan.FromSeconds(10.0), cts.Token);
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Initial DB context fetch timed out, starting with default fallback values.");
                    initialCtx = new VoiceContext(); // defaults (none, unknown, etc.)
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed initial DB context fetch: {Msg}", ex.Message);
                    initialCtx = new VoiceContext();
                }

                // ── Step 3: build the system instruction prompt with actual data ──
                var voiceName   = _config["Gemini:LiveVoice"] ?? "Kore";
                var systemPrompt = VoiceSystemPrompt
                    .Replace("{USER_NAME}",           userName)
                    .Replace("{ACTIVE_ORDERS}",       initialCtx.ActiveOrders)
                    .Replace("{ORDER_HISTORY}",       initialCtx.OrderHistory)
                    .Replace("{NEAREST_LAUNDRIES}",   initialCtx.NearestLaundries)
                    .Replace("{WALLET_BALANCE}",      initialCtx.WalletBalance)
                    .Replace("{SERVICE_PRICE_TABLE}", initialCtx.ServicePriceTable);

                var quickSetup = new BidiGenerateContentSetup();
                quickSetup.Setup.Model = modelName;
                quickSetup.Setup.GenerationConfig.ResponseModalities = new List<string> { "AUDIO" };
                quickSetup.Setup.GenerationConfig.Temperature = 0.7f;
                quickSetup.Setup.GenerationConfig.MaxOutputTokens = 2048;
                quickSetup.Setup.GenerationConfig.SpeechConfig.LanguageCode = "ar-EG";
                quickSetup.Setup.GenerationConfig.SpeechConfig.VoiceConfig.PrebuiltVoiceConfig.VoiceName = voiceName;
                quickSetup.Setup.RealtimeInputConfig.AutomaticActivityDetection.SilenceDurationMs = 2500;           // Wait 2.5s of silence before AI stops
                quickSetup.Setup.RealtimeInputConfig.AutomaticActivityDetection.StartOfSpeechSensitivity = "START_SENSITIVITY_LOW";  // Ignore background noise / accidental sounds
                quickSetup.Setup.RealtimeInputConfig.AutomaticActivityDetection.EndOfSpeechSensitivity = "END_SENSITIVITY_LOW";    // Don't cut Nody off easily
                quickSetup.Setup.SystemInstruction.Parts.Add(new BidiPart { Text = systemPrompt });
                quickSetup.Setup.Tools.Add(new BidiTool
                {
                    FunctionDeclarations = new List<BidiFunctionDeclaration>
                    {
                        new() { Name = "cancel_order",          Description = "Cancels a laundry order. ONLY call when user explicitly confirms cancel AND you have order_id. Only if status is PendingConfirmation.", Parameters = new { type = "object", properties = new { order_id = new { type = "string", description = "The numeric ID of the order to cancel." } }, required = new[] { "order_id" } } },
                        new() { Name = "get_active_orders",     Description = "Returns the customer's current active/in-progress laundry orders." },
                        new() { Name = "get_service_prices",    Description = "Returns market-average prices for available laundry services." },
                        new() { Name = "get_nearest_laundries", Description = "Returns the 3 nearest open laundries to the user's saved default address, sorted by relevance for comparison and recommendation." },
                        new() { Name = "get_laundry_services",  Description = "Returns services and prices for a specific laundry_id.", Parameters = new { type = "object", properties = new { laundry_id = new { type = "integer" } }, required = new[] { "laundry_id" } } },
                        new() { Name = "calculate_order_price", Description = "Calculates total order price including optional coupon.", Parameters = new { type = "object", properties = new { laundry_id = new { type = "integer" }, service_items = new { type = "array", items = new { type = "object", properties = new { service_id = new { type = "integer" }, quantity = new { type = "integer" } }, required = new[] { "service_id", "quantity" } } }, coupon_code = new { type = "string" } }, required = new[] { "laundry_id", "service_items" } } },
                        new() { Name = "place_order",           Description = "Creates a new order. Call after taking price confirmation.", Parameters = new { type = "object", properties = new { laundry_id = new { type = "integer" }, service_items = new { type = "array", items = new { type = "object", properties = new { service_id = new { type = "integer" }, quantity = new { type = "integer" } }, required = new[] { "service_id", "quantity" } } }, coupon_code = new { type = "string" }, payment_method = new { type = "string", description = "Cash or Wallet" } }, required = new[] { "laundry_id", "service_items" } } },
                        new() { Name = "track_order",           Description = "Tracks an order to get full status, estimated delivery time, and courier contact details.", Parameters = new { type = "object", properties = new { order_id = new { type = "string", description = "The numeric ID of the order." } }, required = new[] { "order_id" } } },
                        new() { Name = "file_complaint",        Description = "Files a complaint for an order.", Parameters = new { type = "object", properties = new { order_id = new { type = "string" }, details = new { type = "string" } }, required = new[] { "order_id", "details" } } },
                        new() { Name = "add_address",           Description = "Adds a new saved address.", Parameters = new { type = "object", properties = new { street = new { type = "string" }, city = new { type = "string" }, area = new { type = "string" }, instructions = new { type = "string" } }, required = new[] { "street", "city", "area" } } },
                        new() { Name = "rate_order",            Description = "Rates a completed order from 1 to 5.", Parameters = new { type = "object", properties = new { order_id = new { type = "string" }, rating = new { type = "integer" }, comment = new { type = "string" } }, required = new[] { "order_id", "rating" } } },
                        new() { Name = "charge_wallet",         Description = "Generates a checkout URL to charge the customer's wallet.", Parameters = new { type = "object", properties = new { amount = new { type = "number", description = "The amount to charge." } }, required = new[] { "amount" } } }
                    }
                });

                var setupBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(quickSetup, _jsonOptions));
                await geminiWs.SendAsync(setupBytes, WebSocketMessageType.Text, true, cts.Token);
                _logger.LogInformation("Gemini setup sent. Awaiting setupComplete...");

                // ── Step 4: run upstream/downstream pump ───────────────────────
                var upstreamTask   = UpstreamAsync(clientWs, geminiWs, setupReady, cts.Token);
                var downstreamTask = DownstreamAsync(geminiWs, clientWs, setupReady, captionState, cts.Token);

                // ── Step 5: after setupComplete, inject context as hidden text if it finished late ──
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var ready = await setupReady.Task.WaitAsync(TimeSpan.FromSeconds(30));
                        if (!ready || cts.IsCancellationRequested) return;

                        var ctx = await contextTask;
                        if (geminiWs.State != WebSocketState.Open) return;

                        // Construct the full context string to inject
                        var ctxString = $"""
                            CUSTOMER_NAME: {userName}
                            ACTIVE_ORDERS: {ctx.ActiveOrders}
                            ORDER_HISTORY: {ctx.OrderHistory}
                            NEAREST_LAUNDRIES: {ctx.NearestLaundries}
                            WALLET_BALANCE: {ctx.WalletBalance}
                            SERVICE_PRICE_TABLE: {ctx.ServicePriceTable}
                            """;

                        // Send context as a system-like text turn (user role, no audio expected back)
                        var ctxMsg = new
                        {
                            clientContent = new
                            {
                                turns = new[]
                                {
                                    new { role = "user", parts = new[] { new { text = $"[CONTEXT UPDATE]\n{ctxString}" } } }
                                },
                                turnComplete = false
                            }
                        };
                        var ctxBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ctxMsg, _jsonOptions));
                        if (geminiWs.State == WebSocketState.Open)
                            await geminiWs.SendAsync(ctxBytes, WebSocketMessageType.Text, true, cts.Token);
                        _logger.LogInformation("Late context injected into live session for User={UserId}", userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Context injection skipped: {Msg}", ex.Message);
                    }
                }, cts.Token);

                await Task.WhenAny(upstreamTask, downstreamTask);
                cts.Cancel();
                setupReady.TrySetCanceled(cts.Token);

                try { await Task.WhenAll(upstreamTask, downstreamTask); }
                catch (OperationCanceledException) { /* expected on cancel */ }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in voice session. User={UserId}", userId);
                await SendClientControlAsync(clientWs, "error", "Voice session failed. Please try again.");
            }
            finally
            {
                await SafeCloseAsync(clientWs);
                await SafeCloseAsync(geminiWs);
                _logger.LogInformation("Gemini voice session ended. User={UserId}", userId);
            }
        }

        // ── Helper model and context fetcher ──────────────────────────────────
        private class VoiceContext
        {
            public string ActiveOrders { get; set; } = "none";
            public string OrderHistory { get; set; } = "none";
            public string NearestLaundries { get; set; } = "none";
            public string WalletBalance { get; set; } = "unknown";
            public string ServicePriceTable { get; set; } = "none";
        }

        private async Task<VoiceContext> FetchVoiceContextAsync(string userId)
        {
            var ctx = new VoiceContext();
            try
            {
                var tActive = Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    return await BuildActiveOrdersContextAsync(userId, orderService);
                });

                var tHistory = Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();
                    return await BuildOrderHistoryContextAsync(userId, dbContext);
                });

                var tNearest = Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();
                    return await BuildNearestLaundriesContextAsync(userId, userService, dbContext);
                });

                var tWallet = Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();
                    return await BuildWalletBalanceContextAsync(userId, dbContext);
                });

                var tPrice = Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();
                    return await BuildServicePriceContextAsync(dbContext);
                });

                await Task.WhenAll(tActive, tHistory, tNearest, tWallet, tPrice);

                ctx.ActiveOrders = await tActive;
                ctx.OrderHistory = await tHistory;
                ctx.NearestLaundries = await tNearest;
                ctx.WalletBalance = await tWallet;
                ctx.ServicePriceTable = await tPrice;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed fetching context data: {Msg}", ex.Message);
            }
            return ctx;
        }

        // =====================================================================
        //  Upstream: Client → Gemini   (raw PCM binary → base64 JSON)
        // =====================================================================
        private async Task UpstreamAsync(
            WebSocket clientWs,
            ClientWebSocket geminiWs,
            TaskCompletionSource<bool> setupReady,
            CancellationToken ct)
        {
            using var setupTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            setupTimeout.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                var ready = await setupReady.Task.WaitAsync(setupTimeout.Token);
                if (!ready)
                {
                    _logger.LogWarning("Mic upstream aborted — Gemini setup did not succeed.");
                    return;
                }
                _logger.LogInformation("Mic upstream active — forwarding client audio to Gemini.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Upstream ended before setup: {Message}", ex.Message);
                return;
            }

            var buf         = new byte[16384];
            var binaryAccum = new MemoryStream();
            var textAccum   = new StringBuilder();
            var micStats    = new MicForwardStats();

            try
            {
                while (!ct.IsCancellationRequested
                    && clientWs.State == WebSocketState.Open
                    && geminiWs.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult res;
                    try   { res = await clientWs.ReceiveAsync(new ArraySegment<byte>(buf), ct); }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("[WS Upstream] ReceiveAsync threw: {ExMsg}", ex.Message);
                        break;
                    }

                    if (res.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("[WS Upstream] Received CLOSE frame from client.");
                        break;
                    }

                    if (res.MessageType == WebSocketMessageType.Text)
                    {
                        textAccum.Append(Encoding.UTF8.GetString(buf, 0, res.Count));
                        if (!res.EndOfMessage) continue;

                        var text = textAccum.ToString();
                        textAccum.Clear();

                        if (text.Contains("\"finish_turn\"", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Received finish_turn trigger from client. Forcing Gemini response.");
                            var finishMsg = new
                            {
                                clientContent = new
                                {
                                    turns = new[] { new { role = "user", parts = Array.Empty<object>() } },
                                    turnComplete = true
                                }
                            };
                            var finishBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(finishMsg, _jsonOptions));
                            if (geminiWs.State == WebSocketState.Open)
                                await geminiWs.SendAsync(finishBytes, WebSocketMessageType.Text, true, ct);
                            continue;
                        }

                        if (TryExtractClientAudio(text, out var audioFromJson))
                            await ForwardAudioToGeminiAsync(geminiWs, audioFromJson, ct, micStats, clientWs);
                        continue;
                    }

                    if (res.MessageType != WebSocketMessageType.Binary || res.Count == 0)
                        continue;

                    binaryAccum.Write(buf, 0, res.Count);
                    if (!res.EndOfMessage) continue;

                    var audio = binaryAccum.ToArray();
                    binaryAccum.SetLength(0);
                    Console.WriteLine($"[WS Upstream] Received audio chunk from client: {audio.Length} bytes.");
                    await ForwardAudioToGeminiAsync(geminiWs, audio, ct, micStats, clientWs);
                }
            }
            finally
            {
                _logger.LogInformation("[WS Upstream] Loop exited. clientWs={ClientState}, geminiWs={GeminiState}, cancel={Cancel}", clientWs.State, geminiWs.State, ct.IsCancellationRequested);
            }
        }

        private sealed class MicForwardStats
        {
            public long TotalBytes { get; set; }
            public DateTime LastPingUtc { get; set; } = DateTime.UtcNow;
        }

        private sealed class VoiceCaptionState
        {
            public StringBuilder UserText { get; } = new();
            public StringBuilder AssistantText { get; } = new();
        }

        private static async Task ForwardUserCaptionAsync(
            WebSocket clientWs, VoiceCaptionState captions, BidiTranscription? transcription)
        {
            var fragment = transcription?.Text;
            if (string.IsNullOrWhiteSpace(fragment)) return;

            captions.UserText.Clear();
            captions.UserText.Append(fragment.Trim());

            var text = captions.UserText.ToString();
            await SendClientEventAsync(clientWs, new { type = "caption", role = "user", text, final = transcription?.Finished == true });
            await SendClientEventAsync(clientWs, new { type = "inputTranscript", text });
        }

        private static async Task ForwardAssistantCaptionAsync(
            WebSocket clientWs,
            VoiceCaptionState captions,
            BidiTranscription? transcription,
            bool append)
        {
            var fragment = transcription?.Text;
            if (string.IsNullOrWhiteSpace(fragment)) return;

            if (append)
                captions.AssistantText.Append(fragment);
            else
            {
                captions.AssistantText.Clear();
                captions.AssistantText.Append(fragment);
            }

            var text = captions.AssistantText.ToString();
            await SendClientEventAsync(clientWs, new
            {
                type  = "caption",
                role  = "assistant",
                text,
                final = transcription?.Finished == true
            });
            await SendClientEventAsync(clientWs, new { type = "outputTranscript", text = fragment });
        }

        private static async Task ForwardCaptionsFromRawJsonAsync(
            WebSocket clientWs, VoiceCaptionState captions, string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (TryReadTranscription(root, "inputTranscription", out var inText))
                    await ForwardUserCaptionAsync(clientWs, captions, new BidiTranscription { Text = inText });

                if (TryReadTranscription(root, "outputTranscription", out var outText))
                    await ForwardAssistantCaptionAsync(
                        clientWs, captions, new BidiTranscription { Text = outText }, append: true);

                if (root.TryGetProperty("serverContent", out var sc))
                {
                    if (TryReadTranscription(sc, "inputTranscription", out var inText2))
                        await ForwardUserCaptionAsync(clientWs, captions, new BidiTranscription { Text = inText2 });

                    if (TryReadTranscription(sc, "outputTranscription", out var outText2))
                        await ForwardAssistantCaptionAsync(
                            clientWs, captions, new BidiTranscription { Text = outText2 }, append: true);
                }
            }
            catch { /* ignore */ }
        }

        private static bool TryReadTranscription(JsonElement parent, string property, out string text)
        {
            text = string.Empty;
            if (!parent.TryGetProperty(property, out var el)) return false;
            if (!el.TryGetProperty("text", out var textEl)) return false;
            text = textEl.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(text);
        }

        private async Task ForwardAudioToGeminiAsync(
            ClientWebSocket geminiWs,
            byte[] audio,
            CancellationToken ct,
            MicForwardStats stats,
            WebSocket clientWs)
        {
            if (audio.Length == 0 || geminiWs.State != WebSocketState.Open) return;

            stats.TotalBytes += audio.Length;

            var inlineData = new BidiInlineData
            {
                MimeType = "audio/pcm;rate=16000",
                Data     = Convert.ToBase64String(audio)
            };

            var msg = new BidiRealtimeInputMessage
            {
                RealtimeInput = new BidiRealtimeInput
                {
                    Audio = inlineData
                }
            };

            var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg, _jsonOptions));
            await geminiWs.SendAsync(json, WebSocketMessageType.Text, true, ct);

            var now = DateTime.UtcNow;
            if ((now - stats.LastPingUtc).TotalMilliseconds >= 400)
            {
                stats.LastPingUtc = now;
                await SendClientEventAsync(clientWs, new
                {
                    type          = "listening",
                    bytesReceived = stats.TotalBytes,
                    chunkBytes    = audio.Length
                });
            }
        }

        private static bool TryExtractClientAudio(string text, out byte[] audio)
        {
            audio = Array.Empty<byte>();
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeEl)) return false;
                var type = typeEl.GetString();
                if (type is not ("audio" or "mic")) return false;

                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.String)
                {
                    audio = Convert.FromBase64String(dataEl.GetString()!);
                    return audio.Length > 0;
                }
            }
            catch { /* not client audio JSON */ }

            return false;
        }

        // =====================================================================
        //  Downstream: Gemini → Client   (audio bytes + tool-call handling)
        // =====================================================================
        private async Task DownstreamAsync(
            ClientWebSocket geminiWs,
            WebSocket clientWs,
            TaskCompletionSource<bool> setupReady,
            VoiceCaptionState captionState,
            CancellationToken ct)
        {
            var buf = new byte[65536];
            var sb  = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested
                    && geminiWs.State == WebSocketState.Open
                    && clientWs.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult res;
                    try
                    {
                        res = await geminiWs.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Gemini WebSocket read failed or aborted: {Msg}", ex.Message);
                        break;
                    }

                    if (res.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("[WS Downstream] Received CLOSE frame from Gemini. Status: {Status}, Description: {Desc}", geminiWs.CloseStatus, geminiWs.CloseStatusDescription);
                        break;
                    }

                    if (res.MessageType == WebSocketMessageType.Text || res.MessageType == WebSocketMessageType.Binary)
                    {
                        sb.Append(Encoding.UTF8.GetString(buf, 0, res.Count));
                        if (res.EndOfMessage)
                        {
                            await ProcessGeminiMessageAsync(sb.ToString(), clientWs, geminiWs, setupReady, captionState, ct);
                            sb.Clear();
                        }
                    }
                }
            }
            finally
            {
                _logger.LogInformation("[WS Downstream] Loop exited. clientWs={ClientState}, geminiWs={GeminiState}, cancel={Cancel}", clientWs.State, geminiWs.State, ct.IsCancellationRequested);
            }
        }

        // =====================================================================
        //  Parse & dispatch one complete JSON message from Gemini
        // =====================================================================
        private async Task ProcessGeminiMessageAsync(
            string json,
            WebSocket clientWs,
            ClientWebSocket geminiWs,
            TaskCompletionSource<bool> setupReady,
            VoiceCaptionState captions,
            CancellationToken ct)
        {
            var shortJson = json.Length > 300 ? json.Substring(0, 300) + "..." : json;
            _logger.LogInformation("Received message from Gemini: {ShortJson}", shortJson);

            if (json.Contains("\"error\"", StringComparison.Ordinal))
            {
                _logger.LogError("Gemini returned error response: {Json}", json);
                setupReady.TrySetResult(false);
                await SendClientControlAsync(clientWs, "error", ExtractGeminiErrorMessage(json));
                return;
            }

            try
            {
                var msg = JsonSerializer.Deserialize<BidiServerMessage>(json, _jsonOptions);
                if (msg == null) return;

                // ── Session confirmed ────────────────────────────────────────
                if (msg.SetupComplete != null)
                {
                    _logger.LogInformation("Gemini session setup confirmed.");
                    setupReady.TrySetResult(true);
                    await SendClientEventAsync(clientWs, new
                    {
                        type             = "ready",
                        inputSampleRate  = 16000,
                        outputSampleRate = 24000,
                        format           = "pcm_s16le",
                        channels         = 1
                    });
                    return;
                }

                // Top-level transcriptions (Gemini may send outside serverContent)
                await ForwardUserCaptionAsync(clientWs, captions, msg.InputTranscription);
                await ForwardAssistantCaptionAsync(clientWs, captions, msg.OutputTranscription, append: true);

                var content = msg.ServerContent;
                if (content != null)
                {
                    await ForwardUserCaptionAsync(clientWs, captions, content.InputTranscription);
                    await ForwardAssistantCaptionAsync(clientWs, captions, content.OutputTranscription, append: true);

                    if (content.Interrupted == true)
                    {
                        _logger.LogInformation("User interrupted assistant speech.");
                        await SendClientEventAsync(clientWs, new { type = "interrupted" });
                    }

                    if (content.ModelTurn?.Parts is { Count: > 0 } parts)
                    {
                        await SendClientEventAsync(clientWs, new { type = "speaking" });

                        foreach (var part in parts)
                        {
                            if (!string.IsNullOrWhiteSpace(part.Text))
                                await ForwardAssistantCaptionAsync(
                                    clientWs, captions,
                                    new BidiTranscription { Text = part.Text },
                                    append: true);

                            if (part.InlineData?.Data is { Length: > 0 } b64)
                            {
                                try
                                {
                                    var audio = Convert.FromBase64String(b64);
                                    if (clientWs.State == WebSocketState.Open)
                                        await clientWs.SendAsync(
                                            new ArraySegment<byte>(audio),
                                            WebSocketMessageType.Binary, true, ct);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to forward audio chunk to client");
                                }
                            }
                        }
                    }

                    if (content.TurnComplete == true)
                    {
                        await SendClientEventAsync(clientWs, new
                        {
                            type = "caption",
                            role = "assistant",
                            text = captions.AssistantText.ToString(),
                            final = true
                        });
                        captions.AssistantText.Clear();
                        await SendClientEventAsync(clientWs, new { type = "turnComplete" });
                    }

                    if (content.GenerationComplete == true)
                        await SendClientEventAsync(clientWs, new { type = "generationComplete" });
                }
                else
                {
                    await ForwardCaptionsFromRawJsonAsync(clientWs, captions, json);
                }

                // ── Execute tool calls ───────────────────────────────────────
                if (msg.ToolCall?.FunctionCalls is { Count: > 0 } calls)
                {
                    var responses = new List<BidiFunctionResponse>();

                    foreach (var fc in calls)
                    {
                        _logger.LogInformation("Tool call received: {Tool}", fc.Name);
                        var result = await ExecuteToolAsync(fc.Name, fc.Args);
                        responses.Add(new BidiFunctionResponse
                        {
                            Id       = fc.Id,
                            Name     = fc.Name,
                            Response = new { output = result }
                        });
                    }

                    var toolMsg = new BidiToolResponseMessage
                    {
                        ToolResponse = new BidiToolResponse { FunctionResponses = responses }
                    };

                    var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(toolMsg, _jsonOptions));
                    if (geminiWs.State == WebSocketState.Open)
                        await geminiWs.SendAsync(payload, WebSocketMessageType.Text, true, ct);
                }
            }
            catch (JsonException jex)
            {
                _logger.LogWarning("Non-fatal JSON parse error from Gemini: {Msg}", jex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Gemini message");
            }
        }

        // =====================================================================
        //  Tool router
        // =====================================================================
        private async Task<object> ExecuteToolAsync(string name, Dictionary<string, object> args)
        {
            var userId = GetUserId();

            return name switch
            {
                "cancel_order"          => await CancelOrderToolAsync(userId, args),
                "get_active_orders"     => await GetActiveOrdersToolAsync(userId),
                "get_service_prices"    => await GetServicePricesToolAsync(),
                "get_nearest_laundries" => await GetNearestLaundriesToolAsync(userId),
                "get_laundry_services"  => await GetLaundryServicesToolAsync(args),
                "calculate_order_price" => await CalculateOrderPriceToolAsync(userId, args),
                "place_order"           => await PlaceOrderToolAsync(userId, args),
                "track_order"           => await TrackOrderToolAsync(userId, args),
                "file_complaint"        => await FileComplaintToolAsync(userId, args),
                "add_address"           => await AddAddressToolAsync(userId, args),
                "rate_order"            => await RateOrderToolAsync(userId, args),
                "charge_wallet"         => await ChargeWalletToolAsync(userId, args),
                _                       => (object)$"Unknown tool '{name}'."
            };
        }

        // ── Safely extract a string value from args (handles JsonElement) ────
        private static string? ExtractString(Dictionary<string, object> args, string key)
        {
            if (!args.TryGetValue(key, out var raw) || raw is null) return null;

            if (raw is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.String
                    ? je.GetString()       // string  "123"  → 123
                    : je.ToString();       // number   123   → "123"
            }
            return raw.ToString();
        }

        // ── charge_wallet ─────────────────────────────────────────────────────
        private async Task<object> ChargeWalletToolAsync(string userId, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(userId)) return "المستخدم مش مسجل دخوله.";

            var amountStr = ExtractString(args, "amount");
            if (!decimal.TryParse(amountStr?.Trim(), out var amount) || amount <= 0)
                return "مبلغ الشحن غير صحيح.";

            if (amount > 5000)
                return "عفواً، أقصى مبلغ مسموح بشحنه في المرة الواحدة هو 5000 جنيه.";

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var kashierService = scope.ServiceProvider.GetRequiredService<IKashierService>();

                var result = await kashierService.CreateWalletChargeAsync(userId, amount);

                if (result.IsSuccess && result.Value != null)
                {
                    return $"تم استخراج رابط الشحن بنجاح: {result.Value.CheckoutUrl} (اطلب من المستخدم التحقق من الرابط في التطبيق لإتمام الدفع).";
                }
                
                return $"مقدرتش أعمل رابط الشحن: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChargeWallet AI tool failed for User={UserId}", userId);
                return "حدث خطأ أثناء محاولة شحن المحفظة.";
            }
        }

        // ── cancel_order ──────────────────────────────────────────────────────
        private async Task<object> CancelOrderToolAsync(string userId, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(userId))
                return "المستخدم مش مسجل دخوله، مش قادر ألغي الطلب.";

            var rawId = ExtractString(args, "order_id");
            if (string.IsNullOrWhiteSpace(rawId))
                return "لازم تبعتلي رقم الطلب عشان أقدر ألغيه.";

            if (!int.TryParse(rawId.Trim(), out var orderId) || orderId <= 0)
                return "رقم الطلب مش صحيح.";

            try
            {
                // Use a new scope so the scoped IOrderService gets a fresh DbContext
                using var scope        = _serviceProvider.CreateScope();
                var       orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                var req    = new CancelOrderRequest { CancellationReason = "Cancelled via Voice Assistant" };
                var result = await orderService.CancelOrderAsync(userId, orderId, req);

                return result.IsSuccess
                    ? $"تم إلغاء الطلب رقم {orderId} بنجاح."
                    : $"مقدرتش ألغي الطلب: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId} for user {UserId}", orderId, userId);
                return "حصل خطأ أثناء الإلغاء، حاول تاني.";
            }
        }

        // ── get_active_orders ─────────────────────────────────────────────────
        private async Task<object> GetActiveOrdersToolAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return "المستخدم مش مسجل دخوله.";

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var res = await orderService.GetActiveOrdersAsync(userId);
            if (res == null || !res.IsSuccess || res.Value == null || res.Value.Count == 0)
                return "مفيش طلبات نشطة حالياً.";

            return res.Value.Select(o => (object)new
            {
                order_id    = o.Id,
                status      = o.Status.ToString(),
                price       = $"{o.TotalPrice.ToString("0.##", CultureInfo.InvariantCulture)} جنيه",
                laundry     = o.LaundryName ?? "غير محدد",
                pickup      = o.PickupTime.ToString("yyyy-MM-dd HH:mm"),
                delivery    = o.DeliveryTime.HasValue
                                ? o.DeliveryTime.Value.ToString("yyyy-MM-dd HH:mm")
                                : "غير محدد",
                cancellable = o.Status == OrderStatus.PendingConfirmation
            }).ToList();
        }

        // ── track_order ───────────────────────────────────────────────────────
        private async Task<object> TrackOrderToolAsync(string userId, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(userId))
                return "المستخدم مش مسجل دخوله.";

            var rawId = ExtractString(args, "order_id");
            if (string.IsNullOrWhiteSpace(rawId) || !int.TryParse(rawId.Trim(), out var orderId))
                return "رقم الطلب مش صحيح.";

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var result = await orderService.TrackOrderAsync(userId, orderId);
            if (!result.IsSuccess || result.Value == null)
                return $"مقدرتش أتبع حالة الطلب: {result.ErrorMessage}";

            var track = result.Value;
            return new
            {
                order_id = track.OrderId,
                status = track.Status.ToString(),
                laundry_name = track.LaundryName,
                estimated_delivery_time = track.EstimatedDeliveryTime?.ToString("yyyy-MM-dd HH:mm") ?? track.DeliveryTime?.ToString("yyyy-MM-dd HH:mm") ?? "غير محدد",
                courier_name = !string.IsNullOrWhiteSpace(track.CourierName) ? track.CourierName : "لم يتم تحديد مندوب بعد",
                courier_phone = !string.IsNullOrWhiteSpace(track.CourierPhone) ? track.CourierPhone : "غير متاح"
            };
        }

        // ── get_service_prices ────────────────────────────────────────────────
        private async Task<object> GetServicePricesToolAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();

            var svc = await dbContext.Services
                .Where(s => s.IsAvailable)
                .GroupBy(s => s.ServiceName)
                .Select(g => new
                {
                    service   = g.Key,
                    min_price = g.Min(x => x.Price),
                    avg_price = Math.Round(g.Average(x => x.Price), 2),
                    max_price = g.Max(x => x.Price)
                })
                .OrderBy(x => x.service)
                .Take(20)
                .ToListAsync();

            if (svc.Count == 0) return "لا توجد أسعار خدمات متاحة حالياً.";
            return svc.Cast<object>().ToList();
        }

        // ── get_laundry_services ──────────────────────────────────────────────
        private async Task<object> GetLaundryServicesToolAsync(Dictionary<string, object> args)
        {
            var rawId = ExtractString(args, "laundry_id");
            if (string.IsNullOrWhiteSpace(rawId) || !int.TryParse(rawId.Trim(), out var laundryId))
                return "رقم المغسلة غير صحيح.";

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();

            var services = await dbContext.Services
                .Where(s => s.LaundryId == laundryId && s.IsAvailable)
                .Select(s => new { s.Id, s.ServiceName, s.Price })
                .ToListAsync();

            if (services.Count == 0) return "مفيش خدمات متاحة حالياً للمغسلة دي.";
            return services.Select(s => (object)new { id = s.Id, name = s.ServiceName, price = $"{s.Price.ToString("0.##", CultureInfo.InvariantCulture)} جنيه" }).ToList();
        }

        // ── calculate_order_price ─────────────────────────────────────────────
        private async Task<object> CalculateOrderPriceToolAsync(string userId, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(userId)) return "المستخدم مش مسجل دخوله.";

            var rawLaundryId = ExtractString(args, "laundry_id");
            if (string.IsNullOrWhiteSpace(rawLaundryId) || !int.TryParse(rawLaundryId.Trim(), out var laundryId))
                return "رقم المغسلة غير صحيح.";

            var itemsList = new List<OrderItemRequest>();
            if (args.TryGetValue("service_items", out var rawItems) && rawItems is JsonElement jeItems && jeItems.ValueKind == JsonValueKind.Array)
            {
                foreach (var itemEl in jeItems.EnumerateArray())
                {
                    if (itemEl.TryGetProperty("service_id", out var sidEl) && itemEl.TryGetProperty("quantity", out var qtyEl))
                    {
                        int sId = sidEl.ValueKind == JsonValueKind.Number ? sidEl.GetInt32() : int.Parse(sidEl.GetString()!);
                        int qty = qtyEl.ValueKind == JsonValueKind.Number ? qtyEl.GetInt32() : int.Parse(qtyEl.GetString()!);
                        itemsList.Add(new OrderItemRequest { ServiceId = sId, Quantity = qty });
                    }
                }
            }

            if (itemsList.Count == 0) return "مفيش خدمات متحددة.";

            var couponCode = ExtractString(args, "coupon_code");

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var res = await orderService.CalculatePriceAsync(new CalculatePriceRequest
            {
                LaundryId = laundryId,
                Items = itemsList,
                CouponCode = couponCode
            });

            if (!res.IsSuccess || res.Value == null)
                return $"مقدرتش أحسب التكلفة: {res.ErrorMessage}";

            return new
            {
                total_price = res.Value.TotalPrice,
                discount = res.Value.DiscountAmount,
                final_price = res.Value.FinalPrice
            };
        }

        // ── file_complaint ────────────────────────────────────────────────────
        private async Task<object> FileComplaintToolAsync(string userId, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(userId)) return "المستخدم مش مسجل دخوله.";

            var rawId = ExtractString(args, "order_id");
            if (!int.TryParse(rawId?.Trim(), out var orderId)) return "رقم الطلب غير صحيح.";
            
            var details = ExtractString(args, "details") ?? "شكوى عامة عبر المساعد الصوتي.";

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();

            var order = await dbContext.Orders.Include(o => o.Laundry).FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);
            if (order == null) return "مفيش أوردر مسجل باسمك بالرقم ده.";

            var complaint = new Complaint
            {
                OrderId = orderId,
                CustomerId = userId,
                LaundryId = order.LaundryId,
                LaundryAdminId = order.Laundry.AdminId,
                Details = details,
                Status = ComplaintStatus.Open,
                CreatedAt = DateTime.UtcNow
            };
            
            dbContext.Complaints.Add(complaint);
            await dbContext.SaveChangesAsync();

            return "تم تسجيل الشكوى بنجاح، فريق خدمة العملاء هيتواصل معاك قريب جداً.";
        }

        // ── add_address ───────────────────────────────────────────────────────
        private async Task<object> AddAddressToolAsync(string userId, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(userId)) return "المستخدم مش مسجل دخوله.";

            var req = new AddAddressRequest
            {
                Type = AddressType.Home,
                Label = "عنوان جديد من المساعد الصوتي",
                Street = ExtractString(args, "street") ?? "غير محدد",
                City = ExtractString(args, "city") ?? "غير محدد",
                Area = ExtractString(args, "area") ?? "غير محدد",
                Instructions = ExtractString(args, "instructions"),
                IsDefault = true
            };

            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            var result = await userService.AddAddressAsync(userId, req);
            if (result.IsSuccess)
                return "تم إضافة العنوان بنجاح وتعيينه كعنوان أساسي للتوصيل.";
            return $"فشل إضافة العنوان: {result.ErrorMessage}";
        }

        // ── rate_order ────────────────────────────────────────────────────────
        private async Task<object> RateOrderToolAsync(string userId, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(userId)) return "المستخدم مش مسجل دخوله.";

            var rawId = ExtractString(args, "order_id");
            if (!int.TryParse(rawId?.Trim(), out var orderId)) return "رقم الطلب غير صحيح.";

            var ratingStr = ExtractString(args, "rating");
            if (!int.TryParse(ratingStr?.Trim(), out var rating)) rating = 5;

            var comment = ExtractString(args, "comment");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();
            var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();

            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);
            if (order == null) return "الأوردر مش موجود أو مش متسجل باسمك.";

            var req = new AddReviewRequest
            {
                OrderId = orderId,
                LaundryId = order.LaundryId,
                Rating = rating,
                Comment = comment
            };

            var result = await reviewService.AddReviewAsync(userId, req);
            if (result.IsSuccess) return "بنشكرك جداً على تقييمك، كلامك وصلنا وهنحسن خدمتنا بيه!";
            
            return $"للأسف مقدرتش أضيف التقييم: {result.ErrorMessage}";
        }

        // ── place_order ───────────────────────────────────────────────────────
        private async Task<object> PlaceOrderToolAsync(string userId, Dictionary<string, object> args)
        {
            if (string.IsNullOrEmpty(userId))
                return "المستخدم مش مسجل دخوله.";

            var rawLaundryId = ExtractString(args, "laundry_id");
            if (string.IsNullOrWhiteSpace(rawLaundryId) || !int.TryParse(rawLaundryId.Trim(), out var laundryId))
                return "رقم المغسلة غير صحيح.";

            var itemsList = new List<OrderItemRequest>();
            if (args.TryGetValue("service_items", out var rawItems) && rawItems is JsonElement jeItems && jeItems.ValueKind == JsonValueKind.Array)
            {
                foreach (var itemEl in jeItems.EnumerateArray())
                {
                    if (itemEl.TryGetProperty("service_id", out var sidEl) &&
                        itemEl.TryGetProperty("quantity", out var qtyEl))
                    {
                        int sId = sidEl.ValueKind == JsonValueKind.Number ? sidEl.GetInt32() : int.Parse(sidEl.GetString()!);
                        int qty = qtyEl.ValueKind == JsonValueKind.Number ? qtyEl.GetInt32() : int.Parse(qtyEl.GetString()!);
                        itemsList.Add(new OrderItemRequest { ServiceId = sId, Quantity = qty });
                    }
                }
            }

            if (itemsList.Count == 0) return "مفيش خدمات متحددة في الأوردر.";

            var totalItemsCount = itemsList.Sum(i => i.Quantity);
            if (totalItemsCount > 50)
                return "عفواً، لا يمكن طلب أكثر من 50 قطعة في الطلب الواحد عبر المساعد الصوتي لأسباب أمنية.";

            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            var addrsRes = await userService.GetAddressesAsync(userId);
            var defAddr = addrsRes?.Value?.FirstOrDefault(a => a.IsDefault) ?? addrsRes?.Value?.FirstOrDefault();
            var addressStr = defAddr != null ? $"{defAddr.Street}, {defAddr.City}, {defAddr.Area}" : "عنوان افتراضي";

            var couponCode = ExtractString(args, "coupon_code");
            var paymentMethodStr = ExtractString(args, "payment_method") ?? "Cash";
            PaymentMethod method = paymentMethodStr.Equals("Wallet", StringComparison.OrdinalIgnoreCase) ? PaymentMethod.Wallet : PaymentMethod.Cash;

            var req = new PlaceOrderRequest
            {
                LaundryId = laundryId,
                Items = itemsList,
                PickupTime = DateTime.UtcNow.AddMinutes(120),
                DeliveryTime = null,
                PickupLocation = addressStr,
                DeliveryLocation = addressStr,
                Notes = "أوردر من المساعد الصوتي نودي.",
                CouponCode = couponCode
            };

            try 
            {
                var result = await orderService.PlaceOrderAsync(userId, req);
                if (result.IsSuccess && result.Value != null)
                {
                    var orderId = result.Value.Id;
                    var finalPrice = result.Value.TotalPrice;
                    string paymentMsg = "الدفع كاش عند الاستلام.";
                    
                    if (finalPrice > 0)
                    {
                        var payReq = new ProcessPaymentRequest { OrderId = orderId, Amount = finalPrice, PaymentMethod = method };
                        var payResult = await paymentService.ProcessPaymentAsync(userId, payReq);
                        if (!payResult.IsSuccess)
                        {
                            paymentMsg = $"لكن حصلت مشكلة في الدفع: ({payResult.ErrorMessage}). فالدفع هيكون كاش عند الاستلام.";
                        }
                        else if (method == PaymentMethod.Wallet)
                        {
                            paymentMsg = "وتم خصم المبلغ من محفظتك بنجاح.";
                        }
                    }
                    else
                    {
                        paymentMsg = "التكلفة النهائية صفر بعد الخصومات.";
                    }

                    return $"تم الأوردر بنجاح! رقم الأوردر {orderId}. {paymentMsg}";
                }
                return $"مقرارش أعمل الأوردر. السبب: {result.ErrorMessage}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlaceOrder AI tool failed for User={UserId}", userId);
                return "حدث خطأ غير متوقع أثناء عمل الأوردر.";
            }
        }


        private async Task<object> GetNearestLaundriesToolAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return "مش لاقي بيانات المستخدم.";

            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();

            var addrRes = await userService.GetAddressesAsync(userId);
            if (addrRes?.IsSuccess != true || addrRes.Value == null)
                return "مش لاقي عنوان محفوظ. أضف عنوان من التطبيق الأول.";

            var addr = addrRes.Value
                .FirstOrDefault(a => a.IsDefault && a.Latitude.HasValue && a.Longitude.HasValue)
                ?? addrRes.Value.FirstOrDefault(a => a.Latitude.HasValue && a.Longitude.HasValue);

            if (addr?.Latitude == null || addr.Longitude == null)
                return "العنوان الحالي بدون إحداثيات. أضف إحداثيات لعنوانك من التطبيق.";

            double lat = (double)addr.Latitude.Value;
            double lon = (double)addr.Longitude.Value;

            var laundries = await dbContext.Laundries
                .Where(l => l.Status == LaundryStatus.Active && l.Availability == LaundryAvailability.Open)
                .Select(l => new { l.Id, l.Name, l.Address, l.Latitude, l.Longitude, l.AverageRating })
                .ToListAsync();

            if (laundries.Count == 0) return "مفيش مغاسل متاحة دلوقتي.";

            var topLaundries = laundries
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Address,
                    l.AverageRating,
                    DistKm = Haversine(lat, lon, (double)l.Latitude, (double)l.Longitude)
                })
                .OrderBy(x => x.DistKm)
                .Take(3)
                .ToList();

            var resultList = new List<object>();
            var nearest = topLaundries.FirstOrDefault();
            if (nearest != null)
            {
                var nearestPrices = await dbContext.Services
                    .Where(s => s.LaundryId == nearest.Id && s.IsAvailable)
                    .Select(s => new { s.ServiceName, s.Price })
                    .OrderBy(s => s.ServiceName)
                    .Take(15)
                    .ToListAsync();

                var marketAvgPrices = await dbContext.Services
                    .Where(s => s.IsAvailable)
                    .GroupBy(s => s.ServiceName)
                    .Select(g => new
                    {
                        ServiceName = g.Key,
                        AvgPrice = Math.Round(g.Average(x => x.Price), 2)
                    })
                    .OrderBy(x => x.ServiceName)
                    .Take(15)
                    .ToListAsync();

                foreach (var l in topLaundries)
                {
                    if (l.Id == nearest.Id)
                    {
                        resultList.Add(new
                        {
                            id = l.Id,
                            name = l.Name,
                            address = l.Address,
                            rating = l.AverageRating.ToString("0.0", CultureInfo.InvariantCulture),
                            distance = $"{l.DistKm.ToString("0.00", CultureInfo.InvariantCulture)} كم",
                            is_nearest = true,
                            services_and_prices = nearestPrices.Select(p => new { service = p.ServiceName, price = $"{p.Price.ToString("0.##", CultureInfo.InvariantCulture)} جنيه" }).ToList(),
                            market_average_prices = marketAvgPrices.Select(p => new { service = p.ServiceName, avg_price = $"{p.AvgPrice.ToString("0.##", CultureInfo.InvariantCulture)} جنيه" }).ToList()
                        });
                    }
                    else
                    {
                        resultList.Add(new
                        {
                            id = l.Id,
                            name = l.Name,
                            address = l.Address,
                            rating = l.AverageRating.ToString("0.0", CultureInfo.InvariantCulture),
                            distance = $"{l.DistKm.ToString("0.00", CultureInfo.InvariantCulture)} كم",
                            is_nearest = false
                        });
                    }
                }
            }

            return resultList;
        }

        // =====================================================================
        //  Context builders  (mirror ChatController logic exactly)
        // =====================================================================
        private async Task<string> BuildActiveOrdersContextAsync(string userId, IOrderService orderService)
        {
            if (string.IsNullOrEmpty(userId)) return "none";
            try
            {
                var res = await orderService.GetActiveOrdersAsync(userId);
                if (res == null || !res.IsSuccess || res.Value == null || res.Value.Count == 0)
                    return "none";

                var sb = new StringBuilder();
                foreach (var o in res.Value)
                {
                    bool canCancel = o.Status == OrderStatus.PendingConfirmation;
                    sb.AppendLine(
                        $"- Order#{o.Id} | {o.LaundryName ?? "-"} | {o.Status} | " +
                        $"{o.TotalPrice.ToString("0.##", CultureInfo.InvariantCulture)} جم | " +
                        $"pickup:{o.PickupTime:yyyy-MM-dd HH:mm} | cancellable:{canCancel}");
                }
                return sb.ToString().Trim();
            }
            catch { return "none"; }
        }

        private async Task<string> BuildOrderHistoryContextAsync(string userId, NadeefDbContext dbContext)
        {
            if (string.IsNullOrEmpty(userId)) return "none";
            try
            {
                var recent = await dbContext.Orders
                    .Where(o => o.CustomerId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new { o.Id, o.Status, o.TotalPrice, o.CreatedAt })
                    .ToListAsync();

                if (recent.Count == 0) return "none";

                var sb = new StringBuilder();
                foreach (var o in recent)
                    sb.AppendLine(
                        $"- Order#{o.Id} | {o.Status} | " +
                        $"{o.TotalPrice.ToString("0.##", CultureInfo.InvariantCulture)} جم | " +
                        $"{o.CreatedAt:yyyy-MM-dd}");
                return sb.ToString().Trim();
            }
            catch { return "none"; }
        }

        private async Task<string> BuildNearestLaundriesContextAsync(string userId, IUserService userService, NadeefDbContext dbContext)
        {
            if (string.IsNullOrEmpty(userId)) return "none";
            try
            {
                var addrRes = await userService.GetAddressesAsync(userId);
                if (addrRes?.IsSuccess != true || addrRes.Value == null) return "none";

                var addr = addrRes.Value
                    .FirstOrDefault(a => a.IsDefault && a.Latitude.HasValue && a.Longitude.HasValue)
                    ?? addrRes.Value.FirstOrDefault(a => a.Latitude.HasValue && a.Longitude.HasValue);

                if (addr?.Latitude == null || addr.Longitude == null) return "none";

                double lat = (double)addr.Latitude.Value;
                double lon = (double)addr.Longitude.Value;

                var cache = _serviceProvider.GetService<IMemoryCache>();
                List<ActiveLaundryDto> laundriesList = null;
                if (cache != null && cache.TryGetValue<List<ActiveLaundryDto>>("Voice_ActiveLaundries", out var cachedLaundries))
                {
                    laundriesList = cachedLaundries;
                }
                else
                {
                    var queryResult = await dbContext.Laundries
                        .Where(l => l.Status == LaundryStatus.Active && l.Availability == LaundryAvailability.Open)
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

                if (laundriesList == null || laundriesList.Count == 0) return "none";

                var sb = new StringBuilder();
                var topLaundries = laundriesList
                    .Select(l => new
                    {
                        l.Id, l.Name, l.Address, l.AverageRating,
                        Dist = Haversine(lat, lon, (double)l.Latitude, (double)l.Longitude)
                    })
                    .OrderBy(x => x.Dist)
                    .Take(3)
                    .ToList();

                foreach (var l in topLaundries)
                {
                    sb.AppendLine(
                        $"- ID:{l.Id} | {l.Name} | {l.Dist.ToString("0.00", CultureInfo.InvariantCulture)} كم | " +
                        $"rating:{l.AverageRating.ToString("0.0", CultureInfo.InvariantCulture)} | {l.Address}");
                }

                var nearest = topLaundries.FirstOrDefault();
                if (nearest != null)
                {
                    var nearestPrices = await dbContext.Services
                        .Where(s => s.LaundryId == nearest.Id && s.IsAvailable)
                        .Select(s => new { s.ServiceName, s.Price })
                        .OrderBy(s => s.ServiceName)
                        .Take(10)
                        .ToListAsync();

                    if (nearestPrices.Count > 0)
                    {
                        sb.AppendLine($"PRICES_FOR_NEAREST_LAUNDRY ({nearest.Name}):");
                        foreach (var p in nearestPrices)
                        {
                            sb.AppendLine($"  * {p.ServiceName}: {p.Price.ToString("0.##", CultureInfo.InvariantCulture)} جم");
                        }
                    }
                    else
                    {
                        var avgPrices = await dbContext.Services
                            .Where(s => s.IsAvailable)
                            .GroupBy(s => s.ServiceName)
                            .Select(g => new { ServiceName = g.Key, Avg = g.Average(x => x.Price) })
                            .OrderBy(x => x.ServiceName)
                            .Take(10)
                            .ToListAsync();

                        if (avgPrices.Count > 0)
                        {
                            sb.AppendLine("PRICES_FOR_NEAREST_LAUNDRY (market averages):");
                            foreach (var p in avgPrices)
                            {
                                sb.AppendLine($"  * {p.ServiceName}: avg {p.Avg.ToString("0.##", CultureInfo.InvariantCulture)} جم");
                            }
                        }
                    }
                }

                return sb.ToString().Trim();
            }
            catch { return "none"; }
        }

        private async Task<string> BuildWalletBalanceContextAsync(string userId, NadeefDbContext dbContext)
        {
            if (string.IsNullOrEmpty(userId)) return "unknown";
            try
            {
                var wallet = await dbContext.CustomerWallets
                    .Where(w => w.CustomerId == userId)
                    .Select(w => new { w.Balance, w.TotalCharged })
                    .FirstOrDefaultAsync();

                if (wallet == null) return "الرصيد: 0 جنيه، إجمالي الشحن: 0 جنيه";

                return $"الرصيد المتاح: {wallet.Balance.ToString("0.##", CultureInfo.InvariantCulture)} جنيه، إجمالي الشحن والمصروف: {wallet.TotalCharged.ToString("0.##", CultureInfo.InvariantCulture)} جنيه";
            }
            catch { return "unknown"; }
        }

        private async Task<string> BuildServicePriceContextAsync(NadeefDbContext dbContext)
        {
            var cache = _serviceProvider.GetService<IMemoryCache>();
            if (cache != null && cache.TryGetValue<string>("Voice_ServicePriceContext", out var cachedResult))
            {
                return cachedResult;
            }

            try
            {
                var svc = await dbContext.Services
                    .Where(s => s.IsAvailable)
                    .GroupBy(s => s.ServiceName)
                    .Select(g => new
                    {
                        Name = g.Key,
                        Avg  = Math.Round(g.Average(x => x.Price), 2),
                        Min  = g.Min(x => x.Price),
                        Max  = g.Max(x => x.Price)
                    })
                    .OrderBy(x => x.Name)
                    .Take(20)
                    .ToListAsync();

                if (svc.Count == 0) return "none";

                var sb = new StringBuilder();
                foreach (var s in svc)
                    sb.AppendLine(
                        $"- {s.Name}: avg={s.Avg.ToString("0.##", CultureInfo.InvariantCulture)}, " +
                        $"min={s.Min.ToString("0.##", CultureInfo.InvariantCulture)}, " +
                        $"max={s.Max.ToString("0.##", CultureInfo.InvariantCulture)} جم");

                var result = sb.ToString().Trim();
                if (cache != null)
                {
                    cache.Set("Voice_ServicePriceContext", result, TimeSpan.FromMinutes(10));
                }
                return result;
            }
            catch { return "none"; }
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private static async Task SendClientControlAsync(WebSocket clientWs, string type, string? message)
        {
            if (message == null)
                await SendClientEventAsync(clientWs, new { type });
            else
                await SendClientEventAsync(clientWs, new { type, message });
        }

        private static async Task SendClientEventAsync(WebSocket clientWs, object payload)
        {
            if (clientWs.State != WebSocketState.Open) return;

            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, _jsonOptions));
            await clientWs.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static string ExtractGeminiErrorMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err)
                    && err.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? "Gemini Live API error.";
            }
            catch { /* ignore parse errors */ }

            return "Gemini Live API error.";
        }

        /// <summary>Haversine distance between two lat/lon coordinates in km.</summary>
        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R    = 6371.0;
            var          dLat = (lat2 - lat1) * Math.PI / 180.0;
            var          dLon = (lon2 - lon1) * Math.PI / 180.0;
            var          a    = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                              + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static async Task SafeCloseAsync(
            WebSocket ws,
            WebSocketCloseStatus status      = WebSocketCloseStatus.NormalClosure,
            string            description = "Done")
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(status, description, CancellationToken.None);
            }
            catch { /* ignore */ }
        }

        private static async Task SafeCloseAsync(
            ClientWebSocket ws,
            WebSocketCloseStatus status      = WebSocketCloseStatus.NormalClosure,
            string            description = "Done")
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(status, description, CancellationToken.None);
            }
            catch { /* ignore */ }
        }

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
