namespace Booking.web.Models
{
    public class UserCreateDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string? CompanyType { get; set; }
        public int? CompanyId { get; set; }
    }
}
