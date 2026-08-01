using GameVM.Compiler.Core.IR.Interfaces;
using GameVM.Compiler.Core.IR.Slab;
using GameVM.Compiler.Core.IR.Buffers;
using System;

namespace GameVM.Compiler.Core.IR
{
    public class MlirBuilder : IIRTransformer<HighLevelIR, MidLevelIR>
    {
        public MidLevelIR Transform(HighLevelIR input)
        {
            var transformer = new Transformers.HlirToMlirTransformer();
            return transformer.Transform(input);
        }
#pragma warning disable CS0618 // Type or member is obsolete

        public uint[] TransformSlab(uint[] inputSlab, StringPool stringPool)
        {
            // WILLIMPLEMENT: proper HighLevelIR slab to MidLevelIR slab conversion
            // For now, return empty slab to indicate not implemented
            return Array.Empty<uint>();
        }
    }
}
