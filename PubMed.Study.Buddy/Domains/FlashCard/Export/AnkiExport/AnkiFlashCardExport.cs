using AnkiNet;
using PubMed.Study.Buddy.DTOs;
using System.Text;

namespace PubMed.Study.Buddy.Domains.FlashCard.Export.AnkiExport;

internal class AnkiFlashCardExport : IFlashCardExport
{
    public async Task<(Stream, string extension)> CreateExport(List<CardSet> sets)
    {
        var (collection, noteTypeId) = CreateCollection(CreateNoteType());

        foreach (var set in sets)
        {
            CreateDeck(set, collection, noteTypeId);
        }

        var memoryStream = new MemoryStream();
        await AnkiFileWriter.WriteToStreamAsync(memoryStream, collection);
        return (memoryStream, "apkg");
    }

    private void CreateDeck(CardSet set, AnkiCollection collection, long noteTypeId)
    {
        var deckId = collection.CreateDeck(set.Title);

        foreach (var card in set.Cards)
        {
            collection.CreateNote(deckId, noteTypeId, Format(card.Question), Format(card.Answer), PubMedArticleLink(card.ArticleId));
        }
    }

    private (AnkiCollection, long) CreateCollection(AnkiNoteType noteType)
    {
        var collection = new AnkiCollection();
        var noteTypeId = collection.CreateNoteType(noteType);

        return (collection, noteTypeId);
    }

    private static AnkiNoteType CreateNoteType()
    {
        var cardTypes = new[]
        {
            new AnkiCardType(
                "Forwards",
                0,
                "{{Front}}<br/><a href='{{Link}}'>PubMed</a>",
                "{{Front}}<br/><a href='{{Link}}'>PubMed</a><hr id=\"answer\">{{Back}}"
            ),
            new AnkiCardType(
                "Backwards",
                1,
                "{{Back}}<br/><a href='{{Link}}'>PubMed</a>",
                "{{Back}}<br/><a href='{{Link}}'>PubMed</a><hr id=\"answer\">{{Front}}"
            )
        };

        var noteType = new AnkiNoteType(
            "Basic (With hints)",
            cardTypes,
            new[] { "Front", "Back", "Link" }
        );

        return noteType;
    }

    private static string PubMedArticleLink(string articleId)
    {
        return $@"https://pubmed.ncbi.nlm.nih.gov/{articleId}/";
    }

    private static string Format(string value)
    {
        return value.Replace("'", "''").Replace("\n", "<br/>");
    }
}