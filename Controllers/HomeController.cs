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

            ViewBag.UserName = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                : null;

            try
            {
                var client = _clientFactory.CreateClient("Booking.API");

                var flightsResponse = await client.GetAsync("api/Flights");
                if (flightsResponse.IsSuccessStatusCode)
                {
                    var allFlights = await flightsResponse.Content.ReadFromJsonAsync<List<FlightViewModel>>();
                    model.TopFlights = allFlights?.Take(3).ToList() ?? new List<FlightViewModel>();
                }

                
                var staysResponse = await client.GetAsync("api/Housings/top-rated");
                if (staysResponse.IsSuccessStatusCode)
                {
                    model.TopStays = await staysResponse.Content.ReadFromJsonAsync<List<HousingViewModel>>()
                                     ?? new List<HousingViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao carregar destaques: " + ex.Message);
            }

            return View(model);
        }

        public IActionResult Privacy() => View();

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}