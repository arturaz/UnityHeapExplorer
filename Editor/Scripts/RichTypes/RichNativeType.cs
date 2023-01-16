//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//

using System;
using static HeapExplorer.Option;

namespace HeapExplorer
{
    /// <summary>
    /// An <see cref="PackedNativeType"/> index validated against a <see cref="PackedMemorySnapshot"/>.
    /// </summary>
    public readonly struct RichNativeType
    {
        public RichNativeType(PackedMemorySnapshot snapshot, int nativeTypesArrayIndex)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (nativeTypesArrayIndex < 0 || nativeTypesArrayIndex >= snapshot.nativeTypes.Length)
                throw new ArgumentOutOfRangeException(
                    $"nativeTypesArrayIndex ({nativeTypesArrayIndex}) is out of bounds [0..{snapshot.nativeTypes.Length})"
                );
            
            this.snapshot = snapshot;
            this.nativeTypesArrayIndex = nativeTypesArrayIndex;
        }

        public PackedNativeType packed => snapshot.nativeTypes[nativeTypesArrayIndex];
        public string name => packed.name;

        public Option<RichNativeType> baseType =>
            packed.nativeBaseTypeArrayIndex.valueOut(out var index)
                ? Some(new RichNativeType(snapshot, index))
                : None._; 

        /// <summary>
        /// Gets whether this native type is a subclass of the specified baseType.
        /// </summary>
        public bool IsSubclassOf(int baseTypeIndex) => snapshot.IsSubclassOf(packed, baseTypeIndex);

        public readonly PackedMemorySnapshot snapshot;
        public readonly int nativeTypesArrayIndex;
    }
}
