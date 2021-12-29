namespace ThunderboltIoc.SourceGenerators;

internal static class Consts
{
    internal const string mainNs = nameof(ThunderboltIoc);
    internal const string containerClass = nameof(ThunderboltContainer);
    internal const string factoryClass = "ThunderboltFactory";
    internal const string global = "global::";
    internal const string @protected = "protected";
    internal const string @override = "override";

    internal const string IocContainerTypeFullName = $"{global}{mainNs}.{containerClass}";
    internal const string IIocRegistrarTypeFullName = $"{global}{mainNs}.IThunderboltRegistrar";
    internal const string RegisterMethodName = "Register";

    internal const string includeAttrName = $"{global}{mainNs}.ThunderboltIncludeAttribute";
    internal const string excludeAttrName = $"{global}{mainNs}.ThunderboltExcludeAttribute";
    internal const string regexIncludeAttrName = $"{global}{mainNs}.ThunderboltRegexIncludeAttribute";
    internal const string regexExcludeAttrName = $"{global}{mainNs}.ThunderboltRegexExcludeAttribute";
}
