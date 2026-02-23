namespace Booking.web.Models
{
    public class HousingPriceUpdateViewModel
    {
        public int Id { get; set; }
        public decimal NewPrice { get; set; }
        public decimal OldPrice { get; set; }
    }
}
