//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//

//#define DEBUG_BREAK_ON_ADDRESS
//#define ENABLE_PROFILING
//#define ENABLE_PROFILER
using System.Collections.Generic;
using UnityEngine;
using System;
using static HeapExplorer.Option;

namespace HeapExplorer
{
    public class PackedManagedObjectCrawler
    {
        public static bool s_IgnoreNestedStructs = true;

        long m_TotalCrawled;
        readonly List<PackedManagedObject> m_Crawl = new List<PackedManagedObject>(1024 * 1024);
        readonly Dictionary<ulong, PackedManagedObject.ArrayIndex> m_Seen = 
            new Dictionary<ulong, PackedManagedObject.ArrayIndex>(1024 * 1024);
        readonly List<PackedManagedObject> m_ManagedObjects = new List<PackedManagedObject>(1024 * 1024);
        readonly List<PackedManagedStaticField> m_StaticFields = new List<PackedManagedStaticField>(10 * 1024);
        PackedMemorySnapshot m_Snapshot;
        PackedManagedField m_CachedPtr;
        MemoryReader m_MemoryReader;

#if DEBUG_BREAK_ON_ADDRESS
        ulong DebugBreakOnAddress = 0x2604C8AFEE0;
#endif

        [System.Diagnostics.Conditional("ENABLE_PROFILER")]
        static void BeginProfilerSample(string name)
        {
#if ENABLE_PROFILING
        UnityEngine.Profiling.Profiler.BeginSample(name);
#endif
        }

        [System.Diagnostics.Conditional("ENABLE_PROFILER")]
        static void EndProfilerSample()
        {
#if ENABLE_PROFILING
        UnityEngine.Profiling.Profiler.EndSample();
#endif
        }

        public void Crawl(PackedMemorySnapshot snapshot, List<ulong> substituteAddresses)
        {
            m_TotalCrawled = 0;
            m_Snapshot = snapshot;
            m_MemoryReader = new MemoryReader(m_Snapshot);
            InitializeCachedPtr();

            BeginProfilerSample("CrawlGCHandles");
            CrawlGCHandles();
            EndProfilerSample();

            BeginProfilerSample("substituteAddresses");
            for (var n = 0; n < substituteAddresses.Count; ++n)
            {
                var addr = substituteAddresses[n];
                if (m_Seen.ContainsKey(addr))
                    continue;
                TryAddManagedObject(addr);
            }
            EndProfilerSample();

            BeginProfilerSample("CrawlStatic");
            CrawlStatic();
            m_Snapshot.managedStaticFields = m_StaticFields.ToArray();
            EndProfilerSample();

            BeginProfilerSample("CrawlManagedObjects");
            CrawlManagedObjects();

            m_Snapshot.managedObjects = m_ManagedObjects.ToArray();
            UpdateProgress();
            EndProfilerSample();
        }

        void InitializeCachedPtr()
        {
            var unityEngineObject = m_Snapshot.managedTypes[m_Snapshot.coreTypes.unityEngineObject];

            // UnityEngine.Object types on the managed side have a m_CachedPtr field that
            // holds the native memory address of the corresponding native object of this managed object.
            const string FIELD_NAME = "m_CachedPtr";

            for (int n = 0, nend = unityEngineObject.fields.Length; n < nend; ++n)
            {
                var field = unityEngineObject.fields[n];
                if (field.name != FIELD_NAME)
                    continue;

                m_CachedPtr = field;
                return;
            }
            
            throw new Exception(
                $"Could not find '{FIELD_NAME}' field for Unity object type '{unityEngineObject.name}', this probably means the internal structure "
                + "of Unity has changed and the tool needs updating.");
        }

