# GameVM System Specs and Profiles

## 1. The "Hardware Contract" Philosophy
GameVM does not just target individual consoles; it targets **System Specs**. A Spec (or Profile) is a guaranteed hardware baseline. By developing against a Spec, your game is guaranteed to run on any console that fulfills that spec.

This model is inspired by **J2ME (Java Micro Edition)**, ensuring that the "porting nightmare" is caught by the compiler at design-time, rather than by the developer at build-time.

---

## 2. Hardware Tiers (L1 - L7)

Developers choose a **System Spec** at the start of their project. This "Profile" acts as a boundary—the compiler will prevent you from using features that aren't in your chosen spec.

## 2. Hardware Tiers (L1 - L7)

GameVM uses a **Cumulative Abstraction Model**. Each tier defines a sensible hardware baseline appropriate for its generation, which is then extended by the subsequent tier. This allows developers to rely on a stable set of signatures while moving up the power scale.

### **`GV.Spec.L1` (Bare-Metal Baseline)**
*   **Philosophy**: The core execution contract where the CPU is the "Video Card."
*   **Capsule Model**: A hybrid of **Framework Drivers** and **Developer Kernels**.
*   **Abstractions**: 
    - **Timing (Driver)**: GVM manages the vertical blanking and overscan periods.
    - **Input (Driver)**: GVM provides native drivers for controller ports.
    - **Graphics (Kernel)**: The developer implements the "Display Kernel." This covers both "Racing the Beam" (Atari 2600/VCS) and "DMA Feeding" (RCA Studio II/Pixie) architectures, where CPU-driven synchronization is required for every scanline.
*   **Signatures (Provided by GVM)**:
    - `GameVM_System_AcknowledgeInterrupt`
    - `GameVM_Input_PollPrimaryControllerState`
*   **Signatures (Implemented by Dev)**:
    - `GameVM_Graphics_UpdateScanlineKernel`
    - `GameVM_Graphics_AwaitVerticalBlank`

### **`GV.Spec.L2` (Fixed Display & Multi-Channel IO)**
*   **Philosophy**: Coalescing around the first "Standard" console features.
*   **Extension**: **Extends L1** with object-based abstractions.
*   **Abstractions**:
    - **Graphics**: Static Tiles and Hardware Sprites (no/coarse scrolling).
    - **Audio**: Multi-channel Pulse/Triangle/Noise synthesis.
    - **Input**: **Extends L1** to include D-Pad (8-way) + Multiple Action Buttons.
*   **Signatures**:
    - `GameVM_Graphics_SetSpritePosition`
    - `GameVM_Audio_InitializeSoundChannel`
    - `GameVM_Input_PollExtendedControllerState`

### **`GV.Spec.L3` (Scrolling & Dynamic Viewports)**
*   **Philosophy**: Smooth, screen-filling motion.
*   **Extension**: **Extends L2** by adding movement to the fixed display.
*   **Abstractions**:
    - **Graphics**: Sub-pixel hardware scrolling and large virtual tilemaps.
    - **Audio**: Advanced PSG envelopes and volume modulation.
*   **Signatures**:
    - `GameVM_Graphics_UpdateBackgroundScrollOffsets`
    - `GameVM_Audio_SetChannelVolumeEnvelope`

### **`GV.Spec.L4` (Multi-Layer & FM Synthesis)**
*   **Philosophy**: Depth through parallax and complex harmonics.
*   **Extension**: **Extends L3** by layering the viewports.
*   **Abstractions**:
    - **Graphics**: Multiple independent background planes and higher color depth.
    - **Audio**: FM Synthesis (Frequency Modulation) operators.
*   **Signatures**:
    - `GameVM_Graphics_SelectActiveParallaxPlane`
    - `GameVM_Audio_UpdateFmOperatorParameters`
    - `GameVM_Graphics_LoadHardwarePalette`

