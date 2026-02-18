namespace Booking.web.Models
{
    public class SeatViewModel
    {
        public int Id { get; set; }
        public int Flightid { get; set; }
        public string SeatNumber { get; set; } = "";
        public bool IsBooked { get; set; }
    }
}