        bool ContainsReferenceType(int typeIndex)
        {
            var baseType = m_Snapshot.managedTypes[typeIndex];
            if (!baseType.isValueType)
                return true;

            var managedTypesLength = m_Snapshot.managedTypes.Length;
            var instanceFields = baseType.instanceFields;

            for (int n=0, nend = instanceFields.Length; n < nend; ++n)
            {
                var fieldTypeIndex = instanceFields[n].managedTypesArrayIndex;
                if (fieldTypeIndex < 0 || fieldTypeIndex >= managedTypesLength)
                {
                    m_Snapshot.Error("'{0}' field '{1}' is out of bounds '{2}', ignoring.", baseType.name, n, fieldTypeIndex);
                    continue;
                }

                var fieldType = m_Snapshot.managedTypes[instanceFields[n].managedTypesArrayIndex];
                if (!fieldType.isValueType)
                    return true;
            }

            return false;
        }

        void CrawlManagedObjects()
        {
            var virtualMachineInformation = m_Snapshot.virtualMachineInformation;
            var nestedStructsIgnored = 0;

            //var guard = 0;
            while (m_Crawl.Count > 0)
            {
                //if (++guard > 10000000)
                //{
                //    Debug.LogWarning("Loop guard kicked in");
                //    break;
                //}
                if ((m_TotalCrawled % 1000) == 0)
                {
                    UpdateProgress();
                    if (m_Snapshot.abortActiveStepRequested)
                        break;
                }

                var mo = m_Crawl[m_Crawl.Count - 1];
                m_Crawl.RemoveAt(m_Crawl.Count - 1);

#if DEBUG_BREAK_ON_ADDRESS
                if (mo.address == DebugBreakOnAddress)
                {
                    int a = 0;
                }
#endif

                var loopGuard = 0;
                var typeIndex = mo.managedTypesArrayIndex;

                while (typeIndex != -1)
                {
                    var baseType = m_Snapshot.managedTypes[typeIndex];
                    
                    if (++loopGuard > 264) 
                    {
                        var typeName = baseType.name;
                        Debug.LogWarningFormat(
                            "HeapExplorer: hit loop guard in CrawlManagedObjects(). Type = '{0}'", typeName
                        );
                        break;
                    }

                    AbstractMemoryReader memoryReader = m_MemoryReader;
                    {if (mo.staticBytes.valueOut(out var staticBytes))
                        memoryReader = new StaticMemoryReader(m_Snapshot, staticBytes);}

                    if (baseType.isArray)
                    {
                        if (
                            !baseType.baseOrElementTypeIndex.valueOut(out var baseOrElementTypeIndex) 
                            || baseOrElementTypeIndex >= m_Snapshot.managedTypes.Length
                        ) {
                            m_Snapshot.Error("'{0}.baseOrElementTypeIndex' = {1} at address '{2:X}', ignoring managed object.", baseType.name, baseOrElementTypeIndex, mo.address);
                            break;
                        }

                        var elementType = m_Snapshot.managedTypes[baseOrElementTypeIndex];
                        if (elementType.isValueType && elementType.isPrimitive)
                            break; // don't crawl int[], byte[], etc

                        if (elementType.isValueType && !ContainsReferenceType(elementType.managedTypesArrayIndex))
                            break;

                        int dim0Length;
                        if (mo.address > 0) {
                            if (!memoryReader.ReadArrayLength(mo.address, baseType).valueOut(out dim0Length)) {
                                m_Snapshot.Error($"Can't determine array length for array at {mo.address:X}");
                                break;
                            }
                        }
                        else
                            dim0Length = 0;
                        
                        //if (dim0Length > 1024 * 1024)
                        if (dim0Length > (32*1024) * (32*1024))
                        {
                            m_Snapshot.Error("Array (rank={2}) found at address '{0:X} with '{1}' elements, that doesn't seem right.", mo.address, dim0Length, baseType.arrayRank);
                            break;
                        }

                        for (var k = 0; k < dim0Length; ++k)
                        {
                            if ((m_TotalCrawled % 1000) == 0)
                                UpdateProgress();

                            ulong elementAddr;

                            if (elementType.isArray) {
                                var ptr = mo.address
                                          + (ulong) (k * virtualMachineInformation.pointerSize.sizeInBytes())
                                          + virtualMachineInformation.arrayHeaderSize;
                                if (!memoryReader.ReadPointer(ptr).valueOut(out elementAddr)) {
                                    m_Snapshot.Error($"Can't read ptr={ptr:X} for k={k}, type='{elementType.name}'");
                                    break;
                                }
                            }
                            else if (elementType.isValueType)
                            {
                                elementAddr = 
                                    mo.address 
                                    + (ulong)(k * elementType.size) 
                                    + virtualMachineInformation.arrayHeaderSize 
                                    - virtualMachineInformation.objectHeaderSize;
                            }
                            else {
                                var ptr = mo.address
                                          + (ulong) (k * virtualMachineInformation.pointerSize.sizeInBytes())
                                          + virtualMachineInformation.arrayHeaderSize;
                                if (!memoryReader.ReadPointer(ptr).valueOut(out elementAddr)) {
                                    m_Snapshot.Error($"Can't read ptr={ptr:X} for k={k}, type='{elementType.name}'");
                                    break;
                                }
                            }

#if DEBUG_BREAK_ON_ADDRESS
                            if (elementAddr == DebugBreakOnAddress)
                            {
                                int a = 0;
                            }
#endif

                            if (elementAddr != 0)
                            {
                                if (!m_Seen.TryGetValue(elementAddr, out var newObjectIndex)) {
                                    var managedTypesArrayIndex = elementType.managedTypesArrayIndex;
                                    var managedObjectsArrayIndex = 
                                        PackedManagedObject.ArrayIndex.newObject(m_ManagedObjects.Count);
                                    var newObj = PackedManagedObject.New(
                                        address: elementAddr,
                                        managedTypesArrayIndex: managedTypesArrayIndex,
                                        managedObjectsArrayIndex: managedObjectsArrayIndex
                                    );

                                    if (elementType.isValueType)
                                    {
                                        newObj.managedObjectsArrayIndex = mo.managedObjectsArrayIndex;
                                        newObj.staticBytes = mo.staticBytes;
                                    }
                                    else
                                    {
                                        newObj.managedTypesArrayIndex = 
                                            m_Snapshot.FindManagedObjectTypeOfAddress(elementAddr)
                                                .getOrElse(elementType.managedTypesArrayIndex);

                                        TryConnectNativeObject(ref newObj);
                                    }
                                    SetObjectSize(ref newObj, m_Snapshot.managedTypes[newObj.managedTypesArrayIndex]);

                                    if (!elementType.isValueType)
                                        m_ManagedObjects.Add(newObj);

                                    m_Seen[newObj.address] = newObj.managedObjectsArrayIndex;
                                    m_Crawl.Add(newObj);
                                    m_TotalCrawled++;
                                    newObjectIndex = newObj.managedObjectsArrayIndex;
                                }

                                // If we do not connect the Slot elements at Slot[] 0x1DB2A512EE0
                                if (!elementType.isValueType)
                                {
                                    m_Snapshot.AddConnection(mo.managedObjectsArrayIndex.asPair, newObjectIndex.asPair);
                                }
                            }
                        }

                        break;
                    }

                    for (var n = 0; n < baseType.fields.Length; ++n)
                    {
                        var field = baseType.fields[n];
                        
                        // Skip static fields as they are not a part of the object.
                        if (field.isStatic)
                            continue;

                        var fieldType = m_Snapshot.managedTypes[field.managedTypesArrayIndex];

                        if (fieldType.isValueType)
                        {
                            // Primitive values types do not contain any references that we would care about. 
                            if (fieldType.isPrimitive)
                                continue;

                            if (s_IgnoreNestedStructs && mo.managedTypesArrayIndex == fieldType.managedTypesArrayIndex)
                            {
                                nestedStructsIgnored++;
                                continue;
                            }

                            var newObj = PackedManagedObject.New(
                                address: mo.address + (uint)field.offset - (uint)virtualMachineInformation.objectHeaderSize,
                                managedTypesArrayIndex: fieldType.managedTypesArrayIndex,
                                managedObjectsArrayIndex: mo.managedObjectsArrayIndex
                            );
                            newObj.staticBytes = mo.staticBytes;
                            SetObjectSize(ref newObj, fieldType);

                            m_Crawl.Add(newObj); // Crawl, but do not add value types to the managedlist
                            m_TotalCrawled++;
                            continue;
                        }

                        if (!fieldType.isValueType)
                        {
                            var ptr =
                                mo.staticBytes.isNone
                                ? mo.address + (uint)field.offset
                                : mo.address + (uint)field.offset - (uint)virtualMachineInformation.objectHeaderSize;

                            if (!memoryReader.ReadPointer(ptr).valueOut(out var addr)) {
                                Debug.LogError($"Can't read ptr={ptr:X} for fieldType='{fieldType.name}'");
                                break;
                            }

                            if (addr == 0)
                                continue;

#if DEBUG_BREAK_ON_ADDRESS
                            if (addr == DebugBreakOnAddress)
                            {
                                int a = 0;
                            }
#endif

                            if (!m_Seen.TryGetValue(addr, out var newObjIndex))
                            {
                                var maybeManagedTypesArrayIndex = m_Snapshot.FindManagedObjectTypeOfAddress(addr);
                                var managedTypesArrayIndex =
                                    maybeManagedTypesArrayIndex.getOrElse(fieldType.managedTypesArrayIndex);
                                
                                var newObj = PackedManagedObject.New(
                                    address: addr,
                                    managedObjectsArrayIndex: PackedManagedObject.ArrayIndex.newObject(m_ManagedObjects.Count),
                                    managedTypesArrayIndex: managedTypesArrayIndex
                                );

                                SetObjectSize(ref newObj, m_Snapshot.managedTypes[newObj.managedTypesArrayIndex]);
                                TryConnectNativeObject(ref newObj);

                                m_Seen[newObj.address] = newObj.managedObjectsArrayIndex;
                                m_ManagedObjects.Add(newObj);
                                m_Crawl.Add(newObj);
                                m_TotalCrawled++;
                                newObjIndex = newObj.managedObjectsArrayIndex;
                            }

                            m_Snapshot.AddConnection(mo.managedObjectsArrayIndex.asPair, newObjIndex.asPair);
                        }
                    }

                    if (Some(typeIndex) == baseType.baseOrElementTypeIndex || baseType.isArray)
                        break;

                    typeIndex = baseType.baseOrElementTypeIndex.getOrThrow();
                }
            }

            if (nestedStructsIgnored > 0)
                m_Snapshot.Warning("HeapExplorer: {0} nested structs ignored (Workaround for Unity bug Case 1104590).", nestedStructsIgnored);
        }

