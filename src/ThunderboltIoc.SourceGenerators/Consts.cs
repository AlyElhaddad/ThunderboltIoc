namespace ThunderboltIoc.SourceGenerators;

internal static class Consts
{
    internal const string mainNs = "ThunderboltIoc";
    internal const string global = "global::";
    internal const string @public = "public";
    internal const string @override = "override";

    internal const string IocContainerTypeFullName = $"{global}{mainNs}.IocContainer";
    internal const string IIocRegistrarTypeFullName = $"{global}{mainNs}.IIocRegistrar";
    internal const string RegisterMethodName = "Register";

    internal const string includeAttrName = $"{global}{mainNs}.IocIncludeAttribute";
    internal const string excludeAttrName = $"{global}{mainNs}.IocExcludeAttribute";
    internal const string regexIncludeAttrName = $"{global}{mainNs}.RegexIncludeAttribute";
    internal const string regexExcludeAttrName = $"{global}{mainNs}.RegexExcludeAttribute";
}
