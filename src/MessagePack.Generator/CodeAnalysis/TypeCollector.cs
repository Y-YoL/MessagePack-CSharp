﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.Generator.CodeAnalysis;

public class MessagePackGeneratorResolveFailedException : Exception
{
    public MessagePackGeneratorResolveFailedException(string message)
        : base(message)
    {
    }
}

internal class ReferenceSymbols
{
#pragma warning disable SA1401 // Fields should be private
    internal readonly INamedTypeSymbol MessagePackObjectAttribute;
    internal readonly INamedTypeSymbol UnionAttribute;
    internal readonly INamedTypeSymbol SerializationConstructorAttribute;
    internal readonly INamedTypeSymbol KeyAttribute;
    internal readonly INamedTypeSymbol IgnoreAttribute;
    internal readonly INamedTypeSymbol? IgnoreDataMemberAttribute;
    internal readonly INamedTypeSymbol IMessagePackSerializationCallbackReceiver;
    internal readonly INamedTypeSymbol MessagePackFormatterAttribute;
#pragma warning restore SA1401 // Fields should be private

    public ReferenceSymbols(Compilation compilation, Action<string> logger)
    {
        this.MessagePackObjectAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute")
            ?? throw new InvalidOperationException("failed to get metadata of MessagePack.MessagePackObjectAttribute");

        this.UnionAttribute = compilation.GetTypeByMetadataName("MessagePack.UnionAttribute")
            ?? throw new InvalidOperationException("failed to get metadata of MessagePack.UnionAttribute");

        this.SerializationConstructorAttribute = compilation.GetTypeByMetadataName("MessagePack.SerializationConstructorAttribute")
            ?? throw new InvalidOperationException("failed to get metadata of MessagePack.SerializationConstructorAttribute");

        this.KeyAttribute = compilation.GetTypeByMetadataName("MessagePack.KeyAttribute")
            ?? throw new InvalidOperationException("failed to get metadata of MessagePack.KeyAttribute");

        this.IgnoreAttribute = compilation.GetTypeByMetadataName("MessagePack.IgnoreMemberAttribute")
            ?? throw new InvalidOperationException("failed to get metadata of MessagePack.IgnoreMemberAttribute");

        this.IgnoreDataMemberAttribute = compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute");
        if (this.IgnoreDataMemberAttribute == null)
        {
            logger("failed to get metadata of System.Runtime.Serialization.IgnoreDataMemberAttribute");
        }

        this.IMessagePackSerializationCallbackReceiver = compilation.GetTypeByMetadataName("MessagePack.IMessagePackSerializationCallbackReceiver")
            ?? throw new InvalidOperationException("failed to get metadata of MessagePack.IMessagePackSerializationCallbackReceiver");

        this.MessagePackFormatterAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackFormatterAttribute")
            ?? throw new InvalidOperationException("failed to get metadata of MessagePack.MessagePackFormatterAttribute");
    }
}

internal static class AnalyzerUtilities
{
    internal static string GetHelpLink(string diagnosticId) => $"https://github.com/neuecc/MessagePack-CSharp/blob/master/doc/analyzers/{diagnosticId}.md";
}

public class TypeCollector
{
    public const string UseMessagePackObjectAttributeId = "MsgPack003";
    public const string AttributeMessagePackObjectMembersId = "MsgPack004";
    public const string InvalidMessagePackObjectId = "MsgPack005";
    internal const string Category = "Usage";