        void CrawlStatic()
        {
            var crawlStatic = new List<int>();
            var managedTypes = m_Snapshot.managedTypes;
            var staticManagedTypes = new List<int>(1024);

            // Unity BUG: (Case 984330) PackedMemorySnapshot: Type contains staticFieldBytes, but has no static fields
            for (int n = 0, nend = managedTypes.Length; n < nend; ++n)
            {
                // Some static classes have no staticFieldBytes. As I understand this, the staticFieldBytes
                // are only filled if that static class has been initialized (its cctor called), otherwise it's zero.
                //
                // This is normal behaviour.
                if (managedTypes[n].staticFieldBytes == null || managedTypes[n].staticFieldBytes.Length == 0) 
                {
                    // Debug.LogFormat(
                    //     "HeapExplorer: managed type '{0}' does not have static fields.", managedTypes[n].name
                    // );
                    continue;
                }

                //var hasStaticField = false;
                for (
                    int fieldIndex = 0, fieldIndexEnd = managedTypes[n].fields.Length; 
                    fieldIndex < fieldIndexEnd;
                    ++fieldIndex
                )
                {
                    if (!managedTypes[n].fields[fieldIndex].isStatic)
                        continue;

                    //var field = managedTypes[n].fields[j];
                    //var fieldType = managedTypes[field.managedTypesArrayIndex];
                    //hasStaticField = true;

                    var item = new PackedManagedStaticField(
                        managedTypesArrayIndex: managedTypes[n].managedTypesArrayIndex,
                        fieldIndex: fieldIndex,
                        staticFieldsArrayIndex: m_StaticFields.Count
                    );
                    m_StaticFields.Add(item);

                    crawlStatic.Add(item.staticFieldsArrayIndex);
                }

                //if (hasStaticField)
                    staticManagedTypes.Add(managedTypes[n].managedTypesArrayIndex);
            }

            m_Snapshot.managedStaticTypes = staticManagedTypes.ToArray();

            //var loopGuard = 0;
            while (crawlStatic.Count > 0)
            {
                //if (++loopGuard > 100000)
                //{
                //    m_snapshot.Error("Loop-guard kicked in while analyzing static fields.");
                //    break;
                //}

                m_TotalCrawled++;
                if ((m_TotalCrawled % 1000) == 0)
                {
                    UpdateProgress();
                    if (m_Snapshot.abortActiveStepRequested)
                        break;
                }

                var staticField = m_StaticFields[crawlStatic[crawlStatic.Count - 1]];
                crawlStatic.RemoveAt(crawlStatic.Count - 1);

                var staticClass = m_Snapshot.managedTypes[staticField.managedTypesArrayIndex];
                var field = staticClass.fields[staticField.fieldIndex];
                var fieldType = m_Snapshot.managedTypes[field.managedTypesArrayIndex];
                var staticReader = new StaticMemoryReader(m_Snapshot, staticClass.staticFieldBytes);

                if (fieldType.isValueType)
                {
                    if (staticClass.staticFieldBytes == null || staticClass.staticFieldBytes.Length == 0)
                        continue;

                    var newObj = PackedManagedObject.New(
                        address: (ulong)field.offset,
                        PackedManagedObject.ArrayIndex.newStatic(staticField.staticFieldsArrayIndex),
                        managedTypesArrayIndex: fieldType.managedTypesArrayIndex 
                    );
                    newObj.staticBytes = Some(staticClass.staticFieldBytes);
                    SetObjectSize(ref newObj, fieldType);

                    m_Crawl.Add(newObj);
                    m_TotalCrawled++;
                }
                // If it's a reference type, it simply points to a ManagedObject on the heap and all
                // we need to do it to create a new ManagedObject and add it to the list to crawl.
                else
                {
                    if (!staticReader.ReadPointer((uint) field.offset).valueOut(out var addr)) {
                        m_Snapshot.Error($"Can't do `staticReader.ReadPointer(field.offset={field.offset})`");
                        continue;
                    }
                    if (addr == 0)
                        continue;

#if DEBUG_BREAK_ON_ADDRESS
                    if (addr == DebugBreakOnAddress)
                    {
                        int a = 0;
                    }
#endif

                    if (!m_Seen.TryGetValue(addr, out var newObjIndex))
                    {
                        // The static field could be a basetype, such as `UnityEngine.Object`, but actually point to a `Texture2D`.
                        // Therefore it's important to find the type of the specified address, rather than using the field type.
                        var maybeManagedTypesArrayIndex = m_Snapshot.FindManagedObjectTypeOfAddress(addr);
                        var managedTypesArrayIndex = 
                            maybeManagedTypesArrayIndex.getOrElse(fieldType.managedTypesArrayIndex);

                        var newObj = PackedManagedObject.New(
                            address: addr,
                            managedObjectsArrayIndex: PackedManagedObject.ArrayIndex.newObject(
                                m_ManagedObjects.Count
                            ),
                            managedTypesArrayIndex: managedTypesArrayIndex
                        );

                        // Check if the object has a GCHandle
                        var maybeGcHandleIndex = m_Snapshot.FindGCHandleOfTargetAddress(addr);
                        {if (maybeGcHandleIndex.valueOut(out var gcHandleIndex)) {
                            newObj.gcHandlesArrayIndex = Some(gcHandleIndex);
                            m_Snapshot.gcHandles[gcHandleIndex] =
                                m_Snapshot.gcHandles[gcHandleIndex]
                                .withManagedObjectsArrayIndex(newObj.managedObjectsArrayIndex);

                            m_Snapshot.AddConnection(
                                new PackedConnection.Pair(PackedConnection.Kind.GCHandle, gcHandleIndex),
                                newObj.managedObjectsArrayIndex.asPair
                            );
                        }}
                        SetObjectSize(ref newObj, managedTypes[newObj.managedTypesArrayIndex]);
                        TryConnectNativeObject(ref newObj);

                        m_ManagedObjects.Add(newObj);
                        m_Seen[newObj.address] = newObj.managedObjectsArrayIndex;
                        m_Crawl.Add(newObj);
                        m_TotalCrawled++;
                        newObjIndex = newObj.managedObjectsArrayIndex;
                    }

                    m_Snapshot.AddConnection(
                        new PackedConnection.Pair(PackedConnection.Kind.StaticField, staticField.staticFieldsArrayIndex),
                        newObjIndex.asPair
                    );
                }
            }
        }

