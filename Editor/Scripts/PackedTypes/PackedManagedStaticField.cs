//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//
using System;

namespace HeapExplorer
{
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public readonly struct PackedManagedStaticField
    {
        /// <summary>
        /// The index into <see cref="PackedMemorySnapshot.managedTypes"/> of the type this field belongs to.
        /// </summary>
        public readonly int managedTypesArrayIndex;

        /// <summary>
        /// The index into the <see cref="PackedManagedType.fields"/> array
        /// </summary>
        public readonly int fieldIndex;

        /// <summary>
        /// The index into the <see cref="PackedMemorySnapshot.managedStaticFields"/> array
        /// </summary>
        public readonly int staticFieldsArrayIndex;

        public PackedManagedStaticField(int managedTypesArrayIndex, int fieldIndex, int staticFieldsArrayIndex) {
            this.managedTypesArrayIndex = managedTypesArrayIndex;
            this.fieldIndex = fieldIndex;
            this.staticFieldsArrayIndex = staticFieldsArrayIndex;
        }
    }
}
