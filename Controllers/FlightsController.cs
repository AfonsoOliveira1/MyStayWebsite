using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;

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
        [Authorize(Roles = "AIRLINE,ADMIN")]
        public async Task<IActionResult> Create()
        {
            await LoadFlightViewBags();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "AIRLINE,ADMIN")]
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

            //duracao
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

            var response = await client.PatchAsync("api/Flights/" + id + "/approve", null);
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Voo #" + id + " aprovado!";
            else
                TempData["Error"] = "Erro ao aprovar o voo.";

            return RedirectToAction(nameof(PendingApprovals));
        }

        [HttpGet]
        [Authorize(Roles = "CUSTOMER,ADMIN,AIRLINE")]
        public async Task<IActionResult> Reserve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var flight = await client.GetFromJsonAsync<FlightViewModel>("api/Flights/" + id);
            if (flight == null) return NotFound();

            var seatsResponse = await client.GetAsync("api/FlightBookings/flight/" + id + "/seats");

            if (seatsResponse.IsSuccessStatusCode)
            {
                var seats = await seatsResponse.Content.ReadFromJsonAsync<List<SeatViewModel>>();
                ViewBag.Seats = seats;
            }
            else
            {
                ViewBag.Seats = new List<SeatViewModel>();
                TempData["Error"] = "Erro ao carregar os lugares.";
            }

            return View(flight);
        }
        [HttpPost]
        [Authorize(Roles = "CUSTOMER,ADMIN,AIRLINE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int id, string SeatId)
        {
            if (string.IsNullOrEmpty(SeatId))
            {
                TempData["Error"] = "Selecione pelo menos um lugar.";
                return RedirectToAction("Reserve", new { id = id });
            }

            var seatIdsList = SeatId.Split(',').Select(s => s.Trim()).ToList();

            var dadosReserva = new
            {
                Flightid = id,
                SeatIds = seatIdsList
            };

            var client = _clientFactory.CreateClient("Booking.API");
            var token = await GetToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("api/FlightBookings", dadosReserva);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Reserva concluída com sucesso!";
                return RedirectToAction("Index", "Home");
            }

            var erro = await response.Content.ReadAsStringAsync();
            TempData["Error"] = "Erro na reserva: " + erro;
            return RedirectToAction("Reserve", new { id = id });
        }



        private async Task LoadFlightViewBags()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            try
            {
                var cities = await client.GetFromJsonAsync<List<CityViewModel>>("api/Cities") ?? new List<CityViewModel>();

              
                var culture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
                string displayField = culture.StartsWith("pt") ? "Citynamept" : "Citynameen";

                var cityList = new SelectList(cities, "Id", displayField);

                ViewBag.OriginId = cityList;
                ViewBag.DestinationId = cityList;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Erro ao carregar cidades da API: " + ex.Message;
                ViewBag.OriginId = new SelectList(Enumerable.Empty<SelectListItem>());
                ViewBag.DestinationId = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        private async Task<string> GetToken()
        {
            return await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;
        }
    }
}