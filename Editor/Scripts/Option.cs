using System;
using System.Runtime.CompilerServices;

namespace HeapExplorer; 

/// <summary>
/// Represents a value which may be there (the `Some` case) or may not be there (the `None` case).
/// <para/>
/// Think of type-safe nullable reference types or <see cref="Nullable{T}"/> but it works for both reference and value
/// types.
/// </summary>
public readonly struct Option<A> {
  /// <summary>The stored value.</summary>
  public readonly A __unsafeGet;
  
  /// <summary>Whether <see cref="__unsafeGet"/> contains a value.</summary>
  public readonly bool isSome;

  /// <summary>Whether <see cref="__unsafeGet"/> does not contain a value.</summary>
  public bool isNone => !isSome;

  public Option(A value) {
    __unsafeGet = value;
    isSome = true;
  }

  /// <example><code><![CDATA[
  /// void process(Foo? maybeFoo) {
  ///   if (nullable.valueOut(out var foo)) {
  ///     // use foo
  ///   }
  /// }
  /// ]]></code></example>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool valueOut(out A value) {
    value = __unsafeGet;
    return isSome;
  }

  /// <summary>Returns <see cref="__unsafeGet"/> if this is `Some` or <see cref="ifNoValue"/> otherwise.</summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public A getOrElse(A ifNoValue) =>
    isSome ? __unsafeGet : ifNoValue;
  
  /// <summary>
  /// Returns <see cref="ifNoValue"/> when this is `None` or runs the <see cref="ifHasValue"/> if this is `Some`.
  /// </summary>
  /// <example><code><![CDATA[
  /// m_Value = m_GCHandle.managedObject.fold("", _ => _.type.name);
  /// ]]></code></example>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public B fold<B>(B ifNoValue, Func<A, B> ifHasValue) => 
    isSome ? ifHasValue(__unsafeGet) : ifNoValue;
    
  /// <summary>
  /// Runs the <see cref="mapper"/> if this is `Some` or else returns `None`.
  /// </summary>
  /// <example><code><![CDATA[
  /// Bar? process(Foo? maybeFoo) => maybeFoo.map(foo => foo.toBar());
  /// ]]></code></example>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Option<B> map<B>(Func<A, B> mapper) => 
    isSome ? new Option<B>(mapper(__unsafeGet)) : new Option<B>();
    
  /// <summary><see cref="map{B}"/> operation that allows to avoid a closure allocation.</summary>
  /// <example><code><![CDATA[
  /// Bar? process(Foo? maybeFoo) => maybeFoo.map(data, (foo, data) => foo.toBar(data));
  /// ]]></code></example>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Option<B> map<Data, B>(Data data, Func<A, Data, B> mapper) => 
    isSome ? new Option<B>(mapper(__unsafeGet, data)) : new Option<B>();

  /// <inheritdoc cref="None._"/>
  public static implicit operator Option<A>(None _) => new Option<A>();
}

public static class Option {
  /// <summary>Creates a `Some` variant of the <see cref="Option{A}"/>.</summary>
  public static Option<A> Some<A>(A a) => new Option<A>(a);
}

public readonly struct None {
  /// <summary>
  /// Allows you to easily return a `None` case for <see cref="Option{A}"/>.
  /// </summary>
  /// <example><code><![CDATA[
  /// Option<int> find() {
  ///   // ...
  ///   if (someCondition()) return None._;
  ///   // ...
  /// }
  /// ]]></code></example>
  public static readonly None _ = new None();
}