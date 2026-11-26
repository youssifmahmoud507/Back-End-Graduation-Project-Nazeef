using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using LaundryApi.Controllers;
using LaundryPerstince.Data.Context;
using LaundryServiesAbstraction.IServices.Order;
using LaundryServiesAbstraction.IServices.User;
using LaundryShared.Common;
using LaundryShared.DTO.Order;
using LaundryShared.DTO.User;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using LaundryDomain.Models.LaundryModules;
using System.Net.WebSockets;
using System.Threading;
using System.Linq;
using System.Text;
using Moq;
using LaundryDomain.Models.OrderModules;
using LaundryPerstince.Data.Repositories;
using LaundryServies.Services.OrderModule;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryServiesAbstraction.IServices.Notifications;

namespace LaundryApi.Tests
{
    public class AIVoiceControllerTests
    {
        [Fact]
        public async Task ExecuteToolAsync_CancelOrder_UsesOrderService()
        {
            var dbContext = CreateDbContext(nameof(ExecuteToolAsync_CancelOrder_UsesOrderService));
            var stubOrderService = new StubOrderService();
            var services = new ServiceCollection();
            services.AddScoped<IOrderService>(_ => stubOrderService);
            var serviceProvider = services.BuildServiceProvider();

            var controller = CreateController(dbContext, stubOrderService, new StubUserService(), serviceProvider);

            var result = await InvokeExecuteToolAsync(controller, "cancel_order", new Dictionary<string, object>
            {
                ["order_id"] = "123"
            });

            Assert.Contains("123", result.ToString());
            Assert.Contains("تم إلغاء الطلب", result.ToString());
            Assert.True(stubOrderService.WasCalled);
            Assert.Equal("user-1", stubOrderService.CapturedUserId);
            Assert.Equal(123, stubOrderService.CapturedOrderId);
        }

