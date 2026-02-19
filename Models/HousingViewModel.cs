using System;

namespace Booking.web.Models
{
    public class HousingViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public decimal PricePerNight { get; set; }
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public bool IsAvailable { get; set; } = true;
        public string CityName { get; set; } = "";
        public int CompanyId { get; set; }

        public string ApprovalStatus { get; set; } = "PENDING";
        public int CityId { get; set; } 
        public HousingRatingViewModel HousingRatingViewModel { get; set; } = new HousingRatingViewModel();
    }
}
