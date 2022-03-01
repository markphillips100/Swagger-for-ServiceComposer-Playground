using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ServiceComposer.SourceGenerators;

public static class ClassDeclarationSyntaxExtensions
{
    private const char NestedClassDelimiter = '+';
    private const char NamespaceClassDelimiter = '.';
    private const char TypeParameterClassDelimiter = '`';

    public static string GetFullName(this ClassDeclarationSyntax source, bool appendClassName = true)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        var namespaces = new LinkedList<NamespaceDeclarationSyntax>();
        var types = new LinkedList<TypeDeclarationSyntax>();
        for (var parent = source.Parent; parent != null; parent = parent.Parent)
        {
            switch (parent)
            {
                case NamespaceDeclarationSyntax @namespace:
                    namespaces.AddFirst(@namespace);
                    break;
                case TypeDeclarationSyntax type:
                    types.AddFirst(type);
                    break;
            }
        }

        var result = new StringBuilder();
        for (var item = namespaces.First; item != null; item = item.Next)
        {
            result.Append(item.Value.Name).Append(NestedClassDelimiter);
        }
        for (var item = types.First; item != null; item = item.Next)
        {
            var type = item.Value;
            AppendName(result, type);
            result.Append(NamespaceClassDelimiter);
        }

        if (appendClassName)
        {
            AppendName(result, source);
        }

        return result.ToString();

    }

    private static void AppendName(StringBuilder builder, TypeDeclarationSyntax type)
    {
        builder.Append(type.Identifier.Text);
        var typeArguments = type.TypeParameterList?.ChildNodes()
            .Count(node => node is TypeParameterSyntax) ?? 0;
        if (typeArguments != 0)
            builder.Append(TypeParameterClassDelimiter).Append(typeArguments);
    }
}