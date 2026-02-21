using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Booking.web.Models
{
    public class HousingBookingViewModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("housingid")]
        public int HousingId { get; set; }

        [JsonPropertyName("customerid")] 
        public int CustomerId { get; set; }
        [JsonPropertyName("housingName")]
        public string HousingName { get; set; } = "";    
        public decimal PricePerNight { get; set; }

        [JsonPropertyName("checkindate")] 
        public DateTime CheckInDate { get; set; }

        [JsonPropertyName("checkoutdate")] 
        public DateTime CheckOutDate { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Confirmed";
        public bool IsActive { get; set; } = true;

        public string ImageUrl { get; set; }

        [JsonPropertyName("hasRating")]
        public bool HasRating { get; set; }

        [JsonPropertyName("ratingScore")] 
        public int? RatingScore { get; set; }

        [JsonPropertyName("ratingComment")] 
        public string? RatingComment { get; set; }

        [JsonPropertyName("ratings")]
        public List<HousingRatingViewModel> Ratings { get; set; } = new();
    }
}
