using Microsoft.CodeAnalysis;

using System.Text;

namespace Thunderbolt.GeneratorAbstractions;

internal struct RequiredField
{
    public RequiredField() : this(default, default, default)
    {
    }

    public RequiredField(bool type, bool ctor, bool factory)
    {
        Type = type;
        Ctor = ctor;
        Factory = factory;
    }

    public bool Type { get; set; }
    public bool Ctor { get; set; }
    public bool Factory { get; set; }
    public IList<(string propName, ServiceDescriptor service)> Props { get; } = new List<(string propName, ServiceDescriptor service)>();
}
internal static class GeneratorHelper
{
    #region required fields
    private static void AddTypeField(IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields, TypeDescriptor type, int serviceDefinitionNumber)
    {
        if (requiredFields.TryGetValue(type, out IDictionary<int, RequiredField>? requiredFieldDict))
        {
            if (requiredFieldDict.TryGetValue(serviceDefinitionNumber, out RequiredField requiredField))
            {
                requiredField = requiredField with { Type = true };
            }
            else
            {
                requiredField = new RequiredField() with { Type = true };
            }
            requiredFieldDict[serviceDefinitionNumber] = requiredField;
        }
        else
        {
            requiredFieldDict = new Dictionary<int, RequiredField>() { { serviceDefinitionNumber, new RequiredField() with { Type = true } } };
        }
        requiredFields[type] = requiredFieldDict;
    }
    private static void AddFactoryField(IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields, ServiceDescriptor service, int serviceDefinitionNumber)
    {
        if (requiredFields.TryGetValue(service.ServiceType, out IDictionary<int, RequiredField>? requiredFieldDict))
        {
            if (requiredFieldDict.TryGetValue(serviceDefinitionNumber, out RequiredField requiredField))
            {
                requiredField = requiredField with { Factory = true };
            }
            else
            {
                requiredField = new RequiredField() with { Factory = true };
            }
            requiredFieldDict[serviceDefinitionNumber] = requiredField;
        }
        else
        {
            requiredFieldDict = new Dictionary<int, RequiredField>() { { serviceDefinitionNumber, new RequiredField() with { Factory = true } } };
        }
        requiredFields[service.ServiceType] = requiredFieldDict;
    }
    private static void AddCtorField(IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields, TypeDescriptor type, int serviceDefinitionNumber)
    {
        if (requiredFields.TryGetValue(type, out IDictionary<int, RequiredField>? requiredFieldDict))
        {
            if (requiredFieldDict.TryGetValue(serviceDefinitionNumber, out RequiredField requiredField))
            {
                requiredField = requiredField with { Type = true, Ctor = true };
            }
            else
            {
                requiredField = new RequiredField() with { Type = true, Ctor = true };
            }
            requiredFieldDict[serviceDefinitionNumber] = requiredField;
        }
        else
        {
            requiredFieldDict = new Dictionary<int, RequiredField>() { { serviceDefinitionNumber, new RequiredField() with { Type = true, Ctor = true } } };
        }
        requiredFields[type] = requiredFieldDict;
    }
    private static void AddPropField(IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields, TypeDescriptor type, string propName, ServiceDescriptor service, int serviceDefinitionNumber)
    {
        if (requiredFields.TryGetValue(type, out IDictionary<int, RequiredField>? requiredFieldDict))
        {
            if (requiredFieldDict.TryGetValue(serviceDefinitionNumber, out RequiredField requiredField))
            {
                requiredField = requiredField with { Type = true, Ctor = true };
            }
            else
            {
                requiredField = new RequiredField() with { Type = true, Ctor = true };
            }
            var props = requiredField.Props;
            if (props.FirstIndexOf(item => item.propName == propName) is int currentIndex and > -1)
                props.RemoveAt(currentIndex);
            props.Add((propName, service));
            requiredFieldDict[serviceDefinitionNumber] = requiredField;
        }
        else
        {
            var requiredField = new RequiredField() with { Type = true, Ctor = true };
            var props = requiredField.Props;
            if (props.FirstIndexOf(item => item.propName == propName) is int currentIndex and > -1)
                props.RemoveAt(currentIndex);
            props.Add((propName, service));
            requiredFieldDict = new Dictionary<int, RequiredField>() { { serviceDefinitionNumber, requiredField } };
        }
        requiredFields[type] = requiredFieldDict;
    }
    #endregion

