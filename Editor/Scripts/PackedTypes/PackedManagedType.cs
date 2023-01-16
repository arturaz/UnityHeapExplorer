//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//
using UnityEngine;
using System;
using UnityEditor.Profiling.Memory.Experimental;
using static HeapExplorer.Option;

namespace HeapExplorer
{
    // Description of a managed type.
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct PackedManagedType : PackedMemorySnapshot.TypeForSubclassSearch
    {
        /// <summary>Is this type a value type? (if it's not a value type, it's a reference type)</summary>
        public bool isValueType;

        /// <summary>Is this type an array?</summary>
        public bool isArray;

        /// <summary>
        /// If this is an arrayType, this will return the rank of the array. (1 for a 1-dimensional array, 2 for a
        /// 2-dimensional array, etc)
        /// </summary>
        public int arrayRank;

        /// <summary>
        /// Name of this type.
        /// </summary>
        public string name;

        /// <summary>
        /// Name of the assembly this type was loaded from.
        /// </summary>
        public string assembly;

        /// <summary>
        /// An array containing descriptions of all fields of this type.
        /// </summary>
        public PackedManagedField[] fields;

        /// <summary>
        /// The actual contents of the bytes that store this types static fields, at the point of time when the
        /// snapshot was taken.
        /// </summary>
        public byte[] staticFieldBytes;

        /// <summary>
        /// The base type for this type, pointed to by an index into <see cref="PackedMemorySnapshot.managedTypes"/>.
        /// </summary>
        public Option<int> baseOrElementTypeIndex;

        /// <summary>
        /// Size in bytes of an instance of this type. If this type is an array type, this describes the amount of
        /// bytes a single element in the array will take up.
        /// </summary>
        public int size;

        /// <summary>
        /// The address in memory that contains the description of this type inside the virtual machine.
        /// <para/>
        /// This can be used to match managed objects in the heap to their corresponding TypeDescription, as the first
        /// pointer of a managed object points to its type description.
        /// </summary>
        public ulong typeInfoAddress;

        /// <summary>
        /// This index is an index into the <see cref="PackedMemorySnapshot.managedTypes"/> array.
        /// </summary>
        public int managedTypesArrayIndex;

        /// <summary>
        /// Index into <see cref="PackedMemorySnapshot.nativeTypes"/> if this managed type has a native counterpart or
        /// `None` otherwise.
        /// </summary>
        [NonSerialized]
        public Option<int> nativeTypeArrayIndex;

        /// <summary>
        /// Number of all objects of this type.
        /// </summary>
        [NonSerialized]
        public int totalObjectCount;

        /// <summary>
        /// The size of all objects of this type.
        /// </summary>
        [NonSerialized]
        public ulong totalObjectSize;

        /// <summary>
        /// Whether the type derived from <see cref="UnityEngine.Object"/>.
        /// </summary>
        [NonSerialized]
        public bool isUnityEngineObject;

        /// <summary>
        /// Whether the type contains any field of ReferenceType
        /// </summary>
        [NonSerialized]
        public bool containsFieldOfReferenceType;

        /// <summary>
        /// Whether this or a base class contains any field of a ReferenceType.
        /// </summary>
        [NonSerialized]
        public bool containsFieldOfReferenceTypeInInheritanceChain;

        /// <inheritdoc/>
        string PackedMemorySnapshot.TypeForSubclassSearch.name {
            get { return name; }
        }

        /// <inheritdoc/>
        int PackedMemorySnapshot.TypeForSubclassSearch.typeArrayIndex {
            get { return managedTypesArrayIndex; }
        }

        /// <inheritdoc/>
        Option<int> PackedMemorySnapshot.TypeForSubclassSearch.baseTypeArrayIndex {
            get { return baseOrElementTypeIndex; }
        }
        
        /// <summary>
        /// An array containing descriptions of all instance fields of this type.
        /// </summary>
        public PackedManagedField[] instanceFields
        {
            get
            {
                if (m_InstanceFields == null)
                {
                    // Find how many instance fields there are
                    var count = 0;
                    for (var n = 0; n < fields.Length; ++n)
                    {
                        if (!fields[n].isStatic)
                            count++;
                    }

                    // Allocate an array to hold just the instance fields
                    m_InstanceFields = new PackedManagedField[count];
                    count = 0;

                    // Copy instance field descriptions
                    for (var n = 0; n < fields.Length; ++n)
                    {
                        if (!fields[n].isStatic)
                        {
                            m_InstanceFields[count] = fields[n];
                            count++;
                        }
                    }
                }

                return m_InstanceFields;
            }
        }
        [NonSerialized] PackedManagedField[] m_InstanceFields;

        /// <summary>
        /// An array containing descriptions of all static fields of this type, NOT including static fields of base
        /// type.
        /// </summary>
        public PackedManagedField[] staticFields
        {
            get
            {
                if (m_StaticFields == null)
                {
                    if (staticFieldBytes == null || staticFieldBytes.Length == 0)
                    {
                        m_StaticFields = new PackedManagedField[0];
                    }
                    else
                    {
                        // Find how many static fields there are
                        var count = 0;
                        for (var n = 0; n < fields.Length; ++n)
                        {
                            if (fields[n].isStatic)
                                count++;
                        }

                        // Allocate an array to hold just the static fields
                        m_StaticFields = new PackedManagedField[count];
                        count = 0;

                        // Copy static field descriptions
                        for (var n = 0; n < fields.Length; ++n)
                        {
                            if (fields[n].isStatic)
                            {
                                m_StaticFields[count] = fields[n];
                                count++;
                            }
                        }
                    }
                }

                return m_StaticFields;
            }
        }
        [NonSerialized] PackedManagedField[] m_StaticFields;

        /// <summary>
        /// Gets whether this is a common language runtime primitive type.
        /// </summary>
        public bool isPrimitive
        {
            get
            {
                switch (name)
                {
                    case "System.Char":
                    case "System.Byte":
                    case "System.SByte":
                    case "System.Int16":
                    case "System.UInt16":
                    case "System.Int32":
                    case "System.UInt32":
                    case "System.Int64":
                    case "System.UInt64":
                    case "System.Single":
                    case "System.Double":
                    case "System.Decimal":
                    case "System.Boolean":
                    case "System.String":
                    case "System.Object":
                    case "System.IntPtr":
                    case "System.UIntPtr":
                    case "System.Enum":
                    case "System.ValueType":
                    case "System.ReferenceType":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Gets whether the type is a pointer. This includes ReferenceTypes, IntPtr and UIntPtr.
        /// </summary>
        public bool isPointer
        {
            get
            {
                if (!isValueType)
                    return true;

                switch (name)
                {
                    case "System.IntPtr":
                    case "System.UIntPtr":
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool isDerivedReferenceType
        {
            get
            {
                if (isValueType)
                    return false;

                if (!this.baseOrElementTypeIndex.valueOut(out var baseOrElementTypeIndex))
                    return false;

                if (baseOrElementTypeIndex == managedTypesArrayIndex)
                    return false;

                return true;
            }
        }

        // An enum derives from System.Enum, which derives from System.ValueType.
        public bool isDerivedValueType
        {
            get
            {
                if (!isValueType)
                    return false;

                if (!this.baseOrElementTypeIndex.valueOut(out var baseOrElementTypeIndex))
                    return false;

                if (baseOrElementTypeIndex == managedTypesArrayIndex)
                    return false;

                return true;
            }
        }

        public bool TryGetField(string name, out PackedManagedField field)
        {
            for (int n=0, nend = fields.Length; n < nend; ++n)
            {
                if (fields[n].name == name)
                {
                    field = fields[n];
                    return true;
                }
            }

            field = default;
            return false;
        }

        const int k_Version = 1;

        public static void Write(System.IO.BinaryWriter writer, PackedManagedType[] value)
        {
            writer.Write(k_Version);
            writer.Write(value.Length);

            for (int n = 0, nend = value.Length; n < nend; ++n)
            {
                writer.Write(value[n].isValueType);
                writer.Write(value[n].isArray);
                writer.Write(value[n].arrayRank);
                writer.Write(value[n].name);
                writer.Write(value[n].assembly);

                writer.Write(value[n].staticFieldBytes.Length);
                writer.Write(value[n].staticFieldBytes);
                writer.Write(value[n].baseOrElementTypeIndex.getOrElse(-1));
                writer.Write(value[n].size);
                writer.Write(value[n].typeInfoAddress);
                writer.Write(value[n].managedTypesArrayIndex);

                PackedManagedField.Write(writer, value[n].fields);
            }
        }

        public static void Read(System.IO.BinaryReader reader, out PackedManagedType[] value, out string stateString)
        {
            value = new PackedManagedType[0];
            stateString = "";

            var version = reader.ReadInt32();
            if (version >= 1)
            {
                var length = reader.ReadInt32();
                stateString = $"Loading {length} Managed Types";
                value = new PackedManagedType[length];

                for (int n = 0, nend = value.Length; n < nend; ++n)
                {
                    value[n].isValueType = reader.ReadBoolean();
                    value[n].isArray = reader.ReadBoolean();
                    value[n].arrayRank = reader.ReadInt32();
                    value[n].name = reader.ReadString();
                    value[n].assembly = reader.ReadString();

                    var count = reader.ReadInt32();
                    value[n].staticFieldBytes = reader.ReadBytes(count);
                    var baseOrElementTypeIndex = reader.ReadInt32();
                    value[n].baseOrElementTypeIndex = 
                        baseOrElementTypeIndex == -1 ? None._ : Some(baseOrElementTypeIndex);
                    value[n].size = reader.ReadInt32();
                    value[n].typeInfoAddress = reader.ReadUInt64();
                    value[n].managedTypesArrayIndex = reader.ReadInt32();

                    PackedManagedField.Read(reader, out value[n].fields);

                    // Types without namespace have a preceding period, which we remove here
                    // https://issuetracker.unity3d.com/issues/packedmemorysnapshot-leading-period-symbol-in-typename
                    if (value[n].name != null && value[n].name.Length > 0 && value[n].name[0] == '.')
                        value[n].name = value[n].name.Substring(1);
                }
            }
        }

        public static PackedManagedType[] FromMemoryProfiler(UnityEditor.Profiling.Memory.Experimental.PackedMemorySnapshot snapshot)
        {
            var source = snapshot.typeDescriptions;
            var value = new PackedManagedType[source.GetNumEntries()];

            var sourceAssembly = new string[source.assembly.GetNumEntries()];
            source.assembly.GetEntries(0, source.assembly.GetNumEntries(), ref sourceAssembly);

            var sourceFlags = new TypeFlags[source.flags.GetNumEntries()];
            source.flags.GetEntries(0, source.flags.GetNumEntries(), ref sourceFlags);

            var sourceName = new string[source.typeDescriptionName.GetNumEntries()];
            source.typeDescriptionName.GetEntries(0, source.typeDescriptionName.GetNumEntries(), ref sourceName);

            var sourceSize = new int[source.size.GetNumEntries()];
            source.size.GetEntries(0, source.size.GetNumEntries(), ref sourceSize);

            var sourceTypeInfoAddress = new ulong[source.typeInfoAddress.GetNumEntries()];
            source.typeInfoAddress.GetEntries(0, source.typeInfoAddress.GetNumEntries(), ref sourceTypeInfoAddress);

            var sourceTypeIndex = new int[source.typeIndex.GetNumEntries()];
            source.typeIndex.GetEntries(0, source.typeIndex.GetNumEntries(), ref sourceTypeIndex);

            var sourceBaseOrElementTypeIndex = new int[source.baseOrElementTypeIndex.GetNumEntries()];
            source.baseOrElementTypeIndex.GetEntries(0, source.baseOrElementTypeIndex.GetNumEntries(), ref sourceBaseOrElementTypeIndex);

            var sourceStaticFieldBytes = new byte[source.staticFieldBytes.GetNumEntries()][];
            source.staticFieldBytes.GetEntries(0, source.staticFieldBytes.GetNumEntries(), ref sourceStaticFieldBytes);

            var sourceFieldIndices = new int[source.fieldIndices.GetNumEntries()][];
            source.fieldIndices.GetEntries(0, source.fieldIndices.GetNumEntries(), ref sourceFieldIndices);

            // fields
            var desc = snapshot.fieldDescriptions;

            var fieldName = new string[desc.fieldDescriptionName.GetNumEntries()];
            desc.fieldDescriptionName.GetEntries(0, desc.fieldDescriptionName.GetNumEntries(), ref fieldName);

            var fieldStatic = new bool[desc.isStatic.GetNumEntries()];
            desc.isStatic.GetEntries(0, desc.isStatic.GetNumEntries(), ref fieldStatic);

            var fieldOffset = new int[desc.offset.GetNumEntries()];
            desc.offset.GetEntries(0, desc.offset.GetNumEntries(), ref fieldOffset);

            var fieldTypeIndex = new int[desc.typeIndex.GetNumEntries()];
            desc.typeIndex.GetEntries(0, desc.typeIndex.GetNumEntries(), ref fieldTypeIndex);

            var sourceFieldDescriptions = new PackedManagedField[desc.GetNumEntries()];
            for (int n=0, nend = sourceFieldDescriptions.Length; n < nend; ++n)
            {
                sourceFieldDescriptions[n].name = fieldName[n];
                sourceFieldDescriptions[n].isStatic = fieldStatic[n];
                sourceFieldDescriptions[n].offset = fieldOffset[n];
                sourceFieldDescriptions[n].managedTypesArrayIndex = fieldTypeIndex[n];
            }

            for (int n = 0, nend = value.Length; n < nend; ++n) {
                var baseOrElementTypeIndex = sourceBaseOrElementTypeIndex[n];
                value[n] = new PackedManagedType
                {
                    isValueType = (sourceFlags[n] & TypeFlags.kValueType) != 0,
                    isArray = (sourceFlags[n] & TypeFlags.kArray) != 0,
                    arrayRank = (int)(sourceFlags[n] & TypeFlags.kArrayRankMask)>>16,
                    name = sourceName[n],
                    assembly = sourceAssembly[n],
                    staticFieldBytes = sourceStaticFieldBytes[n],
                    baseOrElementTypeIndex = baseOrElementTypeIndex == -1 ? None._ : Some(baseOrElementTypeIndex),
                    size = sourceSize[n],
                    typeInfoAddress = sourceTypeInfoAddress[n],
                    managedTypesArrayIndex = sourceTypeIndex[n],
                    fields = new PackedManagedField[sourceFieldIndices[n].Length]
                };

                for (var j=0; j< sourceFieldIndices[n].Length; ++j)
                {
                    var i = sourceFieldIndices[n][j];
                    value[n].fields[j].name = sourceFieldDescriptions[i].name;
                    value[n].fields[j].offset = sourceFieldDescriptions[i].offset;
                    value[n].fields[j].isStatic = sourceFieldDescriptions[i].isStatic;
                    value[n].fields[j].managedTypesArrayIndex = sourceFieldDescriptions[i].managedTypesArrayIndex;
                }

                // namespace-less types have a preceding dot, which we remove here
                if (value[n].name != null && value[n].name.Length > 0 && value[n].name[0] == '.')
                    value[n].name = value[n].name.Substring(1);

            }
            return value;
        }

        public override string ToString()
        {
            var text = $"name: {name}, isValueType: {isValueType}, isArray: {isArray}, size: {size}";
            return text;
        }
    }

    public static class PackedManagedTypeUtility
    {
        public static string GetInheritanceAsString(PackedMemorySnapshot snapshot, int managedTypesArrayIndex)
        {
            var sb = new System.Text.StringBuilder(128);
            GetInheritanceAsString(snapshot, managedTypesArrayIndex, sb);
            return sb.ToString();
        }

        public static void GetInheritanceAsString(PackedMemorySnapshot snapshot, int managedTypesArrayIndex, System.Text.StringBuilder target)
        {
            var depth = 0;
            var loopGuard = 0;

            var maybeCurrentManagedTypesArrayIndex = Some(managedTypesArrayIndex);
            {while (maybeCurrentManagedTypesArrayIndex.valueOut(out var currentManagedTypesArrayIndex)) {
                for (var n = 0; n < depth; ++n)
                    target.Append("  ");

                target.AppendFormat("{0}\n", snapshot.managedTypes[currentManagedTypesArrayIndex].name);
                depth++;

                maybeCurrentManagedTypesArrayIndex = snapshot.managedTypes[currentManagedTypesArrayIndex].baseOrElementTypeIndex;
                if (++loopGuard > 64) {
                    Debug.LogError("Loop guard in `GetInheritanceAsString` kicked in.");
                    break;
                }
            }}
        }

        /// <summary>
        /// Gets whether any type in its inheritance chain has an instance field.
        /// </summary>
        public static bool HasTypeOrBaseAnyField(PackedMemorySnapshot snapshot, PackedManagedType type, bool checkInstance, bool checkStatic)
        {
            var loopguard = 0;
            do
            {
                if (++loopguard > 64)
                {
                    Debug.LogError("loopguard kicked in");
                    break;
                }

                if (checkInstance && type.instanceFields.Length > 0)
                    return true;

                if (checkStatic && type.staticFields.Length > 0)
                    return true;

                {if (type.baseOrElementTypeIndex.valueOut(out var baseOrElementTypeIndex))
                    type = snapshot.managedTypes[baseOrElementTypeIndex];}

            } while (type.baseOrElementTypeIndex.isSome && Some(type.managedTypesArrayIndex) != type.baseOrElementTypeIndex);

            return false;
        }

        /// <summary>
        /// Gets whether any type in its inheritance chain has an instance field.
        /// </summary>
        public static bool HasTypeOrBaseAnyInstanceField(PackedMemorySnapshot snapshot, PackedManagedType type)
        {
            var loopGuard = 0;
            do
            {
                if (++loopGuard > 64)
                {
                    Debug.LogError("loopguard kicked in");
                    break;
                }

                if (type.instanceFields.Length > 0)
                    return true;

                {if (type.baseOrElementTypeIndex.valueOut(out var baseOrElementTypeIndex))
                    type = snapshot.managedTypes[baseOrElementTypeIndex];}

            } while (type.baseOrElementTypeIndex.isSome && Some(type.managedTypesArrayIndex) != type.baseOrElementTypeIndex);

            return false;
        }

        public static bool HasTypeOrBaseAnyInstanceField(PackedMemorySnapshot snapshot, PackedManagedType type, out PackedManagedType fieldType)
        {
            fieldType = new PackedManagedType {
                managedTypesArrayIndex = -1
            };

            var loopGuard = 0;
            do
            {
                if (++loopGuard > 64)
                {
                    Debug.LogError("loopguard kicked in");
                    break;
                }

                if (type.instanceFields.Length > 0)
                {
                    fieldType = snapshot.managedTypes[type.instanceFields[0].managedTypesArrayIndex];
                    return true;
                }

                if (type.baseOrElementTypeIndex.valueOut(out var baseOrElementTypeIndex))
                    type = snapshot.managedTypes[baseOrElementTypeIndex];

            } while (type.baseOrElementTypeIndex.isSome && Some(type.managedTypesArrayIndex) != type.baseOrElementTypeIndex);

            return false;
        }

        public static bool HasTypeOrBaseAnyStaticField(PackedMemorySnapshot snapshot, PackedManagedType type, out PackedManagedType fieldType)
        {
            fieldType = new PackedManagedType {
                managedTypesArrayIndex = -1
            };

            var loopGuard = 0;
            do
            {
                if (++loopGuard > 64)
                {
                    Debug.LogError("loopguard kicked in");
                    break;
                }

                if (type.staticFields.Length > 0)
                {
                    fieldType = snapshot.managedTypes[type.staticFields[0].managedTypesArrayIndex];
                    return true;
                }

                if (type.baseOrElementTypeIndex.valueOut(out var baseOrElementTypeIndex))
                    type = snapshot.managedTypes[baseOrElementTypeIndex];

            } while (type.baseOrElementTypeIndex.isSome && Some(type.managedTypesArrayIndex) != type.baseOrElementTypeIndex);

            return false;
        }
    }
}
