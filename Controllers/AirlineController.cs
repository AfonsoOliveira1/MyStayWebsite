using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Booking.web.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AirlineController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AirlineController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AirlineViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");

            var token = await HttpContext.GetTokenAsync("access_token")
                        ?? User.FindFirst("JWToken")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

           
            var response = await client.PostAsJsonAsync("api/Airlines", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "A companhia " + model.AirlineName + " foi criada com sucesso!";
                return RedirectToAction("Create", "Flight");
            }

            var erro = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Erro ao guardar na API: " + erro);

            return View(model);
        }
    }
}
