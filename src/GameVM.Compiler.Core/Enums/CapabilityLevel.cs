namespace GameVM.Compiler.Core.Enums
{
    /// <summary>
    /// Defines the hardware capability levels (L1 - L7) for the GameVM ecosystem.
    /// </summary>
    public enum CapabilityLevel
    {
        /// <summary>
        /// L1: Bare-Metal Baseline (Atari 2600, RCA Studio II)
        /// </summary>
        L1,

        /// <summary>
        /// L2: Fixed Display & Multi-Channel IO (Atari 7800, ColecoVision)
        /// </summary>
        L2,

        /// <summary>
        /// L3: Scrolling & Dynamic Viewports (NES, Master System, Atari 5200)
        /// </summary>
        L3,

        /// <summary>
        /// L4: Multi-Layer & FM Synthesis (Genesis, TurboGrafx-16)
        /// </summary>
        L4,

        /// <summary>
        /// L5: Affine Transformation & PCM Audio (SNES, Neo Geo)
        /// </summary>
        L5,

        /// <summary>
        /// L6: Geometric Pipeline & Media Streaming (PS1, Saturn)
        /// </summary>
        L6,

        /// <summary>
        /// L7: Filtered Pipeline & Vector Precision (N64)
        /// </summary>
        L7
    }
}
