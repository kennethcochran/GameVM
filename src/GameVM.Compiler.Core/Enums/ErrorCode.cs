namespace GameVM.Compiler.Core.Enums
{
    /// <summary>
    /// Machine-readable error codes for <see cref="GameVM.Compiler.Core.Exceptions.GameVmException"/>.
    /// Codes are grouped by domain, per the documented convention:
    /// general (0x0001+), file (0x1000+), network (0x2000+), module (0x3000+),
    /// backend/code-generation (0x4000+).
    /// </summary>
    public enum ErrorCode : uint
    {
        /// <summary>General: out of memory (0x0001).</summary>
        OutOfMemory = 0x0001,

        /// <summary>General: invalid argument (0x0002).</summary>
        InvalidArgument = 0x0002,

        /// <summary>
        /// Backend: an MLIR instruction reached the target transformer with no
        /// LLIR lowering defined (0x4001). The construct is outside the
        /// compilable subset and must fail loudly, never be silently dropped.
        /// </summary>
        UnhandledInstruction = 0x4001,

        /// <summary>
        /// Backend: an LLIR instruction reached code generation with no
        /// machine-code emission defined (0x4002).
        /// </summary>
        UnhandledLlirInstruction = 0x4002,

        /// <summary>
        /// Backend: zero-page variable allocation exhausted the target's
        /// RAM window (0x4003).
        /// </summary>
        ZeroPageExhausted = 0x4003,

        /// <summary>
        /// Backend: a branch displacement is outside the target CPU's
        /// relative-branch range (0x4004).
        /// </summary>
        BranchOutOfRange = 0x4004,
    }
}
