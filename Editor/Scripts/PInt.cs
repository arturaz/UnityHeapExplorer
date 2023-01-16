using System;
using System.Runtime.CompilerServices;

namespace HeapExplorer; 

/// <summary>
/// Positive integer.
/// <para/>
/// Similar to <see cref="uint"/> but is still backed by an <see cref="int"/> so the math behaves the same.
/// </summary>
public readonly struct PInt : IEquatable<PInt> {
  public readonly int asInt;

  PInt(int asInt) {
    this.asInt = asInt;
  }

  public override string ToString() => asInt.ToString();

  /// <summary>Safely casts the <see cref="int"/> value to <see cref="uint"/>.</summary>
  public uint asUInt {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => (uint) asInt;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static implicit operator int(PInt pInt) => pInt.asInt;
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static implicit operator uint(PInt pInt) => pInt.asUInt;
  
  /// <summary>Creates a value, throwing if the supplied <see cref="int"/> is negative.</summary>
  public static PInt createOrThrow(int value) {
    if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "value can't be negative");
    return new PInt(value);
  }

  #region Equality
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool Equals(PInt other) => asInt == other.asInt;
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public override bool Equals(object obj) => obj is PInt other && Equals(other);
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public override int GetHashCode() => asInt;
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool operator ==(PInt left, PInt right) => left.Equals(right);
  
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool operator !=(PInt left, PInt right) => !left.Equals(right);

  #endregion
}