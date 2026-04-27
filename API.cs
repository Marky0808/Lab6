namespace Lab6;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class API
{
    public class Language
    {
        [JsonPropertyName("language")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("supports_formality")]
        public bool SupportsFormality { get; set; }
    }

    public class TranslationRequest
    {
        [JsonPropertyName("text")]
        public List<string> Text { get; set; }

        [JsonPropertyName("target_lang")]
        public string TargetLang { get; set; }
    }

    public class TranslationResponse
    {
        [JsonPropertyName("translations")]
        public List<Translation> Translations { get; set; }
    }

    public class Translation
    {
        [JsonPropertyName("detected_source_language")]
        public string DetectedSourceLanguage { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class UsageStats
    {
        [JsonPropertyName("character_count")]
        public int CharacterCount { get; set; }

        [JsonPropertyName("character_limit")]
        public int CharacterLimit { get; set; }
    }

    public class DeepLService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api-free.deepl.com/v2";

        public DeepLService(string apiKey)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {apiKey}");
        }
        
        public async Task<List<Language>> GetSupportedLanguagesAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/languages?type=target");
            return await ProcessResponseAsync<List<Language>>(response);
        }
        
        public async Task<TranslationResponse> TranslateTextAsync(string text, string targetLang)
        {
            var requestBody = new TranslationRequest
            {
                Text = new List<string> { text },
                TargetLang = targetLang.ToUpper()
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/translate", jsonContent);
            
            return await ProcessResponseAsync<TranslationResponse>(response);
        }
        
        public async Task<UsageStats> GetUsageStatsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/usage");
            return await ProcessResponseAsync<UsageStats>(response);
        }
        
        private async Task<T> ProcessResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Forbidden => "Помилка авторизації: Невірний API ключ.",
                    System.Net.HttpStatusCode.BadRequest => "Некоректний запит: Перевірте введені параметри.",
                    System.Net.HttpStatusCode.TooManyRequests => "Перевищено ліміт запитів до API.",
                    _ => $"Невідома помилка API: {response.StatusCode} - {content}"
                };
                throw new Exception(errorMessage);
            }

            return JsonSerializer.Deserialize<T>(content);
        }
    }
}