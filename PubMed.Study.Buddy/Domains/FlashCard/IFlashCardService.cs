using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard;

internal interface IFlashCardService
{
    Task GenerateFlashCards(List<Article> articles);
}