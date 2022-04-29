using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace Thunderbolt.Extensions.SourceGenerators;

internal static class TempSourceUtil
{
    private static readonly string msGeneratorPath = typeof(ThunderboltMsSourceGenerator).Assembly.Location;
    private static readonly string newtonsoftPath = typeof(JsonConvert).Assembly.Location;

    internal static string Emit(Compilation compilation, AnalyzerConfigOptions globalOptions, string registrationClassName)
    {
        if (!globalOptions.TryGetValue("build_property.projectdir", out var projDir) || string.IsNullOrWhiteSpace(projDir)
            || !globalOptions.TryGetValue("build_property.targetframework", out var tfm) || string.IsNullOrWhiteSpace(tfm)
            || !globalOptions.TryGetValue("build_property.usingmicrosoftnetsdkweb", out var useWebSdkStr) || !bool.TryParse(useWebSdkStr, out bool useWebSdk)
            || !globalOptions.TryGetValue("build_property.enablepreviewfeatures", out var enablePreviewFeaturesStr))
            throw new InvalidOperationException();
        bool.TryParse(enablePreviewFeaturesStr, out bool enablePreviewFeatures);
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
        foreach (string refPath in references.Select(r => r.Display!).Where(path => File.Exists(path)))
        {
            string refFileName = Path.GetFileName(refPath);
            File.Copy(refPath, Path.Combine(emitDir, refFileName), true);
        }
        string emitPath = Path.Combine(emitDir, compilation.Options.ModuleName);
        compilation.Emit(emitPath);
        string tempProjDir = PathUtil.TempProjDir(projDir);
        WriteTempSource(
            tempProjDir,
            registrationClassName,
            tfm,
            useWebSdk,
            enablePreviewFeatures,
            Path.Combine(emitDir, "Thunderbolt.Extensions.Abstractions.dll"),
            Path.GetFileNameWithoutExtension(emitPath).Replace(' ', '_'),
            emitPath);
        return BuildTempSource(tempProjDir);
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
        bool useWebSdk,
        bool enablePreviewFeatures,
        string abstractionsPath,
        string tempRefName,
        string tempRefPath)
    {
        string tempProjPath = Path.Combine(projDir, "thunderbolt_types_util_proj.csproj");
        string programCsPath = Path.Combine(projDir, "Program.cs");
        File.WriteAllText(tempProjPath, TempProjSource(tfm, useWebSdk, enablePreviewFeatures, abstractionsPath, tempRefName, tempRefPath));
        File.WriteAllText(programCsPath, TempProgramSource(regClassFullName));
        return tempProjPath;
    }
    internal static string TempProgramSource(string regClassFullName)
    {
        return
@$"using Newtonsoft.Json;

using System;
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
                new {regClassFullName}().BuilderAction(args);
                if (ThunderboltMsRegistration.BuilderServices is null)
                    return;
                var descriptors
                    = ThunderboltMsRegistration
                    .BuilderServices!
                    .Select(msServiceDescriptor =>
                    {{
                        int lifetime = (int)msServiceDescriptor.Lifetime;
                        bool hasFactory = msServiceDescriptor.ImplementationInstance is not null || msServiceDescriptor.ImplementationFactory is not null;
                        return new ServiceDescriptor(
                            lifetime,
                            TypeDescriptor.FromType(msServiceDescriptor.ServiceType, userAssembly),
                            !hasFactory && msServiceDescriptor.ImplementationType is Type implType ? TypeDescriptor.FromType(implType, userAssembly) : null,
                            null,
                            hasFactory,
                            true);
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
        bool useWebSdk,
        bool enablePreviewFeatures,
        string abstractionsPath,
        string tempRefName,
        string tempRefPath)
    {
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
        {(enablePreviewFeatures ? "<EnablePreviewFeatures>true</EnablePreviewFeatures>" : "")}
        {(enablePreviewFeatures ? "<GenerateRequiresPreviewFeaturesAttribute>true</GenerateRequiresPreviewFeaturesAttribute>" : "")}
    </PropertyGroup >

    <ItemGroup>
        {(useWebSdk ? @"<FrameworkReference Include = ""Microsoft.AspNetCore.App"" />" : "")}
        <Reference Include=""Newtonsoft.Json"">
            <HintPath>{newtonsoftPath}</HintPath>
        </Reference>
        <Reference Include=""Thunderbolt.Extensions.Abstractions"">
            <HintPath>{abstractionsPath}</HintPath>
        </Reference>
        <Reference Include=""Thunderbolt.Extensions.SourceGenerators"">
            <HintPath>{msGeneratorPath}</HintPath>
        </Reference>
        <Reference Include=""{tempRefName}"">
            <HintPath>{tempRefPath}</HintPath>
        </Reference>
    </ItemGroup>
</Project>";
    }
}
