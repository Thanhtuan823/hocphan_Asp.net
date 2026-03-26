namespace lab2.Models
{
    public class UserWithRoles
    {
        public string Id { get; set; }
        public string UserName { get; set; } // Dùng làm tên hiển thị (Nickname)
        public string Email { get; set; }    // Dùng để đăng nhập (Read-only)
        public string? PhoneNumber { get; set; } // Số điện thoại có sẵn trong DB
        public List<string> Roles { get; set; } = new List<string>();
    }
}