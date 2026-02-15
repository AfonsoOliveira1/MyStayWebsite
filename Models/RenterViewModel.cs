namespace Booking.web.Models
{
    public class RenterViewModel
    {
        public int IdRenter { get; set; }
        public string RenterName { get; set; }//nm house
        public string Email { get; set; }
        public int? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
