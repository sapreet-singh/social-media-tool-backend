using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace social_media_tool_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WhatsappController : ControllerBase
    {
        const string _metaVerifyToken= "aiagentwhatsapp";

        private static ConcurrentDictionary<string, bool> knownUsers = new();

        [HttpGet]
        public IActionResult VerifyWebhook([FromQuery(Name = "hub.mode")] string hubMode, [FromQuery(Name = "hub.challenge")] string hubChallenge,
            [FromQuery(Name = "hub.verify_token")] string hubVerifyToken)
        { 
            const string verifyToken = "aiagentwhatsapp "; // Match this with token set on Meta dashboard

            if (hubMode == "subscribe" && hubVerifyToken == _metaVerifyToken)
            {
                return Ok(hubChallenge);
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveMessage([FromBody] JsonElement payload)
        {
            try
            {
                var json = payload.ToString();
                Console.WriteLine("Incoming webhook payload: " + json);

                var entry = payload.GetProperty("entry")[0];
                var changes = entry.GetProperty("changes")[0];
                var value = changes.GetProperty("value");
                var messages = value.GetProperty("messages");

                if (messages.ValueKind != JsonValueKind.Undefined)
                {
                    var message = messages[0];
                    var from = message.GetProperty("from").GetString();
                    var text = message.GetProperty("text").GetProperty("body").GetString();

                    Console.WriteLine($"Message from {from}: {text}");

                    // Auto-reply logic
                    //if (!knownUsers.ContainsKey(from))
                    //{
                    //    knownUsers[from] = true;
                        await SendAutoReply(from); // Send reply
                    //}
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return BadRequest();
            }
        }

        private async Task SendAutoReply(string to)
        {
            using var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.facebook.com/v19.0/609072248947492/messages");
            request.Headers.Add("Authorization", "Bearer EAAaVweMdZBSEBO3ojscSm8uUoZBv4ae2ABt15WSw7iQfCCOFmRCkKfyMD4XPEXSziuKGK1kAZBgeZArhpBsqbIX5l3xyuMoAuR7nSZC4qJcSCFPH0SwIebYXNmevanTeF8frkLkGtL4siFWnwZABiw6ZCvX3BLwZCnvLJSr06eJ9SfP0lI6dZCZCp7SFXgPZAVC0SLOgTwWp8GtPO3ZA54W0mZBqtcezvZAAUnXBvTKzZAGfPGsZBk8WO1Oh1GeA2xkUABi72gZDZD");

            var messageBody = new
            {
                messaging_product = "whatsapp",
                to = to,
                text = new { body = "Hi! Thanks for messaging us. This is an automated reply." }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(messageBody), System.Text.Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Reply sent: " + result);
        }
    }
}
