//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//
using System;

namespace HeapExplorer
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct PackedManagedObject
    {
        /// <summary>
        /// The address of the managed object
        /// </summary>
        public readonly ulong address;

        /// <summary>
        /// `Some` if this object is a static field.
        /// </summary>
        public Option<byte[]> staticBytes;

        /// <summary>
        /// An index into the <see cref="PackedMemorySnapshot.managedTypes"/> array that stores this managed type
        /// </summary>
        public int managedTypesArrayIndex;

        /// <summary>
        /// An index into the <see cref="PackedMemorySnapshot.managedObjects"/> array that stores this managed object
        /// </summary>
        public ArrayIndex managedObjectsArrayIndex;

        /// <summary>
        /// The index into the <see cref="PackedMemorySnapshot.gcHandles"/> array of the snapshot that is connected to
        /// this managed object, if any.
        /// </summary>
        public Option<int> gcHandlesArrayIndex;

        /// <summary>
        /// The index into the <see cref="PackedMemorySnapshot.nativeObjects"/> array of the snapshot that is connected
        /// to this managed object, if any.
        /// </summary>
        public Option<int> nativeObjectsArrayIndex;

        /// <summary>
        /// Size in bytes of this object. `None` if the size is unknown.<br/>
        /// ValueType arrays = count * sizeof(element)<br/>
        /// ReferenceType arrays = count * sizeof(pointer)<br/>
        /// String = length * sizeof(wchar) + strlen("\0\0")
        /// </summary>
        public Option<uint> size;

        public PackedManagedObject(
            ulong address, Option<byte[]> staticBytes, int managedTypesArrayIndex, ArrayIndex managedObjectsArrayIndex, 
            Option<int> gcHandlesArrayIndex, Option<int> nativeObjectsArrayIndex, Option<uint> size
        ) {
            this.address = address;
            this.staticBytes = staticBytes;
            this.managedTypesArrayIndex = managedTypesArrayIndex;
            this.managedObjectsArrayIndex = managedObjectsArrayIndex;
            this.gcHandlesArrayIndex = gcHandlesArrayIndex;
            this.nativeObjectsArrayIndex = nativeObjectsArrayIndex;
            this.size = size;
        }

        public static PackedManagedObject New(
            ulong address,
            ArrayIndex managedObjectsArrayIndex,
            int managedTypesArrayIndex,
            Option<int> gcHandlesArrayIndex = default,
            Option<int> nativeObjectsArrayIndex = default
        ) =>
            new PackedManagedObject(
                address: address,
                managedTypesArrayIndex: managedTypesArrayIndex,
                managedObjectsArrayIndex: managedObjectsArrayIndex,
                gcHandlesArrayIndex: gcHandlesArrayIndex,
                nativeObjectsArrayIndex: nativeObjectsArrayIndex,
                size: None._, 
                staticBytes: None._
            );

        /// <summary>
        /// Named tuple of <see cref="isStatic"/> and <see cref="index"/>.
        /// </summary>
        public readonly struct ArrayIndex : IEquatable<ArrayIndex>
        {
            /// <summary>Is this a reference to a static field?</summary>
            public readonly bool isStatic;

            /// <summary>
            /// An index into the <see cref="PackedMemorySnapshot.managedObjects"/> array or
            /// <see cref="PackedMemorySnapshot.managedStaticFields"/> array that stores this managed
            /// object.
            /// </summary>
            public readonly int index;

            public ArrayIndex(bool isStatic, int index) 
            {
                if (index < 0) throw new ArgumentOutOfRangeException(
                    nameof(index), index, "Index should always be positive, received " + index
                );
                this.isStatic = isStatic;
                this.index = index;
            }

            /// <summary>Indexes into <see cref="PackedMemorySnapshot.managedStaticFields"/>.</summary>
            public static ArrayIndex newStatic(int index) => new ArrayIndex(isStatic: true, index);

            /// <summary>Indexes into <see cref="PackedMemorySnapshot.managedObjects"/>.</summary>
            public static ArrayIndex newObject(int index) => new ArrayIndex(isStatic: false, index);

            public override string ToString() {
                var staticStr = isStatic ? ", for static field" : "";
                return $"ManagedObjectIndex({index}{staticStr})";
            }

            public PackedConnection.Pair asPair =>
                new PackedConnection.Pair(
                    isStatic ? PackedConnection.Kind.StaticField : PackedConnection.Kind.Managed,
                    index
                );

#region Equality
            public bool Equals(ArrayIndex other) => isStatic == other.isStatic && index == other.index;
            public override bool Equals(object obj) => obj is ArrayIndex other && Equals(other);
            public static bool operator ==(ArrayIndex left, ArrayIndex right) => left.Equals(right);
            public static bool operator !=(ArrayIndex left, ArrayIndex right) => !left.Equals(right);

            public override int GetHashCode() {
                unchecked {
                    return (isStatic.GetHashCode() * 397) ^ index;
                }
            }
#endregion
        }
    }
}