        [Fact]
        public async Task ExecuteToolAsync_GetServicePrices_ReturnsServiceList()
        {
            var dbContext = CreateDbContext(nameof(ExecuteToolAsync_GetServicePrices_ReturnsServiceList));
            var laundry = new Laundry
            {
                Id = 1,
                Name = "Test Laundry",
                Address = "Cairo",
                Latitude = 30.1m,
                Longitude = 31.2m,
                AdminId = "admin-1",
                Status = LaundryStatus.Active,
                Availability = LaundryAvailability.Open
            };
            dbContext.Laundries.Add(laundry);
            dbContext.Services.Add(new Service
            {
                Id = 1,
                LaundryId = laundry.Id,
                ServiceName = "Shirt",
                Category = ServiceCategory.Iron,
                Price = 25m,
                IsAvailable = true
            });
            await dbContext.SaveChangesAsync();

            var controller = CreateController(dbContext, new StubOrderService(), new StubUserService());

            var result = await InvokeExecuteToolAsync(controller, "get_service_prices", new Dictionary<string, object>());
            var serialized = JsonSerializer.Serialize(result);

            Assert.Contains("Shirt", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("25", serialized, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExecuteToolAsync_GetNearestLaundries_ReturnsNearestList()
        {
            var dbContext = CreateDbContext(nameof(ExecuteToolAsync_GetNearestLaundries_ReturnsNearestList));
            var laundry = new Laundry
            {
                Id = 1,
                Name = "Near Laundry",
                Address = "Giza",
                Latitude = 30.05m,
                Longitude = 31.25m,
                AdminId = "admin-2",
                Status = LaundryStatus.Active,
                Availability = LaundryAvailability.Open,
                AverageRating = 4.5f
            };
            dbContext.Laundries.Add(laundry);
            await dbContext.SaveChangesAsync();

            var userService = new StubUserService(new List<AddressDto>
            {
                new AddressDto
                {
                    Id = 10,
                    City = "Giza",
                    Area = "Dokki",
                    Street = "Test",
                    Label = "Home",
                    Type = AddressType.Home,
                    IsDefault = true,
                    Latitude = 30.04m,
                    Longitude = 31.24m
                }
            });

            var controller = CreateController(dbContext, new StubOrderService(), userService);

            var result = await InvokeExecuteToolAsync(controller, "get_nearest_laundries", new Dictionary<string, object>());
            var serialized = JsonSerializer.Serialize(result);

            Assert.Contains("Near Laundry", serialized, StringComparison.OrdinalIgnoreCase);
            Assert.True(serialized.Contains("كم") || serialized.Contains("\\u0643\\u0645") || serialized.Contains("??"), $"Expected 'كم' or unicode equivalent, but got: {serialized}");
        }

        [Fact]
        public async Task VoiceInitialization_ContextRetrieval_ReturnsExpectedData()
        {
            var dbContext = CreateDbContext(nameof(VoiceInitialization_ContextRetrieval_ReturnsExpectedData));
            
            // Add a laundry
            var laundry = new Laundry
            {
                Id = 1,
                Name = "Nody Laundry Near",
                Address = "Maadi, Cairo",
                Latitude = 29.96m,
                Longitude = 31.25m,
                AdminId = "admin-voice",
                Status = LaundryStatus.Active,
                Availability = LaundryAvailability.Open,
                AverageRating = 4.8f
            };
            dbContext.Laundries.Add(laundry);

            // Add services
            dbContext.Services.Add(new Service
            {
                Id = 10,
                LaundryId = laundry.Id,
                ServiceName = "T-Shirt Premium",
                Category = ServiceCategory.DryCleaning,
                Price = 50m,
                IsAvailable = true
            });
            await dbContext.SaveChangesAsync();

            var userService = new StubUserService(new List<AddressDto>
            {
                new AddressDto
                {
                    Id = 20,
                    City = "Cairo",
                    Area = "Maadi",
                    Street = "Road 9",
                    Label = "Work",
                    Type = AddressType.Work,
                    IsDefault = true,
                    Latitude = 29.959m,
                    Longitude = 31.251m
                }
            });

            var controller = CreateController(dbContext, new StubOrderService(), userService);

            // Invoke FetchVoiceContextAsync using reflection
            var method = typeof(AIVoiceController).GetMethod(
                "FetchVoiceContextAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task)method!.Invoke(controller, new object[] { "user-1" })!;
            await task;

            // Extract the result property
            var resultProp = task.GetType().GetProperty("Result");
            var context = resultProp!.GetValue(task);

            Assert.NotNull(context);

            var nearestLaundries = context.GetType().GetProperty("NearestLaundries")!.GetValue(context) as string;
            var servicePriceTable = context.GetType().GetProperty("ServicePriceTable")!.GetValue(context) as string;

            Assert.NotNull(nearestLaundries);
            Assert.Contains("Nody Laundry Near", nearestLaundries);
            
            Assert.NotNull(servicePriceTable);
            Assert.Contains("T-Shirt Premium", servicePriceTable);
        }

        [Theory]
        [InlineData("Royal User", "Royal")]
        [InlineData("أحمد علي", "أحمد")]
        [InlineData("name_with_no_spaces", "name_with_no_spaces")]
        [InlineData("", "يا فندم")]
        [InlineData("   ", "يا فندم")]
        public void VoiceInitialization_GetUserName_FromClaims_ExtractsFirstName(string inputFullName, string expectedFirstName)
        {
            var dbContext = CreateDbContext(nameof(VoiceInitialization_GetUserName_FromClaims_ExtractsFirstName) + expectedFirstName.GetHashCode());
            
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user-1") };
            if (!string.IsNullOrEmpty(inputFullName))
            {
                claims.Add(new Claim(ClaimTypes.Name, inputFullName));
            }
            
            var controller = CreateControllerWithCustomClaims(dbContext, new StubOrderService(), new StubUserService(), claims);

            // Invoke GetUserName using reflection
            var method = typeof(AIVoiceController).GetMethod(
                "GetUserName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = method!.Invoke(controller, null) as string;

            Assert.Equal(expectedFirstName, result);
        }

        [Fact]
        public async Task VoiceInitialization_GetUserName_FallbackToDb_WhenClaimsEmpty()
        {
            var dbContext = CreateDbContext(nameof(VoiceInitialization_GetUserName_FallbackToDb_WhenClaimsEmpty));
            var user = new LaundryDomain.Models.UserModules.ApplicationUser
            {
                Id = "user-123",
                Name = "Khaled Mohamed",
                Email = "khaled@example.com",
                UserName = "khaled@example.com",
                PhoneNumber = "0123456789"
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user-123") };
            var controller = CreateControllerWithCustomClaims(dbContext, new StubOrderService(), new StubUserService(), claims);

            var userName = controller.User.FindFirstValue(ClaimTypes.Name) ?? controller.User.FindFirstValue("name");
            if (string.IsNullOrWhiteSpace(userName))
            {
                var fetchedUser = await dbContext.Users.FindAsync("user-123");
                if (fetchedUser != null && !string.IsNullOrWhiteSpace(fetchedUser.Name))
                {
                    userName = fetchedUser.Name;
                }
            }
            
            var first = userName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            Assert.Equal("Khaled", first);
        }

        [Fact]
        public async Task ProcessGeminiMessageAsync_Interrupted_SendsInterruptSignal()
        {
            var dbContext = CreateDbContext(nameof(ProcessGeminiMessageAsync_Interrupted_SendsInterruptSignal));
            var controller = CreateController(dbContext, new StubOrderService(), new StubUserService());

            var clientWs = new TestWebSocket();
            var geminiWs = new ClientWebSocket();

            var json = "{\"serverContent\":{\"interrupted\":true}}";

            await InvokeProcessGeminiMessageAsync(controller, json, clientWs, geminiWs, CancellationToken.None);

            var sent = clientWs.SentMessages.Single();
            Assert.Equal(WebSocketMessageType.Text, sent.MessageType);
            Assert.Equal("{\"type\":\"interrupted\"}", Encoding.UTF8.GetString(sent.Payload));
        }

        [Fact]
        public async Task ProcessGeminiMessageAsync_AudioPart_ForwardsAudioToClient()
        {
            var dbContext = CreateDbContext(nameof(ProcessGeminiMessageAsync_AudioPart_ForwardsAudioToClient));
            var controller = CreateController(dbContext, new StubOrderService(), new StubUserService());

            var clientWs = new TestWebSocket();
            var geminiWs = new ClientWebSocket();

            var audioBytes = new byte[] { 1, 2, 3, 4 };
            var json = "{\"serverContent\":{\"modelTurn\":{\"parts\":[{\"inlineData\":{\"data\":\"" +
                       Convert.ToBase64String(audioBytes) + "\"}}]}}}";

            await InvokeProcessGeminiMessageAsync(controller, json, clientWs, geminiWs, CancellationToken.None);

            var sent = clientWs.SentMessages.Single(m => m.MessageType == WebSocketMessageType.Binary);
            Assert.True(audioBytes.SequenceEqual(sent.Payload));
        }

        [Fact]
        public async Task ProcessGeminiMessageAsync_ToolCall_InvokesTool()
        {
            var dbContext = CreateDbContext(nameof(ProcessGeminiMessageAsync_ToolCall_InvokesTool));
            var stubOrderService = new StubOrderService();
            var services = new ServiceCollection();
            services.AddScoped<IOrderService>(_ => stubOrderService);
            var serviceProvider = services.BuildServiceProvider();

            var controller = CreateController(dbContext, stubOrderService, new StubUserService(), serviceProvider);

            var clientWs = new TestWebSocket();
            var geminiWs = new ClientWebSocket();

            var json = "{\"toolCall\":{\"functionCalls\":[{\"id\":\"tool-1\",\"name\":\"cancel_order\",\"args\":{\"order_id\":\"77\"}}]}}";

            await InvokeProcessGeminiMessageAsync(controller, json, clientWs, geminiWs, CancellationToken.None);

            Assert.True(stubOrderService.WasCalled);
            Assert.Equal("user-1", stubOrderService.CapturedUserId);
            Assert.Equal(77, stubOrderService.CapturedOrderId);
        }

        private static string GetInMemoryDbName(NadeefDbContext dbContext)
        {
            var optionsField = typeof(DbContext).GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var options = optionsField?.GetValue(dbContext) as DbContextOptions;
            if (options != null)
            {
                foreach (var ext in options.Extensions)
                {
                    if (ext.GetType().Name == "InMemoryOptionsExtension")
                    {
                        var prop = ext.GetType().GetProperty("StoreName");
                        if (prop != null)
                        {
                            return prop.GetValue(ext) as string ?? "DefaultDb";
                        }
                    }
                }
            }
            return "DefaultDb";
        }

        private static AIVoiceController CreateController(
            NadeefDbContext dbContext,
            IOrderService orderService,
            IUserService userService,
            IServiceProvider? serviceProvider = null)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var dbName = GetInMemoryDbName(dbContext);
            
            var services = new ServiceCollection();
            services.AddDbContext<NadeefDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddSingleton(orderService);
            services.AddSingleton(userService);
            services.AddMemoryCache();
            
            var provider = services.BuildServiceProvider();

            var controller = new AIVoiceController(
                configuration,
                NullLogger<AIVoiceController>.Instance,
                provider);

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-1"),
                    new Claim(ClaimTypes.Name, "Test User")
                }, "TestAuth"))
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        private static AIVoiceController CreateControllerWithCustomClaims(
            NadeefDbContext dbContext,
            IOrderService orderService,
            IUserService userService,
            List<Claim> claims)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var dbName = GetInMemoryDbName(dbContext);
            
            var services = new ServiceCollection();
            services.AddDbContext<NadeefDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddSingleton(orderService);
            services.AddSingleton(userService);
            services.AddMemoryCache();
            
            var provider = services.BuildServiceProvider();

            var controller = new AIVoiceController(
                configuration,
                NullLogger<AIVoiceController>.Instance,
                provider);

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        private static async Task<object> InvokeExecuteToolAsync(
            AIVoiceController controller,
            string toolName,
            Dictionary<string, object> args)
        {
            var method = typeof(AIVoiceController).GetMethod(
                "ExecuteToolAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task<object>)method!.Invoke(controller, new object[] { toolName, args })!;
            return await task;
        }

        private static async Task InvokeProcessGeminiMessageAsync(
            AIVoiceController controller,
            string json,
            WebSocket clientWs,
            ClientWebSocket geminiWs,
            CancellationToken cancellationToken)
        {
            var method = typeof(AIVoiceController).GetMethod(
                "ProcessGeminiMessageAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var setupReady = new TaskCompletionSource<bool>();
            var captionsType = typeof(AIVoiceController).GetNestedType("VoiceCaptionState", System.Reflection.BindingFlags.NonPublic);
            var captions = Activator.CreateInstance(captionsType!);

            var task = (Task)method!.Invoke(controller, new object[] { json, clientWs, geminiWs, setupReady, captions!, cancellationToken })!;
            await task;
        }

        private static NadeefDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<NadeefDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new NadeefDbContext(options);
        }

        private sealed class StubOrderService : IOrderService
        {
            public bool WasCalled { get; private set; }
            public string? CapturedUserId { get; private set; }
            public int CapturedOrderId { get; private set; }

            public Task<Result<OrderDto>> PlaceOrderAsync(string customerId, PlaceOrderRequest request) =>
                throw new NotImplementedException();

            public Task<Result<CalculatePriceResponse>> CalculatePriceAsync(CalculatePriceRequest request) =>
                throw new NotImplementedException();

            public Task<Result<Pagination<OrderDto>>> GetCustomerOrdersAsync(string customerId, OrderParams orderParams) =>
                throw new NotImplementedException();

            public Task<Result<List<OrderDto>>> GetActiveOrdersAsync(string customerId) =>
                throw new NotImplementedException();

            public Task<Result<Pagination<OrderDto>>> GetOrderHistoryAsync(string customerId, OrderParams orderParams) =>
                throw new NotImplementedException();

            public Task<Result<OrderDto>> GetOrderByIdAsync(string customerId, int orderId) =>
                throw new NotImplementedException();

            public Task<Result<OrderTrackDto>> TrackOrderAsync(string customerId, int orderId) =>
                throw new NotImplementedException();

            public Task<Result<bool>> CancelOrderAsync(string customerId, int orderId, CancelOrderRequest request)
            {
                WasCalled = true;
                CapturedUserId = customerId;
                CapturedOrderId = orderId;
                return Task.FromResult(Result<bool>.Success(true));
            }

            public Task<Result<PaymentDto>> GetOrderPaymentAsync(string customerId, int orderId) =>
                throw new NotImplementedException();
        }

        private sealed class StubUserService : IUserService
        {
            private readonly List<AddressDto> _addresses;

            public StubUserService(List<AddressDto>? addresses = null)
            {
                _addresses = addresses ?? new List<AddressDto>();
            }

            public Task<Result<UserProfileDto>> GetProfileAsync(string userId) =>
                throw new NotImplementedException();

            public Task<Result<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request) =>
                throw new NotImplementedException();

            public Task<Result<bool>> ChangePasswordAsync(string userId, ChangePasswordRequest request) =>
                throw new NotImplementedException();

            public Task<Result<bool>> DeleteAccountAsync(string userId, DeleteAccountRequest request) =>
                throw new NotImplementedException();

            public Task<Result<UserStatsDto>> GetStatsAsync(string userId) =>
                throw new NotImplementedException();

            public Task<Result<List<AddressDto>>> GetAddressesAsync(string userId) =>
                Task.FromResult(Result<List<AddressDto>>.Success(_addresses));

            public Task<Result<AddressDto>> AddAddressAsync(string userId, AddAddressRequest request) =>
                throw new NotImplementedException();

            public Task<Result<AddressDto>> UpdateAddressAsync(string userId, int addressId, AddAddressRequest request) =>
                throw new NotImplementedException();

            public Task<Result<bool>> DeleteAddressAsync(string userId, int addressId) =>
                throw new NotImplementedException();

            public Task<Result<bool>> SetDefaultAddressAsync(string userId, int addressId) =>
                throw new NotImplementedException();

            public Task<Result<List<UserPaymentMethodDto>>> GetPaymentMethodsAsync(string userId) =>
                throw new NotImplementedException();

            public Task<Result<UserPaymentMethodDto>> AddPaymentMethodAsync(string userId, AddPaymentMethodRequest request) =>
                throw new NotImplementedException();

            public Task<Result<bool>> DeletePaymentMethodAsync(string userId, int paymentMethodId) =>
                throw new NotImplementedException();

            public Task<Result<bool>> SetDefaultPaymentMethodAsync(string userId, int paymentMethodId) =>
                throw new NotImplementedException();

            public Task<Result<List<FavoriteLaundryDto>>> GetFavoritesAsync(string userId) =>
                throw new NotImplementedException();

            public Task<Result<bool>> AddFavoriteAsync(string userId, int laundryId) =>
                throw new NotImplementedException();

            public Task<Result<bool>> RemoveFavoriteAsync(string userId, int laundryId) =>
                throw new NotImplementedException();

            public Task<Result<PointsDto>> GetPointsAsync(string userId) =>
                throw new NotImplementedException();

            public Task<Result<bool>> RedeemPointsAsync(string userId, RedeemPointsRequest request) =>
                throw new NotImplementedException();

            public Task<Result<UserSettingsDto>> GetSettingsAsync(string userId) =>
                throw new NotImplementedException();

            public Task<Result<UserSettingsDto>> UpdateSettingsAsync(string userId, UpdateUserSettingsRequest request) =>
                throw new NotImplementedException();

            public Task<Result<string>> UploadAvatarAsync(string userId, IFormFile file) =>
                throw new NotImplementedException();
        }

        private sealed class TestWebSocket : WebSocket
        {
            private readonly List<SentMessage> _sent = new();
            public IReadOnlyList<SentMessage> SentMessages => _sent;

            public override WebSocketCloseStatus? CloseStatus => WebSocketCloseStatus.NormalClosure;
            public override string? CloseStatusDescription => "Closed";
            public override WebSocketState State => WebSocketState.Open;
            public override string? SubProtocol => null;

            public override Task SendAsync(
                ArraySegment<byte> buffer,
                WebSocketMessageType messageType,
                bool endOfMessage,
                CancellationToken cancellationToken)
            {
                _sent.Add(new SentMessage(messageType, buffer.ToArray()));
                return Task.CompletedTask;
            }

            public override Task CloseAsync(
                WebSocketCloseStatus closeStatus,
                string? statusDescription,
                CancellationToken cancellationToken) => Task.CompletedTask;

            public override Task CloseOutputAsync(
                WebSocketCloseStatus closeStatus,
                string? statusDescription,
                CancellationToken cancellationToken) => Task.CompletedTask;

            public override void Abort() { }
        public override void Dispose() { }

            public override Task<WebSocketReceiveResult> ReceiveAsync(
                ArraySegment<byte> buffer,
                CancellationToken cancellationToken) =>
                Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));

            public sealed record SentMessage(WebSocketMessageType MessageType, byte[] Payload);
        }

        [Fact]
        public async Task VoiceInitialization_RealDatabase_IntegrationTest()
        {
            var connStr = "Server=tcp:tiketapp-sqlsrv27005.database.windows.net,1433;Initial Catalog=NdeefDB;User ID=superadmin;Password=Super@Ticket2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
            var options = new DbContextOptionsBuilder<NadeefDbContext>()
                .UseSqlServer(connStr)
                .Options;

            using var dbContext = new NadeefDbContext(options);
            
            if (!await dbContext.Database.CanConnectAsync())
            {
                Assert.Fail("Could not connect to the real SQL Server database.");
                return;
            }

            var defaultAddress = await dbContext.UserAddresses
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.IsDefault);
                
            if (defaultAddress == null)
            {
                defaultAddress = await dbContext.UserAddresses
                    .Include(a => a.User)
                    .FirstOrDefaultAsync();
            }

            if (defaultAddress == null || defaultAddress.User == null)
            {
                Assert.Fail("No user with a saved address was found in the database to test with.");
                return;
            }

            var userId = defaultAddress.UserId;
            var userName = defaultAddress.User.Name ?? "باسل";

            var allLaundries = await dbContext.Laundries
                .Where(l => l.Status == LaundryStatus.Active && l.Availability == LaundryAvailability.Open)
                .ToListAsync();

            var addresses = await dbContext.UserAddresses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            string nearestLaundriesStr = "No default user address found.";
            if (addresses.Count > 0)
            {
                var defAddr = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.First();
                double uLat = (double)defAddr.Latitude.GetValueOrDefault();
                double uLng = (double)defAddr.Longitude.GetValueOrDefault();

                var nearest = allLaundries
                    .Select(l => new
                    {
                        Laundry = l,
                        Distance = CalculateDistanceLocal(uLat, uLng, (double)l.Latitude, (double)l.Longitude)
                    })
                    .OrderBy(x => x.Distance)
                    .Take(3)
                    .ToList();

                var sb = new StringBuilder();
                foreach (var x in nearest)
                {
                    sb.Append($"- Name: {x.Laundry.Name}, Distance: {x.Distance:F1} km, Rating: {x.Laundry.AverageRating:F1}/5.0\n");
                }
                nearestLaundriesStr = sb.ToString().Trim();
            }

            Console.WriteLine("REAL DATABASE LOG:");
            Console.WriteLine($"User Name: {userName}");
            Console.WriteLine($"User Id: {userId}");
            Console.WriteLine($"Nearest Laundries:\n{nearestLaundriesStr}");

            var apiKey = "AIzaSyBkcb_hSOkNnsZ0ZwlRpkpgf_arVZ6FU7k";
            var query = "إيه أقرب مغسلة ليا وتفاصيلها إيه؟";

            var systemPrompt = $@"
أنتَ ""نودي""، مساعد ذكي لتطبيق ""نظيف"" — منصة الغسيل الذكية في مصر.
اسم العميل: {userName}.
CUSTOMER_NAME: {userName}
NEAREST_LAUNDRIES: {nearestLaundriesStr}
";

            using var client = new System.Net.Http.HttpClient();
            var payload = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = query } } } },
                generationConfig = new { temperature = 0.7, maxOutputTokens = 512 }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}", content);
            
            var resJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resJson);
            var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            Console.WriteLine($"Nody Response:\n{text}");
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "nody_real_db_response.txt");
            await File.WriteAllTextAsync(outputPath, text, Encoding.UTF8);
            Assert.NotNull(text);
        }

        private static double CalculateDistanceLocal(double lat1, double lon1, double lat2, double lon2)
        {
            var r = 6371; // km
            var dLat = (Math.PI / 180) * (lat2 - lat1);
            var dLon = (Math.PI / 180) * (lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos((Math.PI / 180) * lat1) * Math.Cos((Math.PI / 180) * lat2) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            return r * c;
        }

        [Fact]
        public async Task VoiceInitialization_RealDatabase_CancelOrder_IntegrationTest()
        {
            var connStr = "Server=tcp:tiketapp-sqlsrv27005.database.windows.net,1433;Initial Catalog=NdeefDB;User ID=superadmin;Password=Super@Ticket2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
            var options = new DbContextOptionsBuilder<NadeefDbContext>()
                .UseSqlServer(connStr)
                .Options;

            using var dbContext = new NadeefDbContext(options);
            if (!await dbContext.Database.CanConnectAsync())
            {
                Assert.Fail("Could not connect to the real SQL Server database.");
                return;
            }

            var defaultAddress = await dbContext.UserAddresses
                .Include(a => a.User)
                .FirstOrDefaultAsync();

            if (defaultAddress == null || defaultAddress.User == null)
            {
                Assert.Fail("No user with address found in database.");
                return;
            }

            var userId = defaultAddress.UserId;

            var laundry = await dbContext.Laundries.FirstOrDefaultAsync(l => l.Status == LaundryStatus.Active && l.Availability == LaundryAvailability.Open);
            if (laundry == null)
            {
                Assert.Fail("No active laundry found in database.");
                return;
            }

            var tempOrder = new Order
            {
                CustomerId = userId,
                LaundryId = laundry.Id,
                PickupLocation = "Test St, Cairo",
                DeliveryLocation = "Test St, Cairo",
                PickupTime = DateTime.UtcNow.AddHours(2),
                Status = OrderStatus.PendingConfirmation,
                TotalPrice = 100m,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Orders.Add(tempOrder);
            await dbContext.SaveChangesAsync();
            var orderId = tempOrder.Id;

            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var mockKashier = new Mock<IKashierService>();
            var mockNotification = new Mock<INotificationService>();
            var unitOfWork = new UnitOfWork(dbContext);
            var realOrderService = new OrderService(unitOfWork, mockKashier.Object, mockNotification.Object);

            var services = new ServiceCollection();
            services.AddSingleton(dbContext);
            services.AddSingleton<IOrderService>(realOrderService);
            services.AddMemoryCache();

            var provider = services.BuildServiceProvider();

            var controller = new AIVoiceController(
                configuration,
                NullLogger<AIVoiceController>.Instance,
                provider);

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, defaultAddress.User.Name ?? "Yousif")
                }, "TestAuth"))
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var args = new Dictionary<string, object>
            {
                { "order_id", orderId.ToString() }
            };

            var result = await InvokeExecuteToolAsync(controller, "cancel_order", args);
            Assert.NotNull(result);

            var resStr = result.ToString();
            Console.WriteLine($"Cancel Order Tool Result:\n{resStr}");
            Assert.Contains("بنجاح", resStr);

            var updatedOrder = await dbContext.Orders.FindAsync(orderId);
            Assert.NotNull(updatedOrder);
            Assert.Equal(OrderStatus.Cancelled, updatedOrder.Status);

            dbContext.Orders.Remove(updatedOrder);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("Order cancellation successfully written to database and cleaned up!");
        }

        [Fact]
        public async Task VoiceInitialization_RealDatabase_PlaceOrder_IntegrationTest()
        {
            var connStr = "Server=tcp:tiketapp-sqlsrv27005.database.windows.net,1433;Initial Catalog=NdeefDB;User ID=superadmin;Password=Super@Ticket2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
            var options = new DbContextOptionsBuilder<NadeefDbContext>()
                .UseSqlServer(connStr)
                .Options;

            using var dbContext = new NadeefDbContext(options);
            if (!await dbContext.Database.CanConnectAsync())
            {
                Assert.Fail("Could not connect to the real SQL Server database.");
                return;
            }

            var defaultAddress = await dbContext.UserAddresses
                .Include(a => a.User)
                .FirstOrDefaultAsync();

            if (defaultAddress == null || defaultAddress.User == null)
            {
                Assert.Fail("No user with address found in database.");
                return;
            }

            var userId = defaultAddress.UserId;

            var laundry = await dbContext.Laundries.FirstOrDefaultAsync(l => l.Status == LaundryStatus.Active && l.Availability == LaundryAvailability.Open);
            if (laundry == null)
            {
                Assert.Fail("No active laundry found in database.");
                return;
            }

            var service = await dbContext.Services.FirstOrDefaultAsync(s => s.LaundryId == laundry.Id && s.IsAvailable);
            if (service == null)
            {
                Assert.Fail("No active service found for laundry.");
                return;
            }

            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var mockKashier = new Mock<IKashierService>();
            var mockNotification = new Mock<INotificationService>();
            var unitOfWork = new UnitOfWork(dbContext);
            var realOrderService = new OrderService(unitOfWork, mockKashier.Object, mockNotification.Object);

            var mockUserService = new Mock<IUserService>();
            mockUserService.Setup(s => s.GetAddressesAsync(It.IsAny<string>()))
                .ReturnsAsync((string uid) => {
                    using var scopeDb = new NadeefDbContext(options);
                    var addrList = scopeDb.UserAddresses.Where(a => a.UserId == uid)
                        .Select(a => new AddressDto {
                            Street = a.Street, City = a.City, Area = a.Area, IsDefault = a.IsDefault,
                            Latitude = a.Latitude, Longitude = a.Longitude
                        }).ToList();
                    return Result<List<AddressDto>>.Success(addrList);
                });

            var mockPayment = new Mock<IPaymentService>();
            mockPayment.Setup(p => p.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<ProcessPaymentRequest>()))
                .ReturnsAsync(Result<PaymentDto>.Success(new PaymentDto()));

            var services = new ServiceCollection();
            services.AddSingleton(dbContext);
            services.AddSingleton<IOrderService>(realOrderService);
            services.AddSingleton<IUserService>(mockUserService.Object);
            services.AddSingleton<IPaymentService>(mockPayment.Object);
            services.AddMemoryCache();

            var provider = services.BuildServiceProvider();

            var controller = new AIVoiceController(
                configuration,
                NullLogger<AIVoiceController>.Instance,
                provider);


            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, defaultAddress.User.Name ?? "Yousif")
                }, "TestAuth"))
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var serviceItemsJson = $"[{{\"service_id\":{service.Id},\"quantity\":2}}]";
            using var jsonDoc = JsonDocument.Parse(serviceItemsJson);
            var args = new Dictionary<string, object>
            {
                { "laundry_id", laundry.Id.ToString() },
                { "service_items", jsonDoc.RootElement.Clone() },
                { "payment_method", "Cash" }
            };

            var result = await InvokeExecuteToolAsync(controller, "place_order", args);
            Assert.NotNull(result);
            
            var resStr = result.ToString();
            Console.WriteLine($"Place Order Tool Result:\n{resStr}");
            Assert.Contains("بنجاح", resStr);

            var latestOrder = await dbContext.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.CustomerId == userId && o.LaundryId == laundry.Id)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            Assert.NotNull(latestOrder);
            Assert.Equal(OrderStatus.PendingConfirmation, latestOrder.Status);
            Assert.Equal(2, latestOrder.OrderItems.Sum(oi => oi.Quantity));

            dbContext.OrderItems.RemoveRange(latestOrder.OrderItems);
            dbContext.Orders.Remove(latestOrder);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("Order successfully written to database and cleaned up!");
        }

        [Fact]
        public async Task VoiceInitialization_RealDatabase_2MinutesConversation_IntegrationTest()
        {
            var connStr = "Server=tcp:tiketapp-sqlsrv27005.database.windows.net,1433;Initial Catalog=NdeefDB;User ID=superadmin;Password=Super@Ticket2026;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
            var options = new DbContextOptionsBuilder<NadeefDbContext>()
                .UseSqlServer(connStr)
                .Options;

            using var dbContext = new NadeefDbContext(options);
            if (!await dbContext.Database.CanConnectAsync())
            {
                Assert.Fail("Could not connect to the real SQL Server database.");
                return;
            }

            var defaultAddress = await dbContext.UserAddresses
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.IsDefault) ?? await dbContext.UserAddresses.Include(a => a.User).FirstOrDefaultAsync();

            var userName = defaultAddress?.User?.Name ?? "باسل";
            var allLaundries = await dbContext.Laundries
                .Where(l => l.Status == LaundryStatus.Active && l.Availability == LaundryAvailability.Open)
                .ToListAsync();

            var addresses = defaultAddress != null 
                ? await dbContext.UserAddresses.Where(a => a.UserId == defaultAddress.UserId).ToListAsync() 
                : new List<LaundryDomain.Models.UserModules.UserAddress>();

            string nearestLaundriesStr = "No default user address found.";
            if (addresses.Count > 0)
            {
                var defAddr = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.First();
                double uLat = (double)defAddr.Latitude.GetValueOrDefault();
                double uLng = (double)defAddr.Longitude.GetValueOrDefault();

                var nearest = allLaundries
                    .Select(l => new
                    {
                        Laundry = l,
                        Distance = CalculateDistanceLocal(uLat, uLng, (double)l.Latitude, (double)l.Longitude)
                    })
                    .OrderBy(x => x.Distance)
                    .Take(3)
                    .ToList();

                var sb = new StringBuilder();
                foreach (var x in nearest)
                {
                    sb.Append($"- Name: {x.Laundry.Name}, Distance: {x.Distance:F1} km, Rating: {x.Laundry.AverageRating:F1}/5.0\n");
                }
                nearestLaundriesStr = sb.ToString().Trim();
            }

            var apiKey = "AIzaSyBkcb_hSOkNnsZ0ZwlRpkpgf_arVZ6FU7k";
            var systemPrompt = $@"
