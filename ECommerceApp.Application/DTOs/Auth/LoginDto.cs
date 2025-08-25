namespace ECommerceApp.Application.DTOs.Auth
{
    public class LoginDto
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
        public string? Lang { get; set; }
        public int? TimeZoneSecond { get; set; }
    }
}
