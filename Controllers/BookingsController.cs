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

            IEnumerable<FlightBookingReadDto> flightBookings = new List<FlightBookingReadDto>();
            if (responseFlight.IsSuccessStatusCode)
            {
                flightBookings = await responseFlight.Content
                    .ReadFromJsonAsync<IEnumerable<FlightBookingReadDto>>() ?? new List<FlightBookingReadDto>();
            }

            //casas
            
            var responseHouse = await client.GetAsync("api/HousingBookings/user/" + userId);

            IEnumerable<HousingBookingReadDto> housingBookings = new List<HousingBookingReadDto>();
            if (responseHouse.IsSuccessStatusCode)
            {
                housingBookings = await responseHouse.Content
                    .ReadFromJsonAsync<IEnumerable<HousingBookingReadDto>>() ?? new List<HousingBookingReadDto>();
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