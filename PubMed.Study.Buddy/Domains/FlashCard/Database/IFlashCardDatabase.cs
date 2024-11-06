using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Database;

public interface IFlashCardDatabase
{
    Task<List<Card>> LoadFlashCards();

    List<Card> RetrieveCardsForArticle(string articleId);

    Task AddFlashCards(List<Card> flashCards);

    Task SaveFlashCards();
}