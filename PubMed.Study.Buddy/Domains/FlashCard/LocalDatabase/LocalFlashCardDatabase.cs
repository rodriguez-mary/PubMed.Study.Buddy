using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.LocalDatabase;

internal class LocalFlashCardDatabase(IAppCache cache, IConfiguration config) : IFlashCardDatabase
{
    private readonly string _filename = "flashcards";
    private readonly string _fileDirectory = config["localIoDirectory"] ?? Environment.CurrentDirectory;

    private readonly string _cacheKey = "LocalFlashCardDatabase_FlashCards";

    public Task<List<Card>> LoadFlashCards()
    {
        var path = Path.Combine(_fileDirectory, _filename);
        if (!File.Exists(path)) return Task.FromResult<List<Card>>([]);

        var json = File.ReadAllText(path);
        try
        {
            var cards = JsonConvert.DeserializeObject<List<Card>>(json) ?? [];
            cache.Add(_cacheKey, cards);

            return Task.FromResult(cards);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing file. {ex.Message}");
            return Task.FromResult<List<Card>>([]);
        }
    }

    public async Task SaveFlashCards(List<Card> cardsToSave)
    {
        if (!cache.TryGetValue<List<Card>>(_cacheKey, out var cachedCards))
            cachedCards = await LoadFlashCards();

        var cacheDirty = false;
        foreach (var card in cardsToSave)
        {
            // if the card is already saved, don't resave (this ain't an updated program)
            if (!cachedCards.Any(x => x.Id == card.Id)) continue;

            cachedCards.Add(card);
            // flag that the cache is now dirty
            cacheDirty = true;
        }

        if (!cacheDirty) return;

        // save to file
        SaveCardsToFile(cachedCards);
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