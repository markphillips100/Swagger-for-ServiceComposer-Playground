using Microsoft.CodeAnalysis.Diagnostics;

namespace ServiceComposerSourceGenerators;

public static class ServiceComposerSourceGenerationExtensions
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

    public static string GetMsBuildProperty(
        this AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        string name,
        string defaultValue = "")
    {
        analyzerConfigOptionsProvider.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
        return value ?? defaultValue;
    }

    // private static string GetNamespaceFrom(SyntaxNode s) =>
    //     s.Parent switch
    //     {
    //         NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.Name.ToString(),
    //         null => string.Empty, // or whatever you want to do
    //         _ =>  GetNamespaceFrom(s.Parent)
    //     };

//     public static string GetPrefix(this SyntaxNode member)
//     {
//         if (member == null) {
//             return "";
//         }
//
//         var sb = new StringBuilder();
//         var node = member;
//
//         node.Parent switch
//         {
//             NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.Name.ToString(),
//             null => string.Empty, // or whatever you want to do
//             _ => GetNamespaceFrom(s.Parent)
//         };
//
//         while(node.Parent != null) {
//             node = node.Parent;
//
//             if (node is NamespaceDeclarationSyntax namespaceDeclaration) {
//                 sb.Insert(0, ".");
//                 sb.Insert(0, namespaceDeclaration.Name.ToString());
//             } else if (node is ClassDeclarationSyntax) {
//                 var classDeclaration = (ClassDeclarationSyntax) node;
//
//                 sb.Insert(0, ".");
//                 sb.Insert(0, classDeclaration.Identifier.ToString());
//             }
//         }
//
//         return sb.ToString();
//     }
}