### **`GV.Spec.L5` (Affine Transformation & PCM Audio)**
*   **Philosophy**: Mathematical manipulation of the 2D plane.
*   **Extension**: **Extends L4** with pixel-level transformations.
*   **Abstractions**:
    - **Graphics**: Affine transformations (Scale, Rotate, Shear) for background/sprites.
    - **Audio**: Digital PCM (Sample-based) audio reproduction.
*   **Signatures**:
    - `GameVM_Math_SetAffineTransformationMatrix`
    - `GameVM_Audio_StreamPcmBufferFragment`

### **`GV.Spec.L6` (Geometric Pipeline & Media Streaming)**
*   **Philosophy**: The transition to 3D and high-capacity storage.
*   **Extension**: **Extends L5** with geometric primitives and modern input.
*   **Abstractions**:
    - **Graphics**: Rasterized polygons (Triangles) and transformation matrices.
    - **Storage**: High-capacity CD-Media data and audio streaming.
    - **Input**: **Extends L2/L5** with high-precision Analog Axes and Triggers.
*   **Signatures**:
    - `GameVM_Graphics_SubmitTriangleMesh`
    - `GameVM_Data_BeginAsynchronousDiskRead`
    - `GameVM_Input_ReadAnalogStickAxis`

### **`GV.Spec.L7` (Filtered Pipeline & Vector Precision)**
*   **Philosophy**: Modernized 3D with hardware depth and smoothing.
*   **Extension**: **Extends L6** with pipeline state controls.
*   **Abstractions**:
    - **Graphics**: Depth management (Z-Buffering) and Anti-Aliasing/Filtering.
    - **Math**: Dedicated high-precision vector and matrix arithmetic.
*   **Signatures**:
    - `GameVM_Graphics_ConfigureDepthBufferRange`
    - `GameVM_Graphics_SetTextureFilteringMode`
    - `GameVM_Math_ExecuteVectorTransform`

---

## 3. System Compatibility Matrix

This table maps every supported console to its **highest guaranteed** hardware spec.

| System                          |  L1   |  L2   |  L3   |  L4   |  L5   |  L6   |  L7   |
| :------------------------------ | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **APF-MP1000**                  |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Atari 2600**                  |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Bitcorp Gamate**              |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Emerson Arcadia 2001**        |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Entex Adventure Vision**      |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Epoch Cassette Vision**       |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Fairchild Channel F**         |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Magnavox Odyssey²**           |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **RCA Studio II**               |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Vectrex**                     |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Atari 7800**                  |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Bally Astrocade**             |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Casio PV-1000**               |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **ColecoVision**                |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Intellivision**               |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Sega SG-1000**                |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Atari 5200**                  |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Atari Lynx**                  |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Atari XEGS**                  |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Epoch Super Cassette Vision** |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Game Boy / Color**            |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Game Gear**                   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Hartung Game Master**         |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Mega Duck**                   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **NES**                         |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Sega Master System**          |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Tiger Game.com**              |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Watara Supervision**          |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |   ❌   |
| **Amstrad GX4000**              |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |
| **Neo Geo Pocket (Mono)**       |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |
| **Sega Genesis / Nomad**        |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |
| **TurboGrafx-16 / Express**     |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |
| **WonderSwan (Mono)**           |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |   ❌   |
| **Casio Loopy**                 |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |
| **Neo Geo (AES/MVS)**           |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |
| **Neo Geo Pocket Color**        |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |
| **SNES**                        |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |
| **WonderSwan Color**            |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |   ❌   |
| **3DO**                         |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **Apple Pippin**                |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **Atari Jaguar**                |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **Commodore CDTV**              |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **FM Towns Marty**              |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **PC-FX**                       |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **Philips CD-i**                |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **Pioneer LaserActive**         |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **Sega Saturn**                 |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **Sony PlayStation**            |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ❌   |
| **Nintendo 64**                 |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |   ✅   |

---

## 5. System Extensions (Hardware Injections)

Extensions are optional hardware capabilities—often provided by custom cartridge hardware or co-processors—that allow a project to access **Signatures from Higher Tiers** without changing the base machine requirements.

