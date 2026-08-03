using System;
using System.Collections.Generic;
using GameVM.Compiler.Core.IR.Buffers;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.SlabProcessing;
using GameVM.Compiler.Core.IR.Interfaces;
using static GameVM.Compiler.Core.IR.Slab.InstructionMetadataFlags;

namespace GameVM.Compiler.Backend.Atari2600
{
    public class MidToLowLevelTransformer : IIRSlabTransformer
    {
        private readonly Dictionary<string, string> _addressMap = new(StringComparer.OrdinalIgnoreCase);
        private int _nextAvailableAddress = 0x80;
        private readonly ArenaAllocator _arena;

        public MidToLowLevelTransformer(ArenaAllocator arena)
        {
            _arena = arena ?? throw new ArgumentNullException(nameof(arena));
        }

        public MidToLowLevelTransformer()
        {
            _arena = new ArenaAllocator();
        }

        private void InitializeAddressMap()
        {
            _addressMap.Clear();
            _nextAvailableAddress = 0x80;
            _addressMap["COLUBK"] = "$09";
            _addressMap["COLUPF"] = "$08";
            _addressMap["COLUP0"] = "$06";
            _addressMap["COLUP1"] = "$07";
        }

        public uint[] TransformSlab(uint[] inputSlab, StringPool stringPool)
        {
            if (inputSlab == null || inputSlab.Length < SlabHeader.HeaderIndex.Length)
            {
                throw new ArgumentException("Invalid MLIR slab: too small or null", nameof(inputSlab));
            }

            var header = SlabHeader.Read(inputSlab);
            if (!header.HasValidMagic())
            {
                throw new ArgumentException("Invalid MLIR slab: invalid magic number");
            }

            InitializeAddressMap();
            _arena.Reset();

            int offset = SlabHeader.HeaderIndex.Length;
            int functionCount = 0;

            var headerOffset = _arena.Allocate(SlabHeader.HeaderIndex.Length);
            var headerData = SlabHeader.ForStage(3, 0);
            var headerBytes = new uint[SlabHeader.HeaderIndex.Length];
            headerData.WriteTo(headerBytes);
            _arena.Write(headerOffset, headerBytes);

            while (offset < inputSlab.Length)
            {
                var metadata = inputSlab[offset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || offset + size > inputSlab.Length)
                    break;

                if (kind == InstructionMetadataFlags.MLIR_LABEL)
                {
                    ProcessFunctionFromSlabToLlirSlab(inputSlab, offset, size, ref functionCount, stringPool);
                }

                offset += size;
            }

            var finalHeader = SlabHeader.ForStage(3, (uint)functionCount, 0);
            var finalHeaderData = new uint[SlabHeader.HeaderIndex.Length];
            finalHeader.WriteTo(finalHeaderData);
            _arena.Write(headerOffset, finalHeaderData);

            return _arena.ToContiguousArray();
        }

        private void ProcessFunctionFromSlabToLlirSlab(uint[] inputSlab, int funcOffset, int funcSize, ref int functionCount, StringPool stringPool)
        {
            var funcNameHash = inputSlab[funcOffset + 1];
            var funcInstrSize = (byte)2;
            var funcStartOffset = _arena.Allocate(funcInstrSize);
            _arena.Write(funcStartOffset, Encode(LLIR_LABEL, funcInstrSize, 1), funcNameHash);
            functionCount++;

            int bodyOffset = funcOffset + funcSize;
            while (bodyOffset < inputSlab.Length)
            {
                var metadata = inputSlab[bodyOffset];
                var size = InstructionMetadata.DecodeSize(metadata);
                var kind = InstructionMetadata.DecodeKind(metadata);

                if (size == 0 || bodyOffset + size > inputSlab.Length)
                    break;

                if (kind == InstructionMetadataFlags.MLIR_LABEL)
                    break;

                ProcessInstructionFromSlab(inputSlab, bodyOffset, kind, stringPool);
                bodyOffset += size;
            }
        }

        private void ProcessInstructionFromSlab(uint[] inputSlab, int offset, byte kind, StringPool stringPool)
        {
            if (kind == InstructionMetadataFlags.MLIR_ASSIGN)
            {
                ProcessAssignmentFromSlab(inputSlab, offset, stringPool);
                return;
            }

            var metadata = inputSlab[offset];
            var size = InstructionMetadata.DecodeSize(metadata);
            var argCount = InstructionMetadata.DecodeArgCount(metadata);

            byte llirKind = MapMlirKindToLlirKind(kind);

            var destOffset = _arena.Allocate(size);
            var buffer = new uint[size];
            Array.Copy(inputSlab, offset, buffer, 0, size);
            buffer[0] = Encode(llirKind, size, argCount);
            _arena.Write(destOffset, buffer);
        }

        private static byte MapMlirKindToLlirKind(byte mlirKind)
        {
            return mlirKind switch
            {
                InstructionMetadataFlags.MLIR_ASSIGN => InstructionMetadataFlags.LLIR_LOAD,
                InstructionMetadataFlags.MLIR_CALL => InstructionMetadataFlags.LLIR_CALL,
                InstructionMetadataFlags.MLIR_BRANCH => InstructionMetadataFlags.LLIR_BRANCH,
                InstructionMetadataFlags.MLIR_LABEL => InstructionMetadataFlags.LLIR_LABEL,
                InstructionMetadataFlags.MLIR_RETURN => InstructionMetadataFlags.LLIR_RETURN,
                _ => InstructionMetadataFlags.LLIR_LABEL
            };
        }

        private void ProcessAssignmentFromSlab(uint[] slab, int offset, StringPool stringPool)
        {
            var targetPoolOffset = slab[offset + 1];
            var valuePoolOffset = slab[offset + 2];

            string targetName = stringPool.Resolve(targetPoolOffset);
            string valueStr = stringPool.Resolve(valuePoolOffset);

            string targetAddr = MapToAddress(targetName);
            int address = ParseHexAddress(targetAddr);

            int value = 0;
            if (int.TryParse(valueStr, out int parsedValue))
            {
                value = parsedValue & 0xFF;
            }

            var loadSize = (byte)2;
            var loadOffset = _arena.Allocate(loadSize);
            _arena.Write(loadOffset, Encode(LLIR_LOAD, loadSize, 1), (uint)value);

            var storeSize = (byte)3;
            var storeOffset = _arena.Allocate(storeSize);
            _arena.Write(storeOffset, Encode(LLIR_STORE, storeSize, 2),
                (uint)(address & 0xFF), (uint)((address >> 8) & 0xFF));
        }

        private static int ParseHexAddress(string addr)
        {
            string s = addr.Trim();
            if (s.Length > 0 && s[0] == '$') s = s.Substring(1);
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
            if (int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out int val))
                return val;
            if (int.TryParse(s, out val))
                return val;
            return 0;
        }

        private string MapToAddress(string target)
        {
            if (_addressMap.TryGetValue(target, out var addr))
                return addr;

            if (target.StartsWith('$'))
                return target;

            var newAddr = $"${_nextAvailableAddress:X2}";
            _addressMap[target] = newAddr;
            _nextAvailableAddress++;
            return newAddr;
        }

        private static uint Encode(byte kind, byte size, byte argCount, bool isTerminator = false, bool hasDiagnostic = false)
        {
            return InstructionMetadata.Encode(kind, size, argCount, isTerminator, hasDiagnostic);
        }
    }
}