    private static string GenerateGet(TypeDescriptor typeDescriptor, int serviceDefinitionNumber, IEnumerable<ServiceDescriptor> allServices, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    {
        StringBuilder builder = new();
        if (typeDescriptor.CollectionTypeArg is TypeDescriptor collectionTypeArg)
        {
            var applicableServices = allServices.Where(service => service.ServiceType == typeDescriptor.CollectionTypeArg); //|| service.ServiceType.Ancestors.Any(ancestor => ancestor == typeDescriptor.CollectionTypeArg));
            if (applicableServices.Count() is int servicesCount && servicesCount > 0)
            {
                builder
                    .Append("new ")
                    .Append(typeDescriptor.CollectionTypeArg?.Name)
                    .Append("[")
                    .Append(servicesCount)
                    .Append("] { ")
                    .Append(string.Join(", ", applicableServices.Select(service => GenerateGet(service.ServiceType, serviceDefinitionNumber, allServices, requiredFields))))
                    .Append(" }");
            }
            else
            {
                builder
                    .Append("global::System.Array.Empty<")
                    .Append(typeDescriptor.CollectionTypeArg?.Name)
                    .Append(">()");
            }
        }
        else if (typeDescriptor.TendsToExternalNonPublic)
        {
            AddTypeField(requiredFields, typeDescriptor, serviceDefinitionNumber);
            builder
                .Append("resolver.GetService(")
                .Append("type_")
                .Append(typeDescriptor.Name.VarNameForm(serviceDefinitionNumber))
                .Append(')');
        }
        else
        {
            builder
                .Append("resolver.Get<")
                .Append(typeDescriptor.Name)
                .Append(">()");
        }
        return builder.ToString();
    }
    private static string GenerateTypeCtorCall(TypeDescriptor typeDescriptor, TypeDescriptor serviceType, int serviceDefinitionNumber, IEnumerable<ServiceDescriptor> allServices, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields, out bool multiline)
    {
        //new T(resolver.Get<TDependency1>(), resolver.Get<TDependency2>()) { InjectedProperty1 = resolver.Get<InjectedProperty1ServiceType>() }
        if (typeDescriptor.Name.Contains("GenericWebHostServiceOptions"))
        {

        }

        var ctorParamTypes = DependencyHelper.FindBestCtors(typeDescriptor, allServices).First();
        var injectedProperties = DependencyHelper.GetInjectedProperties(typeDescriptor, allServices);
        bool hasInjectedProps = injectedProperties.Any();
        multiline = typeDescriptor.TendsToExternalNonPublic && hasInjectedProps;

        StringBuilder builder = new();
        if (typeDescriptor.TendsToExternalNonPublic || typeDescriptor.IsNonClosedGenericType)
        {
            AddCtorField(requiredFields, typeDescriptor, serviceDefinitionNumber);
            if (hasInjectedProps)
            {
                builder
                    .Append("var instance_")
                    .Append(typeDescriptor.Name.VarNameForm(serviceDefinitionNumber))
                    .Append(" = ");
            }
            if (!serviceType.TendsToExternalNonPublic && !serviceType.IsNonClosedGenericType)
            {
                builder
                    .Append('(')
                    .Append(serviceType.Name)
                    .Append(")");
            }
            builder
                .Append(typeDescriptor.IsNonClosedGenericType ? "constructedTypeCtor_" : "typeCtor_")
                .Append(typeDescriptor.Name.VarNameForm(serviceDefinitionNumber))
                .Append(".Invoke(");
            if (typeDescriptor.IsNonClosedGenericType)
            {
                builder
                    .Append("ctorTypes.Select(ctorType => resolver.GetService(ctorType)).ToArray()");
            }
            else if (ctorParamTypes.Count() is int paramsCount && paramsCount > 0)
            {
                builder
                 .Append("new object?[")
                 .Append(paramsCount)
                 .Append("] { ");
            }
            else
            {
                builder
                    .Append("global::System.Array.Empty<object>()");
            }
        }
        else
        {
            builder
                .Append("new ")
                .Append(typeDescriptor.Name)
                .Append('(');
        }
        if (!typeDescriptor.IsNonClosedGenericType)
        {
            bool isFirst = true;
            foreach (var paramType in ctorParamTypes)
            {
                if (isFirst)
                    isFirst = false;
                else
                    builder.Append(", ");
                if (paramType.Name is Consts.IServiceProviderTypeFullName or Consts.IResolverTypeFullName)
                {
                    builder.Append("resolver");
                }
                else
                {
                    builder.Append(GenerateGet(paramType, serviceDefinitionNumber, allServices, requiredFields));
                }
            }
            if (ctorParamTypes.Any() && (typeDescriptor.TendsToExternalNonPublic || typeDescriptor.IsNonClosedGenericType))
            {
                builder.Append(" }");
            }
        }
        builder.Append(')');
        if (hasInjectedProps)
        {
            if (typeDescriptor.TendsToExternalNonPublic || typeDescriptor.IsNonClosedGenericType)
            {
                builder.Append(';');
                foreach (var (propName, service) in injectedProperties)
                {
                    AddPropField(requiredFields, typeDescriptor, propName, service, service.DefinitionNumber);
                    builder
                        .AppendLine()
                        .Append(typeDescriptor.IsNonClosedGenericType ? "constructedProp_" : "prop_")
                        .Append(propName)
                        .Append("_")
                        .Append(service.ServiceType.Name.VarNameForm(service.DefinitionNumber))
                        .Append(".SetValue(")
                        .Append("instance_")
                        .Append(service.ServiceType.Name.VarNameForm(service.DefinitionNumber))
                        .Append(", ")
                        .Append(GenerateGet(service.ServiceType, service.DefinitionNumber, allServices, requiredFields))
                        .Append(");");
                }
            }
            else
            {
                builder
                    .Append(" { ")
                    .Append(string.Join(", ", injectedProperties.Select(prop => $"{prop.propName} = {GenerateGet(prop.service.ServiceType, prop.service.DefinitionNumber, allServices, requiredFields)}")))
                    .Append(" }");
            }
        }
        return builder.ToString();
    }

    private static string GenerateImplSelector(TypeDescriptor serviceType, int serviceDefinitionNumber, IEnumerable<TypeDescriptor> serviceImpls, IEnumerable<ServiceDescriptor> allServices, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    {
        StringBuilder builder = new("global::System.Type implType = implSelector();");
        foreach (var serviceImp in serviceImpls)
        {
            builder.AppendLine();
            string returned;
            if (allServices.TryFind(serviceDescriptor => serviceDescriptor.ServiceType == serviceImp, out ServiceDescriptor serviceDescriptor))
            {
                returned = GenerateDictateValue(serviceDescriptor, allServices, serviceImp, requiredFields);
            }
            else
            {
                //we can safely discard the multiline out param of GenerateTypeCtorCall
                // because implSelectors cannot return external non-public types
                // and therefore GenerateTypeCtorCall will never set multiline to true
                returned = GenerateTypeCtorCall(serviceImp, serviceType, serviceDefinitionNumber, allServices, requiredFields, out _);
            }
            builder.Append($@"if (typeof({serviceImp.Name}) == implType) return {returned};");
        }
        builder
            .AppendLine()
            .Append($@"throw new global::System.InvalidOperationException(string.Format(""'{{0}}' is not a staticly registered implementation for a service of type '{{1}}'."", implType, ""{serviceType.Name.RemovePrefix(Consts.global)}""));");
        return builder.ToString();
    }

    private static string GenerateDictate(ServiceDescriptor serviceDescriptor, IEnumerable<ServiceDescriptor> allServices, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    {
        //this way may seem a bit more cleaner but it's a lot of hassle to write and not worthy of time for version
        //SyntaxFactory.ExpressionStatement(
        //    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
        //        kind: SyntaxKind.SimpleMemberAccessExpression,
        //        SyntaxFactory.IdentifierName("factories"),
        //        SyntaxFactory.IdentifierName("Add")))
        //    .WithArgumentList(SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>().));
        StringBuilder builder = new();
        string serviceTypeName = serviceDescriptor.ServiceType.Name;
        if (serviceDescriptor.DefinitionNumber > 0)
            serviceTypeName += serviceDescriptor.DefinitionNumber.ToString();
        string serviceTypeNameVarForm = serviceTypeName.VarNameForm();
        if (serviceDescriptor.ServiceType.IsNonClosedGenericType)
        {
            AddTypeField(requiredFields, serviceDescriptor.ServiceType, serviceDescriptor.DefinitionNumber);
            builder
                .Append("privateTypes[\"")
                .Append(serviceTypeName)
                .Append("\"] = ")
                .Append("privateType_")
                .Append(serviceTypeNameVarForm)
                .Append(" = ")
                .Append("privateType_")
                .Append(serviceTypeNameVarForm)
                .Append($".WithInitialization({Consts.global}{Consts.mainNs}.ThunderboltServiceLifetime.")
                .Append(serviceDescriptor.Lifetime switch { 0 => Consts.Singleton, 1 => Consts.Scoped, 2 => Consts.Transient, _ => "" })
                .Append(", typeArgs => ")
                .Append(GenerateDictateValue(serviceDescriptor, allServices, null, requiredFields))
                .Append(");");
        }
        else
        {
            builder.Append("reg.Dictate");
            if (serviceDescriptor.ServiceType.TendsToExternalNonPublic)
            {
                AddTypeField(requiredFields, serviceDescriptor.ServiceType, serviceDescriptor.DefinitionNumber);
                builder
                    .Append("(type_")
                    .Append(serviceTypeNameVarForm)
                    .Append(", ");
            }
            else
            {
                builder.Append($"<{serviceDescriptor.ServiceType.Name}>(");
            }
            builder.Append($"(resolver, implSelector, userFactory) => {GenerateDictateValue(serviceDescriptor, allServices, null, requiredFields)}");
            if (serviceDescriptor.ShouldUseFullDictate)
            {
                builder
                    .Append(", ")
                    .Append(Consts.serviceLifetimeEnumFullName)
                    .Append('.')
                    .Append(serviceDescriptor.Lifetime switch { 0 => Consts.Singleton, 1 => Consts.Scoped, 2 => Consts.Transient, _ => "" });
                builder.Append(", null, "); //there can't be implSelector because AttributeRegisters needs const expr and therefore no lambdas, and MS DI don't have this feature

                if (serviceDescriptor.HasFactory)
                {
                    AddFactoryField(requiredFields, serviceDescriptor, serviceDescriptor.DefinitionNumber);
                    builder
                        .Append("resolver => ");
                    if (!serviceDescriptor.ServiceType.TendsToExternalNonPublic && !serviceDescriptor.ServiceType.IsNonClosedGenericType)
                    {
                        builder
                            .Append('(')
                            .Append(serviceDescriptor.ServiceType.Name)
                            .Append(')');
                    }
                    builder
                        .Append("typeFactory_")
                        .Append(serviceTypeNameVarForm)
                        .Append($"(resolver)");
                }
                else
                {
                    builder.Append("null");
                }
            }
            builder.Append(");");
        }
        return builder.ToString();
    }

    private static string GenerateDictateValue(ServiceDescriptor serviceDescriptor, IEnumerable<ServiceDescriptor> allServices, TypeDescriptor? impl, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    {
        if (serviceDescriptor.ServiceType.Name.Contains("GenericWebHostServiceOptions"))
        {

        }
        if (serviceDescriptor.HasFactory)
        {
            return "userFactory!(resolver)";
        }
        if (serviceDescriptor.ImplSelectorTypes is not null)
        {
            return
$@"
{{
{GenerateImplSelector(serviceDescriptor.ServiceType, serviceDescriptor.DefinitionNumber, serviceDescriptor.ImplSelectorTypes, allServices, requiredFields).AddIndentation(1)}
}}";
        }
        if (serviceDescriptor.ImplType is not null)
        {
            impl ??= serviceDescriptor.ImplType;

            if (serviceDescriptor.ServiceType != impl && allServices.TryFind(service => impl.MatchesService(service), out ServiceDescriptor implServiceDescriptor))
            {
                return GenerateDictateValue(implServiceDescriptor, allServices, impl, requiredFields);
            }
        }

        impl ??= serviceDescriptor.ServiceType;

        string ctorCall = $@"{GenerateTypeCtorCall(impl, serviceDescriptor.ServiceType, serviceDescriptor.DefinitionNumber, allServices, requiredFields, out bool multiline)}";
        if (serviceDescriptor.ServiceType.IsNonClosedGenericType)
        {
            AddTypeField(requiredFields, serviceDescriptor.ServiceType, serviceDescriptor.DefinitionNumber);
            var builder
                = new StringBuilder("var constructedType_")
                .Append(impl.Name.VarNameForm())
                .Append("= type_")
                .Append(impl.Name.VarNameForm())
                .Append(".MakeGenericType(typeArgs)!;")
                .AppendLine();

            var ctorParamTypes = DependencyHelper.FindBestCtors(impl, allServices).First().ToArray();
            builder
                .Append("global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Type> typesMap = type_")
                .Append(impl.Name.VarNameForm())
                .Append(".GetGenericArguments().Zip(typeArgs).ToDictionary(item => item.First, item => item.Second);")
                .AppendLine()
                .Append("global::System.Type[] ctorTypes = new global::System.Type[")
                .Append(ctorParamTypes.Length)
                .Append("] { ")
                .Append(string.Join(", ", ctorParamTypes.Select(paramType => paramType.TendsToExternalNonPublic || paramType.IsNonClosedGenericType ? $"type_{paramType.Name.VarNameForm()}" : $"typeof({paramType.Name})")))
                .Append(" };")
                .AppendLine()
                .Append("for (global::System.Int32 i = 0; i < ctorTypes.Length; ++i)")
                .AppendLine()
                .Append('{')
                    .AppendLine()
                    .Append("ctorTypes[i] = constructType(ctorTypes[i], typesMap);".AddIndentation(1))
                    .AppendLine()
                .Append('}')
                .AppendLine()
                .Append("global::System.Reflection.ConstructorInfo constructedTypeCtor_")
                .Append(impl.Name.VarNameForm())
                .Append(" = constructedType_")
                .Append(impl.Name.VarNameForm())
                .Append(".GetConstructor(ctorTypes)!;")
                .AppendLine();

            var injectedProperties = DependencyHelper.GetInjectedProperties(impl, allServices);
            foreach (var (propName, propService) in injectedProperties)
            {
                builder
                    .Append("var constructedProp_")
                    .Append(propName)
                    .Append('_')
                    .Append(propService.ServiceType.Name.VarNameForm(propService.DefinitionNumber))
                    .Append(" = constructedType_")
                    .Append(impl.Name.VarNameForm())
                    .Append(".GetProperty(\"")
                    .Append(propName)
                    .Append("\")!;")
                    .AppendLine();
            }

            builder.Append($"return ({Consts.IResolverTypeFullName} resolver, global::System.Func<global::System.Type>? implSelector, global::System.Func<{Consts.IResolverTypeFullName}, global::System.Object>? userFactory) => ");

            if (multiline)
            {
                builder
                    .Append('{')
                    .AppendLine()
                    .Append(ctorCall.AddIndentation(1))
                    .AppendLine()
                    .Append('}');
            }
            else
            {
                builder.Append(ctorCall);
            }

            builder.Append(';');
            return
$@"
{{
{builder.ToString().AddIndentation(1)}
}}";
        }

        if (multiline)
        {
            return
$@"
{{
{ctorCall.AddIndentation(1)}
}}";
        }
        else
        {
            return ctorCall;
        }
    }
    private static string GenerateDictates(IEnumerable<ServiceDescriptor> allTypes, IEnumerable<ServiceDescriptor> allServices, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    {
        return string.Join(Environment.NewLine, allTypes.Select(item => GenerateDictate(item, allServices, requiredFields)));
    }

    //private static string GenerateStaticRegistrations(IEnumerable<ServiceDescriptor> services, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    //{
    //    StringBuilder builder = new();
    //    bool isFirst = true;
    //    foreach (var service in services)
    //    {
    //        if (isFirst)
    //            isFirst = false;
    //        else
    //            builder.AppendLine();
    //        builder
    //            .Append("reg.Add")
    //            .Append(service.Lifetime switch { 0 => Consts.Singleton, 1 => Consts.Scoped, 2 => Consts.Transient, _ => "" });
    //        if (service.HasFactory)
    //        {
    //            builder.Append("Factory");
    //        }
    //        if (service.ServiceType.TendsToExternalNonPublic || service.ServiceType.IsNonClosedGenericType || service.ImplType?.TendsToExternalNonPublic == true || service.ImplType?.IsNonClosedGenericType == true)
    //        {
    //            AddTypeField(requiredFields, service.ServiceType);
    //            if (service.ImplType is not null)
    //                AddTypeField(requiredFields, service.ImplType);
    //            builder
    //                .Append("Reflection(type_")
    //                .Append(service.ServiceType.Name.VarNameForm());
    //            if (service.HasFactory)
    //            {
    //                builder
    //                    .Append(", ");
    //            }
    //        }
    //        else
    //        {
    //            builder
    //                .Append('<')
    //                .Append(service.ServiceType.Name);
    //            if (service.ImplType is not null)
    //            {
    //                builder
    //                    .Append(", ")
    //                    .Append(service.ImplType?.Name);
    //            }
    //            builder.Append(">(");
    //        }
    //        if (service.HasFactory)
    //        {
    //            AddFactoryField(requiredFields, service);
    //            builder
    //                .Append("resolver => ");
    //            if (!service.ServiceType.TendsToExternalNonPublic && !service.ServiceType.IsNonClosedGenericType)
    //            {
    //                builder
    //                    .Append('(')
    //                    .Append(service.ServiceType.Name)
    //                    .Append(')');
    //            }
    //            builder
    //                 .Append("typeFactory_")
    //                .Append(service.ServiceType.Name.VarNameForm())
    //                .Append($"(resolver)");
    //        }
    //        builder.Append(");");
    //    }
    //    return builder.ToString();
    //}

    internal static string GenerateDictateTypeFactories(IEnumerable<ServiceDescriptor> allTypes, IEnumerable<ServiceDescriptor> allServices, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    {
        return !allTypes.Any() ? "" : GenerateDictates(allTypes, allServices, requiredFields);
    }

    //internal static string GenerateStaticRegister(IEnumerable<(ServiceDescriptor service, INamedTypeSymbol? symbol)> allTypes, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    //{
    //    return !allTypes.Any(item => item.symbol is null) ? "" : GenerateStaticRegistrations(allTypes.Where(item => item.symbol is null && !item.service.ServiceType.IsNonClosedGenericType).Select(item => item.service), requiredFields);
    //}

    internal static string GenerateRequiredFields(IEnumerable<ServiceDescriptor> allServices, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields)
    {
        Dictionary<TypeDescriptor, IDictionary<int, RequiredField>> additionalFields = new();
        var (reqTypes, reqFields) = processFields(requiredFields, additionalFields);
        var (addTypes, addFields) = processFields(additionalFields, null);
        string joined =
            string.Join(Environment.NewLine,
                reqTypes,
                addTypes,
                reqFields,
                addFields);
        if (!string.IsNullOrWhiteSpace(joined))
        {
            StringBuilder builder
                = new StringBuilder(joined)
                .Insert(0, Environment.NewLine)
                .Insert(0, "var privateTypes = (PrivateTypes as global::System.Collections.Generic.Dictionary<global::System.String, global::ThunderboltIoc.PrivateType>)!;")
                .Insert(0, Environment.NewLine)
                .Insert(0,
$@"static global::System.Type constructType(in global::System.Type type, in global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> typesMap)
{{
    if (type.IsGenericParameter)
    {{
        return typesMap[type];
    }}
    else if (type.IsGenericType)
    {{
        global::System.Type[] args = type.GetGenericArguments();
        for (global::System.Int32 i = 0; i < args.Length; i++)
        {{
            global::System.Type typeArg = args[i];
            if (typeArg.IsGenericParameter)
            {{
                args[i] = typesMap[typeArg];
            }}
            else if (typeArg.IsGenericType)
            {{
                args[i] = constructType(typeArg, typesMap);
            }}
        }}
        return (type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition()).MakeGenericType(args)!;
    }}
    return type;
}}");
            joined = builder.ToString();
        }
        return joined;
        (string types, string fields) processFields(IDictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields, IDictionary<TypeDescriptor, IDictionary<int, RequiredField>>? additionalFields)
        {
            StringBuilder builder = new();
            StringBuilder typesBuilder = new();
            foreach (var (type, definitionNumber, requiredField) in requiredFields.SelectMany(rf => rf.Value.Select(kvp => (rf.Key, kvp.Key, kvp.Value))))
            {
                #region types
                typesBuilder
                    .Append("var privateType_")
                    .Append(type.Name.VarNameForm(definitionNumber))
                    .Append(" = privateTypes[\"")
                    .Append(type.Name.DefinitionNumberSuffix(definitionNumber))
                    .Append("\"];");
                if (requiredField.Factory)
                {
                    typesBuilder
                        .AppendLine()
                        .Append("var typeFactory_")
                        .Append(type.Name.VarNameForm(definitionNumber))
                        .Append(" = privateType_")
                        .Append(type.Name.VarNameForm(definitionNumber))
                        .Append(".factory!;");
                }
                if (requiredField.Type || requiredField.Ctor || requiredField.Props.Count > 0)
                {
                    typesBuilder
                        .AppendLine()
                        .Append("var type_")
                        .Append(type.Name.VarNameForm(definitionNumber))
                        .Append(" = privateType_")
                        .Append(type.Name.VarNameForm(definitionNumber))
                        .Append(".type;");
                }
                typesBuilder.AppendLine();
                #endregion

                #region ctor
                if (requiredField.Ctor)
                {
                    var ctorParamTypes = DependencyHelper.FindBestCtors(type, allServices).First();
                    if (type.IsNonClosedGenericType && additionalFields is not null)
                    {
                        foreach (var ctorParamType in ctorParamTypes.Where(ctorParamType => ctorParamType.IsNonClosedGenericType && !requiredFields.Any(r => r.Key == ctorParamType)))
                        {
                            if (additionalFields.TryGetValue(ctorParamType, out var additionalField))
                            {
                                additionalField[definitionNumber] = new RequiredField() with { Type = true };
                            }
                            else
                            {
                                additionalFields[ctorParamType] = new Dictionary<int, RequiredField>() { { definitionNumber, new RequiredField() with { Type = true } } };
                            }
                        }
                        if (allServices.TryFind(service => service.ServiceType == type, out var service))
                        {
                            foreach (var serviceImpl in service.GetPossibleImplementations(allServices).Where(serviceImpl => serviceImpl.IsNonClosedGenericType && !requiredFields.Any(r => r.Key == serviceImpl)))
                            {
                                if (additionalFields.TryGetValue(serviceImpl, out var additionalField))
                                {
                                    additionalField[definitionNumber] = new RequiredField() with { Type = true };
                                }
                                else
                                {
                                    additionalFields[serviceImpl] = new Dictionary<int, RequiredField>() { { definitionNumber, new RequiredField() with { Type = true } } };
                                }
                            }
                        }
                    }
                    builder
                      .Append("global::System.Reflection.ConstructorInfo typeCtor_")
                      .Append(type.Name.VarNameForm(definitionNumber))
                      .Append(" = type_")
                      .Append(type.Name.VarNameForm(definitionNumber))
                      .Append(".GetConstructor(");
                    if (ctorParamTypes.Count() is int paramsCount && paramsCount > 0)
                    {
                        builder
                          .Append("new global::System.Type[")
                          .Append(paramsCount)
                          .Append("] { ")
                          .Append(string.Join(", ", ctorParamTypes.Select(paramType => paramType.TendsToExternalNonPublic || paramType.IsNonClosedGenericType ? $"type_{paramType.Name.VarNameForm(definitionNumber)}" : $"typeof({paramType.Name})")))
                          .Append(" })!;");
                    }
                    else
                    {
                        builder
                            .Append("global::System.Array.Empty<global::System.Type>())!;");
                    }
                    builder
                      .AppendLine();
                }
                #endregion

                #region props
                foreach (var (propName, propService) in requiredField.Props)
                {
                    builder
                        .Append("global::System.Reflection.PropertyInfo prop_")
                        .Append(propName)
                        .Append('_')
                        .Append(propService.ServiceType.Name.VarNameForm(propService.DefinitionNumber))
                        .Append(" = type_")
                        .Append(type.Name.VarNameForm(definitionNumber))
                        .Append(".GetProperty(\"")
                        .Append(propName)
                        .Append("\")!;")
                        .AppendLine();
                }
                #endregion
            }
            return (typesBuilder.ToString(), builder.ToString());
        }
    }
}
