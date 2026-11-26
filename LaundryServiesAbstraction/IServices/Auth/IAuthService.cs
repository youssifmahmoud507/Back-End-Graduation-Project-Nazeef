using LaundryShared.Common;
using LaundryShared.DTO.Auth;

namespace LaundryServiesAbstraction.IServices.Auth
{
    public interface IAuthService
    {
        Task<Result<UserDto>> RegisterAsync(RegisterDto registerDto);
        Task<Result<UserDto>> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// بياخد الـ ID Token من Google
        /// بيتحقق منه
        /// لو اليوزر موجود بيعمله Login
        /// لو مش موجود بيعمل Register تلقائي
        /// </summary>
        Task<Result<UserDto>> GoogleLoginAsync(GoogleLoginDto googleLoginDto);



        Task<Result<UserDto>> FacebookLoginAsync(FacebookLoginDto facebookLoginDto);


        // ✅ الجديدة
        Task<Result<string>> VerifyEmailOtpAsync(VerifyOtpDto dto);
        Task<Result<string>> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<Result<string>> ResetPasswordAsync(ResetPasswordDto dto);

    }
}
