﻿using PubMed.Study.Buddy.DTOs;

namespace PubMed.Study.Buddy.Domains.FlashCard;

public interface IFlashCardService
{
    Task<List<Card>> GetFlashCardSet(ArticleSet articles);
}