        /// <summary>
        /// Creates and stores a <see cref="PackedManagedObject"/> in <see cref="m_ManagedObjects"/>.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>The index into <see cref="m_ManagedObjects"/> array.</returns>
        Option<int> TryAddManagedObject(ulong address)
        {
            // Try to find the ManagedObject of the current GCHandle
            var maybeTypeIndex = m_Snapshot.FindManagedObjectTypeOfAddress(address);
            if (!maybeTypeIndex.valueOut(out var typeIndex))
            {
                #region Unity Bug
                // Unity BUG: (Case 977003) PackedMemorySnapshot: Unable to resolve typeDescription of GCHandle.target
                // https://issuetracker.unity3d.com/issues/packedmemorysnapshot-unable-to-resolve-typedescription-of-gchandle-dot-target
                // [quote=Unity]
                // This is a bug in Mono where it has a few GC handles that point to invalid objects and they should
                // removed from the list of GC handles. The the invalid GC handles can be ignored for now,
                // as they have no affect on the captured snapshot.
                // [/quote]
                #endregion
                m_Snapshot.Warning("HeapExplorer: Cannot find GCHandle target '{0:X}' (Unity bug Case 977003).", address);
                return None._;
            }

            var index = PackedManagedObject.ArrayIndex.newObject(m_ManagedObjects.Count);
            var managedObj = PackedManagedObject.New(
                address: address,
                managedTypesArrayIndex: typeIndex,
                managedObjectsArrayIndex: index
            );

            // If the ManagedObject is the representation of a NativeObject, connect the two
            TryConnectNativeObject(ref managedObj);
            SetObjectSize(ref managedObj, m_Snapshot.managedTypes[managedObj.managedTypesArrayIndex]);

            m_Seen[managedObj.address] = managedObj.managedObjectsArrayIndex;
            m_ManagedObjects.Add(managedObj);
            m_Crawl.Add(managedObj);

            return Some(managedObj.managedObjectsArrayIndex.index);
        }

