namespace ThunderboltIoc.SourceGenerators;

internal static class Consts
{
    internal const string mainNs = nameof(ThunderboltIoc);
    internal const string containerClass = nameof(ThunderboltContainer);
    internal const string factoryClass = nameof(ThunderboltFactory);
    internal const string activatorClass = nameof(ThunderboltActivator);
    internal const string registrarInterface = nameof(IThunderboltRegistrar);
    internal const string global = "global::";
    internal const string partial = "partial";
    internal const string @protected = "protected";
    internal const string @override = "override";

    internal const string ActivatorTypeFullName = $"{global}{mainNs}.{activatorClass}";
    internal const string IocContainerTypeFullName = $"{global}{mainNs}.{containerClass}";
    internal const string IIocRegistrarTypeFullName = $"{global}{mainNs}.{registrarInterface}";
    internal const string AttachMethodName = nameof(ThunderboltActivator.Attach);
    internal const string RegisterMethodName = nameof(ThunderboltRegistration.Register);
    internal const string FactorySuffix = "Factory";

    internal const string includeAttrName = $"{global}{mainNs}.{nameof(ThunderboltIncludeAttribute)}";
    internal const string excludeAttrName = $"{global}{mainNs}.{nameof(ThunderboltExcludeAttribute)}";
    internal const string regexIncludeAttrName = $"{global}{mainNs}.{nameof(ThunderboltRegexIncludeAttribute)}";
    internal const string regexExcludeAttrName = $"{global}{mainNs}.{nameof(ThunderboltRegexExcludeAttribute)}";
}
