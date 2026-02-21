namespace Booking.web.Models
{
    public class UserUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string ?Password { get; set; }
        public string Role { get; set; } // "Admin", "Customer", "Company"
        public int AirlineId { get; set; } 
        public int RenterId { get; set; } 
    }
}
