//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//

using System;

namespace HeapExplorer
{
    /// <summary>
    /// An <see cref="PackedGCHandle"/> index validated against a <see cref="PackedMemorySnapshot"/>.
    /// </summary>
    public readonly struct RichGCHandle
    {
        public readonly PackedMemorySnapshot snapshot;
        public readonly int gcHandleArrayIndex;

        public RichGCHandle(PackedMemorySnapshot snapshot, int gcHandlesArrayIndex) {
            if (gcHandlesArrayIndex >= snapshot.gcHandles.Length) {
                throw new ArgumentOutOfRangeException(
                    $"{gcHandlesArrayIndex} is out of bounds [0..{snapshot.gcHandles.Length})"
                );
            }
            
            this.snapshot = snapshot;
            gcHandleArrayIndex = gcHandlesArrayIndex;
        }

        public PackedGCHandle packed => snapshot.gcHandles[gcHandleArrayIndex];

        public RichManagedObject? managedObject =>
            packed.managedObjectsArrayIndex.valueOut(out var index)
                ? new RichManagedObject(snapshot, index)
                : (RichManagedObject?) null;

        public RichNativeObject? nativeObject => managedObject?.nativeObject;

        public ulong managedObjectAddress => packed.target;

        public int size => snapshot.virtualMachineInformation.pointerSize;
    }
}
