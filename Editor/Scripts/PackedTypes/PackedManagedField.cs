//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace HeapExplorer
{
    // Description of a field of a managed type.
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct PackedManagedField
    {
        /// <summary>
        /// Offset of this field.
        /// </summary>
        public System.Int32 offset;

        /// <summary>
        /// The type index into <see cref="PackedMemorySnapshot.managedTypes"/> of the type this field belongs to.
        /// </summary>
        public System.Int32 managedTypesArrayIndex;

        /// <summary>
        /// Name of this field.
        /// </summary>
        public System.String name;

        /// <summary>
        /// Is this field static?
        /// </summary>
        public System.Boolean isStatic;

        [NonSerialized] public bool isBackingField;

        const System.Int32 k_Version = 1;

        public static void Write(System.IO.BinaryWriter writer, PackedManagedField[] value)
        {
            writer.Write(k_Version);
            writer.Write(value.Length);

            for (int n = 0, nend = value.Length; n < nend; ++n)
            {
                writer.Write(value[n].name);
                writer.Write(value[n].offset);
                writer.Write(value[n].managedTypesArrayIndex);
                writer.Write(value[n].isStatic);
            }
        }

        public static void Read(System.IO.BinaryReader reader, out PackedManagedField[] value)
        {
            value = new PackedManagedField[0];

            var version = reader.ReadInt32();
            if (version >= 1)
            {
                var length = reader.ReadInt32();
                value = new PackedManagedField[length];

                for (int n = 0, nend = value.Length; n < nend; ++n)
                {
                    value[n].name = reader.ReadString();
                    value[n].offset = reader.ReadInt32();
                    value[n].managedTypesArrayIndex = reader.ReadInt32();
                    value[n].isStatic = reader.ReadBoolean();
                }
            }
        }

        public override string ToString()
        {
            var text = string.Format("name: {0}, offset: {1}, typeIndex: {2}, isStatic: {3}", name, offset, managedTypesArrayIndex, isStatic);
            return text;
        }
    }
}
