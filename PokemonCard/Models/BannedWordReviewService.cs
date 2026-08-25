using Microsoft.EntityFrameworkCore;

namespace PokemonCard.Models;

public class BannedWordReviewService
{
    private readonly PicartchuContext _context;

    public BannedWordReviewService(PicartchuContext context)
    {
        _context = context;
    }

    public async Task<List<BannedWordReviewResult>> ReviewAsync(Dictionary<string, string?> fields)
    {
        var enabledBannedWords = await _context.BannedWords
            .Where(bannedWord => bannedWord.IsEnabled)
            .Select(bannedWord => bannedWord.BannedWords.Trim())
            .Where(word => word != string.Empty)
            .ToListAsync();

        var results = new List<BannedWordReviewResult>();

        foreach (var (fieldDisplayName, fieldValue) in fields)
        {
            if (string.IsNullOrWhiteSpace(fieldValue))
            {
                continue;
            }

            foreach (var bannedWord in enabledBannedWords)
            {
                if (fieldValue.Contains(bannedWord, StringComparison.CurrentCultureIgnoreCase))
                {
                    results.Add(new BannedWordReviewResult
                    {
                        FieldDisplayName = fieldDisplayName,
                        MatchedWord = bannedWord,
                        OriginalText = fieldValue
                    });
                }
            }
        }

        return results;
    }
}

public class BannedWordReviewResult
{
    public string FieldDisplayName { get; set; } = string.Empty;

    public string MatchedWord { get; set; } = string.Empty;

    public string OriginalText { get; set; } = string.Empty;
}
