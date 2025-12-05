using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherClient.Services
{
    public static class OpenRouterService
    {
        private static readonly HttpClient _http = new();
        private const string _key = "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";
        private const string _url = "https://openrouter.ai/api/v1/chat/completions";

        //public static async Task<string> GetStyleAdviceAsync(StyleAdviceRequest req)
        //{
        //    if (string.IsNullOrWhiteSpace(req.City))
        //        return "Город не выбран";

        //    if (req.Temp < -60 || req.Temp > 60)
        //        return "Странная температура – проверь данные";

        //    var prompt = $"{req.City}, {req.Temp:F0}°C, ветер {req.WindSpeed:F0} м/с, {req.Description}, " +
        //                 $"{req.Gender}, {req.Age} лет, стиль {req.Style}.";

        //    var body = new
        //    {
        //        model = "deepseek/deepseek-chat-v3-0324",
        //        messages = new[]
        //        {
        //            new { role = "system", content = "Ты стилист. Дай короткий совет (2-3 предложения), что надеть сегодня. Учти: температуру, ветер, пол, возраст, стиль." },
        //            new { role = "user",   content = prompt }
        //        },
        //        max_tokens = 120,
        //        temperature = 0.7
        //    };

        //    _http.DefaultRequestHeaders.Clear();
        //    _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");
        //    _http.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/XNeedX/WeatherClient");
        //    _http.DefaultRequestHeaders.Add("X-Title", "WeatherClient");

        //    var resp = await _http.PostAsJsonAsync(_url, body);
        //    if (!resp.IsSuccessStatusCode) return "Совет недоступен 😔";

        //    using var stream = await resp.Content.ReadAsStreamAsync();
        //    var root = await JsonSerializer.DeserializeAsync<OrChatResponse>(stream);
        //    return root?.Choices?[0]?.Message?.Content?.Trim() ?? "Нет совета";
        //}
        public static async Task<string> AskWeatherStyleAsync(StyleAdviceRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.City))
                return "Сначала выберите город 🙂";

            using var http = new HttpClient();

            // 1. Тело запроса 1:1 вашему Qt-коду + Pop
            var body = new
            {
                model = "deepseek/deepseek-v3.2",
                messages = new[]
                {
            new { role = "system", content = "Ты стилист. Дай короткий совет (2-3 предложения), что надеть сегодня. Учти: температуру, ветер, пол, возраст, стиль, ВЕРОЯТНОСТЬ осадков (%)." },
            new { role = "user",   content = $"{req.City}, {req.Temp:0}°C, ветер {req.WindSpeed:0} м/с, {req.Description}, ВЕРОЯТНОСТЬ осадков {(req.Pop * 100):0}%, {req.Gender}, {req.Age} лет, стиль {req.Style}." }
        },
                max_tokens = 200,
                temperature = 0.4
            };

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 2. Headers
            http.DefaultRequestHeaders.Clear();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");
            http.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/XNeedX/WeatherClient");
            http.DefaultRequestHeaders.Add("X-Title", "WeatherClient");

            try
            {
                using var resp = await http.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
                Debug.WriteLine($"[HTTP] {resp.StatusCode}");

                // 3. Сырой JSON для отладки
                var raw = await resp.Content.ReadAsStringAsync();
                Debug.WriteLine($"[RAW]  {raw}");

                if (string.IsNullOrWhiteSpace(raw))
                    return "Сервер вернул пустое тело";

                // 4. Ручная десериализация (100 % надёжно)
                using var doc = JsonDocument.Parse(raw);

                if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    return "Сервер не вернул выбор";

                var choice = choices[0];
                if (!choice.TryGetProperty("message", out var message))
                    return "Нет поля message";

                if (!message.TryGetProperty("content", out var contentProp))
                    return "Нет поля content";

                var text = contentProp.GetString();
                return string.IsNullOrWhiteSpace(text) ? "Совет пустой" : text.Trim();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return "Лимит исчерпан, попробуй позже.";
            }
            catch (Exception ex)
            {
                return $"Сетевая ошибка: {ex.Message}";
            }
        }

        private class OrChatResponse
        {
            public Choice[] Choices { get; set; }
        }

        private class Choice
        {
            public Message Message { get; set; }
        }

        private class Message
        {
            public string Content { get; set; }
        }
    }
}