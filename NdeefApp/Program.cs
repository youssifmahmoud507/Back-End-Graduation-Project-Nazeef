using Hangfire;
using LaundryApi.Extensions.Auth;
using LaundryDomain.IRepositories;
using LaundryDomain.Models.LaundryModules;
using LaundryPerstince.Data.Context;
using Microsoft.EntityFrameworkCore;
using LaundryPerstince.Data.DataSeeding;
//using LaundryPerstince.Data.DataSeeding.LaundryPerstince.Data.DataSeeding;
using LaundryPerstince.Data.Repositories;
using LaundryServies.Common;
using LaundryServies.Hubs;
using LaundryServies.Services.AI;
using LaundryServies.Services.Analytics;
using LaundryServies.Services.Chat;
using LaundryServies.Services.Fraud;
using LaundryServies.Services.Hangfire_Scheduler;
using LaundryServies.Services.LaundryAdminService;
using LaundryServies.Services.LaundryServicesModule;
using LaundryServies.Services.Matching;
using LaundryServies.Services.NotificationModule;
using LaundryServies.Services.OrderModule;
using LaundryServies.Services.Payment;
using LaundryServies.Services.PaymentModule;
using LaundryServies.Services.ServicesModule;
using LaundryServies.Services.SuperAdmin;
using LaundryServies.Services.UserModule;
using LaundryServies.Services.Verification;
using LaundryServiesAbstraction.IServices.AI;
using LaundryServiesAbstraction.IServices.Analytics;
using LaundryServiesAbstraction.IServices.Chat;
using LaundryServiesAbstraction.IServices.Fraud;
using LaundryServiesAbstraction.IServices.Laundry;
using LaundryServiesAbstraction.IServices.LaundryAdmin;
using LaundryServiesAbstraction.IServices.Matching;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Order;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryServiesAbstraction.IServices.Services;
using LaundryServiesAbstraction.IServices.SuperAdmin;
using LaundryServiesAbstraction.IServices.User;
using LaundryServiesAbstraction.IServices.Verification;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;

namespace NdeefApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddMemoryCache();
            builder.Services.AddSwaggerGen(options =>
            {
                options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });

            // CORS: الأصول المسموحة بتيجي من Cors:AllowedOrigins (مفصولة بفاصلة).
            // في بيئة Development لو مفيش قيمة بنسمح بـ localhost بس، وفي غيرها بنسمح بدومين الفرونت.
            var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (allowedOrigins.Length == 0)
            {
                allowedOrigins = builder.Environment.IsDevelopment()
                    ? new[] { "http://localhost:3000", "https://localhost:3000", "http://localhost:3001" }
                    : new[] { "https://www.nazeef.tech", "https://nazeef.tech" };
            }

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllFrontends", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // لما الـ API يكون ورا reverse proxy (Azure / Hugging Face) لازم نقرا الـ IP الحقيقي للعميل
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.AddApplicationServices(builder.Configuration);

            builder.Services.AddScoped<IFileService, CloudinaryService>();
            builder.Services.AddScoped<ILaundryAdminService, LaundryAdminService>();

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ILaundryService, LaundryService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IServiceModule, ServiceModule>();
            builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();

            // Configure HttpClient for KashierService (if needed later)
            // builder.Services.AddHttpClient<IKashierService, KashierService>();

            // Register Payout and Payment services
            builder.Services.AddScoped<PayoutScheduler>();
            builder.Services.AddScoped<IKashierService, KashierService>();
            builder.Services.AddHttpClient<ISentimentService, SentimentService>();


            // builder.Services.AddHangfire(config =>
            //     config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"))
            // );
            // builder.Services.AddHangfireServer();

            builder.Services.AddSignalR();

            // Rate limiting: حد عام لكل IP + حد أشد لـ endpoints الـ Auth (login / register / OTP / reset)
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 600,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                options.AddPolicy("auth", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            builder.Services.AddHttpClient<IPriceRecommendationService, PriceRecommendationService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });


            builder.Services.AddHttpClient<IDemandForecastService, DemandForecastService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60); // DeepSeek R1 بياخد وقت أكتر
            });





            // grok chat 
            builder.Services.AddScoped<IChatService, GroqChatService>();

            // google
            builder.Services.AddScoped<IMatchingService, MatchingService>();

            // user service
            builder.Services.AddScoped<IUserService, UserService>();

            // fraud service
            builder.Services.AddScoped<IFraudService, FraudService>();

            // Didit verification service
            builder.Services.AddHttpClient<IDiditVerificationService, DiditVerificationService>();


            // bearer
            //builder.Services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nadeef API", Version = "v1" });

            //    // Add Bearer token support to Swagger UI
            //    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            //    {
            //        Name = "Authorization",
            //        Type = SecuritySchemeType.ApiKey,
            //        Scheme = "Bearer",
            //        BearerFormat = "JWT",
            //        In = ParameterLocation.Header,
            //        Description = "Enter: Bearer {your token here}\n\nExample: Bearer eyJhbGci..."
            //    });

            //    c.AddSecurityRequirement(new OpenApiSecurityRequirement
            //        {
            //            {
            //                new OpenApiSecurityScheme
            //                {
            //                    Reference = new OpenApiReference
            //                    {
            //                        Type = ReferenceType.SecurityScheme,
            //                        Id = "Bearer"
            //                    }
            //                },
            //                Array.Empty<string>()
            //            }
            //        });
            //                });














            var app = builder.Build();

            app.UseForwardedHeaders();

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                    logger.LogError(exception, "Unhandled exception: {Message}", exception?.Message);

                    // تفاصيل الـ exception بتظهر بس في Development، في غيرها بنرجع رسالة عامة
                    var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    if (env.IsDevelopment())
                    {
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = exception?.Message,
                            detail = exception?.InnerException?.Message,
                            stackTrace = exception?.StackTrace
                        }));
                    }
                    else
                    {
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "An unexpected error occurred. Please try again later."
                        }));
                    }
                });
            });

            // Swagger بيشتغل بس في Development أو لو Swagger:Enabled=true
            if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ndeef API v1");
                });
            }

            // Seed database
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();
                await context.Database.MigrateAsync();
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<LaundryDomain.Models.UserModules.ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

                // الـ Seed (حسابات افتراضية + بيانات ديمو) مقفول افتراضياً ويتفعّل من الـ configuration (Seed:*)
                var seedOptions = new SeedOptions
                {
                    Enabled = app.Configuration.GetValue<bool>("Seed:Enabled"),
                    DefaultPassword = app.Configuration["Seed:DefaultPassword"]
                };
                app.Configuration.GetSection("Seed:SuperAdmins").Bind(seedOptions.SuperAdmins);

                var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                await DataInitializer.SeedAsync(context, userManager, roleManager, seedLogger, seedOptions);
            }






            app.UseHttpsRedirection();

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                await next();
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors("AllowAllFrontends");

            app.UseRateLimiter();

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<NotificationHub>("/notifications-hub");
            app.MapControllers();
            app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

            await app.RunAsync();
        }
    }
}
