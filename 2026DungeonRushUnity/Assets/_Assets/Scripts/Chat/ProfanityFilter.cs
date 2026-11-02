using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

// Lọc từ tục trước khi gửi chat (class rp gốc): regex \b(từ1|từ2|...)\b không phân biệt hoa thường, mỗi chữ i/o/t/a/e/l/s
// nhận cả ký tự lách (1 ! | 0 7 @ 4 3 $ 5), từ khớp thay bằng "***". Server lọc lại bằng cùng danh sách (chat.ts).
public static class ProfanityFilter
{
    private const string REPLACEMENT = "***";

    private static readonly HashSet<string> WORDS = new HashSet<string>
    {
        "fuck", "fucker", "fuckers", "fucking", "fucked", "fucks", "motherfucker", "motherfucking", "shit", "shits", "shitty", "shitting",
        "bullshit", "ass", "asshole", "assholes", "asses", "bitch", "bitches", "bitchy", "bitching", "damn", "damned", "damnit", "goddamn",
        "goddamnit", "hell", "dick", "dicks", "dickhead", "dickheads", "cock", "cocks", "cocksucker", "cocksuckers", "pussy", "pussies", "cunt",
        "cunts", "bastard", "bastards", "slut", "sluts", "slutty", "whore", "whores", "piss", "pissed", "pissing", "crap", "crappy", "tits",
        "titty", "titties", "boob", "boobs", "anus", "dildo", "dildos", "fag", "fags", "faggot", "faggots", "nigger", "niggers", "nigga",
        "niggas", "retard", "retards", "retarded", "wanker", "wankers", "wank", "twat", "twats", "porn", "porno", "pornography", "penis",
        "vagina", "jerk", "jerks", "jerkoff", "douche", "douchebag", "douchebags", "spic", "spics", "chink", "chinks", "kike", "kikes",
        "wetback", "wetbacks", "beaner", "beaners", "nazi", "nazis", "rape", "raped", "raping", "rapist", "molest", "molester", "pedophile",
        "pedo", "pedos", "stfu", "gtfo", "lmfao", "wtf", "milf",
    };

    private static Regex regex;

    // irf gốc.
    public static string Filter(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        return GetRegex().Replace(text, REPLACEMENT);
    }

    // ire gốc.
    public static bool Contains(string text)
    {
        return string.IsNullOrEmpty(text) == false && GetRegex().IsMatch(text);
    }

    // irc gốc: dựng 1 lần.
    private static Regex GetRegex()
    {
        if (regex != null)
        {
            return regex;
        }

        StringBuilder sb = new StringBuilder("\\b(");
        bool first = true;
        foreach (string word in WORDS)
        {
            if (!first)
            {
                sb.Append('|');
            }
            sb.Append(ToPattern(word));
            first = false;
        }
        sb.Append(")\\b");
        regex = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        return regex;
    }

    // ird gốc.
    private static string ToPattern(string word)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in word)
        {
            switch (c)
            {
                case 'i': sb.Append("[i1!|]"); break;
                case 'o': sb.Append("[o0]"); break;
                case 't': sb.Append("[t7]"); break;
                case 'a': sb.Append("[a@4]"); break;
                case 'e': sb.Append("[e3]"); break;
                case 'l': sb.Append("[l1|]"); break;
                case 's': sb.Append("[s$5]"); break;
                default: sb.Append(Regex.Escape(c.ToString())); break;
            }
        }
        return sb.ToString();
    }
}
