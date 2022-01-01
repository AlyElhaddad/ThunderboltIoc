using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThunderboltIoc.SourceGenerators;

internal static class RoslynSyntactixExtensions
{
    public static IEnumerable<MethodDeclarationSyntax> GetDeclaredMethods(this ClassDeclarationSyntax classDeclaration, string methodName, params string[] modifiers)
    {
        return
            classDeclaration.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == methodName && (modifiers?.Any() != true || (m.Modifiers.Count == modifiers.Length && modifiers.All(modifier => m.Modifiers.Any(mod => mod.Text == modifier)))));
    }

}

