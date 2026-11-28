using Google.Apis.Auth;
using LaundryDomain.Models.UserModules;
using LaundryServies.Services.Auth.Helper;
using LaundryServiesAbstraction.IServices.Auth;
using LaundryShared.Common;
using LaundryShared.DTO.Auth;
using LaundryShared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LaundryServies.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<Result<UserDto>> RegisterAsync(RegisterDto registerDto)
        {
            // التسجيل العام مسموح بس للأدوار دي. SuperAdmin بيتعمل بس عن طريق الـ Seed أو من لوحة التحكم.
            if (registerDto.Role != UserRole.Customer &&
                registerDto.Role != UserRole.Courier &&
                registerDto.Role != UserRole.LaundryAdmin)
            {
                return Result<UserDto>.Failure("Invalid role.");
            }

            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
                return Result<UserDto>.Failure("Email is already registered.");

            // For LaundryAdmin, skip email verification and return token immediately
            var isLaundryAdmin = registerDto.Role == UserRole.LaundryAdmin;
            var otp = isLaundryAdmin ? null : GenerateOtp();

            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                Name = registerDto.Name,
                PhoneNumber = registerDto.PhoneNumber,
                Role = registerDto.Role,
                IsActive = true,
                IsEmailVerified = isLaundryAdmin, // Auto-verify for LaundryAdmin
                OtpCode = otp,
                OtpExpiry = isLaundryAdmin ? null : DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result<UserDto>.Failure($"Registration failed: {errors}");
            }

            // Only send OTP for non-LaundryAdmin users
            if (!isLaundryAdmin)
            {
                await SendOtpEmailAsync(
                    user.Email,
                    user.Name,
                    otp,
                    "Verify your Nadeef account",
                    "Use the code below to verify your email address:"
                );
            }

            return Result<UserDto>.Success(new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                Token = isLaundryAdmin ? GenerateJwtToken(user) : null, // Return token immediately for LaundryAdmin
                NeedsVerification = isLaundryAdmin // LaundryAdmin needs identity verification
            });
        }
        
        public async Task<Result<UserDto>> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
                return Result<UserDto>.Failure(InvalidCredentialsMessage); // رسالة موحدة عشان ما نكشفش الإيميلات المسجلة

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
                return Result<UserDto>.Failure(InvalidCredentialsMessage);

            // Skip email verification check for LaundryAdmin (they are auto-verified during registration)
            if (!user.IsEmailVerified && user.Role != UserRole.LaundryAdmin)
                return Result<UserDto>.Failure("Please verify your email first.");

            var isDefaultLaundryAdmin =
                user.Role == UserRole.LaundryAdmin &&
                string.Equals(user.Email, "laundryadmin@ndeef.com", StringComparison.OrdinalIgnoreCase);

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                Token = GenerateJwtToken(user),
                NeedsVerification = user.Role == UserRole.LaundryAdmin && !user.IsIdentityVerified && !isDefaultLaundryAdmin
            };

            return Result<UserDto>.Success(userDto); // إرجاع النجاح
        }

        public async Task<Result<UserDto>> GoogleLoginAsync(GoogleLoginDto googleLoginDto)
        {
            // 1. بنتحقق من الـ Token مع Google
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"] }
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(
                    googleLoginDto.IdToken, settings);
            }
            catch
            {
                // رجعنا الـ Error الطبيعي: أي Token مش طالع من جوجل بجد هيترفض.
                return Result<UserDto>.Failure("Invalid Google token");
            }

            var email = payload.Email;
            var name = payload.Name;

            // 2. بندور على اليوزر بالإيميل بتاعه
            var user = await _userManager.FindByEmailAsync(email);

            // 3. لو مش موجود
            if (user is null)
            {
                // لو Courier أو LaundryAdmin، لازم يسجلوا يدوياً أول مرة
                if (googleLoginDto.Role == UserRole.Courier || googleLoginDto.Role == UserRole.LaundryAdmin)
                {
                    return Result<UserDto>.Failure("Please register with us first. Courier and Laundry Admin accounts require manual registration.");
                }

                // للـ Customers بنعمله Register تلقائي
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = name,
                    IsActive = true,
                    Role = UserRole.Customer,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true,
                    IsEmailVerified = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return Result<UserDto>.Failure($"Failed to create account: {errors}");
                }
            }
            else
            {
                // لو موجود، بنتحقق إن الـ Role يطابق
                if (user.Role != googleLoginDto.Role)
                {
                    return Result<UserDto>.Failure($"This account is registered as {user.Role}. Please login with the correct role.");
                }

                // تأكيد توثيق الإيميل
                var needsUpdate = false;
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    needsUpdate = true;
                }
                if (!user.IsEmailVerified)
                {
                    user.IsEmailVerified = true;
                    needsUpdate = true;
                }
                if (needsUpdate)
                {
                    await _userManager.UpdateAsync(user);
                }
            }

            // 4. بنتأكد إن الأكونت مش موقوف
            if (!user.IsActive)
                return Result<UserDto>.Failure("Your account has been suspended");

            // 5. بنعمل Manual Mapping ونرجع الـ Token
            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                Token = GenerateJwtToken(user)
            };

            return Result<UserDto>.Success(userDto);
        }

        public async Task<Result<UserDto>> FacebookLoginAsync(FacebookLoginDto facebookLoginDto)
        {
            // 1. بنتحقق من الـ Access Token مع Facebook Graph API
            using var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(
                $"https://graph.facebook.com/me?fields=id,name,email&access_token={facebookLoginDto.AccessToken}");

            if (!response.IsSuccessStatusCode)
                return Result<UserDto>.Failure("Invalid Facebook token.");

            var content = await response.Content.ReadAsStringAsync();
            var fbUser = JsonSerializer.Deserialize<FacebookUserInfo>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (fbUser == null || string.IsNullOrEmpty(fbUser.Email))
                return Result<UserDto>.Failure("Could not retrieve email from Facebook.");

            // 2. بندور على اليوزر بالإيميل
            var user = await _userManager.FindByEmailAsync(fbUser.Email);

            // 3. لو مش موجود بنعمله Register تلقائي
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = fbUser.Email,
                    Email = fbUser.Email,
                    Name = fbUser.Name,
                    IsActive = true,
                    Role = UserRole.Customer,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return Result<UserDto>.Failure($"Failed to create account: {errors}");
                }
            }

            // 4. بنتأكد إن الأكونت مش موقوف
            if (!user.IsActive)
                return Result<UserDto>.Failure("Your account has been suspended.");

            // 5. بنرجع الـ JWT Token
            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                Token = GenerateJwtToken(user)
            };

            return Result<UserDto>.Success(userDto);
        }

        // ✅ الجديدة
        public async Task<Result<string>> VerifyEmailOtpAsync(VerifyOtpDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Result<string>.Failure("User not found.");

            if (user.IsEmailVerified)
                return Result<string>.Failure("Email is already verified.");

            if (OtpAttempts.IsLocked(dto.Email))
                return Result<string>.Failure("Too many failed attempts. Please try again later.");

            if (!OtpMatches(user.OtpCode, dto.OtpCode) || user.OtpExpiry < DateTime.UtcNow)
            {
                OtpAttempts.RegisterFailure(dto.Email);
                return Result<string>.Failure("Invalid or expired OTP.");
            }

            OtpAttempts.Reset(dto.Email);
            user.IsEmailVerified = true;
            user.OtpCode = null;
            user.OtpExpiry = null;

            await _userManager.UpdateAsync(user);

            var token = GenerateJwtToken(user);
            return Result<string>.Success(token);
        }
        // ✅ الجديدة
        private async Task SendOtpEmailAsync(string toEmail, string userName, string otp, string subject, string purpose)
        {
            var smtpSettings = _configuration.GetSection("Smtp");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtpSettings["SenderName"],
                smtpSettings["SenderEmail"]
            ));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $"""
            <div style="font-family: Arial, sans-serif; max-width: 480px; margin: auto; 
                        border: 1px solid #e0e0e0; border-radius: 8px; padding: 32px;">
                <h2 style="color: #2563eb;">Nadeef 🧺</h2>
                <p>Hello <strong>{userName}</strong>,</p>
                <p>{purpose}</p>
                <div style="text-align: center; margin: 24px 0;">
                    <span style="font-size: 36px; font-weight: bold; 
                                 letter-spacing: 8px; color: #2563eb;">
                        {otp}
                    </span>
                </div>
                <p style="color: #6b7280; font-size: 13px;">
                    This code expires in <strong>10 minutes</strong>.
                </p>
                <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;"/>
                <p style="color: #9ca3af; font-size: 12px;">
                    If you didn't request this, please ignore this email.
                </p>
            </div>
        """
            };

            message.Body = bodyBuilder.ToMessageBody();
            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtpSettings["Host"],
                int.Parse(smtpSettings["Port"]),
                SecureSocketOptions.StartTls
            );
            await client.AuthenticateAsync(
                smtpSettings["SenderEmail"],
                smtpSettings["Password"]
            );
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        public async Task<Result<string>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Result<string>.Failure("User not found.");

            // Trim the OTP to prevent issues with trailing whitespaces from the frontend
            if (OtpAttempts.IsLocked(dto.Email))
                return Result<string>.Failure("Too many failed attempts. Please try again later.");

            if (string.IsNullOrEmpty(user.OtpCode) || !OtpMatches(user.OtpCode, dto.OtpCode?.Trim()) || user.OtpExpiry < DateTime.UtcNow)
            {
                OtpAttempts.RegisterFailure(dto.Email);
                return Result<string>.Failure("Invalid or expired OTP.");
            }

            OtpAttempts.Reset(dto.Email);

            // Remove the old password directly to ensure it cannot be used anymore
            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    return Result<string>.Failure("Failed to remove old password.");
                }
            }

            // Add the new password directly
            var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return Result<string>.Failure($"Password reset failed: {errors}");
            }

            // Update Security Stamp to invalidate old sessions/tokens if applicable
            await _userManager.UpdateSecurityStampAsync(user);

            user.OtpCode = null;
            user.OtpExpiry = null;
            await _userManager.UpdateAsync(user);

            return Result<string>.Success("Password has been reset successfully.");
        }
        public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || !user.IsActive)
                return Result<string>.Success("If this email exists, an OTP has been sent.");

            var otp = GenerateOtp();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            await _userManager.UpdateAsync(user);

            await SendOtpEmailAsync(
                user.Email,
                user.Name,
                otp,
                "Reset your Nadeef password",
                "Use the code below to reset your password:"
            );

            return Result<string>.Success("If this email exists, an OTP has been sent.");
        }
        // ✅ الجديدة
        private string GenerateOtp()
        {
            // RNG آمن تشفيرياً (Random.Shared قابل للتخمين)
            return System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        private const string InvalidCredentialsMessage = "Invalid email or password.";

        private static bool OtpMatches(string? stored, string? provided)
        {
            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(provided)) return false;
            var a = Encoding.UTF8.GetBytes(stored);
            var b = Encoding.UTF8.GetBytes(provided);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>
        /// عداد محاولات OTP الغلط (في الذاكرة): 5 محاولات غلط لكل إيميل خلال 10 دقايق وبعدها بيتقفل لحد ما الكود يتجدد أو الوقت يعدي.
        /// </summary>
        private static class OtpAttempts
        {
            private const int MaxAttempts = 5;
            private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
            private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Count, DateTime FirstFailure)> Failures = new();

            private static string Key(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

            public static bool IsLocked(string? email)
            {
                if (!Failures.TryGetValue(Key(email), out var entry)) return false;
                if (DateTime.UtcNow - entry.FirstFailure > Window)
                {
                    Failures.TryRemove(Key(email), out _);
                    return false;
                }
                return entry.Count >= MaxAttempts;
            }

            public static void RegisterFailure(string? email)
            {
                Failures.AddOrUpdate(
                    Key(email),
                    _ => (1, DateTime.UtcNow),
                    (_, old) => DateTime.UtcNow - old.FirstFailure > Window
                        ? (1, DateTime.UtcNow)
                        : (old.Count + 1, old.FirstFailure));
            }

            public static void Reset(string? email) => Failures.TryRemove(Key(email), out _);
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("Name", user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("uid", user.Id),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(double.Parse(jwtSettings["DurationInDays"])),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
