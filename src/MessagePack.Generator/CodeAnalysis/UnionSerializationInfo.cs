﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace MessagePack.Generator.CodeAnalysis;

public record UnionSerializationInfo(
    string? Namespace,
    string Name,
    string FullName,
    UnionSubTypeInfo[] SubTypes) : IResolverRegisterInfo
{
    public IReadOnlyCollection<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();

    public string FileNameHint => $"{CodeAnalysisUtilities.AppendNameToNamespace("Formatters", this.Namespace)}.{this.FormatterNameWithoutNamespace}";

    public string FormatterName => CodeAnalysisUtilities.QualifyWithOptionalNamespace(FormatterNameWithoutNamespace, $"Formatters::{this.Namespace}");

    public string FormatterNameWithoutNamespace => this.Name + "Formatter";

    public virtual bool Equals(UnionSerializationInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FullName == other.FullName
            && Name == other.Name
            && Namespace == other.Namespace
            && SubTypes.SequenceEqual(other.SubTypes);
    }

    public override int GetHashCode() => throw new NotImplementedException();
}
