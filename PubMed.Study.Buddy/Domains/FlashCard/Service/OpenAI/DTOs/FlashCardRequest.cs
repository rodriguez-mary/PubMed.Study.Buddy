using System.Text.Json.Serialization;

namespace PubMed.Study.Buddy.Domains.FlashCard.Service.OpenAI.DTOs;

internal class FlashCardRequest
{
    [JsonPropertyName("prompt")]
    public Prompt Prompt { get; set; } = new();
}

internal class Prompt
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("journal-year")]
    public string JournalYear { get; set; } = string.Empty;

    [JsonPropertyName("journal")]
    public string Journal { get; set; } = string.Empty;

    [JsonPropertyName("abstract")]
    public string Abstract { get; set; } = string.Empty;
}