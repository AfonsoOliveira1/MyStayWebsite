using System.Diagnostics;
using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Booking.web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                TopFlights = new List<FlightViewModel>(),
                TopStays = new List<HousingViewModel>()
            };

            try
            {
                var client = _clientFactory.CreateClient("Booking.API");
                client.Timeout = TimeSpan.FromSeconds(5); // evita bloqueio longo

                var flightsResponse = await client.GetAsync("api/Flights");
                if (flightsResponse.IsSuccessStatusCode)
                {
                    model.TopFlights = await flightsResponse.Content.ReadFromJsonAsync<List<FlightViewModel>>()
                                       ?? new List<FlightViewModel>();
                }

                var staysResponse = await client.GetAsync("api/Housing");
                if (staysResponse.IsSuccessStatusCode)
                {
                    model.TopStays = await staysResponse.Content.ReadFromJsonAsync<List<HousingViewModel>>()
                                     ?? new List<HousingViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao ligar à API no Index: " + ex.Message);
                ViewBag.Error = "De momento, não conseguimos carregar as ofertas. Tente mais tarde.";
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}