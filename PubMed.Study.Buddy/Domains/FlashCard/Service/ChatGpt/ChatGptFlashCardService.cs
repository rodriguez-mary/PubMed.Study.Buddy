﻿using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard.Service.ChatGpt;

public class ChatGptFlashCardService : IFlashCardService
{
    // cluster data

    // generate a set of flashcards proportional to each articles impact score in its cluster
    // the impact score should be normed in the cluster--so if there is only one article in the cluster
    // it should get a lot of flashcards even if the impact score is low
    public Task<List<Card>> GetFlashCardSet(ArticleSet articles)
    {
        throw new NotImplementedException();
    }
}