using System;
using System.Collections.Generic;

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
        public decimal? BookingCommissionRate { get; set; }

        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; } 
        public List<HousingRatingReadDto> Ratings { get; set; } = new List<HousingRatingReadDto>();
    }
}