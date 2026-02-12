using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Booking.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public BookingsController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        
        public async Task<IActionResult> Index()
        {
           
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim.Value);
            var client = _clientFactory.CreateClient("Booking.API");

            // voos
            
            var responseFlight = await client.GetAsync("api/FlightBookings/passenger/" + userId);

            IEnumerable<FlightBookingViewModel> flightBookings = new List<FlightBookingViewModel>();
            if (responseFlight.IsSuccessStatusCode)
            {
                flightBookings = await responseFlight.Content
                    .ReadFromJsonAsync<IEnumerable<FlightBookingViewModel>>() ?? new List<FlightBookingViewModel>();
            }

            //casas
            
            var responseHouse = await client.GetAsync("api/HousingBookings/user/" + userId);

            IEnumerable<HousingBookingViewModel> housingBookings = new List<HousingBookingViewModel>();
            if (responseHouse.IsSuccessStatusCode)
            {
                housingBookings = await responseHouse.Content
                    .ReadFromJsonAsync<IEnumerable<HousingBookingViewModel>>() ?? new List<HousingBookingViewModel>();
            }

          
            var history = new UserBookingHistory { Flights = flightBookings, Housings = housingBookings };

            return View(history);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var client = _clientFactory.CreateClient("Booking.API");
           
            var response = await client.PutAsync("api/HousingBookings/cancel/" + bookingId, null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Reserva " + bookingId + " cancelada com sucesso.";
            }
            else
            {
                TempData["Error"] = "Erro ao cancelar a reserva " + bookingId + ".";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}