using Booking.web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Booking.web.Controllers
{
    public class ChatController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public ChatController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IActionResult Index()
        {
            return View(new ChatViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> EnviarMensagem(ChatViewModel model)
        {
            var client = _clientFactory.CreateClient("Booking.API");

            
            var response = await client.PostAsJsonAsync("api/ai/chat", model.Mensagem);

            if (response.IsSuccessStatusCode)
            {
                
                var resultado = await response.Content.ReadFromJsonAsync<ChatViewModel>();

                model.Resposta = resultado?.Resposta ?? "A IA não devolveu conteúdo.";
            }
            else
            {
                model.Resposta = "Erro: Não foi possível obter resposta da API.";
            }

            return View("Index", model);
        }
    }
}