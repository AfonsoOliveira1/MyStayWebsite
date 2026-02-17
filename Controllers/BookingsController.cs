using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public BookingsController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }


        public async Task<IActionResult> Index(bool? showFlights,
            bool? showHousings,
            bool? showConfirmed,
            bool? showCancelled,
            bool? showActive)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim.Value);

            var token = User.FindFirst("JWToken")?.Value;
            var client = _clientFactory.CreateClient("Booking.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            /* ======= VOOS =======
            var responseFlight = await client.GetAsync($"api/FlightBookings/passenger/{userId}");
            IEnumerable<FlightBookingReadDto> flightBookings = new List<FlightBookingReadDto>();
            if (responseFlight.IsSuccessStatusCode)
            {
                flightBookings = await responseFlight.Content
                    .ReadFromJsonAsync<IEnumerable<FlightBookingReadDto>>() ?? new List<FlightBookingReadDto>();
            }
            */

            // ======= ALOJAMENTOS =======
            var responseHouse = await client.GetAsync($"api/HousingBookings/user/{userId}");
            IEnumerable<HousingBookingReadDto> housingBookings = new List<HousingBookingReadDto>();
            if (responseHouse.IsSuccessStatusCode)
            {
                housingBookings = await responseHouse.Content
                    .ReadFromJsonAsync<IEnumerable<HousingBookingReadDto>>() ?? new List<HousingBookingReadDto>();
            }

            // Monta o histórico
            var history = new UserBookingHistory
            {
                //Flights = flightBookings,
                Housings = housingBookings
            };

            // Passa os filtros de volta para a View
            ViewData["ShowFlights"] = showFlights;
            ViewData["ShowHousings"] = showHousings;
            ViewData["ShowConfirmed"] = showConfirmed;
            ViewData["ShowCancelled"] = showCancelled;
            ViewData["ShowActive"] = showActive;

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