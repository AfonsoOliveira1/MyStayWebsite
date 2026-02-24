namespace Booking.web.Models
{
    public class RenterFinanceViewModel
    {
        public decimal TotalEarnings { get; set; }
        public List<BookingHistoryViewModel> Bookings { get; set; } = new List<BookingHistoryViewModel>();
    }
}
