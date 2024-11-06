using System.Text.Json.Serialization;

namespace PubMed.Study.Buddy.Domains.FlashCard.Service.OpenAI.DTOs;

internal class FlashCardResponse
{
    [JsonInclude]
    [JsonPropertyName("flashcard")]
    public FlashCard FlashCard { get; set; } = new();
}

internal class FlashCard
{
    [JsonInclude]
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;
}