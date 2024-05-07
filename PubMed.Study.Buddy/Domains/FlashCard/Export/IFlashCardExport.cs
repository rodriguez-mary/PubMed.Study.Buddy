using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Export;

public interface IFlashCardExport
{
    string CreateExport(List<CardSet> sets);
}