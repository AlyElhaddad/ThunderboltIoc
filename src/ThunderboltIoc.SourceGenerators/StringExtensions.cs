namespace ThunderboltIoc.SourceGenerators;

public static class StringExtensions
{
    public static string RemovePrefix(this string str, string prefix)
    {
        if (string.IsNullOrEmpty(str) || !str.StartsWith(prefix))
            return str;
        return str.Substring(prefix.Length);
    }
}
