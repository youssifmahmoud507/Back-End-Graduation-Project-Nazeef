using LaundryDomain.Models.BundleModules;
using LaundryDomain.Models.LaundryModules;
using LaundryDomain.Models.OrderModules;
using LaundryDomain.Models.UserModules;
using LaundryPerstince.Data.Context;
using LaundryShared.Enums;
using LaundryShared.Enums.CustomerEnums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaundryPerstince.Data.DataSeeding
{
    /// <summary> 
    /// Performs initial seed for the database.
    /// Safe-guards automatic migration when the DB login lacks CREATE DATABASE permission.
    /// </summary>
    public class DataInitializer
    {
        public static async Task SeedAsync(NadeefDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger? logger = null)
        {
            // If we cannot connect to the target database, skip migration & seeding.
            // This avoids MigrateAsync trying to CREATE DATABASE when the login lacks permission.
            if (!await context.Database.CanConnectAsync())
            {
                logger?.LogWarning("Database is not reachable. Skipping migrations and seeding. " +
                    "Create the database or run migrations from a privileged environment.");
                return;
            }

            // Try to apply migrations (safe now because the DB is reachable).
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                // Log and rethrow only if you want the app to fail. Here we log and continue.
                logger?.LogError(ex, "Error while applying migrations. Continuing to seeding if possible.");
            }

            // Proceed with seeding only if we can still connect to the database.
            if (!await context.Database.CanConnectAsync())
            {
                logger?.LogError("Cannot connect to the database after migration attempt. Aborting seeding.");
                return;
            }

            // بنعمل Seed لكل الـ Roles و الـ Users
            await SeedRolesAndUsersAsync(userManager, roleManager, logger);

            // بنعمل Seed للمغاسل لو مفيش
            await SeedLaundriesAsync(context);

            // بنعمل Seed للخدمات لو مفيش
            await SeedServicesAsync(context);

            // بنعمل Seed للـ Bundles لو مفيش
            await SeedBundlesAsync(context, userManager, logger);

            // بنعمل Seed للأوردرات لو مفيش
            await SeedOrdersAsync(context, userManager, roleManager, logger);
        }

        // ============================================================
        // Seed الأدوار والمستخدمين (Roles & Default Users)
        // ============================================================
        private static async Task SeedRolesAndUsersAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger? logger = null)
        {
            string[] roleNames = { "Customer", "Courier", "LaundryAdmin", "SuperAdmin" };
            const string defaultSeedPassword = "Basel@2004";

            // إنشاء الأدوار لو مش موجودة
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // إنشاء مستخدم افتراضي لكل دور
            foreach (var roleName in roleNames)
            {
                var userEmail = $"{roleName.ToLower()}@ndeef.com";
                var user = await userManager.FindByEmailAsync(userEmail);

                if (user == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = roleName,
                        Email = userEmail,
                        Name = $"{roleName} User",
                        FirstName = roleName,
                        LastName = "Default",
                        Role = Enum.Parse<UserRole>(roleName),
                        EmailConfirmed = true,
                        IsEmailVerified = true,
                        IsActive = true,
                        IsIdentityVerified = roleName == "LaundryAdmin",
                        AdminApprovalStatus = roleName == "LaundryAdmin"
                            ? LaundryAdminApprovalStatus.Approved
                            : null,
                        AdminRejectionReason = null
                    };

                    // إنشاء المستخدم بباسورد افتراضي
                    var result = await userManager.CreateAsync(newUser, defaultSeedPassword);
                    if (result.Succeeded)
                    {
                        // ربطه بالرول بتاعه
                        await userManager.AddToRoleAsync(newUser, roleName);
                    }
                    else
                    {
                        logger?.LogError($"فشل في إنشاء مستخدم {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    // لو المستخدم موجود بالفعل نحدّث حالته للتأكد من تجاوز التحقق
                    var updated = false;
                    if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        updated = true;
                    }
                    if (!user.IsEmailVerified)
                    {
                        user.IsEmailVerified = true;
                        updated = true;
                    }
                    if (!user.IsActive)
                    {
                        user.IsActive = true;
                        updated = true;
                    }

                    if (roleName == "LaundryAdmin")
                    {
                        if (!user.IsIdentityVerified)
                        {
                            user.IsIdentityVerified = true;
                            updated = true;
                        }

                        if (user.AdminApprovalStatus != LaundryAdminApprovalStatus.Approved)
                        {
                            user.AdminApprovalStatus = LaundryAdminApprovalStatus.Approved;
                            updated = true;
                        }

                        if (!string.IsNullOrWhiteSpace(user.AdminRejectionReason))
                        {
                            user.AdminRejectionReason = null;
                            updated = true;
                        }
                    }

                    if (updated)
                    {
                        await userManager.UpdateAsync(user);
                    }

                    // تأكد إن الباسورد الافتراضي مضبوط للحسابات الافتراضية حتى لو كانت موجودة قبل كده
                    var hasPassword = await userManager.HasPasswordAsync(user);
                    if (!hasPassword)
                    {
                        var addPasswordResult = await userManager.AddPasswordAsync(user, defaultSeedPassword);
                        if (!addPasswordResult.Succeeded)
                        {
                            logger?.LogError("Failed to set default password for {RoleName}: {Errors}",
                                roleName,
                                string.Join(", ", addPasswordResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        var passwordMatches = await userManager.CheckPasswordAsync(user, defaultSeedPassword);
                        if (!passwordMatches)
                        {
                            var removePasswordResult = await userManager.RemovePasswordAsync(user);
                            if (!removePasswordResult.Succeeded)
                            {
                                logger?.LogError("Failed to reset password for {RoleName}: {Errors}",
                                    roleName,
                                    string.Join(", ", removePasswordResult.Errors.Select(e => e.Description)));
                            }
                            else
                            {
                                var addPasswordResult = await userManager.AddPasswordAsync(user, defaultSeedPassword);
                                if (!addPasswordResult.Succeeded)
                                {
                                    logger?.LogError("Failed to set default password for {RoleName} after reset: {Errors}",
                                        roleName,
                                        string.Join(", ", addPasswordResult.Errors.Select(e => e.Description)));
                                }
                            }
                        }
                    }

                    // تأكد أن لديه الدور المطلوب
                    var inRole = await userManager.IsInRoleAsync(user, roleName);
                    if (!inRole)
                    {
                        await userManager.AddToRoleAsync(user, roleName);
                    }
                }
            }

            // ============================================================
            // Seed Custom Super Admins
            // ============================================================
            var customSuperAdmins = new[]
            {
                new { Email = "basel@nazeef.tech", Name = "Basel Ahmed", FirstName = "basel" },
                new { Email = "fero@nazeef.tech", Name = "Fero", FirstName = "fero" },
                new { Email = "yousif@nazeef.tech", Name = "Yousif", FirstName = "yousif" },
                new { Email = "menna@nazeef.tech", Name = "Menna", FirstName = "menna" },
                new { Email = "yasmin@nazeef.tech", Name = "Yasmin", FirstName = "yasmin" },
                new { Email = "shahd@nazeef.tech", Name = "Shahd", FirstName = "shahd" },
                new { Email = "DrM.Bahlol_IT@nazeef.tech", Name = "DrM Bahlol", FirstName = "DrM.Bahlol_IT" },
                new { Email = "DrFarmawy_BU@nazeef.tech", Name = "DrFarmawy BU", FirstName = "DrFarmawy_BU" }
            };

            foreach (var sa in customSuperAdmins)
            {
                var user = await userManager.FindByEmailAsync(sa.Email);
                string pwd = $"{sa.FirstName}@Admin";

                if (user == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = sa.Email,
                        Email = sa.Email,
                        Name = sa.Name,
                        FirstName = sa.FirstName,
                        LastName = "Admin",
                        Role = UserRole.SuperAdmin,
                        EmailConfirmed = true,
                        IsEmailVerified = true,
                        IsActive = true
                    };

                    var result = await userManager.CreateAsync(newUser, pwd);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newUser, "SuperAdmin");
                    }
                    else
                    {
                        logger?.LogError($"فشل في إنشاء مدير {sa.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    // Update existing role to SuperAdmin if needed
                    if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
                    {
                        await userManager.AddToRoleAsync(user, "SuperAdmin");
                    }
                    if (user.Role != UserRole.SuperAdmin)
                    {
                        user.Role = UserRole.SuperAdmin;
                        await userManager.UpdateAsync(user);
                    }

                    // Reset/set password to ensure it matches the specified one
                    var hasPassword = await userManager.HasPasswordAsync(user);
                    if (!hasPassword)
                    {
                        await userManager.AddPasswordAsync(user, pwd);
                    }
                    else
                    {
                        var passwordMatches = await userManager.CheckPasswordAsync(user, pwd);
                        if (!passwordMatches)
                        {
                            await userManager.RemovePasswordAsync(user);
                            await userManager.AddPasswordAsync(user, pwd);
                        }
                    }
                }
            }
        }

        // ============================================================
        // Seed الأوردرات
        // ============================================================
        private static async Task SeedOrdersAsync(
            NadeefDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger? logger = null)
        {
            if (await context.Orders.AnyAsync()) return;

            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "laundryadmin@ndeef.com");
            if (adminUser == null)
            {
                logger?.LogWarning("Default laundry admin not found. Skipping orders seeding.");
                return;
            }

            var laundry = await context.Laundries.FirstOrDefaultAsync(l => l.AdminId == adminUser.Id);
            if (laundry == null)
            {
                logger?.LogWarning("No laundry found for default admin. Skipping orders seeding.");
                return;
            }

            var services = await context.Services
                .Where(s => s.LaundryId == laundry.Id)
                .OrderBy(s => s.Id)
                .Take(3)
                .ToListAsync();

            if (services.Count == 0)
            {
                logger?.LogWarning("No services found for default laundry. Skipping orders seeding.");
                return;
            }

            var customerSeeds = new[]
            {
                    new { Email = "ahmed.sayed@ndeef.com", Name = "أحمد السيد", Phone = "01012345678" },
                    new { Email = "mona.ali@ndeef.com", Name = "منى علي", Phone = "01123456789" },
                    new { Email = "khaled.hassan@ndeef.com", Name = "خالد حسن", Phone = "01234567890" },
                    new { Email = "basel.dev@ndeef.com", Name = "باسل أحمد", Phone = "01055551111" },
                    new { Email = "menna.test@ndeef.com", Name = "منه صالح", Phone = "01055552222" }
                };

            var customers = new List<ApplicationUser>();
            foreach (var seed in customerSeeds)
            {
                var existing = await userManager.FindByEmailAsync(seed.Email);
                if (existing == null)
                {
                    var customer = new ApplicationUser
                    {
                        UserName = seed.Email,
                        Email = seed.Email,
                        Name = seed.Name,
                        PhoneNumber = seed.Phone,
                        Role = UserRole.Customer,
                        EmailConfirmed = true,
                        IsEmailVerified = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createResult = await userManager.CreateAsync(customer, "Basel@2004");
                    if (!createResult.Succeeded)
                    {
                        logger?.LogError("Failed to seed customer {Email}: {Errors}", seed.Email,
                            string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        continue;
                    }

                    await userManager.AddToRoleAsync(customer, "Customer");
                    customers.Add(customer);
                }
                else
                {
                    customers.Add(existing);
                }
            }

            if (customers.Count == 0) return;

            var now = DateTime.UtcNow;
            var orders = new List<Order>
                {
                    BuildOrder(1001, customers[0].Id, laundry.Id, services, "مدينة نصر - الحي الثامن", "العباسية - شارع الجيش", now.AddHours(2), OrderStatus.Accepted, PaymentMethod.Cash),
                    BuildOrder(1002, customers[1].Id, laundry.Id, services, "المعادي - شارع 9", "زهراء المعادي", now.AddHours(3), OrderStatus.PendingConfirmation, PaymentMethod.Cash),
                    BuildOrder(1003, customers[2].Id, laundry.Id, services, "المقطم - الهضبة الوسطى", "العباسية - رمسيس", now.AddHours(4), OrderStatus.Delivered, PaymentMethod.CreditCard, deliveredAt: now.AddHours(-2)),
                    BuildOrder(1004, customers[0].Id, laundry.Id, services, "شبرا - شارع ترعة الإسماعيلية", "روض الفرج", now.AddHours(5), OrderStatus.ReadyForPickup, PaymentMethod.MobilePayment)
                    ,
                    // Test orders for frontend QA (Basel & Menna)
                    BuildOrder(1005, customers.First(c => c.Email == "basel.dev@ndeef.com").Id, laundry.Id, services, "المعادي - شارع النصر", "المعادي - شارع النصر", now.AddHours(1), OrderStatus.PendingConfirmation, PaymentMethod.Cash),
                    BuildOrder(1006, customers.First(c => c.Email == "menna.test@ndeef.com").Id, laundry.Id, services, "المعادى - تقاطع 10", "مدينة نصر - ميدان الراحة", now.AddHours(6), OrderStatus.Accepted, PaymentMethod.MobilePayment)
                };

            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();
        }

        private static Order BuildOrder(
            int orderId,
            string customerId,
            int laundryId,
            List<Service> services,
            string pickup,
            string delivery,
            DateTime pickupTime,
            OrderStatus status,
            PaymentMethod paymentMethod,
            DateTime? deliveredAt = null)
        {
            var items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ServiceId = services[0].Id,
                        Quantity = 2,
                        UnitPrice = services[0].Price,
                        Subtotal = services[0].Price * 2
                    },
                    new OrderItem
                    {
                        ServiceId = services.Count > 1 ? services[1].Id : services[0].Id,
                        Quantity = 1,
                        UnitPrice = services.Count > 1 ? services[1].Price : services[0].Price,
                        Subtotal = (services.Count > 1 ? services[1].Price : services[0].Price)
                    }
                };

            var total = items.Sum(i => i.Subtotal);

            return new Order
            {
                Id = orderId,
                CustomerId = customerId,
                LaundryId = laundryId,
                PickupLocation = pickup,
                DeliveryLocation = delivery,
                PickupTime = pickupTime,
                Status = status,
                TotalPrice = total,
                CreatedAt = DateTime.UtcNow,
                DeliveredAt = deliveredAt,
                OrderItems = items,
                Payment = new Payment
                {
                    Amount = total,
                    PaymentStatus = status == OrderStatus.Delivered ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentMethod = paymentMethod,
                    PaymentDate = status == OrderStatus.Delivered ? DateTime.UtcNow : null
                }
            };
        }

        // ============================================================
        // Seed المغاسل
        // ============================================================
        private static async Task SeedLaundriesAsync(NadeefDbContext context)
        {
            // لو في مغاسل موجودة بالفعل مش بنعمل حاجة
            if (await context.Laundries.AnyAsync()) return;

            // بنجيب المسار الصح سواء الأبلكيشن شغال من NdeefApp أو من مكان تاني
            var baseDir = Directory.GetCurrentDirectory();
            var filePath = Path.Combine(baseDir, "Data", "DataSeeding", "Json", "laundries.json");

            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(baseDir, "..", "LaundryPerstince", "Data", "DataSeeding", "Json", "laundries.json");
            }

            if (!File.Exists(filePath)) return;

            var json = await File.ReadAllTextAsync(filePath);

            var laundries = JsonSerializer.Deserialize<List<Laundry>>(json,
                new JsonSerializerOptions
                {
                    // بيتعامل مع الـ Property Names بغض النظر عن الـ Case
                    PropertyNameCaseInsensitive = true
                });

            if (laundries is null || !laundries.Any()) return;

            // الـ AdminId مطلوب ومينفعش يبقى null
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "laundryadmin@ndeef.com");
            if (adminUser != null)
            {
                foreach (var laundry in laundries)
                {
                    laundry.AdminId = adminUser.Id;
                }
            }

            // علشان نقدر ندخل الـ ID بتاع المغسلة بايدينا (اللي جاي من الـ JSON)
            await context.Database.OpenConnectionAsync();
            try
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Laundries ON");
                await context.Laundries.AddRangeAsync(laundries);
                await context.SaveChangesAsync();
            }
            finally
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Laundries OFF");
                await context.Database.CloseConnectionAsync();
            }
        }

        // ============================================================
        // Seed الخدمات
        // ============================================================
        private static async Task SeedServicesAsync(NadeefDbContext context)
        {
            // لو في خدمات موجودة بالفعل مش بنعمل حاجة
            if (await context.Services.AnyAsync()) return;

            var baseDir = Directory.GetCurrentDirectory();
            var filePath = Path.Combine(baseDir, "Data", "DataSeeding", "Json", "services.json");

            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(baseDir, "..", "LaundryPerstince", "Data", "DataSeeding", "Json", "services.json");
            }

            if (!File.Exists(filePath)) return;

            var json = await File.ReadAllTextAsync(filePath);

            var services = JsonSerializer.Deserialize<List<Service>>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (services is null || !services.Any()) return;

            // علشان نقدر ندخل الـ ID بتاع الخدمة بايدينا (اللي جاي من الـ JSON)
            await context.Database.OpenConnectionAsync();
            try
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Services ON");
                await context.Services.AddRangeAsync(services);
                await context.SaveChangesAsync();
            }
            finally
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Services OFF");
                await context.Database.CloseConnectionAsync();
            }
        }
        
