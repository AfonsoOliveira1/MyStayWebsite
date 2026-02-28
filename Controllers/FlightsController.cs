using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _clientFactory.CreateClient("BookingAPI");

            var response = await client.GetAsync($"api/flights/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var flightDto = await response.Content.ReadFromJsonAsync<FlightReadDto>();

            var model = new FlightViewModel
            {
                Id = flightDto.Id,
                OriginId = flightDto.OriginId ?? 0,
                DestinationId = flightDto.DestinationId ?? 0,
                DepartureTime = flightDto.DepartureTime,
                ArrivalTime = flightDto.ArrivalTime,
                Price = flightDto.Price,
                AirlineId = flightDto.AirlineId,
                ApprovalStatus = flightDto.ApprovalStatus,
                OriginCity = flightDto.OriginCity,
                DestinationCity = flightDto.DestinationCity,
                AirlineName = flightDto.AirlineName
            };

            await LoadCities(client, model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FlightViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadCities(_clientFactory.CreateClient("BookingAPI"), model);
                return View(model);
            }

            var client = _clientFactory.CreateClient("BookingAPI");

            var token = HttpContext.Session.GetString("JWToken");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var updateDto = new FlightUpdateDto
            {
                Id = model.Id,
                OriginId = model.OriginId,
                DestinationId = model.DestinationId,
                DepartureTime = model.DepartureTime,
                ArrivalTime = model.ArrivalTime,
                Price = model.Price,
                ApprovalStatus = model.ApprovalStatus
            };

            var response = await client.PutAsJsonAsync($"api/flights/{id}", updateDto);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Voo atualizado com sucesso!";
                return RedirectToAction("List");
            }

            ModelState.AddModelError("", "Erro ao atualizar voo.");
            await LoadCities(client, model);

            return View(model);
        }
        private async Task LoadCities(HttpClient client, FlightViewModel model)
        {
            var citiesResponse = await client.GetAsync("api/cities");

            if (citiesResponse.IsSuccessStatusCode)
            {
                var cities = await citiesResponse.Content.ReadFromJsonAsync<List<CityDto>>();

                ViewBag.OriginId = new SelectList(cities, "Id", "Name", model?.OriginId);
                ViewBag.DestinationId = new SelectList(cities, "Id", "Name", model?.DestinationId);
            }
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