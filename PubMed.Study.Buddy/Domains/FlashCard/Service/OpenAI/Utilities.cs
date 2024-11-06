using PubMed.Study.Buddy.Domains.FlashCard.Service.OpenAI.DTOs;
using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Service.ChatGpt;

internal static class Utilities
{
    public static FlashCardRequest ArticleToRequest(Article article)
    {
        return new FlashCardRequest
        {
            Prompt = new()
            {
                Abstract = article.Abstract,
                Author = FirstAuthorSurname(article),
                Title = article.Title,
                Journal = article.Publication == null ? string.Empty : article.Publication.JournalName,
                JournalYear = article.PublicationDate.Year.ToString()
            }
        };
    }

    private static string FirstAuthorSurname(Article article)
    {
        if (article.AuthorList == null || article.AuthorList.Count <= 0) return string.Empty;

        var firstAuthor = article.AuthorList.FirstOrDefault();

        return firstAuthor == null ? string.Empty : firstAuthor.LastName;
    }
}