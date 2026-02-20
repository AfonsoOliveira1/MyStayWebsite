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

        [HttpGet]
        public async Task<IActionResult> Revenue()
        {
            return await GetFinancialData();
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> FinancialDashboard()
        {
            return await GetFinancialData();
        }
        private async Task<IActionResult> GetFinancialData()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;

            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Users/dashboard/finance");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var summary = await response.Content.ReadFromJsonAsync<RevenueSummaryViewModel>(options);
                return View("Revenue", summary);
            }

            TempData["Error"] = "Erro ao carregar dados da API.";
            return View("Revenue", new RevenueSummaryViewModel());
        }
    }
}