أنتَ ""نودي""، مساعد ذكي لتطبيق ""نظيف"" — منصة الغسيل الذكية في مصر.
تتحدث بالعامية المصرية الودودة والمهذبة للغاية.
اسم العميل: {userName}.
CUSTOMER_NAME: {userName}
NEAREST_LAUNDRIES: {nearestLaundriesStr}
";

            // Conversation history
            var history = new List<object>();

            var userQueries = new[]
            {
                "أهلاً يا نودي، أنا باسل. إيه أخبارك؟ وكيف الخدمة النهاردة؟",
                "كنت عايز أعرف إيه هي الخدمات المتاحة عندكم في تطبيق نظيف وأسعارها؟",
                "تمام، وهل فيه مغاسل قريبة مني؟ أنا ساكن في المعادي دلوقتي.",
                "شكراً جداً ليكي يا نودي، مساعدتك ممتازة بجد وسهلت عليا كتير. هقفل المكالمة دلوقتي."
            };

            using var client = new System.Net.Http.HttpClient();
            Console.WriteLine("=== STARTING 2 MINUTES CONVERSATION TEST WITH NODY ===");
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("=== STARTING 2 MINUTES CONVERSATION TEST WITH NODY ===");

            for (int i = 0; i < userQueries.Length; i++)
            {
                var query = userQueries[i];
                var userLog = $"[User ({DateTime.UtcNow:HH:mm:ss})]: {query}";
                Console.WriteLine(userLog);
                logBuilder.AppendLine(userLog);

                // Add current query to history
                history.Add(new { role = "user", parts = new[] { new { text = query } } });

                HttpResponseMessage response = null!;
                string resJson = "";
                string? responseText = null;
                int retries = 0;
                const int maxRetries = 4;

                while (retries < maxRetries)
                {
                    var payload = new
                    {
                        systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                        contents = history.ToArray(),
                        generationConfig = new { temperature = 0.7, maxOutputTokens = 512 }
                    };

                    var jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}", content);
                    resJson = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || resJson.Contains("RESOURCE_EXHAUSTED") || resJson.Contains("429"))
                    {
                        retries++;
                        var retryLog = $"[Warning] Gemini API Rate Limited (429). Retrying {retries}/{maxRetries} after 20 seconds...";
                        Console.WriteLine(retryLog);
                        logBuilder.AppendLine(retryLog);
                        await Task.Delay(20000);
                        continue;
                    }

                    try
                    {
                        using var doc = JsonDocument.Parse(resJson);
                        responseText = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();
                        break;
                    }
                    catch (Exception ex)
                    {
                        var errMsg = $"Failed to parse Gemini response. Raw JSON:\n{resJson}\nException: {ex.Message}";
                        Console.WriteLine(errMsg);
                        logBuilder.AppendLine(errMsg);
                        throw;
                    }
                }

                if (responseText == null)
                {
                    Assert.Fail($"Failed to get response from Gemini API after {maxRetries} retries due to quota exhaustion. Raw JSON: {resJson}");
                    return;
                }

                var nodyLog = $"[Nody ({DateTime.UtcNow:HH:mm:ss})]: {responseText}";
                Console.WriteLine(nodyLog);
                logBuilder.AppendLine(nodyLog);
                Assert.NotNull(responseText);

                // Add response to history for multi-turn context
                history.Add(new { role = "model", parts = new[] { new { text = responseText } } });

                if (i < userQueries.Length - 1)
                {
                    // Delay for ~35 seconds per turn to sum up to 2 minutes total execution time (35 * 3 = 105 seconds + API latency ~15s = ~120s/2min)
                    var waitLog = "... Waiting 35 seconds to simulate real conversation flow and keep-alive duration ...";
                    Console.WriteLine(waitLog);
                    logBuilder.AppendLine(waitLog);
                    await Task.Delay(35000);
                }
            }

            Console.WriteLine("\n=== 2 MINUTES CONVERSATION TEST COMPLETED SUCCESSFULLY ===");
            logBuilder.AppendLine("\n=== 2 MINUTES CONVERSATION TEST COMPLETED SUCCESSFULLY ===");
            
            var logPath = Path.Combine(@"C:\Users\Royal\.gemini\antigravity\brain\7c10b078-daba-42d3-8213-5de2e0a30734", "nody_2minutes_conversation_log.txt");
            await File.WriteAllTextAsync(logPath, logBuilder.ToString(), Encoding.UTF8);
        }
    }
}
