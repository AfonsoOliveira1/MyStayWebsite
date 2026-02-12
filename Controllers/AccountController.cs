using System.Net.Http.Headers;
using System.Security.Claims;
using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

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

                    TempData["Message"] = "Bem-vindo de volta!";
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
            var response = await client.PostAsJsonAsync("api/users/register", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Registo efetuado! Por favor faça login.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Falha no registo. Tente um email diferente.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Profile()
        {
           
            var user = new UserUpdateDto
            {
                Id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                Name = User.Identity?.Name ?? "Utilizador",
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer"
            };
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ProfileEdit(UserUpdateDto profile, string? newpass, string? confirmpass)
        {
           
            if (!string.IsNullOrEmpty(newpass))
            {
                if (newpass != confirmpass)
                {
                    ModelState.AddModelError("", "A nova password e a confirmação não coincidem.");
                    return View("Profile", profile);
                }
                profile.Password = newpass;
            }

            // obter o id do user
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            profile.Id = int.Parse(userIdStr);

            // tokn JWT
            var token = User.FindFirst("JWToken")?.Value;
            var client = _clientFactory.CreateClient("Booking.API");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync("api/users/update", profile);

            if (response.IsSuccessStatusCode)
            {
               
                if (!string.IsNullOrEmpty(newpass))
                {
                    return RedirectToAction("Logout");
                }
                TempData["Message"] = "Perfil atualizado com sucesso!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Erro ao atualizar o perfil na API.");
            return View("Profile", profile);
        }
    }
}