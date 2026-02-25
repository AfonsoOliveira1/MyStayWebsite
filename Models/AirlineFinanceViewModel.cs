namespace Booking.web.Models
{
    public class AirlineFinanceViewModel
    {
        public decimal TotalEarnings { get; set; }
        public List<FlightBookingHistoryViewModel> Bookings { get; set; } = new List<FlightBookingHistoryViewModel>();
    }
}
