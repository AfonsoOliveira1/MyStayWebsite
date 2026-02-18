using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Booking.Web.Controllers
{
    [Authorize]
    public class FlightController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public FlightController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }


        [AllowAnonymous]
        public async Task<IActionResult> List()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var flights = await client.GetFromJsonAsync<List<FlightViewModel>>("api/Flights")
                          ?? new List<FlightViewModel>();
            return View(flights);
        }


        [Authorize(Roles = "AIRLINE")]
        public async Task<IActionResult> MyFlights()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Flights/my-flights");
            if (response.IsSuccessStatusCode)
            {
                var myFlights = await response.Content.ReadFromJsonAsync<List<FlightViewModel>>();
                return View(myFlights);
            }
            return View(new List<FlightViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadFlightViewBags();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlightViewModel model)
        {
            
            ModelState.Remove("AirlineId");

            if (model.OriginId == model.DestinationId)
            {
                ModelState.AddModelError("", "A cidade de origem não pode ser a mesma que o destino.");
            }

            if (!ModelState.IsValid)
            {
                await LoadFlightViewBags();
                return View(model);
            }

            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            
            model.ArrivalTime = model.DepartureTime.AddHours(2);

            var response = await client.PostAsJsonAsync("api/Flights", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Voo registado com sucesso e aguarda aprovação!";
                return RedirectToAction(nameof(MyFlights));
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Erro ao comunicar com a API: " + errorMsg);
            await LoadFlightViewBags();
            return View(model);
        }


        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> PendingApprovals()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var flights = await client.GetFromJsonAsync<List<FlightViewModel>>("api/Flights/pending")
                          ?? new List<FlightViewModel>();
            return View(flights);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Approve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PatchAsync($"api/Flights/{id}/approve", null);
            if (response.IsSuccessStatusCode)
                TempData["Success"] = $"Voo #{id} aprovado!";
            else
                TempData["Error"] = "Erro ao aprovar o voo.";

            return RedirectToAction(nameof(PendingApprovals));
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Reject(int id)
        {

            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PatchAsync($"api/Flights/{id}/reject", null);
            if (response.IsSuccessStatusCode)
                TempData["Success"] = $"Voo #{id} rejeitado!";
            else
                TempData["Error"] = "Erro ao rejeitar o voo.";

            return RedirectToAction(nameof(PendingApprovals));
        }


        private async Task LoadFlightViewBags()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var cities = await client.GetFromJsonAsync<List<CityViewModel>>("api/Cities") ?? new List<CityViewModel>();
            var cityList = new SelectList(cities, "Id", "Name");
            ViewBag.OriginId = cityList;
            ViewBag.DestinationId = cityList;
        }

        private async Task<string> GetToken()
        {
            return await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;
        }

        // get
        [HttpGet("Flight/Reserve/{id}")]
        [Authorize(Roles = "CUSTOMER,ADMIN,AIRLINE")]
        public async Task<IActionResult> Reserve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

          
            var flight = await client.GetFromJsonAsync<FlightViewModel>($"api/Flights/{id}");
            if (flight == null) return NotFound();

          
            var seatsResponse = await client.GetAsync($"api/FlightBookings/flight/{id}/seats");

            if (seatsResponse.IsSuccessStatusCode)
            {
                var seats = await seatsResponse.Content.ReadFromJsonAsync<List<SeatViewModel>>();
                ViewBag.Seats = seats;
            }
            else
            {
                ViewBag.Seats = new List<SeatViewModel>();
                TempData["Error"] = "Erro ao carregar os lugares do avião.";
            }

            return View(flight);
        }

        // post
        [HttpPost("Flight/Reserve/{id}")]
        [Authorize(Roles = "CUSTOMER,ADMIN,AIRLINE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int Id, string SeatId)
        {
            if (string.IsNullOrEmpty(SeatId))
            {
                TempData["Error"] = "Por favor, selecione um lugar no mapa.";
                return RedirectToAction(nameof(Reserve), new { id = Id });
            }

            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var bookingDto = new
            {
                Flightid = Id,
                Seatid = SeatId 
            };

            var response = await client.PostAsJsonAsync("api/FlightBookings", bookingDto);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Reserva confirmada! Lugar: {SeatId}";
               
                return RedirectToAction("Index", "Bookings");
            }

           
            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = "Não foi possível reservar: " + error;
            return RedirectToAction(nameof(Reserve), new { id = Id });
        }
    }
}