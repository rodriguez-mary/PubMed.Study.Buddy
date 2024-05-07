using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Service;

public interface IFlashCardService
{
    Task<List<Card>> GetFlashCardSet(ArticleSet articles);
}