    internal static readonly DiagnosticDescriptor TypeMustBeMessagePackObject = new DiagnosticDescriptor(
        id: UseMessagePackObjectAttributeId,
        title: "Use MessagePackObjectAttribute",
        category: Category,
        messageFormat: "Type must be marked with MessagePackObjectAttribute. {0}.", // type.Name
        description: "Type must be marked with MessagePackObjectAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(UseMessagePackObjectAttributeId));

    internal static readonly DiagnosticDescriptor PublicMemberNeedsKey = new DiagnosticDescriptor(
        id: AttributeMessagePackObjectMembersId,
        title: "Attribute public members of MessagePack objects",
        category: Category,
        messageFormat: "Public members of MessagePackObject-attributed types require either KeyAttribute or IgnoreMemberAttribute. {0}.{1}.", // type.Name + "." + item.Name
        description: "Public member must be marked with KeyAttribute or IgnoreMemberAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AttributeMessagePackObjectMembersId));

    internal static readonly DiagnosticDescriptor BothStringAndIntKeyAreNull = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: "Attribute public members of MessagePack objects",
        category: Category,
        messageFormat: "Both int and string keys are null. {0}.{1}.", // type.Name + "." + item.Name
        description: "An int or string key must be supplied to the KeyAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AttributeMessagePackObjectMembersId));

    private static readonly SymbolDisplayFormat BinaryWriteFormat = new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

    private static readonly SymbolDisplayFormat ShortTypeNameFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

    private static readonly HashSet<string> EmbeddedTypes = new(new[]
    {
        "short",
        "int",
        "long",
        "ushort",
        "uint",
        "ulong",
        "float",
        "double",
        "bool",
        "byte",
        "sbyte",
        "decimal",
        "char",
        "string",
        "object",
        "System.Guid",
        "System.TimeSpan",
        "System.DateTime",
        "System.DateTimeOffset",

        "MessagePack.Nil",

        // and arrays
        "short[]",
        "int[]",
        "long[]",
        "ushort[]",
        "uint[]",
        "ulong[]",
        "float[]",
        "double[]",
        "bool[]",
        "byte[]",
        "sbyte[]",
        "decimal[]",
        "char[]",
        "string[]",
        "System.DateTime[]",
        "System.ArraySegment<byte>",
        "System.ArraySegment<byte>?",

        // extensions
        "UnityEngine.Vector2",
        "UnityEngine.Vector3",
        "UnityEngine.Vector4",
        "UnityEngine.Quaternion",
        "UnityEngine.Color",
        "UnityEngine.Bounds",
        "UnityEngine.Rect",
        "UnityEngine.AnimationCurve",
        "UnityEngine.RectOffset",
        "UnityEngine.Gradient",
        "UnityEngine.WrapMode",
        "UnityEngine.GradientMode",
        "UnityEngine.Keyframe",
        "UnityEngine.Matrix4x4",
        "UnityEngine.GradientColorKey",
        "UnityEngine.GradientAlphaKey",
        "UnityEngine.Color32",
        "UnityEngine.LayerMask",
        "UnityEngine.Vector2Int",
        "UnityEngine.Vector3Int",
        "UnityEngine.RangeInt",
        "UnityEngine.RectInt",
        "UnityEngine.BoundsInt",

        "System.Reactive.Unit",
    });

    private static readonly Dictionary<string, string> KnownGenericTypes = new()
    {
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line
        { "System.Collections.Generic.List<>", "MsgPack::Formatters.ListFormatter<TREPLACE>" },
        { "System.Collections.Generic.LinkedList<>", "MsgPack::Formatters.LinkedListFormatter<TREPLACE>" },
        { "System.Collections.Generic.Queue<>", "MsgPack::Formatters.QueueFormatter<TREPLACE>" },
        { "System.Collections.Generic.Stack<>", "MsgPack::Formatters.StackFormatter<TREPLACE>" },
        { "System.Collections.Generic.HashSet<>", "MsgPack::Formatters.HashSetFormatter<TREPLACE>" },
        { "System.Collections.ObjectModel.ReadOnlyCollection<>", "MsgPack::Formatters.ReadOnlyCollectionFormatter<TREPLACE>" },
        { "System.Collections.Generic.IList<>", "MsgPack::Formatters.InterfaceListFormatter2<TREPLACE>" },
        { "System.Collections.Generic.ICollection<>", "MsgPack::Formatters.InterfaceCollectionFormatter2<TREPLACE>" },
        { "System.Collections.Generic.IEnumerable<>", "MsgPack::Formatters.InterfaceEnumerableFormatter<TREPLACE>" },
        { "System.Collections.Generic.Dictionary<,>", "MsgPack::Formatters.DictionaryFormatter<TREPLACE>" },
        { "System.Collections.Generic.IDictionary<,>", "MsgPack::Formatters.InterfaceDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Generic.SortedDictionary<,>", "MsgPack::Formatters.SortedDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Generic.SortedList<,>", "MsgPack::Formatters.SortedListFormatter<TREPLACE>" },
        { "System.Linq.ILookup<,>", "MsgPack::Formatters.InterfaceLookupFormatter<TREPLACE>" },
        { "System.Linq.IGrouping<,>", "MsgPack::Formatters.InterfaceGroupingFormatter<TREPLACE>" },
        { "System.Collections.ObjectModel.ObservableCollection<>", "MsgPack::Formatters.ObservableCollectionFormatter<TREPLACE>" },
        { "System.Collections.ObjectModel.ReadOnlyObservableCollection<>", "MsgPack::Formatters.ReadOnlyObservableCollectionFormatter<TREPLACE>" },
        { "System.Collections.Generic.IReadOnlyList<>", "MsgPack::Formatters.InterfaceReadOnlyListFormatter<TREPLACE>" },
        { "System.Collections.Generic.IReadOnlyCollection<>", "MsgPack::Formatters.InterfaceReadOnlyCollectionFormatter<TREPLACE>" },
        { "System.Collections.Generic.ISet<>", "MsgPack::Formatters.InterfaceSetFormatter<TREPLACE>" },
        { "System.Collections.Concurrent.ConcurrentBag<>", "MsgPack::Formatters.ConcurrentBagFormatter<TREPLACE>" },
        { "System.Collections.Concurrent.ConcurrentQueue<>", "MsgPack::Formatters.ConcurrentQueueFormatter<TREPLACE>" },
        { "System.Collections.Concurrent.ConcurrentStack<>", "MsgPack::Formatters.ConcurrentStackFormatter<TREPLACE>" },
        { "System.Collections.ObjectModel.ReadOnlyDictionary<,>", "MsgPack::Formatters.ReadOnlyDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Generic.IReadOnlyDictionary<,>", "MsgPack::Formatters.InterfaceReadOnlyDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Concurrent.ConcurrentDictionary<,>", "MsgPack::Formatters.ConcurrentDictionaryFormatter<TREPLACE>" },
        { "System.Lazy<>", "MsgPack::Formatters.LazyFormatter<TREPLACE>" },
        { "System.Threading.Tasks<>", "MsgPack::Formatters.TaskValueFormatter<TREPLACE>" },

        { "System.Tuple<>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,,,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },

        { "System.ValueTuple<>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,,,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },

        { "System.Collections.Generic.KeyValuePair<,>", "MsgPack::Formatters.KeyValuePairFormatter<TREPLACE>" },
        { "System.Threading.Tasks.ValueTask<>", "MsgPack::Formatters.KeyValuePairFormatter<TREPLACE>" },
        { "System.ArraySegment<>", "MsgPack::Formatters.ArraySegmentFormatter<TREPLACE>" },

        // extensions
        { "System.Collections.Immutable.ImmutableArray<>", "MsgPack::ImmutableCollection.ImmutableArrayFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableList<>", "MsgPack::ImmutableCollection.ImmutableListFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableDictionary<,>", "MsgPack::ImmutableCollection.ImmutableDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableHashSet<>", "MsgPack::ImmutableCollection.ImmutableHashSetFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableSortedDictionary<,>", "MsgPack::ImmutableCollection.ImmutableSortedDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableSortedSet<>", "MsgPack::ImmutableCollection.ImmutableSortedSetFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableQueue<>", "MsgPack::ImmutableCollection.ImmutableQueueFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableStack<>", "MsgPack::ImmutableCollection.ImmutableStackFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableList<>", "MsgPack::ImmutableCollection.InterfaceImmutableListFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableDictionary<,>", "MsgPack::ImmutableCollection.InterfaceImmutableDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableQueue<>", "MsgPack::ImmutableCollection.InterfaceImmutableQueueFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableSet<>", "MsgPack::ImmutableCollection.InterfaceImmutableSetFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableStack<>", "MsgPack::ImmutableCollection.InterfaceImmutableStackFormatter<TREPLACE>" },

        { "Reactive.Bindings.ReactiveProperty<>", "MsgPack::ReactivePropertyExtension.ReactivePropertyFormatter<TREPLACE>" },
        { "Reactive.Bindings.IReactiveProperty<>", "MsgPack::ReactivePropertyExtension.InterfaceReactivePropertyFormatter<TREPLACE>" },
        { "Reactive.Bindings.IReadOnlyReactiveProperty<>", "MsgPack::ReactivePropertyExtension.InterfaceReadOnlyReactivePropertyFormatter<TREPLACE>" },
        { "Reactive.Bindings.ReactiveCollection<>", "MsgPack::ReactivePropertyExtension.ReactiveCollectionFormatter<TREPLACE>" },
#pragma warning restore SA1509 // Opening braces should not be preceded by blank line
    };

    private readonly bool isForceUseMap;
    private readonly IGeneratorContext? context;
    private readonly AnalyzerOptions options;
    private readonly ReferenceSymbols typeReferences;
    private readonly ITypeSymbol? targetType;
    private readonly bool excludeArrayElement;
    private readonly HashSet<string> externalIgnoreTypeNames;

    // visitor workspace:
#pragma warning disable RS1024 // Compare symbols correctly (https://github.com/dotnet/roslyn-analyzers/issues/5246)
    private readonly HashSet<ITypeSymbol> alreadyCollected = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
    private readonly ImmutableSortedSet<ObjectSerializationInfo>.Builder collectedObjectInfo = ImmutableSortedSet.CreateBuilder<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<EnumSerializationInfo>.Builder collectedEnumInfo = ImmutableSortedSet.CreateBuilder<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<GenericSerializationInfo>.Builder collectedGenericInfo = ImmutableSortedSet.CreateBuilder<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<UnionSerializationInfo>.Builder collectedUnionInfo = ImmutableSortedSet.CreateBuilder<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default);

    private readonly Compilation compilation;

    private TypeCollector(Compilation compilation, AnalyzerOptions options, ITypeSymbol targetType, IGeneratorContext? context)
    {
        this.typeReferences = new ReferenceSymbols(compilation, _ => { });
        this.isForceUseMap = options.UsesMapMode;
        this.context = context;
        this.options = options;
        this.externalIgnoreTypeNames = new HashSet<string>(options.IgnoreTypeNames ?? Array.Empty<string>());
        this.compilation = compilation;
        this.excludeArrayElement = true;
        this.context = context;

        if (IsAllowedAccessibility(targetType.DeclaredAccessibility))
        {
            if (((targetType.TypeKind == TypeKind.Interface) && targetType.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)))
                || ((targetType.TypeKind == TypeKind.Class && targetType.IsAbstract) && targetType.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)))
                || ((targetType.TypeKind == TypeKind.Class) && targetType.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute)))
                || ((targetType.TypeKind == TypeKind.Struct) && targetType.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute))))
            {
                this.targetType = targetType;
            }
        }
    }

    public static FullModel? Collect(Compilation compilation, AnalyzerOptions options, TypeDeclarationSyntax typeDeclaration, IGeneratorContext? generatorContext, CancellationToken cancellationToken)
    {
        SemanticModel semanticModel = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is ITypeSymbol typeSymbol)
        {
            if (Collect(compilation, options, typeSymbol, generatorContext) is FullModel model)
            {
                return model;
            }
        }

        return null;
    }

    public static FullModel? Collect(Compilation compilation, AnalyzerOptions options, ITypeSymbol targetType, IGeneratorContext? context)
    {
        TypeCollector collector = new(compilation, options, targetType, context);
        if (collector.targetType is null)
        {
            return null;
        }

        FullModel model = collector.Collect();
        return model;
    }

    private void ResetWorkspace()
    {
        this.alreadyCollected.Clear();
        this.collectedObjectInfo.Clear();
        this.collectedEnumInfo.Clear();
        this.collectedGenericInfo.Clear();
        this.collectedUnionInfo.Clear();
    }

    // EntryPoint
    public FullModel Collect()
    {
        this.ResetWorkspace();

        if (this.targetType is not null)
        {
            this.CollectCore(this.targetType);
        }

        return new FullModel(
            this.collectedObjectInfo.ToImmutable(),
            this.collectedEnumInfo.ToImmutable(),
            this.collectedGenericInfo.ToImmutable(),
            this.collectedUnionInfo.ToImmutable(),
            this.options);
    }

    // Gate of recursive collect
    private void CollectCore(ITypeSymbol typeSymbol)
    {
        if (!this.alreadyCollected.Add(typeSymbol))
        {
            return;
        }

        var typeSymbolString = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToString() ?? throw new InvalidOperationException();
        if (EmbeddedTypes.Contains(typeSymbolString))
        {
            return;
        }

        if (this.externalIgnoreTypeNames.Contains(typeSymbolString))
        {
            return;
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            this.CollectArray((IArrayTypeSymbol)this.ToTupleUnderlyingType(arrayTypeSymbol));
            return;
        }

        if (!this.IsAllowAccessibility(typeSymbol))
        {
            return;
        }

        if (!(typeSymbol is INamedTypeSymbol type))
        {
            return;
        }

        var customFormatterAttr = typeSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute));
        if (customFormatterAttr != null)
        {
            return;
        }

        if (type.EnumUnderlyingType != null)
        {
            this.CollectEnum(type, type.EnumUnderlyingType);
            return;
        }

        if (type.IsGenericType)
        {
            this.CollectGeneric((INamedTypeSymbol)this.ToTupleUnderlyingType(type));
            return;
        }

        if (type.Locations[0].IsInMetadata)
        {
            return;
        }

        if (type.TypeKind == TypeKind.Interface || (type.TypeKind == TypeKind.Class && type.IsAbstract))
        {
            this.CollectUnion(type);
            return;
        }

        this.CollectObject(type);
    }

    private void CollectEnum(INamedTypeSymbol type, ISymbol enumUnderlyingType)
    {
        EnumSerializationInfo info = new(
            type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
            type.ToDisplayString(ShortTypeNameFormat).Replace(".", "_"),
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            enumUnderlyingType.ToDisplayString(BinaryWriteFormat));
        this.collectedEnumInfo.Add(info);
    }

    private void CollectUnion(INamedTypeSymbol type)
    {
        ImmutableArray<TypedConstant>[] unionAttrs = type.GetAttributes().Where(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)).Select(x => x.ConstructorArguments).ToArray();
        if (unionAttrs.Length == 0)
        {
            throw new MessagePackGeneratorResolveFailedException("Serialization Type must mark UnionAttribute." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        // 0, Int  1, SubType
        UnionSubTypeInfo UnionSubTypeInfoSelector(ImmutableArray<TypedConstant> x)
        {
            if (!(x[0] is { Value: int key }) || !(x[1] is { Value: ITypeSymbol typeSymbol }))
            {
                throw new NotSupportedException("AOT code generation only supports UnionAttribute that uses a Type parameter, but the " + type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) + " type uses an unsupported parameter.");
            }

            var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return new UnionSubTypeInfo(key, typeName);
        }

        var info = new UnionSerializationInfo(
            type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
            type.Name,
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            unionAttrs.Select(UnionSubTypeInfoSelector).OrderBy(x => x.Key).ToArray());

        this.collectedUnionInfo.Add(info);
    }

    private void CollectGenericUnion(INamedTypeSymbol type)
    {
        var unionAttrs = type.GetAttributes().Where(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)).Select(x => x.ConstructorArguments);
        using var enumerator = unionAttrs.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }

        do
        {
            var x = enumerator.Current;
            if (x[1] is { Value: INamedTypeSymbol unionType } && this.alreadyCollected.Contains(unionType) == false)
            {
                this.CollectCore(unionType);
            }
        }
        while (enumerator.MoveNext());
    }

    private void CollectArray(IArrayTypeSymbol array)
    {
        ITypeSymbol elemType = array.ElementType;
        if (!this.excludeArrayElement)
        {
            this.CollectCore(elemType);
        }

        var fullName = array.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var elementTypeDisplayName = elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string formatterName;
        if (array.IsSZArray)
        {
            formatterName = "MsgPack::Formatters.ArrayFormatter<" + elementTypeDisplayName + ">";
        }
        else
        {
            formatterName = array.Rank switch
            {
                2 => "MsgPack::Formatters.TwoDimensionalArrayFormatter<" + elementTypeDisplayName + ">",
                3 => "MsgPack::Formatters.ThreeDimensionalArrayFormatter<" + elementTypeDisplayName + ">",
                4 => "MsgPack::Formatters.FourDimensionalArrayFormatter<" + elementTypeDisplayName + ">",
                _ => throw new InvalidOperationException("does not supports array dimension, " + fullName),
            };
        }

        var info = new GenericSerializationInfo(fullName, formatterName, elemType is ITypeParameterSymbol);
        this.collectedGenericInfo.Add(info);
    }

    private ITypeSymbol ToTupleUnderlyingType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol array)
        {
            return this.compilation.CreateArrayTypeSymbol(this.ToTupleUnderlyingType(array.ElementType), array.Rank);
        }

        if (typeSymbol is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return typeSymbol;
        }

        namedType = namedType.TupleUnderlyingType ?? namedType;
        var newTypeArguments = namedType.TypeArguments.Select(this.ToTupleUnderlyingType).ToArray();
        if (!namedType.TypeArguments.SequenceEqual(newTypeArguments))
        {
            return namedType.ConstructedFrom.Construct(newTypeArguments);
        }

        return namedType;
    }

    private void CollectGeneric(INamedTypeSymbol type)
    {
        INamedTypeSymbol genericType = type.ConstructUnboundGenericType();
        var genericTypeString = genericType.ToDisplayString();
        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isOpenGenericType = this.IsOpenGenericTypeRecursively(type);

        // special case
        if (fullName == "global::System.ArraySegment<byte>" || fullName == "global::System.ArraySegment<byte>?")
        {
            return;
        }

        // nullable
        if (genericTypeString == "T?")
        {
            var firstTypeArgument = type.TypeArguments[0];
            this.CollectCore(firstTypeArgument);

            if (EmbeddedTypes.Contains(firstTypeArgument.ToString()!))
            {
                return;
            }

            var info = new GenericSerializationInfo(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), "MsgPack::Formatters.NullableFormatter<" + firstTypeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">", isOpenGenericType);
            this.collectedGenericInfo.Add(info);
            return;
        }

        // collection
        if (KnownGenericTypes.TryGetValue(genericTypeString, out var formatter))
        {
            foreach (ITypeSymbol item in type.TypeArguments)
            {
                this.CollectCore(item);
            }

            var typeArgs = string.Join(", ", type.TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            var f = formatter.Replace("TREPLACE", typeArgs);

            var info = new GenericSerializationInfo(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), f, isOpenGenericType);

            this.collectedGenericInfo.Add(info);

            if (genericTypeString != "System.Linq.ILookup<,>")
            {
                return;
            }

            formatter = KnownGenericTypes["System.Linq.IGrouping<,>"];
            f = formatter.Replace("TREPLACE", typeArgs);

            var groupingInfo = new GenericSerializationInfo("global::System.Linq.IGrouping<" + typeArgs + ">", f, isOpenGenericType);
            this.collectedGenericInfo.Add(groupingInfo);

            formatter = KnownGenericTypes["System.Collections.Generic.IEnumerable<>"];
            typeArgs = type.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            f = formatter.Replace("TREPLACE", typeArgs);

            var enumerableInfo = new GenericSerializationInfo("global::System.Collections.Generic.IEnumerable<" + typeArgs + ">", f, isOpenGenericType);
            this.collectedGenericInfo.Add(enumerableInfo);
            return;
        }

        // Generic types
        if (type.IsDefinition)
        {
            this.CollectGenericUnion(type);
            this.CollectObject(type);
            return;
        }
        else
        {
            // Collect substituted types for the properties and fields.
            // NOTE: It is used to register formatters from nested generic type.
            //       However, closed generic types such as `Foo<string>` are not registered as a formatter.
            this.GetObjectInfo(type);

            // Collect generic type definition, that is not collected when it is defined outside target project.
            this.CollectCore(type.OriginalDefinition);
        }

        // Collect substituted types for the type parameters (e.g. Bar in Foo<Bar>)
        foreach (var item in type.TypeArguments)
        {
            this.CollectCore(item);
        }

        var formatterBuilder = new StringBuilder();
        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            formatterBuilder.Append(type.ContainingNamespace.ToDisplayString() + ".");
        }

        formatterBuilder.Append(type.Name);
        formatterBuilder.Append("Formatter<");
        var typeArgumentIterator = type.TypeArguments.GetEnumerator();
        {
            if (typeArgumentIterator.MoveNext())
            {
                formatterBuilder.Append(typeArgumentIterator.Current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            while (typeArgumentIterator.MoveNext())
            {
                formatterBuilder.Append(", ");
                formatterBuilder.Append(typeArgumentIterator.Current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        formatterBuilder.Append('>');

        var genericSerializationInfo = new GenericSerializationInfo(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), $"Formatters::{formatterBuilder}", isOpenGenericType);
        this.collectedGenericInfo.Add(genericSerializationInfo);
    }

    private void CollectObject(INamedTypeSymbol type)
    {
        ObjectSerializationInfo? info = this.GetObjectInfo(type);
        if (info is not null)
        {
            this.collectedObjectInfo.Add(info);
        }
    }

    private ObjectSerializationInfo? GetObjectInfo(INamedTypeSymbol type)
    {
        List<Diagnostic> diagnostics = new();
        var isClass = !type.IsValueType;
        var isOpenGenericType = type.IsGenericType;

        AttributeData? contractAttr = type.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute));
        if (contractAttr is null)
        {
            diagnostics.Add(Diagnostic.Create(TypeMustBeMessagePackObject, ((BaseTypeDeclarationSyntax)type.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        var isIntKey = true;
        var intMembers = new Dictionary<int, MemberSerializationInfo>();
        var stringMembers = new Dictionary<string, MemberSerializationInfo>();

        if (this.isForceUseMap || (contractAttr?.ConstructorArguments[0] is { Value: bool firstConstructorArgument } && firstConstructorArgument))
        {
            // All public members are serialize target except [Ignore] member.
            isIntKey = false;

            var hiddenIntKey = 0;

            foreach (IPropertySymbol item in type.GetAllMembers().OfType<IPropertySymbol>().Where(x => !x.IsOverride))
            {
                if (item.GetAttributes().Any(x => (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass?.Name == this.typeReferences.IgnoreDataMemberAttribute?.Name)))
                {
                    continue;
                }

                var isReadable = item.GetMethod != null && IsAllowedAccessibility(item.GetMethod.DeclaredAccessibility) && !item.IsStatic;
                var isWritable = item.SetMethod != null && IsAllowedAccessibility(item.SetMethod.DeclaredAccessibility) && !item.IsStatic;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
                var member = new MemberSerializationInfo(true, isWritable, isReadable, hiddenIntKey++, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                stringMembers.Add(member.StringKey, member);

                if (customFormatterAttr == null)
                {
                    this.CollectCore(item.Type); // recursive collect
                }
            }

            foreach (IFieldSymbol item in type.GetAllMembers().OfType<IFieldSymbol>())
            {
                if (item.GetAttributes().Any(x => (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass?.Name == this.typeReferences.IgnoreDataMemberAttribute?.Name)))
                {
                    continue;
                }

                if (item.IsImplicitlyDeclared)
                {
                    continue;
                }

                var isReadable = IsAllowedAccessibility(item.DeclaredAccessibility) && !item.IsStatic;
                var isWritable = IsAllowedAccessibility(item.DeclaredAccessibility) && !item.IsReadOnly && !item.IsStatic;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
                var member = new MemberSerializationInfo(false, isWritable, isReadable, hiddenIntKey++, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                stringMembers.Add(member.StringKey, member);
                if (customFormatterAttr == null)
                {
                    this.CollectCore(item.Type); // recursive collect
                }
            }
        }
        else
        {
            // Only KeyAttribute members
            var searchFirst = true;
            var hiddenIntKey = 0;

            foreach (IPropertySymbol item in type.GetAllMembers().OfType<IPropertySymbol>())
            {
                if (item.IsIndexer)
                {
                    continue; // .tt files don't generate good code for this yet: https://github.com/neuecc/MessagePack-CSharp/issues/390
                }

                if (item.GetAttributes().Any(x =>
                {
                    var typeReferencesIgnoreDataMemberAttribute = this.typeReferences.IgnoreDataMemberAttribute;
                    return typeReferencesIgnoreDataMemberAttribute != null && (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass.ApproximatelyEqual(typeReferencesIgnoreDataMemberAttribute));
                }))
                {
                    continue;
                }

                var isReadable = item.GetMethod != null && IsAllowedAccessibility(item.GetMethod.DeclaredAccessibility) && !item.IsStatic;
                var isWritable = item.SetMethod != null && IsAllowedAccessibility(item.SetMethod.DeclaredAccessibility) && !item.IsStatic;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
                TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                if (key is null)
                {
                    if (contractAttr is not null)
                    {
                        diagnostics.Add(Diagnostic.Create(PublicMemberNeedsKey, ((PropertyDeclarationSyntax)item.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
                    }
                }
                else
                {
                    var intKey = key is { Value: int intKeyValue } ? intKeyValue : default(int?);
                    var stringKey = key is { Value: string stringKeyValue } ? stringKeyValue : default;
                    if (intKey == null && stringKey == null)
                    {
                        diagnostics.Add(Diagnostic.Create(BothStringAndIntKeyAreNull, ((PropertyDeclarationSyntax)item.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
                    }

                    if (searchFirst)
                    {
                        searchFirst = false;
                        isIntKey = intKey != null;
                    }
                    else
                    {
                        if ((isIntKey && intKey == null) || (!isIntKey && stringKey == null))
                        {
                            throw new MessagePackGeneratorResolveFailedException("all members key type must be same." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }
                    }

                    if (isIntKey)
                    {
                        if (intMembers.ContainsKey(intKey!.Value))
                        {
                            throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, intKey!.Value, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        intMembers.Add(member.IntKey, member);
                    }
                    else if (stringKey is not null)
                    {
                        if (stringMembers.ContainsKey(stringKey!))
                        {
                            throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, hiddenIntKey++, stringKey!, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        stringMembers.Add(member.StringKey, member);
                    }
                }

                var messagePackFormatter = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0];

                if (messagePackFormatter == null)
                {
                    this.CollectCore(item.Type); // recursive collect
                }
            }

            foreach (IFieldSymbol item in type.GetAllMembers().OfType<IFieldSymbol>())
            {
                if (item.IsImplicitlyDeclared)
                {
                    continue;
                }

                if (item.GetAttributes().Any(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute)))
                {
                    continue;
                }

                var isReadable = IsAllowedAccessibility(item.DeclaredAccessibility) && !item.IsStatic;
                var isWritable = IsAllowedAccessibility(item.DeclaredAccessibility) && !item.IsReadOnly && !item.IsStatic;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
                TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                if (key is null)
                {
                    if (contractAttr is not null)
                    {
                        diagnostics.Add(Diagnostic.Create(PublicMemberNeedsKey, item.DeclaringSyntaxReferences[0].GetSyntax().GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
                    }
                }
                else
                {
                    var intKey = key is { Value: int intKeyValue } ? intKeyValue : default(int?);
                    var stringKey = key is { Value: string stringKeyValue } ? stringKeyValue : default;
                    if (intKey == null && stringKey == null)
                    {
                        throw new MessagePackGeneratorResolveFailedException("both IntKey and StringKey are null." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                    }

                    if (searchFirst)
                    {
                        searchFirst = false;
                        isIntKey = intKey != null;
                    }
                    else
                    {
                        if ((isIntKey && intKey == null) || (!isIntKey && stringKey == null))
                        {
                            throw new MessagePackGeneratorResolveFailedException("all members key type must be same." + " type: " + type.Name + " member:" + item.Name);
                        }
                    }

                    if (isIntKey)
                    {
                        if (intMembers.ContainsKey(intKey!.Value))
                        {
                            throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, intKey!.Value, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        if (stringMembers.ContainsKey(stringKey!))
                        {
                            throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, hiddenIntKey++, stringKey!, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        stringMembers.Add(member.StringKey, member);
                    }
                }

                this.CollectCore(item.Type); // recursive collect
            }
        }

        // GetConstructor
        var ctorEnumerator = default(IEnumerator<IMethodSymbol>);
        var ctor = type.Constructors.Where(x => IsAllowedAccessibility(x.DeclaredAccessibility)).SingleOrDefault(x => x.GetAttributes().Any(y => y.AttributeClass != null && y.AttributeClass.ApproximatelyEqual(this.typeReferences.SerializationConstructorAttribute)));
        if (ctor == null)
        {
            ctorEnumerator = type.Constructors.Where(x => IsAllowedAccessibility(x.DeclaredAccessibility)).OrderByDescending(x => x.Parameters.Length).GetEnumerator();

            if (ctorEnumerator.MoveNext())
            {
                ctor = ctorEnumerator.Current;
            }
        }

        // struct allows null ctor
        if (ctor == null && isClass)
        {
            throw new MessagePackGeneratorResolveFailedException("can't find public constructor. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        var constructorParameters = new List<MemberSerializationInfo>();
        if (ctor != null)
        {
            var constructorLookupDictionary = stringMembers.ToLookup(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);
            do
            {
                constructorParameters.Clear();
                var ctorParamIndex = 0;
                foreach (IParameterSymbol item in ctor!.Parameters)
                {
                    MemberSerializationInfo paramMember;
                    if (isIntKey)
                    {
                        if (intMembers.TryGetValue(ctorParamIndex, out paramMember!))
                        {
                            if (item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == paramMember.Type && paramMember.IsReadable)
                            {
                                constructorParameters.Add(paramMember);
                            }
                            else
                            {
                                if (ctorEnumerator != null)
                                {
                                    ctor = null;
                                    continue;
                                }
                                else
                                {
                                    throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, parameterType mismatch. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterIndex:" + ctorParamIndex + " parameterType:" + item.Type.Name);
                                }
                            }
                        }
                        else
                        {
                            if (ctorEnumerator != null)
                            {
                                ctor = null;
                                continue;
                            }
                            else
                            {
                                throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, index not found. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterIndex:" + ctorParamIndex);
                            }
                        }
                    }
                    else
                    {
                        IEnumerable<KeyValuePair<string, MemberSerializationInfo>> hasKey = constructorLookupDictionary[item.Name];
                        using var enumerator = hasKey.GetEnumerator();

                        // hasKey.Count() == 0
                        if (!enumerator.MoveNext())
                        {
                            if (ctorEnumerator == null)
                            {
                                throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, index not found. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterName:" + item.Name);
                            }

                            ctor = null;
                            continue;
                        }

                        var first = enumerator.Current.Value;

                        // hasKey.Count() != 1
                        if (enumerator.MoveNext())
                        {
                            if (ctorEnumerator == null)
                            {
                                throw new MessagePackGeneratorResolveFailedException("duplicate matched constructor parameter name:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterName:" + item.Name + " parameterType:" + item.Type.Name);
                            }

                            ctor = null;
                            continue;
                        }

                        paramMember = first;
                        if (item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == paramMember.Type && paramMember.IsReadable)
                        {
                            constructorParameters.Add(paramMember);
                        }
                        else
                        {
                            if (ctorEnumerator == null)
                            {
                                throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, parameterType mismatch. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterName:" + item.Name + " parameterType:" + item.Type.Name);
                            }

                            ctor = null;
                            continue;
                        }
                    }

                    ctorParamIndex++;
                }
            }
            while (TryGetNextConstructor(ctorEnumerator, ref ctor));

            if (ctor == null)
            {
                throw new MessagePackGeneratorResolveFailedException("can't find matched constructor. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        var hasSerializationConstructor = type.AllInterfaces.Any(x => x.ApproximatelyEqual(this.typeReferences.IMessagePackSerializationCallbackReceiver));
        var needsCastOnBefore = true;
        var needsCastOnAfter = true;
        if (hasSerializationConstructor)
        {
            needsCastOnBefore = !type.GetMembers("OnBeforeSerialize").Any();
            needsCastOnAfter = !type.GetMembers("OnAfterDeserialize").Any();
        }

        ObjectSerializationInfo info = new(isClass, isOpenGenericType, isOpenGenericType ? type.TypeParameters.Select(ToGenericTypeParameterInfo).ToArray() : Array.Empty<GenericTypeParameterInfo>(), constructorParameters.ToArray(), isIntKey, isIntKey ? intMembers.Values.ToArray() : stringMembers.Values.ToArray(), isOpenGenericType ? GetGenericFormatterClassName(type) : GetMinimallyQualifiedClassName(type), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(), hasSerializationConstructor, needsCastOnAfter, needsCastOnBefore)
        {
            Diagnostics = diagnostics,
        };

        return info;
    }

    private static GenericTypeParameterInfo ToGenericTypeParameterInfo(ITypeParameterSymbol typeParameter)
    {
        var constraints = new List<string>();

        // `notnull`, `unmanaged`, `class`, `struct` constraint must come before any constraints.
        if (typeParameter.HasNotNullConstraint)
        {
            constraints.Add("notnull");
        }

        if (typeParameter.HasReferenceTypeConstraint)
        {
            constraints.Add(typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
        }

        if (typeParameter.HasValueTypeConstraint)
        {
            constraints.Add(typeParameter.HasUnmanagedTypeConstraint ? "unmanaged" : "struct");
        }

        // constraint types (IDisposable, IEnumerable ...)
        foreach (var t in typeParameter.ConstraintTypes)
        {
            var constraintTypeFullName = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            constraints.Add(constraintTypeFullName);
        }

        // `new()` constraint must be last in constraints.
        if (typeParameter.HasConstructorConstraint)
        {
            constraints.Add("new()");
        }

        return new GenericTypeParameterInfo(typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), string.Join(", ", constraints));
    }

    private static string GetGenericFormatterClassName(INamedTypeSymbol type)
    {
        return type.Name;
    }

    private static string GetMinimallyQualifiedClassName(INamedTypeSymbol type)
    {
        var name = type.ContainingType is object ? GetMinimallyQualifiedClassName(type.ContainingType) + "_" : string.Empty;
        name += type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        name = name.Replace('.', '_');
        name = name.Replace('<', '_');
        name = name.Replace('>', '_');
        name = Regex.Replace(name, @"\[([,])*\]", match => $"Array{match.Length - 1}");
        name = name.Replace("?", string.Empty);
        return name;
    }

    private static bool TryGetNextConstructor(IEnumerator<IMethodSymbol>? ctorEnumerator, ref IMethodSymbol? ctor)
    {
        if (ctorEnumerator == null || ctor != null)
        {
            return false;
        }

        if (ctorEnumerator.MoveNext())
        {
            ctor = ctorEnumerator.Current;
            return true;
        }
        else
        {
            ctor = null;
            return false;
        }
    }

    private static bool IsAllowedAccessibility(Accessibility accessibility) => accessibility is Accessibility.Public or Accessibility.Internal;

    private bool IsAllowAccessibility(ITypeSymbol symbol)
    {
        do
        {
            if (!IsAllowedAccessibility(symbol.DeclaredAccessibility))
            {
                return false;
            }

            symbol = symbol.ContainingType;
        }
        while (symbol is not null);

        return true;
    }

    private bool IsOpenGenericTypeRecursively(INamedTypeSymbol type)
    {
        return type.IsGenericType && type.TypeArguments.Any(x => x is ITypeParameterSymbol || (x is INamedTypeSymbol symbol && this.IsOpenGenericTypeRecursively(symbol)));
    }
}
