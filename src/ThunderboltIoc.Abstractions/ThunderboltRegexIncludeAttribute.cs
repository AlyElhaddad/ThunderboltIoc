namespace ThunderboltIoc;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public class ThunderboltRegexIncludeAttribute : Attribute
{
    public ThunderboltRegexIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, string regex, string implRegex, string joinKeyRegex)
    {
        ServiceLifetime = serviceLifetime;
        Regex = regex;
        ImplRegex = implRegex;
        JoinKeyRegex = joinKeyRegex;
    }
    public ThunderboltRegexIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, string regex)
    {
        ServiceLifetime = serviceLifetime;
        Regex = regex;
        ImplRegex = null;
        JoinKeyRegex = null;
    }
    public ThunderboltServiceLifetime ServiceLifetime { get; }
    public string Regex { get; }
    public string? ImplRegex { get; }
    public string? JoinKeyRegex { get; }
}
