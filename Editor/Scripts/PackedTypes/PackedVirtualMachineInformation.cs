//
// Heap Explorer for Unity. Copyright (c) 2019-2020 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityHeapExplorer/
//
using System;
using static HeapExplorer.Option;

namespace HeapExplorer
{
    /// <summary>Pointer size in bytes. We only support 32 and 64 bit architectures.</summary>
    public enum PointerSize : byte { _32Bit = 4, _64Bit = 8 }

    public static class PointerSize_ {
        /// <summary>Converts the <see cref="PointerSize"/> from the byte count returning `None` on unsupported values.</summary>
        public static Option<PointerSize> fromByteCount(int value) =>
            value == 4 ? Some(PointerSize._32Bit)
            : value == 8 ? Some(PointerSize._64Bit)
            : None._;
    }
    
    public static class PointerSizeExts {
        /// <summary>Converts the <see cref="PointerSize"/> to byte count.</summary>
        public static byte sizeInBytes(this PointerSize s) => (byte) s;
        
        /// <summary>
        /// Reads a pointer value at <see cref="offset"/> from the <see cref="array"/> based on the pointer size.
        /// </summary>
        public static ulong readPointer(this PointerSize s, byte[] array, int offset) =>
            s == PointerSize._64Bit
                ? BitConverter.ToUInt64(array, offset)
                : BitConverter.ToUInt32(array, offset);
    }
    
    // Information about a virtual machine that provided a memory snapshot.
    [Serializable]
    public struct PackedVirtualMachineInformation
    {
        // Size in bytes of a pointer.
        public PointerSize pointerSize;

        // Size in bytes of the header of each managed object.
        public PInt objectHeaderSize;

        // Size in bytes of the header of an array object.
        public PInt arrayHeaderSize;

        // Offset in bytes inside the object header of an array object where the bounds of the array is stored.
        public PInt arrayBoundsOffsetInHeader;

        // Offset in bytes inside the object header of an array object where the size of the array is stored.
        public PInt arraySizeOffsetInHeader;

        // Allocation granularity in bytes used by the virtual machine allocator.
        public PInt allocationGranularity;

        // A version number that will change when the object layout inside the managed heap will change.
        public System.Int32 heapFormatVersion;

        const System.Int32 k_Version = 1;

        public static void Write(System.IO.BinaryWriter writer, PackedVirtualMachineInformation value)
        {
            writer.Write(k_Version);
            writer.Write(value.pointerSize.sizeInBytes());
            writer.Write(value.objectHeaderSize);
            writer.Write(value.arrayHeaderSize);
            writer.Write(value.arrayBoundsOffsetInHeader);
            writer.Write(value.arraySizeOffsetInHeader);
            writer.Write(value.allocationGranularity);
            writer.Write(value.heapFormatVersion);
        }

        public static void Read(System.IO.BinaryReader reader, out PackedVirtualMachineInformation value, out string stateString)
        {
            value = new PackedVirtualMachineInformation();
            stateString = "Loading VM Information";

            var version = reader.ReadInt32();
            if (version >= 1)
            {
                value.pointerSize = PointerSize_.fromByteCount(reader.ReadInt32()).getOrThrow("unsupported pointer size");
                value.objectHeaderSize = PInt.createOrThrow(reader.ReadInt32());
                value.arrayHeaderSize = PInt.createOrThrow(reader.ReadInt32());
                value.arrayBoundsOffsetInHeader = PInt.createOrThrow(reader.ReadInt32());
                value.arraySizeOffsetInHeader = PInt.createOrThrow(reader.ReadInt32());
                value.allocationGranularity = PInt.createOrThrow(reader.ReadInt32());
                value.heapFormatVersion = reader.ReadInt32();
            }
        }

        public static PackedVirtualMachineInformation FromMemoryProfiler(UnityEditor.Profiling.Memory.Experimental.PackedMemorySnapshot snapshot)
        {
            var source = snapshot.virtualMachineInformation;

            var value = new PackedVirtualMachineInformation
            {
                pointerSize = PointerSize_.fromByteCount(source.pointerSize).getOrThrow("unsupported pointer size"),
                objectHeaderSize = PInt.createOrThrow(source.objectHeaderSize),
                arrayHeaderSize = PInt.createOrThrow(source.arrayHeaderSize),
                arrayBoundsOffsetInHeader = PInt.createOrThrow(source.arrayBoundsOffsetInHeader),
                arraySizeOffsetInHeader = PInt.createOrThrow(source.arraySizeOffsetInHeader),
                allocationGranularity = PInt.createOrThrow(source.allocationGranularity),
                heapFormatVersion = 2019,
            };
            return value;
        }
    }
}
