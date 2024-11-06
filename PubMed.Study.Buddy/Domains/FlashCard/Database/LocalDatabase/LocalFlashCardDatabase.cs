using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Database.LocalDatabase;

internal class LocalFlashCardDatabase(IAppCache cache, IConfiguration config) : IFlashCardDatabase
{
    private static int callCount = 0;
    private static int triggerCount = 10;

    private readonly string _filename = "flashCards.json";
    private readonly string _fileDirectory = config["localIoDirectory"] ?? Environment.CurrentDirectory;

    private readonly string _cacheKey = "LocalFlashCardDatabase_FlashCards";

    private Dictionary<string, List<Card>> _cardsByArticle;

    public List<Card> RetrieveCardsForArticle(string articleId)
    {
        if (_cardsByArticle == null) LoadFlashCards();

        _cardsByArticle!.TryGetValue(articleId, out List<Card>? cards);

        return cards == null ? new List<Card>() : cards;
    }

    public Task<List<Card>> LoadFlashCards()
    {
        _cardsByArticle = new();
        var path = Path.Combine(_fileDirectory, _filename);
        if (!File.Exists(path)) return Task.FromResult<List<Card>>([]);

        var json = File.ReadAllText(path);
        try
        {
            var cards = JsonConvert.DeserializeObject<List<Card>>(json) ?? [];
            cache.Add(_cacheKey, cards);

            foreach (var card in cards)
            {
                if (!_cardsByArticle.ContainsKey(card.ArticleId)) _cardsByArticle.Add(card.ArticleId, new List<Card>());

                _cardsByArticle[card.ArticleId].Add(card);
            }

            return Task.FromResult(cards);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing file. {ex.Message}");
            return Task.FromResult<List<Card>>([]);
        }
    }

    public Task SaveFlashCards()
    {
        var cards = new List<Card>();

        foreach (var cardList in _cardsByArticle.Values)
            cards.AddRange(cardList);

        EnsureFilePathCreated();
        var path = Path.Combine(_fileDirectory, _filename);
        var json = JsonConvert.SerializeObject(cards, Formatting.Indented);
        File.WriteAllText(path, json);

        return Task.CompletedTask;
    }

    public async Task AddFlashCards(List<Card> cardsToSave)
    {
        callCount++;

        if (!cache.TryGetValue<List<Card>>(_cacheKey, out var cachedCards))
            cachedCards = await LoadFlashCards();

        var cacheDirty = false;
        foreach (var card in cardsToSave)
        {
            // if the card is already saved, don't resave (this ain't an update program)
            if (cachedCards.Any(x => x.Id == card.Id)) continue;

            cachedCards.Add(card);

            if (!_cardsByArticle.ContainsKey(card.ArticleId)) _cardsByArticle.Add(card.ArticleId, new List<Card>());
            _cardsByArticle[card.ArticleId].Add(card);

            // flag that the cache is now dirty
            cacheDirty = true;
        }

        if (!cacheDirty) return;

        // save to file
        if (callCount % triggerCount == 0) SaveCardsToFile(cachedCards);

        //update cache
        cache.Remove(_cacheKey);
        cache.Add(_cacheKey, cachedCards);
    }

    private void SaveCardsToFile(List<Card> cards)
    {
        EnsureFilePathCreated();
        var path = Path.Combine(_fileDirectory, _filename);
        var json = JsonConvert.SerializeObject(cards, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    private void EnsureFilePathCreated()
    {
        if (!Directory.Exists(_fileDirectory))
            Directory.CreateDirectory(_fileDirectory);
    }
}