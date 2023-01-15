//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeapExplorer
{
    /// <summary>
    /// An <see cref="PackedNativeUnityEngineObject"/> index validated against a <see cref="PackedMemorySnapshot"/>.
    /// </summary>
    public readonly struct RichNativeObject
    {
        public RichNativeObject(PackedMemorySnapshot snapshot, int nativeObjectsArrayIndex)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (nativeObjectsArrayIndex < 0 || nativeObjectsArrayIndex >= snapshot.nativeObjects.Length)
                throw new ArgumentOutOfRangeException(
                    $"nativeObjectsArrayIndex ({nativeObjectsArrayIndex})is out of bounds [0..{snapshot.nativeObjects.Length})"
                );
            this.snapshot = snapshot;
            this.nativeObjectsArrayIndex = nativeObjectsArrayIndex;
        }

        public PackedNativeUnityEngineObject packed => snapshot.nativeObjects[nativeObjectsArrayIndex];

        public RichNativeType type => new RichNativeType(snapshot, packed.nativeTypesArrayIndex);

        public RichManagedObject? managedObject =>
            packed.managedObjectsArrayIndex.valueOut(out var index)
                    ? new RichManagedObject(snapshot, index)
                    : (RichManagedObject?) null;

        public RichGCHandle? gcHandle => managedObject?.gcHandle;

        public string name => packed.name;

        public ulong address => packed.nativeObjectAddress;

        public HideFlags hideFlags => packed.hideFlags;

        public int instanceId => packed.instanceId;

        public bool isDontDestroyOnLoad => packed.isDontDestroyOnLoad;

        public bool isManager => packed.isManager;

        public bool isPersistent => packed.isPersistent;

        public ulong size => packed.size;

        public void GetConnections(List<PackedConnection> references, List<PackedConnection> referencedBy)
        {
            snapshot.GetConnections(packed, references, referencedBy);
        }

        public void GetConnectionsCount(out int referencesCount, out int referencedByCount)
        {
            snapshot.GetConnectionsCount(PackedConnection.Kind.Native, nativeObjectsArrayIndex, out referencesCount, out referencedByCount);
        }

        public readonly PackedMemorySnapshot snapshot;
        public readonly int nativeObjectsArrayIndex;
    }
}