// ============================================================
// Seed الـ Bundles (6 bundles from business plan)
// ============================================================
private static async Task SeedBundlesAsync(
    NadeefDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger? logger = null)
        {
            // إذا في bundles موجودة بالفعل مش بنعمل حاجة (idempotent)
            if (await context.Bundles.AnyAsync()) return;

            // بنجيب الـ SuperAdmin عشان نحط الـ CreatedByAdminId
            var superAdmin = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "superadmin@ndeef.com");

            if (superAdmin == null)
            {
                logger?.LogWarning(
                    "SuperAdmin not found. Skipping bundles seeding.");
                return;
            }

            // بنجيب كل الـ services الموجودة في الداتابيز
            var allServices = await context.Services.ToListAsync();

            if (!allServices.Any())
            {
                logger?.LogWarning(
                    "No services found. Skipping bundles seeding.");
                return;
            }

            // Helper: بيجيب أول service تنفع لـ category معينة
            Service? GetService(ServiceCategory category) =>
                allServices.FirstOrDefault(s => s.Category == category);

            var now = DateTime.UtcNow;

            // ── 1. Fresh Start Pack ──────────────────────────────────
            var ironService = GetService(ServiceCategory.Iron);
            var freshStartBundle = new Bundle
            {
                Id = Guid.NewGuid(),
                Name = "Fresh Start Pack",
                Description = "5 ironing items to get your wardrobe fresh and crisp.",
                BundlePrice = 90m,
                OriginalPrice = 110m,
                Status = BundleStatus.Active,
                OwnerType = BundleOwnerType.Platform,
                CreatedByAdminId = superAdmin.Id,
                LaundryId = null,
                DisplayOrder = 1,
                IsDeleted = false,
                StartDate = null,
                ExpiryDate = null
            };

            if (ironService != null)
            {
                freshStartBundle.BundleItems.Add(new BundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleId = freshStartBundle.Id,
                    ServiceId = ironService.Id,
                    Quantity = 5,
                    AllowedItemCategory = ServiceCategory.Iron
                });
            }

            // ── 2. Family Care Bundle ────────────────────────────────
            var washService = GetService(ServiceCategory.Wash);
            var familyCareBundle = new Bundle
            {
                Id = Guid.NewGuid(),
                Name = "Family Care Bundle",
                Description = "Wash & Iron 10 mixed items for the whole family.",
                BundlePrice = 190m,
                OriginalPrice = 250m,
                Status = BundleStatus.Active,
                OwnerType = BundleOwnerType.Platform,
                CreatedByAdminId = superAdmin.Id,
                LaundryId = null,
                DisplayOrder = 2,
                IsDeleted = false,
                StartDate = null,
                ExpiryDate = null
            };

            if (washService != null)
            {
                familyCareBundle.BundleItems.Add(new BundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleId = familyCareBundle.Id,
                    ServiceId = washService.Id,
                    Quantity = 10,
                    AllowedItemCategory = ServiceCategory.Wash
                });
            }

            // ── 3. Wardrobe Reset ────────────────────────────────────
            var dryCleanService = GetService(ServiceCategory.DryCleaning);
            var wardrobeResetBundle = new Bundle
            {
                Id = Guid.NewGuid(),
                Name = "Wardrobe Reset",
                Description = "Dry clean 4 formal items and look your best.",
                BundlePrice = 420m,
                OriginalPrice = 520m,
                Status = BundleStatus.Active,
                OwnerType = BundleOwnerType.Platform,
                CreatedByAdminId = superAdmin.Id,
                LaundryId = null,
                DisplayOrder = 3,
                IsDeleted = false,
                StartDate = null,
                ExpiryDate = null
            };

            if (dryCleanService != null)
            {
                wardrobeResetBundle.BundleItems.Add(new BundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleId = wardrobeResetBundle.Id,
                    ServiceId = dryCleanService.Id,
                    Quantity = 4,
                    AllowedItemCategory = ServiceCategory.DryCleaning
                });
            }

            // ── 4. Home Refresh Pack ─────────────────────────────────
            var homeRefreshBundle = new Bundle
            {
                Id = Guid.NewGuid(),
                Name = "Home Refresh Pack",
                Description = "Dry clean 3 blankets and keep your home fresh.",
                BundlePrice = 380m,
                OriginalPrice = 480m,
                Status = BundleStatus.Active,
                OwnerType = BundleOwnerType.Platform,
                CreatedByAdminId = superAdmin.Id,
                LaundryId = null,
                DisplayOrder = 4,
                IsDeleted = false,
                StartDate = null,
                ExpiryDate = null
            };

            if (dryCleanService != null)
            {
                homeRefreshBundle.BundleItems.Add(new BundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleId = homeRefreshBundle.Id,
                    ServiceId = dryCleanService.Id,
                    Quantity = 3,
                    AllowedItemCategory = ServiceCategory.DryCleaning
                });
            }

            // ── 5. B Bundle for Bags ─────────────────────────────────
            var specialtyService = GetService(ServiceCategory.Specialty);
            var bagsBundleBundle = new Bundle
            {
                Id = Guid.NewGuid(),
                Name = "B Bundle for Bags",
                Description = "Wash 6 bag items and keep them looking brand new.",
                BundlePrice = 550m,
                OriginalPrice = 650m,
                Status = BundleStatus.Active,
                OwnerType = BundleOwnerType.Platform,
                CreatedByAdminId = superAdmin.Id,
                LaundryId = null,
                DisplayOrder = 5,
                IsDeleted = false,
                StartDate = null,
                ExpiryDate = null
            };

            if (specialtyService != null)
            {
                bagsBundleBundle.BundleItems.Add(new BundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleId = bagsBundleBundle.Id,
                    ServiceId = specialtyService.Id,
                    Quantity = 6,
                    AllowedItemCategory = ServiceCategory.Specialty
                });
            }

            // ── 6. S Bundle for Shoes ────────────────────────────────
            var shoesBundleBundle = new Bundle
            {
                Id = Guid.NewGuid(),
                Name = "S Bundle for Shoes",
                Description = "Wash 3 pairs of shoes and step out in style.",
                BundlePrice = 435m,
                OriginalPrice = 540m,
                Status = BundleStatus.Active,
                OwnerType = BundleOwnerType.Platform,
                CreatedByAdminId = superAdmin.Id,
                LaundryId = null,
                DisplayOrder = 6,
                IsDeleted = false,
                StartDate = null,
                ExpiryDate = null
            };

            if (specialtyService != null)
            {
                shoesBundleBundle.BundleItems.Add(new BundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleId = shoesBundleBundle.Id,
                    ServiceId = specialtyService.Id,
                    Quantity = 3,
                    AllowedItemCategory = ServiceCategory.Specialty
                });
            }

            // ── Save all bundles ─────────────────────────────────────
            var bundles = new List<Bundle>
    {
        freshStartBundle,
        familyCareBundle,
        wardrobeResetBundle,
        homeRefreshBundle,
        bagsBundleBundle,
        shoesBundleBundle
    };

            await context.Bundles.AddRangeAsync(bundles);
            await context.SaveChangesAsync();

            logger?.LogInformation(
                "Successfully seeded {Count} bundles.", bundles.Count);
        }

    }
}
