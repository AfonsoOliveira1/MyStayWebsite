using Booking.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Booking.web.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AdminController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // metodo aux para config o cliente com o token
        private HttpClient GetAuthorizedClient()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        [HttpGet]
        public async Task<IActionResult> Revenue()
        {
            var client = GetAuthorizedClient();

            var response = await client.GetAsync("api/Users/dashboard/finance");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var summary = await response.Content.ReadFromJsonAsync<RevenueSummaryViewModel>(options);
                return View(summary);
            }

            TempData["ErrorMessage"] = "Não foi possível carregar os dados financeiros.";
            return View(new RevenueSummaryViewModel());
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> FinancialDashboard()
        {
            return await Revenue();
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public IActionResult EditCommission(int id)
        {
            return View(id);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> UpdateCommission(int id, decimal newRate)
        {
            var client = GetAuthorizedClient();
            var url = "api/Housings/" + id + "/update-commission";

            var response = await client.PatchAsJsonAsync(url, newRate);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Comissão atualizada com sucesso!";
                return RedirectToAction("List", "Housing");
            }

            TempData["ErrorMessage"] = "Erro ao atualizar a comissão na API.";
            return RedirectToAction("EditCommission", new { id = id });
        }
    }
}