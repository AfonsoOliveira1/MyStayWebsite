using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace Booking.Web.Controllers
{
    public class HousingController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public HousingController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> List(decimal? minPrice, decimal? maxPrice, int? cityId)
        {
            var client = _clientFactory.CreateClient("Booking.API");

            var housings = await client.GetFromJsonAsync<List<HousingViewModel>>("api/Housings")
                           ?? new List<HousingViewModel>();

            var response = await client.GetAsync("api/Cities");
            IEnumerable<CityViewModel> cities = new List<CityViewModel>();

            if (response.IsSuccessStatusCode)
            {
                cities = await response.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                         ?? new List<CityViewModel>();
            }

            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept", cityId);

            if (minPrice.HasValue)
                housings = housings.Where(h => h.PricePerNight >= minPrice).ToList();

            if (maxPrice.HasValue)
                housings = housings.Where(h => h.PricePerNight <= maxPrice).ToList();

            if (cityId.HasValue)
                housings = housings.Where(h => h.CityId == cityId).ToList();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_HousingCards", housings);

            return View(housings);
        }

        [Authorize(Roles = "RENTER,ADMIN")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.GetAsync("api/Cities");

            IEnumerable<CityViewModel> cities = new List<CityViewModel>();

            if (response.IsSuccessStatusCode)
            {
                cities = await response.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                         ?? new List<CityViewModel>();
            }

            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept");

            return View("Create");
        }

        [Authorize(Roles = "RENTER,ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HousingCreateDto model)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("api/Housings", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Alojamento enviado para aprovação!";
                return RedirectToAction("List");
            }

            var citiesResponse = await client.GetAsync("api/Cities");
            var cities = citiesResponse.IsSuccessStatusCode
                ? await citiesResponse.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>()
                : new List<CityViewModel>();

            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Reserve(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var housing = await client.GetFromJsonAsync<HousingViewModel>("api/Housings/" + id);
            if (housing == null) return NotFound();

            var model = new HousingBookingViewModel
            {
                HousingId = housing.Id,
                CustomerId = int.Parse(userIdStr),
                HousingName = housing.Name,
                PricePerNight = housing.PricePerNight,
                CheckInDate = DateTime.Now,
                CheckOutDate = DateTime.Now.AddDays(1),

                Ratings = housing.Ratings?.Select(r => new HousingRatingViewModel
                {
                    Score = r.Score,
                    Comment = r.Comment,
                    CustomerName = r.CustomerName
                }).ToList() ?? new List<HousingRatingViewModel>()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetBookedDates(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.GetAsync("api/HousingBookings/occupied-dates/" + id);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }
            return BadRequest();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReservation(HousingBookingViewModel model)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var checkResponse = await client.GetAsync("api/HousingBookings/occupied-dates/" + model.HousingId);
            if (checkResponse.IsSuccessStatusCode)
            {
                var occupied = await checkResponse.Content.ReadFromJsonAsync<List<OccupiedRangeDto>>();
                var checkIn = DateOnly.FromDateTime(model.CheckInDate);
                var checkOut = DateOnly.FromDateTime(model.CheckOutDate);

                bool isOverlapping = occupied?.Any(o =>
                    (checkIn >= o.from && checkIn < o.to) ||
                    (checkOut > o.from && checkOut <= o.to) ||
                    (checkIn <= o.from && checkOut >= o.to)
                ) ?? false;

                if (isOverlapping)
                {
                    ModelState.AddModelError("", "As datas selecionadas já não estão disponíveis. Por favor, escolha outro período.");
                    return View("Reserve", model);
                }
            }

            var bookingData = new
            {
                Housingid = model.HousingId,
                Customerid = model.CustomerId,
                Checkindate = model.CheckInDate,
                Checkoutdate = model.CheckOutDate
            };

            var response = await client.PostAsJsonAsync("api/HousingBookings", bookingData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Reserva guardada na base de dados!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine("Erro da API: " + errorBody);

                ModelState.AddModelError("", "A API rejeitou a reserva. Verifique se as datas estão disponíveis.");
                return View("Reserve", model);
            }
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> PendingApprovals()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Housings/pending");
            var housings = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<HousingViewModel>>()
                : new List<HousingViewModel>();

            return View(housings);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id, decimal commissionRate, string description)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var approvalData = new { CommissionRate = commissionRate, Description = description };
            var response = await client.PostAsJsonAsync("api/Housings/" + id + "/approve", approvalData);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Alojamento aprovado!";
            else
                TempData["Error"] = "Erro: " + response.StatusCode;

            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Reject(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync("api/Housings/" + id + "/reject", null);
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Alojamento rejeitado!";
            else
                TempData["Error"] = "Erro ao rejeitar alojamento na API.";

            return RedirectToAction("PendingApprovals");
        }

        [Authorize(Roles = "RENTER")]
        public async Task<IActionResult> MyHousings()
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Housings/MyHousings");
            var housings = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<HousingViewModel>>()
                : new List<HousingViewModel>();

            return View(housings);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var housing = await client.GetFromJsonAsync<HousingViewModel>("api/Housings/" + id);
            if (housing == null) return NotFound();

            var citiesResponse = await client.GetAsync("api/Cities");
            var cities = await citiesResponse.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>() ?? new List<CityViewModel>();
            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept", housing.CityId);

            return View(housing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HousingViewModel model)
        {
            ModelState.Remove("HousingRatingViewModel");
            ModelState.Remove("CityName");
            ModelState.Remove("ApprovalStatus");

            var client = _clientFactory.CreateClient("Booking.API");
            var token = User.FindFirst("JWToken")?.Value;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var updateDto = new
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                PricePerNight = model.PricePerNight,
                CityId = model.CityId,
                ImageUrl = model.ImageUrl,
                CompanyId = model.CompanyId,
                IsAvailable = model.IsAvailable
            };

            var response = await client.PutAsJsonAsync("api/Housings/" + model.Id, updateDto);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Alojamento atualizado com sucesso!";
                return RedirectToAction("MyHousings", "Housing");
            }

            var citiesResponse = await client.GetAsync("api/Cities");
            var cities = await citiesResponse.Content.ReadFromJsonAsync<IEnumerable<CityViewModel>>() ?? new List<CityViewModel>();
            ViewBag.Cities = new SelectList(cities, "Id", "Citynamept", model.CityId);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarRating(int housingId, int score, string comment)
        {
            if (housingId <= 0) return RedirectToAction("MyBookings", "Bookings");

            var client = _clientFactory.CreateClient("Booking.API");
            var token = await HttpContext.GetTokenAsync("access_token") ?? User.FindFirst("JWToken")?.Value;
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var responseReservas = await client.GetAsync("api/HousingBookings/my-bookings/" + userIdClaim);
            if (responseReservas.IsSuccessStatusCode)
            {
                var reservas = await responseReservas.Content.ReadFromJsonAsync<List<HousingBookingViewModel>>();
                DateOnly hoje = DateOnly.FromDateTime(DateTime.Now);

                var podeAvaliar = reservas?.Any(r => r.HousingId == housingId && DateOnly.FromDateTime(r.CheckInDate) <= hoje) ?? false;
                if (!podeAvaliar) return RedirectToAction("MyBookings", "Bookings");
            }

            var ratingData = new { housingid = housingId, customerid = int.Parse(userIdClaim), score = score, comment = comment };
            await client.PostAsJsonAsync("api/HousingRatings", ratingData);

            return RedirectToAction("MyBookings", "Bookings");
        }
    }
}