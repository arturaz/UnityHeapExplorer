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
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct PackedManagedObject
    {
        /// <summary>
        /// The address of the managed object
        /// </summary>
        public System.UInt64 address;

        /// <summary>
        /// If this object is a static field
        /// </summary>
        public System.Byte[] staticBytes;

        /// <summary>
        /// An index into the <see cref="PackedMemorySnapshot.managedTypes"/> array that stores this managed type
        /// </summary>
        public System.Int32 managedTypesArrayIndex;

        /// <summary>
        /// An index into the <see cref="PackedMemorySnapshot.managedObjects"/> array that stores this managed object
        /// </summary>
        public System.Int32 managedObjectsArrayIndex;

        /// <summary>
        /// The index into the <see cref="PackedMemorySnapshot.gcHandles"/> array of the snapshot that is connected to
        /// this managed object, if any.
        /// </summary>
        public System.Int32 gcHandlesArrayIndex;

        /// <summary>
        /// The index into the <see cref="PackedMemorySnapshot.nativeObjects"/> array of the snapshot that is connected
        /// to this managed object, if any.
        /// </summary>
        public System.Int32 nativeObjectsArrayIndex;

        // Size in bytes of this object.
        // ValueType arrays = count * sizeof(element)
        // ReferenceType arrays = count * sizeof(pointer)
        // String = length * sizeof(wchar) + strlen("\0\0")
        public uint size;

        public static PackedManagedObject New()
        {
            return new PackedManagedObject()
            {
                managedTypesArrayIndex = -1,
                managedObjectsArrayIndex = -1,
                gcHandlesArrayIndex = -1,
                nativeObjectsArrayIndex = -1,
            };
        }
    }
}
