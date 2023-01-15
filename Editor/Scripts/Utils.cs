using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static HeapExplorer.Option;

namespace HeapExplorer {
  public static class Utils {
    /// <summary>
    /// Returns the <see cref="ulong"/> value as <see cref="long"/>, clamping it if it doesn't fit. 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToLongClamped(this ulong value, bool doLog = true) {
      if (value > long.MaxValue) {
        if (doLog)
          Debug.LogWarningFormat(
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
    public static uint ToUIntClamped(this int value, bool doLog = true) {
      if (value < 0) {
        if (doLog)
          Debug.LogWarningFormat(
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
    public static ulong ToULongClamped(this long value, bool doLog = true) {
      if (value < 0) {
        if (doLog)
          Debug.LogWarningFormat(
            "HeapExplorer: clamping long value {0} to ulong 0, this shouldn't happen.", value
          );
        return 0;
      }

      return (ulong) value;
    }

    /// <summary>
    /// <see cref="IDictionary{TKey,TValue}.TryGetValue"/> but returns an <see cref="Option{A}"/> instead.
    /// </summary>
    /// <example><code><![CDATA[
    /// var maybeList = m_ConnectionsFrom.get(key);
    /// ]]></code></example>
    public static Option<V> get<K, V>(this IDictionary<K, V> dictionary, K key) => 
      dictionary.TryGetValue(key, out var value) ? Some(value) : new Option<V>();

    /// <summary>
    /// Gets a value by the key from the dictionary. If a key is not stored yet it is created using the
    /// <see cref="ifMissing"/> function and then stored in the dictionary. 
    /// </summary>
    /// <example><code><![CDATA[
    /// var list = m_ConnectionsFrom.getOrUpdate(key, _ => new List<int>());
    /// ]]></code></example>
    public static V getOrUpdate<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> ifMissing) {
      if (!dictionary.TryGetValue(key, out var value)) {
        value = dictionary[key] = ifMissing(key);
      }

      return value;
    }

    /// <summary>
    /// Gets a value by the key from the dictionary. If a key is not stored yet it returns <see cref="ifMissing"/>
    /// instead.
    /// </summary>
    /// <example><code><![CDATA[
    /// var list = m_ConnectionsFrom.getOrUpdate(key, _ => new List<int>());
    /// ]]></code></example>
    public static V getOrElse<K, V>(this IDictionary<K, V> dictionary, K key, V ifMissing) => 
      dictionary.TryGetValue(key, out var value) ? value : ifMissing;
  }
}