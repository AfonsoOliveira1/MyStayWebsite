namespace Booking.web.Models
{
    public class HousingCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal PricePerNight { get; set; } 
        public long CityId { get; set; }         
        public string ImageUrl { get; set; }
        public int CompanyId { get; set; }
    }
}
