using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using Booking.web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;
using System.Text.Json;

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

            //if (response.IsSuccessStatusCode)
            //{ActivatorUtilitiesConstructorAttribute 
            //    TempData["Message"] = "Registo efetuado! Por favor faça login.";
            //    return RedirectToAction("Login");
            //}

            ModelState.AddModelError("", "Falha no registo. Tente um email diferente.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ConfirmEmailCode(RegisterModel model)
        {
            TempData["RegisterModel"] = JsonSerializer.Serialize(model);
            TempData["Email"] = model.Email;
            TempData.Keep("Email");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendCodeConfirm(string Email)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/send-code", new VerifyCodeDTO { Email = Email });

            if (!response.IsSuccessStatusCode)
            {
                //le o conteúdo da resposta de erro
                var content = await response.Content.ReadAsStringAsync();
                string message;
                // desserializa o JSON { "message": "..." }
                var apiResponse = JsonSerializer
                    .Deserialize<ApiMessage>(content);
                ModelState.AddModelError("", apiResponse?.Message ?? "Falha ao enviar código.");
            }
            return View("ConfirmEmailCode", new VerifyCodeDTO { Email = Email });
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmEmailCode(VerifyCodeDTO model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/verify-code", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Code failed.");
                return View("ConfirmEmailCode", new VerifyCodeDTO { Email = model.Email });
            }
            var registerModelJson = TempData["RegisterModel"] as string;
            RegisterModel registerModel = JsonSerializer.Deserialize<RegisterModel>(registerModelJson ?? "{}") ?? new RegisterModel();
            return await Register(registerModel); //Chama o método de registo após a confirmação do código
        }

        [HttpGet]
        public async Task<IActionResult> VerifyCode() => View();

        [HttpPost]
        public async Task<IActionResult> VerifyCode(VerifyCodeDTO model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/verify-code", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Code failed.");
                return View("VerifyCode", new VerifyCodeDTO { Email = model.Email });
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ResendCode(string Email)
        {
            var client = _clientFactory.CreateClient("Booking.API");
            var response = await client.PostAsJsonAsync("api/Email/verify-code", new VerifyCodeDTO { Email = Email });

            if (!response.IsSuccessStatusCode)
            {
                //le o conteúdo da resposta de erro
                var content = await response.Content.ReadAsStringAsync();
                string message;
                // desserializa o JSON { "message": "..." }
                var apiResponse = JsonSerializer
                    .Deserialize<ApiMessage>(content);
                ModelState.AddModelError("", apiResponse?.Message ?? "Falha ao enviar código.");
            }
            return View("VerifyCode", new VerifyCodeDTO { Email = Email });
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