### The "Hybrid" Logic
A project remains anchored to its **Base Level** (for timing and core I/O) but can "inject" specific capabilities from the future.

*   **Example: "The DPC Injection"**
    - **Base Console**: Atari 2600 (**L1**).
    - **Injection**: `Ext.Snd.Polyphonic` (via the custom DPC chip developed for *Pitfall II*).
    - **Result**: The project remains an L1 game (racing the beam), but the compiler allows the use of **L5-grade PCM** or multi-voice audio signatures that the base 2600 TIA could never achieve.

### Extension Categories
| Extension ID       | Category | Effect                                                            |
| :----------------- | :------- | :---------------------------------------------------------------- |
| `Ext.Gfx.Mapper`   | Graphics | Permits high-tier bank-switching or scroll-assistance signatures. |
| `Ext.Snd.Injected` | Audio    | Permits high-tier synthesis (FM/PCM) on low-tier machines.        |
| `Ext.Math.Fast`    | Math     | Permits high-tier vector/matrix signatures (e.g. SuperFX, DPC).   |

---

## 6. Software Fallbacks (Polyfills)

If a developer uses a feature that is not natively supported by the hardware *or* an injection, the compiler may provide a **Software Fallback** (emulation). 

### Implementation Logic Gate
For every capability signature used, the compiler follows this priority:
1.  **NATIVE ✅**: Target hardware supports it directly.
2.  **INJECTED 💉**: Registered hardware extension provides it.
3.  **EMULATED ⚠️**: Compiler injects a software polyfill. Issues a **Performance Warning**.
4.  **IMPOSSIBLE ❌**: Feature exceeds target resources. Issues an **Error**.

### Fallback Availability Matrix
| Module           |  L1   |  L2   |  L3   |  L4   |  L5   |  L6   |  L7   | Fallback Logic                    |
| :--------------- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :-------------------------------- |
| `HAL.Gfx.Tile`   | **⚠️** | **✅** | **✅** | **✅** | **✅** | **✅** | **✅** | Software Rasterization (High CPU) |
| `HAL.Gfx.Sprite` | **⚠️** | **✅** | **✅** | **✅** | **✅** | **✅** | **✅** | Software Blitting                 |
| `HAL.Gfx.Scroll` | **❌** | **⚠️** | **✅** | **✅** | **✅** | **✅** | **✅** | Requires RAM for full tilemaps.   |
| `HAL.Gfx.Affine` | **❌** | **❌** | **❌** | **⚠️** | **✅** | **✅** | **✅** | Software Fixed-Point (Very Slow)  |
| `HAL.Snd.PCM`    | **⚠️** | **⚠️** | **⚠️** | **⚠️** | **✅** | **✅** | **✅** | Bit-Banging (High CPU overhead)   |

---

## 7. Advisory Enforcement (The "Hacker" Mode)

By default, the profile set in `gamevm.yaml` is a **Strict Contract**. However, GameVM allows an **Advisory Mode** for experimentation.

### Enforcement Matrix
| Context      | Native Support | Fallback Possible | Impossible (Hard Stop) |
| :----------- | :------------- | :---------------- | :--------------------- |
| **Strict**   | Allow          | Error             | Error                  |
| **Advisory** | Allow          | Warning + Inject  | Error (Hard Stop)      |

Developers can enable this in their project configuration:
```yaml
profile: GV.Spec.L3
enforcement: advisory
```

---

## 8. Developer Workflow

1.  **Set the Budget**: Define `profile: GV.Spec.L3` across `gamevm.yaml`.
2.  **Define Injections**: Register any custom hardware (e.g. `Ext.Snd.Injected`) if your target cartridge supports it.
3.  **Implementation Discovery**: The compiler analyzes calls to high-level signatures.
4.  **Performance Feedback**: 
    - If a feature is natively supported or injected: No warning.
    - If a feature is emulated: Warning: *"Using software fallback for Tiles on L1 target. Expect significant slowdown."*
5.  **Universal Port**: The code remains the same; only the "Performance Reality" changes based on the target and its injections.