        void CrawlGCHandles()
        {
            var gcHandles = m_Snapshot.gcHandles;

            for (int n=0, nend = gcHandles.Length; n < nend; ++n) 
            {
                var gcHandle = gcHandles[n];
                if (gcHandle.target == 0)
                {
                    m_Snapshot.Warning("HeapExplorer: Cannot find GCHandle target '{0:X}' (Unity bug Case 977003).", gcHandle.target);
                    continue;
                }

#if DEBUG_BREAK_ON_ADDRESS
                if (gcHandle.target == DebugBreakOnAddress)
                {
                    int a = 0;
                }
#endif

                var maybeManagedObjectIndex = TryAddManagedObject(gcHandle.target);
                if (!maybeManagedObjectIndex.valueOut(out var managedObjectIndex)) {
                    Debug.LogWarning($"Can't find managed object for GC handle {gcHandle.target:X}, skipping!");
                    continue;
                }

                var managedObj = m_ManagedObjects[managedObjectIndex];
                managedObj.gcHandlesArrayIndex = Some(gcHandle.gcHandlesArrayIndex);
                m_ManagedObjects[managedObjectIndex] = managedObj;

                // Connect GCHandle to ManagedObject
                m_Snapshot.AddConnection(
                    new PackedConnection.Pair(PackedConnection.Kind.GCHandle, gcHandle.gcHandlesArrayIndex), 
                    new PackedConnection.Pair(PackedConnection.Kind.Managed, managedObj.managedObjectsArrayIndex.index)
                );

                // Update the GCHandle with the index to its managed object
                gcHandle = gcHandle.withManagedObjectsArrayIndex(managedObj.managedObjectsArrayIndex);
                gcHandles[n] = gcHandle;

                m_TotalCrawled++;

                if ((m_TotalCrawled % 1000) == 0)
                    UpdateProgress();
            }
        }

