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

        
        public async Task<IActionResult> List()
        {
            try
            {
                var client = _clientFactory.CreateClient("Booking.API");
                var flights = await client.GetFromJsonAsync<List<FlightViewModel>>("api/Flights");
                return View(flights ?? new List<FlightViewModel>());
            }
            catch
            {
                return View(new List<FlightViewModel>());
            }
        }

       
        [HttpGet]
        public async Task<IActionResult> Reserve(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
           
            var flight = await client.GetFromJsonAsync<FlightViewModel>($"api/Flights/{id}");

            if (flight == null) return NotFound();

            return View(flight);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = _clientFactory.CreateClient("Booking.API");

            // Carregar Cidades 
            var cities = await client.GetFromJsonAsync<List<CityViewModel>>("api/Cities")
                         ?? new List<CityViewModel>();

            //  Carregar Users 
            var users = await client.GetFromJsonAsync<List<UserViewModel>>("api/Users")
                        ?? new List<UserViewModel>();

            // Preencher SelectLists 
            ViewBag.OriginId = new SelectList(cities, "Id", "CityName");
            ViewBag.DestinationId = new SelectList(cities, "Id", "CityName");
            ViewBag.AirlineId = new SelectList(users, "Id", "Name");

            return View(new FlightViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlightViewModel model)
        {
            var client = _clientFactory.CreateClient("Booking.API");

            if (ModelState.IsValid)
            {
                var response = await client.PostAsJsonAsync("api/Flights", model);
                if (response.IsSuccessStatusCode) return RedirectToAction(nameof(List));

                ModelState.AddModelError("", "Erro ao gravar na API.");
            }

            // carregar as listas de novo se der erro em cima 
            var cities = await client.GetFromJsonAsync<List<CityViewModel>>("api/Cities") ?? new List<CityViewModel>();
            var users = await client.GetFromJsonAsync<List<UserViewModel>>("api/Users") ?? new List<UserViewModel>();

            ViewBag.OriginId = new SelectList(cities, "Id", "CityName");
            ViewBag.DestinationId = new SelectList(cities, "Id", "CityName");
            ViewBag.AirlineId = new SelectList(users, "Id", "Name");

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int Id, int SeatId)
        {
            var bookingData = new
            {
                Flightid = Id,
                Seatid = SeatId,
                Passengerid = _dummyPassengerId
            };

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/FlightBookings", bookingData);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(List));

            TempData["Error"] = "Não foi possível realizar a reserva.";
            return RedirectToAction(nameof(List)); // Redirecionar para a lista
        }

        
    }
}