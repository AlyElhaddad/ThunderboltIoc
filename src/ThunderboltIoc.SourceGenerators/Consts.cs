namespace ThunderboltIoc.SourceGenerators;

internal static class Consts
{
    internal const string mainNs = "ThunderboltIoc";
    internal const string registrationClass = "ThunderboltRegistration";
    internal const string activatorClass = "ThunderboltActivator";
    internal const string registrarInterface = "IThunderboltRegistrar";
    internal const string global = "global::";
    internal const string partial = "partial";
    internal const string @protected = "protected";
    internal const string @override = "override";

    internal const string Singleton = "Singleton";
    internal const string Scoped = "Scoped";
    internal const string Transient = "Transient";

    internal const string RegistrationTypeFullName = $"{global}{mainNs}.{registrationClass}";
    internal const string ActivatorTypeFullName = $"{global}{mainNs}.{activatorClass}";
    internal const string IIocRegistrarTypeFullName = $"{global}{mainNs}.{registrarInterface}";
    internal const string IIocDictatorTypeFullName = $"{global}{mainNs}.IThunderboltFactoryDictator";
    internal const string AttachMethodName = "Attach";
    internal const string RegisterMethodName = "Register";
    internal const string FactorySuffix = "Factory";

    internal const string includeAttrName = $"{global}{mainNs}.ThunderboltIncludeAttribute";
    internal const string excludeAttrName = $"{global}{mainNs}.ThunderboltExcludeAttribute";
    internal const string regexIncludeAttrName = $"{global}{mainNs}.ThunderboltRegexIncludeAttribute";
    internal const string regexExcludeAttrName = $"{global}{mainNs}.ThunderboltRegexExcludeAttribute";
}
