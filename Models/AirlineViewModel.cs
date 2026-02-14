namespace Booking.web.Models
{
    public class AirlineViewModel
    {
        public int IdAirline { get; set; }

        public string AirlineName { get; set; } = "";

        public string? Email { get; set; } 
        public string? Phone { get; set; }
        public bool IsDeleted { get; set; }
    }
}
