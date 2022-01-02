namespace ThunderboltIoc;

/// <summary>
/// Includes the matched servcies in the attribute registration process.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public class ThunderboltRegexIncludeAttribute : Attribute
{
    /// <param name="serviceLifetime">The <see cref="ThunderboltServiceLifetime"/> used to register the matched service(s).</param>
    /// <param name="regex">The regular expressions pattern used to look up service types.</param>
    /// <param name="implRegex">The regular expressions pattern used to look up service implementation types.</param>
    /// <param name="joinKeyRegex">The regular expressions pattern used to match keys that map services with their corresponding implementations.</param>
    public ThunderboltRegexIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, string regex, string implRegex, string joinKeyRegex)
    {
        ServiceLifetime = serviceLifetime;
        Regex = regex;
        ImplRegex = implRegex;
        JoinKeyRegex = joinKeyRegex;
    }

    /// <param name="serviceLifetime">The <see cref="ThunderboltServiceLifetime"/> used to register the matched service(s).</param>
    /// <param name="regex">The regular expressions pattern used to look up service types.</param>
    public ThunderboltRegexIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, string regex)
    {
        ServiceLifetime = serviceLifetime;
        Regex = regex;
        ImplRegex = null;
        JoinKeyRegex = null;
    }
    /// <summary>
    /// The <see cref="ThunderboltServiceLifetime"/> used to register the matched service(s).
    /// </summary>
    public ThunderboltServiceLifetime ServiceLifetime { get; }
    /// <summary>
    /// The regular expressions pattern used to look up service types.
    /// </summary>
    public string Regex { get; }
    /// <summary>
    /// (optional) The regular expressions pattern used to look up service implementation types.
    /// </summary>
    public string? ImplRegex { get; }
    /// <summary>
    /// (optional) The regular expressions pattern used to match keys that map services with their corresponding implementations.
    /// </summary>
    public string? JoinKeyRegex { get; }
}
