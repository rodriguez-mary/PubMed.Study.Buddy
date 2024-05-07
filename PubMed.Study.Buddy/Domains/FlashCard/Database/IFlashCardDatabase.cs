using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Database;

public interface IFlashCardDatabase
{
    Task<List<Card>> LoadFlashCards();

    Task SaveFlashCards(List<Card> cards);
}