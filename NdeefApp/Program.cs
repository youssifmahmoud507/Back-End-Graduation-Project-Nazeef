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
using Microsoft.OpenApi.Models;
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

            // Allow any frontend domain to call the API
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllFrontends", policy =>
                {
                    policy
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
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

            // Rate limiter disabled — frontend infinite-loop root cause is fixed.
            // Re-enable and tune before going to production.
            // builder.Services.AddRateLimiter(...);

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

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                    logger.LogError(exception, "Unhandled exception: {Message}", exception?.Message);

                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        message = exception?.Message,
                        detail = exception?.InnerException?.Message,
                        stackTrace = exception?.StackTrace
                    }));
                });
            });

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ndeef API v1");
            });

            // Seed database
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NadeefDbContext>();
                await context.Database.MigrateAsync();
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<LaundryDomain.Models.UserModules.ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

                await DataInitializer.SeedAsync(context, userManager, roleManager);
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

            // app.UseRateLimiter(); // disabled — see AddRateLimiter comment above

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseCors("AllowAllFrontends");

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<NotificationHub>("/notifications-hub");
            app.MapControllers();

            await app.RunAsync();
        }
    }
}
