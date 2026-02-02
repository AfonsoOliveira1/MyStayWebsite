using Microsoft.AspNetCore.Mvc;
using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Net.Http.Json;

namespace Booking.web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AccountController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/users/login", model);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

                if (loginResponse != null && loginResponse.User != null)
                {
                    var claims = new List<Claim>
            {
                // Agora o compilador já reconhece o .Id porque usamos UserViewModel
                new Claim(ClaimTypes.NameIdentifier, loginResponse.User.Id.ToString()),
                new Claim(ClaimTypes.Name, loginResponse.User.Name ?? loginResponse.User.Email),
                new Claim(ClaimTypes.Email, loginResponse.User.Email),
                new Claim(ClaimTypes.Role, loginResponse.User.Role),
                new Claim("JWToken", loginResponse.Token)
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity)
                    );

                    TempData["Message"] = "Welcome back!";
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Email ou password incorretos.");
            return View(model);
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");

            
            var response = await client.PostAsJsonAsync("api/users", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Registration failed. Try a different email.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }

    
}