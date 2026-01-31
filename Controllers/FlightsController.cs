using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;

namespace Booking.Web.Controllers
{
    public class FlightController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly int _dummyPassengerId = 1;

        public FlightController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // GET: Flight/List
        public async Task<IActionResult> List()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var flights = await client.GetFromJsonAsync<List<FlightViewModel>>("api/flights");
            return View(flights);
        }

        // GET: Flight/Create
        // Este método prepara o formulário e carrega os dados necessários da API
        public async Task<IActionResult> Create()
        {
            var client = _clientFactory.CreateClient("Booking.API");

            // Carregamos as origens, destinos e utilizadores para as dropdowns
            var origins = await client.GetFromJsonAsync<List<OriginViewModel>>("api/origins") ?? new List<OriginViewModel>();
            var destinations = await client.GetFromJsonAsync<List<DestinationViewModel>>("api/destinations") ?? new List<DestinationViewModel>();
            var users = await client.GetFromJsonAsync<List<UserViewModel>>("api/users") ?? new List<UserViewModel>();

            // Usamos SelectList para facilitar a criação do <select> na View
            ViewBag.OriginId = new SelectList(origins, "Id", "CityName");
            ViewBag.DestinationId = new SelectList(destinations, "Id", "CityName");
            ViewBag.AirlineId = new SelectList(users, "Id", "Name");

            return View();
        }

        // POST: Flight/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlightViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");

            // Enviamos o novo voo para a API, que o guardará na BD
            var response = await client.PostAsJsonAsync("api/flights", model);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(List));

            ModelState.AddModelError("", "Erro ao comunicar com a API para criar o voo.");
            return View(model);
        }

        // GET: Flight/Reserve/5
        public async Task<IActionResult> Reserve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var flight = await client.GetFromJsonAsync<FlightViewModel>("api/flights/" + id);

            if (flight == null) return NotFound();

            return View(flight);
        }

        // POST: Flight/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int Id, int SeatId)
        {
            var booking = new FlightBookingViewModel
            {
                FlightId = Id,
                SeatId = SeatId,
                PassengerId = _dummyPassengerId
            };

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/flightbooking", booking);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(List));

            TempData["Error"] = "Não foi possível reservar este lugar.";
            return RedirectToAction(nameof(Reserve), new { id = Id });
        }
    }
}