using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace ServiceComposerSourceGenerators;

/// <summary>
/// Generates merged viewmodel for a given endpoint based on referenced assemblies and their included
/// ICompositionHandlers.
/// </summary>
/// <remarks>
/// Answer at the following URL tells that we can only inspect syntax tree for currently un-compiled source, hence
/// we must first obtain the referenced assemblies and scan for the types we want from the compiled assemblies.
/// https://stackoverflow.com/questions/68055210/generate-source-based-on-other-assembly-classes-c-source-generator
/// </remarks>
[Generator]
public class ServiceComposerSwaggerSourceGenerator : IIncrementalGenerator
{
//        private const string ConstantsClassName = "RootNamespacePrefixConstants";

    private record CompositionResponseMetadata(
        string RouteTemplate,
        string ClassNamespace,
        string ClassName,
        string CompositionFullyQualifiedTypeName,
        string CompositionPropertyName);
    
    /// <summary>
    /// Handles the source generation.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
//            Console.WriteLine("MyRootNamespacePrefixConstantGenerator. Execute called");

        IncrementalValuesProvider<ClassDeclarationSyntax> classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => IsSyntaxTargetForGeneration(s),
                (t, _) => GetSemanticTargetForGeneration(t))
            .Where(a => a != null)!;

        var providers
            = context.CompilationProvider
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Combine(classes.Collect());

        //IncrementalValueProvider<(Compilation, ImmutableArray<AttributeSyntax>)> compilation
        //    = context.CompilationProvider
        //        .Combine(attributes.Collect());

        context.RegisterSourceOutput(providers, (spc, source) =>
        {
            var ((compilation, options), attributes) = source;

            Execute(compilation, options, attributes, spc);
        });
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax;

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        var implementedInterfaces = classSymbol?.AllInterfaces;
            
        return implementedInterfaces?.Any(x => x.Name.ToString() == "ICompositionRequestsHandler") == true
            ? classDeclarationSyntax
            : null;
    }

    private static void Execute(
        Compilation compilation,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        ImmutableArray<ClassDeclarationSyntax> classes,
        SourceProductionContext context)
    {
        var rootNamespace = analyzerConfigOptionsProvider.GetMsBuildProperty("RootNamespace");
        var compositionResponseMetadataItems = GetCompositionResponseMetadata(compilation, classes, context);

        var source = GenerateCompositionDocumentationHandler(
            rootNamespace,
            "ServiceComposerDocumentationHandlers",
            compositionResponseMetadataItems);
        context.AddSource($"ServiceComposerDocumentationHandlers.g", SourceText.From(source, Encoding.UTF8));
    }

    private static IEnumerable<CompositionResponseMetadata> GetCompositionResponseMetadata(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classes,
        SourceProductionContext context)
    {
        foreach (var classDeclarationSyntax in classes)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (classSymbol == null) continue;
                
            foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
            {
                var methodDeclarationSyntax = (MethodDeclarationSyntax) memberDeclarationSyntax;
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                if (methodSymbol?.Parameters.Length == 1 && methodSymbol.Parameters[0].Type.Name == "HttpRequest")
                {
                    var compositionAttributes = methodDeclarationSyntax.AttributeLists
                        .SelectMany(attrList => attrList
                            .Attributes
                            .Where(x => 
                                x.Name.NormalizeWhitespace().ToFullString() == "ProducesCompositionResponseType")
                            )
                        .ToList();

                    var responseAttributes = methodDeclarationSyntax.AttributeLists
                        .SelectMany(attrList => attrList
                            .Attributes
                            .Where(x => 
                                x.Name.NormalizeWhitespace().ToFullString() == "ProducesResponseType")
                            )
                        .ToList();

                    if (responseAttributes.Count != 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor("SC0001",
                                "ProducesResponseType attribute",
                                "Method {0} cannot have a ProducesResponseType attribute.  Use ProducesCompositionResponseType attribute instead.",
                                "ServiceComposer",
                                DiagnosticSeverity.Error,
                                true),
                            methodSymbol.Locations.FirstOrDefault(),
                            methodSymbol.Name));
                        continue;
                    }
                    
                    if (compositionAttributes.Count == 1)
                    {
                        var compositionAttribute = compositionAttributes[0];
                        
                        
                        var httpAttributes = methodDeclarationSyntax.AttributeLists
                            .SelectMany(attrList => attrList
                                .Attributes
                                .Where(x => 
                                    x.Name.NormalizeWhitespace().ToFullString() == "HttpGet")
                                )
                            .ToList();

                        foreach (var httpAttribute in httpAttributes)
                        {
                            var firstArgument = httpAttribute.ArgumentList?.Arguments.First();
                            if (firstArgument != null)
                            {
                                var template = firstArgument.Expression.NormalizeWhitespace().ToFullString();
                                var classNamespace = classDeclarationSyntax.GetFullName();
                                var compositionPropertyName = compositionAttribute.ArgumentList?.Arguments[0]
                                    .Expression.NormalizeWhitespace().ToFullString().Replace("\"", "");
                                var compositionPropertyTypeTypeOfExpressionSyntax = compositionAttribute.ArgumentList
                                    ?.Arguments[1].Expression as TypeOfExpressionSyntax;
                                if (compositionPropertyTypeTypeOfExpressionSyntax == null) continue;

                                var type = compositionPropertyTypeTypeOfExpressionSyntax.Type;
                                var compositionTypeName = type.NormalizeWhitespace().ToFullString();
                                
                                if (type is IdentifierNameSyntax identifierNameSyntax)
                                {
                                    var identifierTypeInfo = semanticModel.GetTypeInfo(identifierNameSyntax);
                                    var containingNamespace = (identifierTypeInfo.Type as INamedTypeSymbol)?.ContainingNamespace;
                                    compositionTypeName = $"{containingNamespace}.{compositionTypeName}";
                                }
                                
                                if (string.IsNullOrWhiteSpace(compositionPropertyName) ||
                                    string.IsNullOrWhiteSpace(compositionTypeName) ||
                                    string.IsNullOrWhiteSpace(classNamespace)) continue;
                                
                                yield return new CompositionResponseMetadata(
                                    template,
                                    classNamespace,
                                    classSymbol.Name,
                                    compositionTypeName,
                                    compositionPropertyName);
                            }
                        }
                    }
                }
            }
        }
    }

    // private static void AnalyseHandlerClassDeclaration(
    //     Compilation compilation,
    //     ClassDeclarationSyntax classDeclarationSyntax)
    // {
    //     var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
    //     // var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
    //     //
    //     // if (classSymbol == null) return;
    //
    //     foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
    //     {
    //         var methodDeclarationSyntax = (MethodDeclarationSyntax) memberDeclarationSyntax;
    //         var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
    //         if (methodSymbol?.Parameters.Length == 1 && methodSymbol.Parameters[0].Type.Name == "HttpRequest")
    //         {
    //             Console.WriteLine($"Found method {methodSymbol.Name}");
    //             
    //             var httpAttributes = methodDeclarationSyntax.AttributeLists
    //                 .First()
    //                 .Attributes
    //                 .Where(x => 
    //                     x.Name.NormalizeWhitespace().ToFullString() == "HttpGet" ||
    //                     x.Name.NormalizeWhitespace().ToFullString() == "HttpPost")
    //                 .ToList();
    //
    //             foreach (var httpAttribute in httpAttributes)
    //             {
    //                 var firstArgument = httpAttribute.ArgumentList?.Arguments.First();
    //                 if (firstArgument != null)
    //                 {
    //                     var template = firstArgument.Expression.NormalizeWhitespace().ToFullString();
    //                     var text =
    //                         $"Found {httpAttribute.Name} with path template {template}";
    //                     Console.WriteLine(text);
    //                 }
    //             }
    //         }
    //     }
    //     
    // }
    
    private static string GenerateCompositionDocumentationHandler(
        string rootNamespace,
        string generatedClassName,
        IEnumerable<CompositionResponseMetadata> compositionResponseMetadataItems)
    {
        var routeTemplateGrouping = compositionResponseMetadataItems
            .GroupBy(g => g.RouteTemplate);
        var sb = new StringBuilder();
        sb.Append($@"
namespace {rootNamespace}
{{");

        
        foreach (var group in routeTemplateGrouping)
        {
            var firstInGroup = group.First();
            var documentationHandlerNamespace = firstInGroup.ClassNamespace
                .Replace(".", "")
                .Replace("+", "");
            var documentationHandlerName = $"{documentationHandlerNamespace}DocumentationHandler";
            var routeTemplate = firstInGroup.RouteTemplate;
            var compositionModelName = $"{documentationHandlerNamespace}DocumentationCompositionModel";

            sb.Append($@"
    public class {compositionModelName}
    {{");

            foreach (var compositionPart in group)
            {
                sb.Append($@"
        public {compositionPart.CompositionFullyQualifiedTypeName} {compositionPart.CompositionPropertyName} {{ get; set; }}
");
                
            }
            sb.Append($@"
        }}
");

            sb.Append($@"
    public class {documentationHandlerName} : ICompositionRequestsHandler
    {{
        [HttpGet({routeTemplate})]
        [ProducesResponseType(typeof({compositionModelName}), StatusCodes.Status200OK)]
        public Task Handle(HttpRequest request)
        {{
            return Task.CompletedTask;
        }}
    }}
");

        }
        sb.Append($@"
}}
");

        return sb.ToString();

    }
}