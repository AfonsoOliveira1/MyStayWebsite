namespace Booking.web.Models
{
    public class ProfileViewModel
    {
        public string Name { get; set; }
        public string Email { get; set; }

        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public string Role { get; set; }
    }
}
