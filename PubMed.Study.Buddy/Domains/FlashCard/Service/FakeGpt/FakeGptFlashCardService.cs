using PubMed.Study.Buddy.Domains.FlashCard.Database;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Service.FakeGpt;

public class FakeGptFlashCardService(IFlashCardDatabase database) : IFlashCardService
{
    public async Task<List<Card>> GetFlashCardSet(ArticleSet articleSet)
    {
        var dbCards = await LoadCards();

        var cards = new List<Card>();

        foreach (var article in articleSet.Articles)
        {
            cards.AddRange(GetFlashCardsForArticle(dbCards, article, 1));
        }

        return cards;
    }

    private List<Card> GetFlashCardsForArticle(Dictionary<string, List<Card>> cardsByArticleId, Article article, int numberOfCardsToGet)
    {
        // if there are no flashcards in the DB, load em up from the service
        if (!cardsByArticleId.ContainsKey(article.Id))
        {
            var cards = GenerateFlashCardsForArticle(article, numberOfCardsToGet);
            return cards;
        }

        // otherwise, load all available cards from the DB
        var existingCards = cardsByArticleId[article.Id];

        if (existingCards.Count == numberOfCardsToGet) return existingCards;
        if (existingCards.Count > numberOfCardsToGet)
        {
            var r = new Random();
            return existingCards.OrderBy(x => r.Next()).Take(numberOfCardsToGet).ToList();
        }

        // create some if the existing ones aren't enough
        existingCards.AddRange(GenerateFlashCardsForArticle(article, numberOfCardsToGet - existingCards.Count));
        return existingCards;
    }

    private List<Card> GenerateFlashCardsForArticle(Article article, int numberOfCards)
    {
        var cards = new List<Card>();
        for (var i = 0; i < numberOfCards; i++)
        {
            cards.Add(new Card
            {
                Id = Guid.NewGuid(),
                Question = $"Question #{i} for article {article.Id}",
                Answer = $"Answer #{i} for article {article.Id}",
                ArticleId = article.Id
            });
        }

        database.AddFlashCards(cards);
        return cards;
    }

    private async Task<Dictionary<string, List<Card>>> LoadCards()
    {
        var cardSet = await database.LoadFlashCards();

        var cardsByArticleId = new Dictionary<string, List<Card>>();

        foreach (var card in cardSet)
        {
            var articleId = card.ArticleId;
            if (!cardsByArticleId.ContainsKey(articleId))
                cardsByArticleId.Add(articleId, new List<Card>());

            cardsByArticleId[articleId].Add(card);
        }

        return cardsByArticleId;
    }
}