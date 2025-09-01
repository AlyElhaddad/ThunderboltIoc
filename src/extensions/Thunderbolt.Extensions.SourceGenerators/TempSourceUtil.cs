using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Newtonsoft.Json;

using Thunderbolt.GeneratorAbstractions;

namespace Thunderbolt.Extensions.SourceGenerators;

internal static class TempSourceUtil
{
    private static readonly string msGeneratorPath = typeof(ThunderboltMsSourceGenerator).Assembly.Location;

    private static (IReadOnlyDictionary<string, string> buildProperties, IReadOnlyDictionary<string, string> buildReferences) GetBuildMetadata(GeneratorExecutionContext context)
    {
        IReadOnlyDictionary<string, string> buildProperties;
        if (context.AdditionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path) == "BuildProperties.json") is AdditionalText buildPropertiesFile)
        {
            string? buildPropertiesFileText = buildPropertiesFile.GetText()?.ToString();
            var buildPropertiesDict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(buildPropertiesFileText))
            {
                using (StringReader strReader = new StringReader(buildPropertiesFileText))
                {
                    using (JsonTextReader jsonReader = new JsonTextReader(strReader))
                    {
                        while (jsonReader.Read())
                        {
                            if (jsonReader.TokenType != JsonToken.PropertyName)
                                continue;

                            if (jsonReader.Value?.ToString() != "properties")
                                continue;

                            string? currentPropertyName = null;
                            while (jsonReader.Read())
                            {
                                if (jsonReader.TokenType == JsonToken.PropertyName)
                                {
                                    currentPropertyName = jsonReader.Value?.ToString();
                                }
                                else if (jsonReader.TokenType == JsonToken.String && jsonReader.Value != null && !string.IsNullOrWhiteSpace(currentPropertyName))
                                {
                                    buildPropertiesDict[currentPropertyName!] = jsonReader.Value.ToString();
                                }
                            }
                            break;
                        }
                    }
                }
            }
            buildProperties = buildPropertiesDict;
        }
        else
        {
            buildProperties = new Dictionary<string, string>();
        }

        IReadOnlyDictionary<string, string> buildReferences;
        if (context.AdditionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path) == "BuildReferences.json") is AdditionalText buildReferencesFile)
        {
            string? buildReferencesFileText = buildReferencesFile.GetText()?.ToString();
            var buildReferencesDict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(buildReferencesFileText))
            {
                using (var strReader = new StringReader(buildReferencesFileText))
                {
                    using (var jsonReader = new JsonTextReader(strReader))
                    {
                        while (jsonReader.Read())
                        {
                            if (jsonReader.TokenType != JsonToken.PropertyName)
                                continue;

                            if (jsonReader.Value?.ToString() != "references")
                                continue;

                            string? currentPropertyName = null;
                            string? currentPropertyValue = null;
                            string? includeValue = null;
                            while (jsonReader.Read())
                            {
                                if (jsonReader.TokenType == JsonToken.PropertyName)
                                {
                                    currentPropertyName = jsonReader.Value?.ToString();
                                }
                                else if (jsonReader.TokenType == JsonToken.String && jsonReader.Value != null && !string.IsNullOrWhiteSpace(currentPropertyName))
                                {
                                    currentPropertyValue = jsonReader.Value.ToString();
                                    if (currentPropertyName == "include")
                                    {
                                        includeValue = currentPropertyValue;
                                    }
                                    else if (currentPropertyName == "itemType" && !string.IsNullOrWhiteSpace(includeValue) && !string.IsNullOrWhiteSpace(currentPropertyValue))
                                    {
                                        buildReferencesDict[includeValue!] = currentPropertyValue;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            buildReferences = buildReferencesDict;
        }
        else
        {
            buildReferences = new Dictionary<string, string>();
        }

        return (buildProperties, buildReferences);
    }

    internal static string Emit(GeneratorExecutionContext context, string registrationClassName)
    {
        var options = context.AnalyzerConfigOptions.GlobalOptions;
        var compilation = context.Compilation;

        var (buildProperties, buildReferences) = GetBuildMetadata(context);

        string projDir = buildProperties["ProjectDir"];
        string tfm = buildProperties["TargetFramework"];
        if (!buildProperties.TryGetValue("EnablePreviewFeatures", out string? enablePreviewFeatures))
            enablePreviewFeatures = null;
        if (!buildProperties.TryGetValue("GenerateRequiresPreviewFeaturesAttribute", out string? generateRequiresPreviewFeaturesAttribute))
            generateRequiresPreviewFeaturesAttribute = null;
        string[] frameworkReferences = buildReferences.Where(kvp => kvp.Value == "FrameworkReference").Select(kvp => kvp.Key).ToArray();

        var references = compilation
                .References
                .Select(r =>
                { //avoid working with reference assemblies
                  //see: https://stackoverflow.com/a/64926814/3602352
                    DirectoryInfo dir = new(Path.GetDirectoryName(r.Display));
                    if (dir.Name == "ref")
                    {
                        // bin\debug\net6.0\ref > bin\debug\net6.0
                        if (dir.Parent?.Parent?.Name is "bin" or "obj" || dir.Parent?.Parent?.Parent?.Name is "bin" or "obj")
                        {
                            string refPath = Path.Combine(dir.Parent.FullName, Path.GetFileName(r.Display));
                            if (File.Exists(refPath))
                            {
                                return MetadataReference.CreateFromFile(refPath, r.Properties);
                            }
                        }
                    }
                    return r;
                })
                .Where(r => r.Display != msGeneratorPath); //exclude the generator
        compilation
            = compilation
            .WithReferences(references);


        //Copy references
        PathUtil.DeleteTempDirs(projDir);
        PathUtil.CreateTempDirs(projDir);
        string emitDir = PathUtil.EmitDir(projDir);
        foreach (string refPath in references.Where(r => !IsReferenceAssembly(compilation, r)).Select(r => r.Display!).Where(path => File.Exists(path)))
        {
            string refFileName = Path.GetFileName(refPath);
            File.Copy(refPath, Path.Combine(emitDir, refFileName), true);
        }
        string emitPath = Path.Combine(emitDir, compilation.Options.ModuleName);
        var emitResult = compilation.Emit(emitPath);
        string tempProjDir = PathUtil.TempProjDir(projDir);
        WriteTempSource(
            tempProjDir,
            registrationClassName,
            tfm,
            frameworkReferences,
            enablePreviewFeatures,
            generateRequiresPreviewFeaturesAttribute,
            Path.Combine(emitDir, "Thunderbolt.Extensions.Abstractions.dll"),
            Path.GetFileNameWithoutExtension(emitPath).Replace(' ', '_'),
            emitPath);
        return BuildTempSource(tempProjDir);
    }
    private static bool IsReferenceAssembly(Compilation compilation, MetadataReference reference)
    {
        if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol symbol)
            return false;

        const string referenceAssemblyAttribute = "global::System.Runtime.CompilerServices.ReferenceAssemblyAttribute";
        return symbol.GetAttributes().Any(attr => attr.AttributeClass?.GetFullyQualifiedName() == referenceAssemblyAttribute);
    }

    internal static string RunTempSource(string binaryPath, string? startupArgs)
    {
        return Utils.RunExecutable("dotnet", $@"""{binaryPath}""{(string.IsNullOrWhiteSpace(startupArgs) ? "" : $" {startupArgs}")}");
    }
    internal static string BuildTempSource(string projDir)
    {
        Utils.RunExecutable("dotnet", $@"clean ""{projDir}""");
        Utils.RunExecutable("dotnet", $@"build ""{projDir}""");
        return Path.Combine(projDir, "bin", "thunderbolt_types_util_proj.dll");
    }
    internal static string WriteTempSource(
        string projDir,
        string regClassFullName,
        string tfm,
        string[] frameworkReferences,
        string? enablePreviewFeatures,
        string? generateRequiresPreviewFeaturesAttribute,
        string abstractionsPath,
        string tempRefName,
        string tempRefPath)
    {
        string tempProjPath = Path.Combine(projDir, "thunderbolt_types_util_proj.csproj");
        string programCsPath = Path.Combine(projDir, "Program.cs");
        File.WriteAllText(tempProjPath, TempProjSource(tfm, frameworkReferences, enablePreviewFeatures, generateRequiresPreviewFeaturesAttribute, abstractionsPath, tempRefName, tempRefPath));
        File.WriteAllText(programCsPath, TempProgramSource(regClassFullName));
        return tempProjPath;
    }
    internal static string TempProgramSource(string regClassFullName)
    {
        return
@$"using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Thunderbolt.Extensions.Abstractions;
using Thunderbolt.GeneratorAbstractions;

namespace thunderbolt_types_util_proj
{{
    public class Program
    {{
        private static readonly Assembly userAssembly = typeof({regClassFullName}).Assembly;
        public static void Main(string[] args)
        {{
            try
            {{
                ThunderboltMsRegistration.isGeneratingCode = true;
                try {{ new {regClassFullName}().BuilderAction(args); }}
                catch (ThunderboltCodeGenerationIntentionalException) {{ }}
                if (ThunderboltMsRegistration.BuilderServices is null)
                    return;
                var returnedServiceCount = new Dictionary<string, int>();
                var descriptors
                    = ThunderboltMsRegistration
                    .BuilderServices!
                    .Select(msServiceDescriptor =>
                    {{
                        string serviceTypeName = msServiceDescriptor.ServiceType.GetFullyQualifiedName();
                        int definitionNumber = returnedServiceCount.TryGetValue(serviceTypeName, out int lastDefinitionNumber) ? lastDefinitionNumber + 1 : 0;
                        returnedServiceCount[serviceTypeName] = definitionNumber;
                        int lifetime = (int)msServiceDescriptor.Lifetime;
                        bool hasFactory = msServiceDescriptor.ImplementationInstance is not null || msServiceDescriptor.ImplementationFactory is not null;
                        return new ServiceDescriptor(
                            lifetime,
                            TypeDescriptor.FromType(msServiceDescriptor.ServiceType, userAssembly),
                            !hasFactory && msServiceDescriptor.ImplementationType is Type implType ? TypeDescriptor.FromType(implType, userAssembly) : null,
                            null,
                            hasFactory,
                            true,
                            definitionNumber);
                    }});
                Console.Write(JsonConvert.SerializeObject(
                    descriptors,
                    new JsonSerializerSettings()
                    {{
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    }}));
            }}
            catch (Exception ex)
            {{
                Console.Write(ex);
            }}
        }}
    }}
}}";

    }
    internal static string TempProjSource(
        string tfm,
        string[] frameworkReferences,
        string? enablePreviewFeatures,
        string? generateRequiresPreviewFeaturesAttribute,
        string abstractionsPath,
        string tempRefName,
        string tempRefPath)
    {
        string thunderboltReferences =
#if DEBUG
@"
		<ProjectReference Include=""..\..\..\..\..\..\..\..\Repos\ThunderboltIoc\src\extensions\Thunderbolt.Extensions.Abstractions\Thunderbolt.Extensions.Abstractions.csproj"" />
		<ProjectReference Include=""..\..\..\..\..\..\..\..\Repos\ThunderboltIoc\src\extensions\Thunderbolt.Extensions.AspNetCore\Thunderbolt.Extensions.AspNetCore.csproj"" />
		<ProjectReference Include=""..\..\..\..\..\..\..\..\Repos\ThunderboltIoc\src\extensions\Thunderbolt.Extensions.Hosting\Thunderbolt.Extensions.Hosting.csproj"" />
		<ProjectReference Include=""..\..\..\..\..\..\..\..\Repos\ThunderboltIoc\src\extensions\Thunderbolt.Extensions.SourceGenerators\Thunderbolt.Extensions.SourceGenerators.csproj"" />
";
#else
@$"
        <PackageReference Include=""Thunderbolt.Extensions.Abstractions"" Version=""$({GeneratedBuildInfo.Version})"" />
        <PackageReference Include=""Thunderbolt.Extensions.AspNetCore"" Version=""$({GeneratedBuildInfo.Version})"" />
        <PackageReference Include=""Thunderbolt.Extensions.Hosting"" Version=""$({GeneratedBuildInfo.Version})"" />
        <PackageReference Include=""Thunderbolt.Extensions.SourceGenerators"" Version=""$({GeneratedBuildInfo.Version})"" />
";
#endif

        return
@$"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>{tfm}</TargetFramework>
        <OutputPath>bin</OutputPath>
        <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
        <Optimize>true</Optimize>
        <LangVersion>9</LangVersion>
        <Nullable>enable</Nullable>
        <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
        {(!string.IsNullOrWhiteSpace(enablePreviewFeatures) ? $"<EnablePreviewFeatures>{enablePreviewFeatures}</EnablePreviewFeatures>" : "")}
        {(!string.IsNullOrWhiteSpace(generateRequiresPreviewFeaturesAttribute) ? $"<GenerateRequiresPreviewFeaturesAttribute>{generateRequiresPreviewFeaturesAttribute}</GenerateRequiresPreviewFeaturesAttribute>" : "")}
    </PropertyGroup >

    <ItemGroup>
        {(frameworkReferences?.Any(fr => !string.IsNullOrEmpty(fr)) == true ? string.Join(Environment.NewLine, frameworkReferences.Where(fr => !string.IsNullOrEmpty(fr)).Select(fr => @$"<FrameworkReference Include = ""{fr}"" />")) : "")}
        <Reference Include=""{tempRefName}"">
            <HintPath>{tempRefPath}</HintPath>
        </Reference>

{thunderboltReferences}

        <!--  Below are all the package references of the source generator. They are needed.  -->
		<PackageReference Include=""Microsoft.CodeAnalysis.Analyzers"" Version=""4.14.0"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""Microsoft.CodeAnalysis.CSharp"" Version=""4.14.0"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""9.0.8"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""Microsoft.Extensions.DependencyInjection.Abstractions"" Version=""9.0.8"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""Microsoft.Extensions.DependencyModel"" Version=""9.0.8"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
        <!--  Above, were the direct references; below are the indirect source generator references.  -->
		<PackageReference Include=""Microsoft.CodeAnalysis.Common"" Version=""4.14.0"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Collections.Immutable"" Version=""9.0.0"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Memory"" Version=""4.5.5"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Buffers"" Version=""4.5.1"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Numerics.Vectors"" Version=""4.5.0"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Runtime.CompilerServices.Unsafe"" Version=""6.0.0"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Reflection.Metadata"" Version=""9.0.0"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Text.Encoding.CodePages"" Version=""7.0.0"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Threading.Tasks.Extensions"" Version=""4.5.4"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""Microsoft.Bcl.AsyncInterfaces"" Version=""9.0.8"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Text.Encodings.Web"" Version=""9.0.8"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.Text.Json"" Version=""9.0.8"" GeneratePathProperty=""true"" PrivateAssets=""all"" />
		<PackageReference Include=""System.IO.Pipelines"" Version=""9.0.8"" GeneratePathProperty=""true"" PrivateAssets=""all"" />	</ItemGroup>
    </ItemGroup>
</Project>";
    }

    #region AnalyzerConfigOptions Extensions
    private static string GetString(this AnalyzerConfigOptions options, string key)
        => options.TryGetValue(key, out var option) ? option : throw new InvalidOperationException($"Could not find the required option: '{key}'");

    private static bool GetBoolean(this AnalyzerConfigOptions options, string key)
        => bool.TryParse(options.GetString(key), out bool boolean) && boolean;
    #endregion
}
