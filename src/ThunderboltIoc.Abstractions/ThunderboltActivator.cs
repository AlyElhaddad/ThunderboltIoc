namespace ThunderboltIoc;

public static class ThunderboltActivator
{
    private static ThunderboltContainer? container;
    public static IThunderboltContainer Container => container ?? throw new InvalidOperationException($"Cannot use the container before attaching at least one registration. To attach a registration, after creating its  corresponding class, call '{nameof(ThunderboltActivator)}.{nameof(Attach)}'.");

    public static T Get<T>() => Container.Get<T>();

    public static void Attach<TRegistration>()
        where TRegistration : notnull, ThunderboltRegistration, new()
    {
        if (container is null)
            container = new();
        container.Attach<TRegistration>();
    }
}
