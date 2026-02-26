namespace Booking.web.Models
{
    public class FlightBookingHistoryViewModel
    {
        public int BookingId { get; set; }
        public string FlightNumber { get; set; }
        public string PassengerName { get; set; }
        public DateTime DepartureDate { get; set; }
        public decimal Value { get; set; }
        public string Status { get; set; }
    }
}
