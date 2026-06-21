using System.Text.Json;
using System.Text.RegularExpressions;
using AsterSupportAgent.Models;

namespace AsterSupportAgent.Services;

public interface IKbSearchService
{
    List<KbArticle> Search(string query, int topK = 2);
}

/// <summary>
/// Naive but effective keyword-overlap scoring.
/// A vecotr/embedding search would generalize better to paraphrased
/// queries, but adds a dependency, an indexing step, and embedding
/// API cost. For a KB this small (&lt;50 articles), keyword scoring
/// is cheap, deterministic, and zero-latency.
/// </summary>
public class KbSearchService : IKbSearchService
{
    private readonly List<KbArticle> _articles;

    private static readonly HashSet<string> StopWords =
    [
        "the",
        "a",
        "an",
        "is",
        "are",
        "was",
        "were",
        "do",
        "does",
        "did",
        "can",
        "could",
        "would",
        "should",
        "i",
        "my",
        "me",
        "you",
        "your",
        "to",
        "of",
        "in",
        "on",
        "for",
        "and",
        "or",
        "it",
        "this",
        "that",
        "what",
        "how",
        "when",
        "where",
        "will",
        "have",
        "has",
        "with",
    ];

    public KbSearchService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "articles.json");
        var json = File.ReadAllText(path);
        _articles =
            JsonSerializer.Deserialize<List<KbArticle>>(json, JsonOptions.CaseInsensitive) ?? [];
    }

    public List<KbArticle> Search(string query, int topK = 2)
    {
        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0)
            return [];

        var scored = _articles
            .Select(article =>
            {
                var articleTokens = Tokenize(article.Title + " " + article.Content);
                var haystackSet = articleTokens.ToHashSet();
                var titleTokens = Tokenize(article.Title).ToHashSet();

                double score = 0;
                foreach (var token in queryTokens)
                {
                    if (haystackSet.Contains(token))
                    {
                        score += 2;
                    }
                    else
                    {
                        var hasStemMatch = haystackSet.Any(t =>
                            t.Length >= 4
                            && token.Length >= 4
                            && (
                                t.StartsWith(token, StringComparison.Ordinal)
                                || token.StartsWith(t, StringComparison.Ordinal)
                            )
                        );
                        if (hasStemMatch)
                            score += 0.5; // Partial credit for stem matches
                    }

                    if (titleTokens.Contains(token))
                        score += 1.5; // Bonus for title matches
                }
                return (article, score);
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => x.article)
            .ToList();
        return scored;
    }

    private static List<string> Tokenize(string text)
    {
        var cleaned = Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9\\s]", "");
        return
        [
            .. cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !StopWords.Contains(w)),
        ];
    }
}
