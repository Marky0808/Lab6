namespace Lab6;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
    

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Console.WriteLine("=== Telegram Bot Backend: DeepL Перекладач ===");
            Console.Write("Введіть ваш DeepL API Key: ");
            string apiKey = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("API ключ не може бути порожнім. Вихід.");
                return;
            }

            var deepLService = new DeepLService(apiKey);

            while (true)
            {
                Console.WriteLine("\nОберіть дію:");
                Console.WriteLine("1. Показати доступні мови для перекладу (з обробкою даних)");
                Console.WriteLine("2. Перекласти текст");
                Console.WriteLine("3. Перевірити статистику використання (ліміти)");
                Console.WriteLine("0. Вихід");
                Console.Write("Ваш вибір: ");

                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await ShowLanguagesAsync(deepLService);
                            break;
                        case "2":
                            await TranslateUserTextAsync(deepLService);
                            break;
                        case "3":
                            await ShowUsageAsync(deepLService);
                            break;
                        case "0":
                            return;
                        default:
                            Console.WriteLine("Невірний вибір. Спробуйте ще раз.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[ПОМИЛКА]: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        private static async Task ShowLanguagesAsync(DeepLService service)
        {
            Console.WriteLine("\nОтримуємо список мов...");
            var languages = await service.GetSupportedLanguagesAsync();
            
            var filteredLanguages = languages.Where(l => l.SupportsFormality).ToList();
            
            var sortedLanguages = filteredLanguages.OrderBy(l => l.Name).ToList();

            Console.WriteLine($"Знайдено {sortedLanguages.Count} мов (відфільтровано за підтримкою формальності та відсортовано):");
            foreach (var lang in sortedLanguages)
            {
                Console.WriteLine($"- {lang.Name} (Код: {lang.Code})");
            }
        }

        private static async Task TranslateUserTextAsync(DeepLService service)
        {
            Console.Write("\nВведіть текст для перекладу: ");
            string text = Console.ReadLine();

            Console.Write("Введіть код мови, на яку перекласти (наприклад, UK, EN-US, DE, PL): ");
            string targetLang = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLang))
            {
                Console.WriteLine("Текст та код мови не можуть бути порожніми.");
                return;
            }

            Console.WriteLine("Виконуємо переклад...");
            var result = await service.TranslateTextAsync(text, targetLang);

            var translation = result.Translations.FirstOrDefault();
            if (translation != null)
            {
                Console.WriteLine($"\nРозпізнана мова оригіналу: {translation.DetectedSourceLanguage}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Результат: {translation.Text}");
                Console.ResetColor();
            }
        }

        private static async Task ShowUsageAsync(DeepLService service)
        {
            Console.WriteLine("\nОтримуємо статистику...");
            var stats = await service.GetUsageStatsAsync();
            
            double percentageUsed = (double)stats.CharacterCount / stats.CharacterLimit * 100;

            Console.WriteLine($"Використано символів: {stats.CharacterCount} з {stats.CharacterLimit}");
            Console.WriteLine($"Відсоток використання: {Math.Round(percentageUsed, 2)}%");
            
            if (percentageUsed > 80)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Увага: Ви наближаєтесь до ліміту вашого тарифу!");
                Console.ResetColor();
            }
        }
    }
}