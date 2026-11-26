using LaundryDomain.IRepositories;
using LaundryDomain.Models.UserModules;
using LaundryPerstince.Data.Context;
using LaundryPerstince.Data.Repositories;
using LaundryServies.Common;
using LaundryServies.Services.Analytics;
using LaundryServies.Services.Auth;
using LaundryServies.Services.BundleModule;
using LaundryServies.Services.Courier;
using LaundryServies.Services.LaundryAdminService;
using LaundryServies.Services.LaundryServicesModule;
using LaundryServies.Services.NotificationModule;
using LaundryServies.Services.OrderModule;
using LaundryServies.Services.PaymentModule;
using LaundryServies.Services.ReviewModule;
using LaundryServies.Services.ServicesModule;
using LaundryServies.Services.SuperAdmin;
using LaundryServies.Services.UserModule;
using LaundryServiesAbstraction.IServices.Analytics;
using LaundryServiesAbstraction.IServices.Auth;
using LaundryServiesAbstraction.IServices.BundleModule;
using LaundryServiesAbstraction.IServices.Courier;
using LaundryServiesAbstraction.IServices.Laundry;
using LaundryServiesAbstraction.IServices.LaundryAdmin;
using LaundryServiesAbstraction.IServices.Notifications;
using LaundryServiesAbstraction.IServices.Order;
using LaundryServiesAbstraction.IServices.Payment;
using LaundryServiesAbstraction.IServices.Reviews;
using LaundryServiesAbstraction.IServices.Services;
using LaundryServiesAbstraction.IServices.SuperAdmin;
using LaundryServiesAbstraction.IServices.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LaundryApi.Extensions.Auth
{
    public static class ServiceExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // 1. Database Context Configuration
            services.AddDbContext<NadeefDbContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString("DefaultConnection"),
                    b =>
                    {
                        b.MigrationsAssembly("LaundryPerstince");
                        b.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            // 2. Identity Configuration
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<NadeefDbContext>()
            .AddDefaultTokenProviders();

            // 3. JWT Authentication Configuration
            var jwtSettings = config.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/notifications-hub") || path.StartsWithSegments("/api/voice/socket")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .AddGoogle(options =>
            {
                // بنجيب الـ Credentials من الـ appsettings
                options.ClientId = config["Authentication:Google:ClientId"];
                options.ClientSecret = config["Authentication:Google:ClientSecret"];

                // الـ Endpoint اللي Google هترجع عليه بعد الـ Login
                options.CallbackPath = "/signin-google";
            });

            // 4. Dependency Injection (Services Registration)
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            // Register File Service
            services.AddScoped<IFileService, CloudinaryService>();

            // ILaundryService
            services.AddScoped<ILaundryAdminService, LaundryAdminService>();

            // في ServiceExtensions.cs
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ILaundryService, LaundryService>();  

            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IServiceModule, ServiceModule>();
            services.AddScoped<ISuperAdminService, SuperAdminService>();
            services.AddScoped<ICourierService, CourierService>();




            services.AddScoped<IBundleService, BundleService>();
            services.AddScoped<IBundleOrderService, BundleOrderService>();
            services.AddHostedService<BundleExpiryBackgroundService>();


        }
    }
}
