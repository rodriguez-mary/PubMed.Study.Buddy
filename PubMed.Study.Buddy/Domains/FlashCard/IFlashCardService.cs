using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard;

public interface IFlashCardService
{
    Task<FlashCardSet> GenerateFlashCards(ArticleSet articles);
}