namespace Booking.web.Models
{
    public class HousingBookingReadDto
    {
        public int Id { get; set; }
        public string HousingName { get; set; }
        public string CustomerName { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public decimal? PlatformProfit { get; set; } // lucro
    }
}
