using System.Text;

namespace LongRest.ConsoleApp;

public static class CommandLine
{
    public static string[] Split(string text)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        var quoted = false;
        foreach (var character in text)
        {
            if (character == '"') quoted = !quoted;
            else if (char.IsWhiteSpace(character) && !quoted)
            {
                if (word.Length > 0) { words.Add(word.ToString()); word.Clear(); }
            }
            else word.Append(character);
        }
        if (quoted) throw new ArgumentException("Close the double quote at the end of the name.");
        if (word.Length > 0) words.Add(word.ToString());
        return words.ToArray();
    }
}
