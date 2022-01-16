namespace ThunderboltIoc;

/// <summary>
/// The entry point to ThunderboltIoc where we may attach registrations and access the singleton container.
/// </summary>
public static class ThunderboltActivator
{
    private static ThunderboltContainer? container;
    /// <summary>
    /// A singleton <see cref="IThunderboltContainer"/> that may be accessed after attaching at least one registration.
    /// </summary>
    public static IThunderboltContainer Container => container ?? throw new InvalidOperationException($"Cannot use the container before attaching at least one registration. To attach a registration, after creating its corresponding class, call '{nameof(ThunderboltActivator)}.{nameof(Attach)}'.");

    /// <summary>
    /// A short-hand for <see cref="Container.Get{T}()"/>.
    /// </summary>
    public static T Get<T>() where T : notnull => Container.Get<T>();

    /// <summary>
    /// A short-hand for <see cref="Container.CreateScope()"/>.
    /// </summary>
    public static IThunderboltScope CreateScope() => Container.CreateScope();

    /// <summary>
    /// Attached the specified <see cref="ThunderboltRegistration"/> to the singleton container.
    /// </summary>
    public static void Attach<TRegistration>()
        where TRegistration : notnull, ThunderboltRegistration, new()
        => (container ??= new()).Attach<TRegistration>();
}
