using System.Runtime.CompilerServices;
using UnityEngine;

namespace HeapExplorer; 

public static class Utils 
{
  /// <summary>
  /// Returns the <see cref="ulong"/> value as <see cref="long"/>, clamping it if it doesn't fit. 
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static long ToLongClamped(this ulong value, bool doLog = true) 
  {
    if (value > long.MaxValue) 
    {
      if (doLog) Debug.LogWarningFormat(
        "HeapExplorer: clamping ulong value {0} to long value {1}, this shouldn't happen.",
        value, long.MaxValue
      );
      return long.MaxValue;
    }
    
    return (long) value;  
  }
  
  /// <summary>
  /// Returns the <see cref="int"/> value as <see cref="uint"/>, clamping it if it is less than 0. 
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static uint ToUIntClamped(this int value, bool doLog = true) 
  {
    if (value < 0) 
    {
      if (doLog) Debug.LogWarningFormat(
        "HeapExplorer: clamping int value {0} to uint 0, this shouldn't happen.", value
      );
      return 0;
    }
    return (uint) value;  
  }
  
  /// <summary>
  /// Returns the <see cref="long"/> value as <see cref="ulong"/>, clamping it if it is less than 0. 
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ulong ToULongClamped(this long value, bool doLog = true) 
  {
    if (value < 0) 
    {
      if (doLog) Debug.LogWarningFormat(
        "HeapExplorer: clamping long value {0} to ulong 0, this shouldn't happen.", value
      );
      return 0;
    }
    return (ulong) value;  
  }
}