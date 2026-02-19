namespace Booking.web.Models
{
    public class CityViewModel
    {
        public int Id { get; set; }

        public string Citynamept { get; set; }
        public string Citynameen { get; set; }
        public int Countryid { get; set; } = 1;
        public int? StateId { get; set; }
        public string? Timezone { get; set; }
        public bool? IsCapital { get; set; }
    }
}
