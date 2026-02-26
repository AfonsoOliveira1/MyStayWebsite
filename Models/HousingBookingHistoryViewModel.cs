namespace Booking.web.Models
{
    public class HousingBookingHistoryViewModel
    {
        public int BookingId { get; set; }
        public string HousingName { get; set; }
        public string GuestName { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }

        public decimal CommissionValue { get; set; }

        public string StayPeriod => CheckIn.ToString("dd/MM/yyyy") + " - " + CheckOut.ToString("dd/MM/yyyy");
    }
}
