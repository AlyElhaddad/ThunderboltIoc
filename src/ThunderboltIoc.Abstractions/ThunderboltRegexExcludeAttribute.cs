namespace ThunderboltIoc;

/// <summary>
/// Excludes all the matched types from the attribute registration process.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public class ThunderboltRegexExcludeAttribute : Attribute
{
    /// <param name="regex">The regular expressions pattern used to look up service types.</param>
    public ThunderboltRegexExcludeAttribute(string regex)
    {
        Regex = regex;
    }

    /// <summary>
    /// The regular expressions pattern used to look up service types.
    /// </summary>
    public string Regex { get; }
}
