namespace Thunderbolt.GeneratorAbstractions;

internal static class Consts
{
    #region Prefix/Suffix

    internal const string global = "global::";
    internal const string FactorySuffix = "Factory";

    #endregion


    #region Keywords

    internal const string partial = "partial";
    internal const string @protected = "protected";
    internal const string @override = "override";

    #endregion


    #region Namespaces

    internal const string mainNs = "ThunderboltIoc";
    internal const string extNs = "Thunderbolt.Extensions";
    internal const string system = nameof(System);

    #endregion


    #region Interfaces

    internal const string containerInteface = "IThunderboltContainer";
    internal const string IContainerTypeFullName = $"{global}{mainNs}.{containerInteface}";

    internal const string scopeInteface = "IThunderboltScope";
    internal const string IScopeTypeFullName = $"{global}{mainNs}.{scopeInteface}";

    internal const string resolverInteface = "IThunderboltResolver";
    internal const string IResolverTypeFullName = $"{global}{mainNs}.{resolverInteface}";

    internal const string serviceProviderInteface = nameof(IServiceProvider);
    internal const string IServiceProviderTypeFullName = $"{global}{system}.{serviceProviderInteface}";

    internal const string registrarInterface = "IThunderboltRegistrar";
    internal const string IRegistrarTypeFullName = $"{global}{mainNs}.{registrarInterface}";

    internal const string dictatorInterface = "IThunderboltFactoryDictator";
    internal const string IDictatorTypeFullName = $"{global}{mainNs}.{dictatorInterface}";

    #endregion


    #region Classes

    internal const string registrationClass = "ThunderboltRegistration";
    internal const string RegistrationTypeFullName = $"{global}{mainNs}.{registrationClass}";
    
    internal const string msRegistrationClass = "ThunderboltMsRegistration";
    internal const string MsRegistrationTypeFullName = $"{global}{mainNs}.{msRegistrationClass}";

    internal const string activatorClass = "ThunderboltActivator";
    internal const string ActivatorTypeFullName = $"{global}{mainNs}.{activatorClass}";

    internal const string extensionsClass = "ThunderboltExtensions";
    internal const string ExtensionsTypeFullName = $"{global}{extNs}.{extensionsClass}";

    #endregion

    #region Enums

    internal const string serviceLifetimeEnum = "ThunderboltServiceLifetime";
    internal const string serviceLifetimeEnumFullName = $"{global}{mainNs}.{serviceLifetimeEnum}";

    #endregion

    #region Attributes

    internal const string includeAttrName = $"{global}{mainNs}.ThunderboltIncludeAttribute";
    internal const string excludeAttrName = $"{global}{mainNs}.ThunderboltExcludeAttribute";
    internal const string regexIncludeAttrName = $"{global}{mainNs}.ThunderboltRegexIncludeAttribute";
    internal const string regexExcludeAttrName = $"{global}{mainNs}.ThunderboltRegexExcludeAttribute";

    #endregion


    #region Members

    internal const string Singleton = "Singleton";
    internal const string Scoped = "Scoped";
    internal const string Transient = "Transient";

    internal const string AttachMethodName = "Attach";
    internal const string UseMethodName = "UseThunderbolt";
    internal const string RegisterMethodName = "Register";

    #endregion


    #region Values

    internal const int SingletonValue = 0;
    internal const int ScopedValue = 1;
    internal const int TransientValue = 2;

    #endregion
}
