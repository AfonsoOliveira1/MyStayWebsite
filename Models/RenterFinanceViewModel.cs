namespace Booking.web.Models
{
    public class RenterFinanceViewModel
    {
        public decimal TotalEarnings { get; set; }
        public List<HousingBookingHistoryViewModel> Bookings { get; set; } = new List<HousingBookingHistoryViewModel>();
    }
}
