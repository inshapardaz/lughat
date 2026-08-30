using System.Text;

namespace Lughat.Engine.Api.Search;

/// <summary>
/// The classic Porter stemming algorithm (M.F. Porter, 1980) for English. A direct, from-scratch
/// implementation of the published five-step rule set — no external stemming library, so it
/// carries no extra dependency and stays trivially auditable against the original paper.
/// </summary>
public sealed class EnglishPorterStemmer : IStemmer
{
    public string LanguageCode => "en";

    public string Stem(string token)
    {
        var word = token.ToLowerInvariant();
        if (word.Length <= 2)
        {
            return word;
        }

        word = Step1A(word);
        word = Step1B(word);
        word = Step1C(word);
        word = Step2(word);
        word = Step3(word);
        word = Step4(word);
        word = Step5A(word);
        word = Step5B(word);
        return word;
    }

    // --- character classification -----------------------------------------------------

    private static bool IsConsonant(string w, int i)
    {
        var c = w[i];
        if (c is 'a' or 'e' or 'i' or 'o' or 'u')
        {
            return false;
        }

        if (c != 'y')
        {
            return true;
        }

        return i == 0 || !IsConsonant(w, i - 1);
    }

    /// <summary>The "measure" m: the number of consonant-sequence → vowel-sequence transitions.</summary>
    private static int Measure(string stem)
    {
        var m = 0;
        var i = 0;
        var n = stem.Length;

        while (i < n && IsConsonant(stem, i))
        {
            i++;
        }

        while (i < n)
        {
            while (i < n && !IsConsonant(stem, i))
            {
                i++;
            }

            if (i >= n)
            {
                break;
            }

            m++;
            while (i < n && IsConsonant(stem, i))
            {
                i++;
            }
        }

        return m;
    }

    private static bool ContainsVowel(string stem)
    {
        for (var i = 0; i < stem.Length; i++)
        {
            if (!IsConsonant(stem, i))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EndsWithDoubleConsonant(string w)
    {
        var n = w.Length;
        return n >= 2 && w[n - 1] == w[n - 2] && IsConsonant(w, n - 1);
    }

    /// <summary>Ends consonant-vowel-consonant, where the second consonant isn't w/x/y.</summary>
    private static bool EndsWithCvc(string w)
    {
        var n = w.Length;
        if (n < 3)
        {
            return false;
        }

        return IsConsonant(w, n - 3) && !IsConsonant(w, n - 2) && IsConsonant(w, n - 1)
            && w[n - 1] is not ('w' or 'x' or 'y');
    }

    private static bool TryReplaceSuffix(string w, string suffix, out string stem)
    {
        if (w.Length > suffix.Length && w.EndsWith(suffix, StringComparison.Ordinal))
        {
            stem = w[..^suffix.Length];
            return true;
        }

        stem = w;
        return false;
    }

    // --- steps ---------------------------------------------------------------------------

    private static string Step1A(string w)
    {
        if (w.EndsWith("sses", StringComparison.Ordinal))
        {
            return w[..^2];
        }

        if (w.EndsWith("ies", StringComparison.Ordinal))
        {
            return w[..^2];
        }

        if (w.EndsWith("ss", StringComparison.Ordinal))
        {
            return w;
        }

        if (w.EndsWith('s') && w.Length > 1)
        {
            return w[..^1];
        }

        return w;
    }

    private static string Step1B(string w)
    {
        string stem;
        if (TryReplaceSuffix(w, "eed", out stem))
        {
            return Measure(stem) > 0 ? stem + "ee" : w;
        }

        bool applied = false;
        if (TryReplaceSuffix(w, "ed", out stem) && ContainsVowel(stem))
        {
            w = stem;
            applied = true;
        }
        else if (TryReplaceSuffix(w, "ing", out stem) && ContainsVowel(stem))
        {
            w = stem;
            applied = true;
        }

        if (!applied)
        {
            return w;
        }

        if (w.EndsWith("at", StringComparison.Ordinal) || w.EndsWith("bl", StringComparison.Ordinal) || w.EndsWith("iz", StringComparison.Ordinal))
        {
            return w + "e";
        }

        if (EndsWithDoubleConsonant(w) && !w.EndsWith('l') && !w.EndsWith('s') && !w.EndsWith('z'))
        {
            return w[..^1];
        }

        if (Measure(w) == 1 && EndsWithCvc(w))
        {
            return w + "e";
        }

        return w;
    }

    private static string Step1C(string w)
    {
        if ((w.EndsWith('y') || w.EndsWith("Y", StringComparison.Ordinal)) && w.Length > 1 && ContainsVowel(w[..^1]))
        {
            return w[..^1] + "i";
        }

        return w;
    }

    private static readonly (string Suffix, string Replacement)[] Step2Rules =
    [
        ("ational", "ate"), ("tional", "tion"), ("enci", "ence"), ("anci", "ance"), ("izer", "ize"),
        ("abli", "able"), ("alli", "al"), ("entli", "ent"), ("eli", "e"), ("ousli", "ous"),
        ("ization", "ize"), ("ation", "ate"), ("ator", "ate"), ("alism", "al"), ("iveness", "ive"),
        ("fulness", "ful"), ("ousness", "ous"), ("aliti", "al"), ("iviti", "ive"), ("biliti", "ble"),
    ];

    private static string Step2(string w)
    {
        foreach (var (suffix, replacement) in Step2Rules)
        {
            if (TryReplaceSuffix(w, suffix, out var stem) && Measure(stem) > 0)
            {
                return stem + replacement;
            }
        }

        return w;
    }

    private static readonly (string Suffix, string Replacement)[] Step3Rules =
    [
        ("icate", "ic"), ("ative", ""), ("alize", "al"), ("iciti", "ic"), ("ical", "ic"), ("ful", ""), ("ness", ""),
    ];

    private static string Step3(string w)
    {
        foreach (var (suffix, replacement) in Step3Rules)
        {
            if (TryReplaceSuffix(w, suffix, out var stem) && Measure(stem) > 0)
            {
                return stem + replacement;
            }
        }

        return w;
    }

    private static readonly string[] Step4Suffixes =
    [
        "al", "ance", "ence", "er", "ic", "able", "ible", "ant", "ement", "ment", "ent",
        "ou", "ism", "ate", "iti", "ous", "ive", "ize",
    ];

    private static string Step4(string w)
    {
        if (TryReplaceSuffix(w, "ion", out var ionStem) && ionStem.Length > 0 && ionStem[^1] is 's' or 't' && Measure(ionStem) > 1)
        {
            return ionStem;
        }

        foreach (var suffix in Step4Suffixes)
        {
            if (TryReplaceSuffix(w, suffix, out var stem) && Measure(stem) > 1)
            {
                return stem;
            }
        }

        return w;
    }

    private static string Step5A(string w)
    {
        if (!w.EndsWith('e'))
        {
            return w;
        }

        var stem = w[..^1];
        var m = Measure(stem);
        if (m > 1 || (m == 1 && !EndsWithCvc(stem)))
        {
            return stem;
        }

        return w;
    }

    private static string Step5B(string w) =>
        Measure(w) > 1 && EndsWithDoubleConsonant(w) && w.EndsWith('l') ? w[..^1] : w;
}