        void UpdateProgress()
        {
            m_Snapshot.busyString =
                $"Analyzing Managed Objects\n{m_TotalCrawled} crawled, {m_ManagedObjects.Count} extracted";
        }

        void SetObjectSize(ref PackedManagedObject managedObj, PackedManagedType type)
        {
            if (managedObj.size.isSome)
                return; // size is set already

            if (!m_MemoryReader.ReadObjectSize(managedObj.address, type).valueOut(out var objectSize)) {
                Debug.LogError($"Can't read object size for managed object of type '{type.name}' at {managedObj.address:X}");
                return;
            }
            managedObj.size = Some(objectSize.ToUIntClamped());
        }

        void TryConnectNativeObject(ref PackedManagedObject managedObj)
        {
            if (managedObj.nativeObjectsArrayIndex.isSome)
                return; // connected already

            // If it's not derived from UnityEngine.Object, it does not have the m_CachedPtr field and we can skip it
            var type = m_Snapshot.managedTypes[managedObj.managedTypesArrayIndex];
            if (type.isValueType || type.isArray)
                return;

            // Only types derived from UnityEngine.Object have the m_cachePtr field
            if (!type.isUnityEngineObject)
                return;

            BeginProfilerSample("ReadPointer");
            // Read the m_cachePtr value
            var nativeObjectAddressPtr = managedObj.address + (uint) m_CachedPtr.offset;
            if (
                !m_MemoryReader.ReadPointer(nativeObjectAddressPtr).valueOut(out var nativeObjectAddress)
            ) {
                Debug.LogError(
                    $"Can't read {nameof(m_CachedPtr)} from a managed object at ptr={nativeObjectAddressPtr:X}"
                );
                return;
            }
            EndProfilerSample();
            // If the native object address is 0 then we have a managed object without the native side, which happens
            // when you have a leaked managed object.
            if (nativeObjectAddress == 0)
                return;

            // Try to find the corresponding native object
            BeginProfilerSample("FindNativeObjectOfAddress");
            var maybeNativeObjectArrayIndex = m_Snapshot.FindNativeObjectOfAddress(nativeObjectAddress);
            EndProfilerSample();
            {if (maybeNativeObjectArrayIndex.valueOut(out var nativeObjectArrayIndex)) {
                // Connect ManagedObject <> NativeObject
                managedObj.nativeObjectsArrayIndex = Some(nativeObjectArrayIndex);
                m_Snapshot.nativeObjects[nativeObjectArrayIndex].managedObjectsArrayIndex = Some(managedObj.managedObjectsArrayIndex);

                // Connect the ManagedType <> NativeType
                var nativeTypesArrayIndex = m_Snapshot.nativeObjects[nativeObjectArrayIndex].nativeTypesArrayIndex;
                m_Snapshot.managedTypes[managedObj.managedTypesArrayIndex].nativeTypeArrayIndex = 
                    Some(nativeTypesArrayIndex);
                m_Snapshot.nativeTypes[nativeTypesArrayIndex].managedTypeArrayIndex = 
                    Some(m_Snapshot.managedTypes[managedObj.managedTypesArrayIndex].managedTypesArrayIndex);

                BeginProfilerSample("AddConnection");
                // Add a Connection from ManagedObject to NativeObject (m_CachePtr)
                m_Snapshot.AddConnection(
                    managedObj.managedObjectsArrayIndex.asPair, 
                    new PackedConnection.Pair(PackedConnection.Kind.Native, nativeObjectArrayIndex)
                );
                EndProfilerSample();
            }}
        }
    }
}
