namespace Booking.web.Models
{
    public class CityViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CountryId { get; set; } = 1;// valor default
        public int? StateId { get; set; }
        public string? Timezone { get; set; }
        public int IsCapital { get; set; } 
    }
}
