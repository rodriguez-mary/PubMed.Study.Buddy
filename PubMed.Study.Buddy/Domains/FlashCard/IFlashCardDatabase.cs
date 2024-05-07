using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard;

public interface IFlashCardDatabase
{
    Task<List<Card>> LoadFlashCards();

    Task SaveFlashCards(List<Card> cards);
}