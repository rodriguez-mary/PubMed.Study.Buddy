using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Export;

public interface IFlashCardExport
{
    Task<(Stream, string extension)> CreateExport(List<CardSet> sets);
}