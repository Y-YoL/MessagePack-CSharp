﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public static class CodeAnalysisUtilities
{
    private static readonly HashSet<char> InvalidFileNameChars = new(Path.GetInvalidFileNameChars());

    static CodeAnalysisUtilities()
    {
        // Roslyn really doesn't like angle brackets in file names, even on operating systems that allow them (e.g. linux).
        // See https://github.com/dotnet/roslyn/issues/67653
        InvalidFileNameChars.Add('<');
        InvalidFileNameChars.Add('>');
    }

    public static string QualifyWithOptionalNamespace(string leafTypeOrNamespace, string? baseNamespace)
    {
        return string.IsNullOrEmpty(baseNamespace) ? leafTypeOrNamespace : (baseNamespace!.EndsWith("::") ? $"{baseNamespace}{leafTypeOrNamespace}" : $"{baseNamespace}.{leafTypeOrNamespace}");
    }

    public static string AppendNameToNamespace(string left, string? right)
    {
        return string.IsNullOrEmpty(right) ? left : $"{left}.{right}";
    }

    public static string GetSanitizedFileName(string fileName)
    {
        foreach (char c in InvalidFileNameChars)
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

    internal static IEnumerable<BaseTypeDeclarationSyntax> FindInaccessibleTypes(ITypeSymbol target)
    {
        return from x in EnumerateTypeAndContainingTypes(target)
               where x.Symbol.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal)
               select x.FirstDeclaration;
    }

    internal static IEnumerable<(ITypeSymbol Symbol, BaseTypeDeclarationSyntax? FirstDeclaration)> EnumerateTypeAndContainingTypes(ITypeSymbol target)
    {
        ITypeSymbol? focusedSymbol = target;
        while (focusedSymbol is not null)
        {
            yield return (focusedSymbol, focusedSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as BaseTypeDeclarationSyntax);
            focusedSymbol = focusedSymbol.ContainingType;
        }
    }

    internal static int GetArity(ITypeSymbol dataType)
        => dataType switch
        {
            INamedTypeSymbol namedType => namedType.Arity,
            IArrayTypeSymbol arrayType => GetArity(arrayType.ElementType),
            ITypeParameterSymbol => 0,
            _ => throw new NotSupportedException(),
        };

    internal static ImmutableArray<string> GetTypeParameters(ITypeSymbol dataType)
        => dataType switch
        {
            INamedTypeSymbol namedType => namedType.TypeParameters.Select(t => t.Name).ToImmutableArray(),
            IArrayTypeSymbol arrayType => GetTypeParameters(arrayType.ElementType),
            ITypeParameterSymbol => ImmutableArray<string>.Empty,
            _ => throw new NotSupportedException(),
        };

    internal static ImmutableArray<string> GetTypeArguments(ITypeSymbol dataType)
        => dataType switch
        {
            INamedTypeSymbol namedType => namedType.TypeArguments.Select(t => t.GetCanonicalTypeFullName()).ToImmutableArray(),
            IArrayTypeSymbol arrayType => GetTypeArguments(arrayType.ElementType),
            ITypeParameterSymbol => ImmutableArray<string>.Empty,
            _ => throw new NotSupportedException(),
        };
}
