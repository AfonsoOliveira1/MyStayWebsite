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

    

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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

                //   Voos da base de dados
                var flightsResponse = await client.GetAsync("api/Flights");
                if (flightsResponse.IsSuccessStatusCode)
                {
                    model.TopFlights = await flightsResponse.Content.ReadFromJsonAsync<List<FlightViewModel>>() ?? new();
                }

                //  Estadias base de dados
                var staysResponse = await client.GetAsync("api/Housing");
                if (staysResponse.IsSuccessStatusCode)
                {
                    model.TopStays = await staysResponse.Content.ReadFromJsonAsync<List<HousingViewModel>>() ?? new();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Não foi possível carregar os dados da API.";
            }

            
            return View(model);
        }
    }
}
