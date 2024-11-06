using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Threads;
using PubMed.Study.Buddy.Domains.FlashCard.Database;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Service.ChatGpt;

public class OpenAIFlashCardService : IFlashCardService
{
    private readonly string _apiKey;
    private readonly string _assistantId;
    private readonly ILogger<OpenAIFlashCardService> _logger;
    private readonly IFlashCardDatabase _flashCardDatabase;

    #region constructor

    public OpenAIFlashCardService(ILogger<OpenAIFlashCardService> logger, IConfiguration configuration, IFlashCardDatabase flashCardDatabase)
    {
        _apiKey = configuration["openAiApiKey"] ?? string.Empty;
        _assistantId = configuration["openAiAssistantId"] ?? string.Empty;
        _logger = logger;
        _flashCardDatabase = flashCardDatabase;
    }

    #endregion constructor

    public async Task<List<Card>> GetFlashCardSet(ArticleSet articleSet)
    {
        using var api = new OpenAIClient(_apiKey);

        var assistant = await api.AssistantsEndpoint.RetrieveAssistantAsync(_assistantId);

        var cards = new List<Card>();
        foreach (var article in articleSet.Articles)
        {
            try
            {
                // only generate cards if we don't have any in the database
                var existingCards = _flashCardDatabase.RetrieveCardsForArticle(article.Id);
                if (existingCards.Count > 0)
                {
                    cards.AddRange(existingCards);
                }
                else
                {
                    /* TODO uncomment when you're feeling good about this
                    var card = await GenerateFlashCard(assistant, api, article);
                    cards.Add(card);
                    await _flashCardDatabase.AddFlashCards(new List<Card> { card });*/
                }
            }
            catch (FlashCardGenerationException)
            {
                _logger.LogError("Failed to generate a flashcard for article {ArticleId}", article.Id);
            }
        }

        return cards;
    }

    private async Task<Card> GenerateFlashCard(AssistantResponse assistant, OpenAIClient api, Article article)
    {
        var thread = await api.ThreadsEndpoint.CreateThreadAsync();

        var request = Utilities.ArticleToRequest(article);
        var prompt = JsonConvert.SerializeObject(request);

        var message = await thread.CreateMessageAsync(prompt);

        var run = await thread.CreateRunAsync(assistant);
        Thread.Sleep(500);
        var runId = run.Id;
        Thread.Sleep(500);
        var listRunStep = await run.ListRunStepsAsync();

        FlashCardResponse? flashCardResponse = null;

        foreach (var item in listRunStep.Items)
        {
            var runStep = await run.RetrieveRunStepAsync(item.Id);
            runStep = await runStep.UpdateAsync();
            while (runStep.Status != RunStatus.Completed)
            {
                runStep = await runStep.UpdateAsync();
            }

            var messageList = await api.ThreadsEndpoint.ListMessagesAsync(thread);
            foreach (var itemMessage in messageList.Items)
            {
                var messageContent = itemMessage.PrintContent();
                if (messageContent.Equals(prompt))
                    continue;

                flashCardResponse = JsonConvert.DeserializeObject<FlashCardResponse>(messageContent);
            }
        }

        if (flashCardResponse != null)
        {
            return new Card
            {
                ArticleId = article.Id,
                Question = flashCardResponse.FlashCard.Question,
                Answer = flashCardResponse.FlashCard.Answer,
                Id = Guid.NewGuid()
            };
        }
        else
        {
            throw new FlashCardGenerationException();
        }
    }
}