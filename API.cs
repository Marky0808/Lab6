namespace Lab6;
using System.Collections.Generic;
using System.Text.Json.Serialization;


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
}