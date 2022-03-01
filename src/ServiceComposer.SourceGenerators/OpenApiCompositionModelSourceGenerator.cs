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

namespace ServiceComposer.SourceGenerators;

/// <summary>
/// Generates merged viewmodel and documentation composition handler for a given endpoint.
/// </summary>
/// <remarks>
/// Currently only works for handlers in the host project or current compilation tree.
/// Requires additional effor to support handlers in pacakge referenced assemblies.
/// Answer at the following URL tells that we can only inspect syntax tree for currently un-compiled source, hence
/// we must first obtain the referenced assemblies and scan for the types we want from the compiled assemblies.
/// https://stackoverflow.com/questions/68055210/generate-source-based-on-other-assembly-classes-c-source-generator
/// </remarks>
[Generator]
public class OpenApiCompositionModelSourceGenerator : IIncrementalGenerator
{
    public class CompositionResponseMetadata
    {
        public string RouteTemplate { get; set; }
        public string ClassNamespace { get; set; }
        public string CompositionFullyQualifiedTypeName { get; set; }
        public string CompositionPropertyName { get; set; }
    }
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => IsSyntaxTargetForGeneration(s),
                (t, _) => GetSemanticTargetForGeneration(t))
            .Where(a => a != null)!;

        var providers
            = context.CompilationProvider
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Combine(classes.Collect());

        context.RegisterSourceOutput(providers, (spc, source) =>
        {
            var ((compilation, options), attributes) = source;

            Execute(compilation, options, attributes, spc);
        });
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax;

    private static ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
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
        var compositionResponseMetadataItems = GetCompositionResponseMetadata(compilation, classes, context).ToList();
        GenerateCompositionDocumentationHandler(compositionResponseMetadataItems, context);
    }

    private static IEnumerable<CompositionResponseMetadata> GetCompositionResponseMetadata(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classes,
        SourceProductionContext context)
    {
        foreach (var classDeclarationSyntax in classes)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                
            foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
            {
                var methodDeclarationSyntax = (MethodDeclarationSyntax) memberDeclarationSyntax;
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                if (methodSymbol?.Parameters.Length != 1 ||
                    methodSymbol.Parameters[0].Type.Name != "HttpRequest") continue;
                
                var compositionAttributes = GetCompositionAttributeSyntaxList(methodDeclarationSyntax);
                var responseAttributes = GetProducesResponseTypeAttributeSyntaxList(methodDeclarationSyntax);

                if (responseAttributes.Count != 0)
                {
                    ReportInvalidUseOfCompositionAttribute(context, methodSymbol);
                    continue;
                }

                var compositionAttribute = compositionAttributes.FirstOrDefault();
                if (compositionAttribute == null) continue;

                var metadataItems = GetMetadataItemsForMethodHttpGetAttributes(
                    methodDeclarationSyntax,
                    classDeclarationSyntax,
                    compositionAttribute,
                    semanticModel);
                
                foreach(var item in metadataItems)
                {
                    yield return item;
                }
            }
        }
    }

    private static IEnumerable<CompositionResponseMetadata> GetMetadataItemsForMethodHttpGetAttributes(
        MethodDeclarationSyntax methodDeclarationSyntax,
        ClassDeclarationSyntax classDeclarationSyntax,
        AttributeSyntax compositionAttribute,
        SemanticModel semanticModel)
    {
        var classNamespace = classDeclarationSyntax.GetFullName();

        return GetHttpAttributeSyntaxList(methodDeclarationSyntax)
            .Select(httpAttribute =>
        {
            var firstArgument = httpAttribute.ArgumentList?.Arguments.First();
            if (firstArgument == null) return null;

            var template = firstArgument.Expression.NormalizeWhitespace().ToFullString();
            var compositionPropertyName = GetCompositionPropertyName(compositionAttribute);
            var compositionTypeName = GetCompositionPropertyType(compositionAttribute, semanticModel);

            if (string.IsNullOrWhiteSpace(compositionPropertyName) ||
                string.IsNullOrWhiteSpace(compositionTypeName) ||
                string.IsNullOrWhiteSpace(classNamespace)) return null;

            return new CompositionResponseMetadata
            {
                RouteTemplate = template,
                ClassNamespace = classNamespace,
                CompositionFullyQualifiedTypeName = compositionTypeName,
                CompositionPropertyName = compositionPropertyName
            };
        })
            .Where(m => m != null)
            .Cast<CompositionResponseMetadata>();
    }

    private static string GetCompositionPropertyType(AttributeSyntax compositionAttribute, SemanticModel semanticModel)
    {
        if (compositionAttribute.ArgumentList?.Arguments[1].Expression
            is not TypeOfExpressionSyntax compositionPropertyTypeTypeOfExpressionSyntax) return string.Empty;

        var type = compositionPropertyTypeTypeOfExpressionSyntax.Type;
        var compositionTypeName = type.NormalizeWhitespace().ToFullString();

        if (type is not IdentifierNameSyntax identifierNameSyntax) return compositionTypeName;
        
        var identifierTypeInfo = semanticModel.GetTypeInfo(identifierNameSyntax);
        var containingNamespace =
            (identifierTypeInfo.Type as INamedTypeSymbol)?.ContainingNamespace;
        compositionTypeName = $"{containingNamespace}.{compositionTypeName}";

        return compositionTypeName;
    }

    private static string GetCompositionPropertyName(AttributeSyntax compositionAttribute)
    {
        return compositionAttribute.ArgumentList?.Arguments[0]
            .Expression.NormalizeWhitespace().ToFullString().Replace("\"", "");
    }

    private static List<AttributeSyntax> GetHttpAttributeSyntaxList(
        MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var httpAttributes = methodDeclarationSyntax.AttributeLists
            .SelectMany(attrList => attrList
                .Attributes
                .Where(x =>
                    x.Name.NormalizeWhitespace().ToFullString() == "HttpGet")
            )
            .ToList();
        return httpAttributes;
    }

    private static void ReportInvalidUseOfCompositionAttribute(
        SourceProductionContext context,
        IMethodSymbol methodSymbol)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor("SC0001",
                "ProducesResponseType attribute",
                "Method {0} cannot have a ProducesResponseType attribute for a 2XX HTTP status code.  Use ProducesCompositionResponseType attribute instead.",
                "ServiceComposer",
                DiagnosticSeverity.Error,
                true),
            methodSymbol.Locations.FirstOrDefault(),
            methodSymbol.Name));
    }

    private static List<AttributeSyntax> GetProducesResponseTypeAttributeSyntaxList(
        MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var responseAttributes = methodDeclarationSyntax.AttributeLists
            .SelectMany(attrList => attrList
                .Attributes
                .Where(x =>
                    x.Name.NormalizeWhitespace().ToFullString() == "ProducesResponseType")
            )
            .ToList();
        return responseAttributes
            .Where(attr => attr.ArgumentList?.Arguments
                .Any(arg => arg.NameEquals?.NormalizeWhitespace().ToFullString() == "StatusCode" &&
                            arg.Expression.NormalizeWhitespace().ToFullString().StartsWith("2")) == true)
            .ToList();
    }

    private static List<AttributeSyntax> GetCompositionAttributeSyntaxList(
        MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var compositionAttributes = methodDeclarationSyntax.AttributeLists
            .SelectMany(attrList => attrList
                .Attributes
                .Where(x =>
                    x.Name.NormalizeWhitespace().ToFullString() == "ProducesCompositionResponseType")
            )
            .ToList();
        return compositionAttributes;
    }

    private static void GenerateCompositionDocumentationHandler(
        IEnumerable<CompositionResponseMetadata> compositionResponseMetadataItems,
        SourceProductionContext context)
    {
        var routeTemplateGrouping = compositionResponseMetadataItems
            .GroupBy(g => g.RouteTemplate);

        foreach (var group in routeTemplateGrouping)
        {
            var firstInGroup = group.First();
            var documentationHandlerNamespace = firstInGroup.ClassNamespace
                .Replace(".", "")
                .Replace("+", "");
            const string documentationHandlerName = $"DocumentationHandler";
            const string compositionModelName = $"CompositionModel";
            var routeTemplate = firstInGroup.RouteTemplate;

            var sb = new StringBuilder();
            sb.Append($@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore;

namespace {documentationHandlerNamespace}
{{
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
}}
");
            context.AddSource(
                $"{documentationHandlerNamespace}{documentationHandlerName}.g",
                SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}