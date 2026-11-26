namespace LaundryShared.DTO.Auth
{
    public class UserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; } // هنرجعه كـ String (مثلاً "Customer") عشان يكون مقروء للـ Frontend
        public string Token { get; set; } // الـ JWT Token
        public bool NeedsVerification { get; set; } = false; // يشير إذا كان المستخدم محتاج يعمل verification
    }
}
