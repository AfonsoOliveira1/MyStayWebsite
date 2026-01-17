namespace Booking.web.Models
{
    public class FlightBookingViewModel
    {
        public int Id { get; set; }
        public int FlightId { get; set; }
        public int SeatId { get; set; }
        public int PassengerId { get; set; }
        public string Status { get; set; } = "Confirmed";
    }
}
