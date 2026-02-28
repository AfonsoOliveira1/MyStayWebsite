using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Booking.web.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public BookingsController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        private HttpClient GetAuthorizedClient()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        [HttpGet]
        [Route("Bookings/Index")]
        public async Task<IActionResult> MyBookings()
        {
            var client = GetAuthorizedClient();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var model = new UserBookingHistory();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var flightResponse = await client.GetAsync("api/FlightBookings/passenger/" + userId);
            if (flightResponse.IsSuccessStatusCode)
            {
                model.Flights = await flightResponse.Content.ReadFromJsonAsync<List<FlightBookingViewModel>>(options)
                                 ?? new List<FlightBookingViewModel>();
            }

            var housingResponse = await client.GetAsync("api/HousingBookings/my-bookings/" + userId);
            if (housingResponse.IsSuccessStatusCode)
            {
                model.Housings = await housingResponse.Content.ReadFromJsonAsync<List<HousingBookingViewModel>>(options)
                                  ?? new List<HousingBookingViewModel>();

                
                foreach (var h in model.Housings)
                {
                    var ratingResponse = await client.GetAsync("api/HousingRatings/housing/" + h.HousingId);
                    if (ratingResponse.IsSuccessStatusCode)
                    {
                        var ratings = await ratingResponse.Content.ReadFromJsonAsync<List<HousingRatingReadDto>>(options);
                        h.HasRating = ratings != null && ratings.Any(r => r.CustomerName == User.Identity.Name);
                    }
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Não foi possível carregar as suas reservas de alojamento.";
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var client = GetAuthorizedClient();
            var response = await client.GetAsync("api/FlightBookings/details/" + id);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var model = await response.Content.ReadFromJsonAsync<FlightBookingViewModel>(options);
                return View(model);
            }

            TempData["ErrorMessage"] = "Não foi possível carregar o talão.";
            return RedirectToAction("MyBookings");
        }
    }
}