namespace ThunderboltIoc;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public class ThunderboltRegexExcludeAttribute : Attribute
{
    public ThunderboltRegexExcludeAttribute(string regex)
    {
        Regex = regex;
    }
    public string Regex { get; }
}
