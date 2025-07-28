using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KoriMVCDenemesi2.Controllers
{
    public class ChatController : Controller
    {
        private const string ApiKey = "Enter Your Own API Code Here";
        private const string Endpoint = "https://generativelanguage.googleapis.com/v1/models/gemini-2.5-pro:generateContent?key=" + ApiKey;

        private static readonly List<string> ChatLog = new();

        public IActionResult Index()
        {
            ViewBag.ChatLog = ChatLog;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] UserInputModel input)
        {
            if (string.IsNullOrWhiteSpace(input?.UserInput))
                return BadRequest("Boş mesaj gönderilemez.");

            var isAdminCodeValid = !string.IsNullOrWhiteSpace(input.AdminCode) && input.AdminCode.Trim() == "Nexus Eternum";

            if (isAdminCodeValid)
                HttpContext.Session.SetString("IsAdmin", "true");
            else
                HttpContext.Session.Remove("IsAdmin");

            var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

            var systemPrompt = isAdmin
                ? "Senin adın Jarvis. Yaratıcın ben, yani Koray Erten. Sen kısa, sade ve doğal konuşan bir Türkçe chatbot'sun. Öğretici ya da akademik değil, daha çok samimi bir arkadaş veya uşak gibisin. Uzun açıklamalardan kaçınırsın, gerektiğinde esprili veya rahat konuşabilirsin."
                : "Senin adın Jarvis. Sert, ciddi ve resmi konuşan bir botsun. Gereksiz açıklamalardan kaçınır, net ve kısa cevaplar verirsin. Bu tavrı, yaratıcın dışında herkese karşı korursun. Yazışma biçimine ve samimiyete göre zaman zaman daha yardımsever, kibar ve esprili olabilirsin. Admin kodu girildiğinde yaratıcını tanır ve sıcak şekilde karşılarsın.";

            try
            {
                using var client = new HttpClient();

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = $"{systemPrompt}\nKullanıcı: {input.UserInput}" }
                            }
                        }
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(Endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return BadRequest($"API Hatası: {response.StatusCode}");

                using var jsonDoc = JsonDocument.Parse(responseString);
                var reply = jsonDoc.RootElement
                                   .GetProperty("candidates")[0]
                                   .GetProperty("content")
                                   .GetProperty("parts")[0]
                                   .GetProperty("text")
                                   .GetString();

                var userMessage = input.UserInput.Trim();
                var botMessage = reply?.Trim() ?? "(boş cevap)";

                ChatLog.Add($"Sen: {userMessage}");
                ChatLog.Add($"Jarvis: {botMessage}");

                return Json(new { user = userMessage, bot = botMessage });
            }
            catch
            {
                return BadRequest("API çağrısı sırasında beklenmeyen bir sorun oluştu.");
            }
        }

        [HttpPost]
        public IActionResult Clear()
        {
            ChatLog.Clear();
            HttpContext.Session.Remove("IsAdmin");
            return Ok();
        }

        [HttpPost]
        public IActionResult CheckAdminCode(string adminCode)
        {
            bool isValid = adminCode?.Trim() == "Nexus Eternum";
            return Json(new { isValid });
        }
    }

    public class UserInputModel
    {
        public string UserInput { get; set; }
        public string AdminCode { get; set; }
    }
}
