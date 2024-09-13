﻿namespace Pure.DI.Core.Models;

using System.Runtime.CompilerServices;
using Code;

internal readonly record struct Injection(
    ITypeSymbol Type,
    object? Tag)
{
    public override string ToString() => $"{Type}{(Tag != default && Tag is not MdTagOnSites ? $"({Tag.ValueToString()})" : "")}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Injection other) =>
        (ReferenceEquals(Type, other.Type) || SymbolEqualityComparer.Default.Equals(Type, other.Type))
        && EqualTags(Tag, other.Tag);

    public override int GetHashCode() =>
        SymbolEqualityComparer.Default.GetHashCode(Type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualTags(object? tag, object? otherTag) =>
        SpecialEqualTags(tag, otherTag) || SpecialEqualTags(otherTag, tag)
                                        || Equals(tag, otherTag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SpecialEqualTags(object? tag, object? otherTag) =>
        ReferenceEquals(tag, MdTag.ContextTag) || (tag is MdTagOnSites tagOn && tagOn.Equals(otherTag));
}