# Standard Library Specification

## Purpose
Defines the GameVM Standard Library, which provides a comprehensive set of utilities, data structures, and algorithms designed for retro game development, combining classic programming paradigms with performance-optimized implementations tailored for resource-constrained environments. Aspirational — not yet implemented.

## Requirements

### Requirement: Design Philosophy
The Standard Library SHALL be performance-first, retro-focused, memory-conscious, hardware-aware, and developer-friendly, and SHALL be organized into a Core Library, a Game Library, and a Platform Library.

#### Scenario: Library organization
- **WHEN** the Standard Library is organized
- **THEN** it SHALL be divided into a Core Library (data types, memory utilities, math, strings, I/O), a Game Library (graphics, audio, input, collision, animation), and a Platform Library (HAL integration, platform optimizations, system utilities, debugging tools).

### Requirement: Fixed-Point Mathematics
The Standard Library SHALL provide 16.16 and 8.8 fixed-point types and fixed-point operations including addition, subtraction, multiplication, division, square root, sine, and cosine, which SHALL be available as superinstructions.

#### Scenario: Fixed-point arithmetic
- **WHEN** a program performs fixed-point add, subtract, multiply, divide, or square root
- **THEN** the library SHALL provide the corresponding operation on 16.16 and 8.8 fixed-point values.

#### Scenario: Fixed-point triggers
- **WHEN** a program computes sine or cosine of a fixed-point angle
- **THEN** the library SHALL provide FixedSin and FixedCos, available as superinstructions.

### Requirement: Vector Mathematics
The library SHALL provide a 2D vector type based on fixed-point components and vector operations (add, subtract, scalar multiply, dot, cross, length, normalize, rotate), available as superinstructions.

#### Scenario: Vector operations
- **WHEN** a program performs 2D vector arithmetic, dot/cross product, length, normalization, or rotation
- **THEN** the library SHALL provide the corresponding operation with a fixed-point Vector2D type.

### Requirement: Memory Management
The Standard Library SHALL provide a memory pool for fixed-size allocations (create, allocate, deallocate, reset, destroy) and a stack-based allocator supporting marks (create, allocate, push/pop mark, reset).

#### Scenario: Memory pool allocation
- **WHEN** a program needs fixed-size allocations
- **THEN** it SHALL use the memory pool API to create, allocate from, deallocate from, reset, and destroy a pool.

#### Scenario: Stack allocator with marks
- **WHEN** a program needs scoped temporary allocations
- **THEN** it SHALL use the stack allocator API, including push and pop of stack marks.

### Requirement: Collections
The library SHALL provide a bounds-checked static array (create, add, get, set, length) and a circular buffer (create, enqueue, dequeue, and emptiness/fullness queries).

#### Scenario: Static array with bounds checking
- **WHEN** a program uses a static array with a specified capacity
- **THEN** the library SHALL provide create, add, get, set, and length operations that check bounds.

#### Scenario: Circular buffer
- **WHEN** a program uses a circular buffer of an item type with a capacity
- **THEN** the library SHALL provide create, enqueue, and dequeue operations plus empty and full queries.

### Requirement: String Operations
The library SHALL provide a fixed-size string type (up to 256 characters) and operations (create, convert to/from string, compare, concat, copy, length), with the core operations available as superinstructions.

#### Scenario: Fixed-size string handling
- **WHEN** a program manipulates strings
- **THEN** the library SHALL provide the fixed-size string type and create, convert, compare, concat, copy, and length operations.

### Requirement: Trigonometric and Geometry Functions
The library SHALL provide precomputed lookup tables and fast trigonometric functions (sin, cos, tan, atan2) plus geometry/collision operations (point-in-circle, point-in-rect, circle collision, rect collision), all available as superinstructions.

#### Scenario: Fast trigonometry via lookup tables
- **WHEN** a program computes fast sine, cosine, tangent, or atan2
- **THEN** the library SHALL provide those functions backed by precomputed lookup tables.

#### Scenario: Geometry queries
- **WHEN** a program tests point-in-circle, point-in-rect, circle-circle, or rect-rect collision
- **THEN** the library SHALL provide the corresponding geometry/collision operations as superinstructions.

### Requirement: Graphics System
The library SHALL provide a color type and drawing primitives (set/get pixel, line, rectangle, filled rectangle, circle, filled circle, ellipse).

#### Scenario: Drawing primitives
- **WHEN** a program draws primitive shapes
- **THEN** the library SHALL provide set/get pixel, line, rectangle, filled rectangle, circle, filled circle, and ellipse operations.

### Requirement: Sprite System
The library SHALL provide a sprite type and operations to load a sprite and to draw it, draw a frame, draw scaled, draw rotated, and draw flipped.

#### Scenario: Sprite loading and drawing
- **WHEN** a program loads and draws sprites
- **THEN** the library SHALL provide LoadSprite and DrawSprite operations, including frame, scaled, rotated, and flipped variants.

### Requirement: Tile System
The library SHALL provide tile and tilemap types and operations to load a tile, create a tilemap, set/get tiles, and draw a tilemap or individual tile.

#### Scenario: Tilemap manipulation
- **WHEN** a program manages tiled maps
- **THEN** the library SHALL provide tile loading, tilemap creation, set/get tile index, and tile/tilemap drawing operations, including a collision mask per tile.

### Requirement: Audio System
The library SHALL provide sound and music types and operations to load, play (once or looped), stop, set volume and pan (sound), set tempo and volume (music), and query play state.

#### Scenario: Sound effect playback
- **WHEN** a program plays sound effects
- **THEN** the library SHALL provide load, play, loop, stop, set volume, set pan, and play-state operations.

#### Scenario: Music playback
- **WHEN** a program plays background music
- **THEN** the library SHALL provide load, play, stop, set volume, set tempo, and play-state operations.

### Requirement: Input System
The library SHALL provide a controller state type with button constants and operations to get controller state, query button press/just-pressed/just-released state, get axis values, and rumble a controller.

#### Scenario: Controller input
- **WHEN** a program reads controller input
- **THEN** the library SHALL provide GetControllerState, IsButtonPressed, IsButtonJustPressed, IsButtonJustReleased, GetAxisValue, and RumbleController operations with standard button constants.

### Requirement: Animation System
The library SHALL provide animation types and operations to create an animation, add a frame, play, stop, update, draw, and query completion.

#### Scenario: Animation playback
- **WHEN** a program manages sprite animations
- **THEN** the library SHALL provide create, add frame, play, stop, update, draw, and completion-query operations.

### Requirement: Collision Detection Superinstructions
The library SHALL provide collision detection superinstructions for pixel, rectangle, circle, sprite, and tile collision.

#### Scenario: Collision checks
- **WHEN** a program checks for collision between sprites, rectangles, circles, or map tiles
- **THEN** the library SHALL provide the corresponding pixel, rect, circle, sprite, and tile collision superinstructions.

### Requirement: Rendering Optimizations
The library SHALL provide rendering superinstructions for sprite batching, optimized tilemap, textured quad drawing, and anti-aliased line and circle drawing.

#### Scenario: Batch rendering
- **WHEN** a program renders large numbers of sprites or drawing primitives
- **THEN** the library SHALL provide sprite batch, optimized tilemap, textured quad, and anti-aliased line/circle superinstructions.

### Requirement: Physics Calculations
The library SHALL provide physics superinstructions for position/velocity/acceleration integration, gravity application, boundary collision, and collision resolution.

#### Scenario: Physics updates
- **WHEN** a program updates game physics
- **THEN** the library SHALL provide superinstructions for physics integration, gravity, boundary collision, and collision resolution.

### Requirement: Time and Timing
The library SHALL provide time/timing operations (current time, frame time, delay, wait for vertical blank, wait for horizontal blank, cycle counter).

#### Scenario: Timing operations
- **WHEN** a program needs time or frame timing
- **THEN** the library SHALL provide current time, frame time, delay, VBlank/HBlank waits, and CPU cycle counter operations.

### Requirement: Random Number Generation
The library SHALL provide a seedable random generator with create, random integer, random fixed-point, random range, and reseed operations.

#### Scenario: Random generation
- **WHEN** a program needs random values
- **THEN** the library SHALL provide a seedable generator with integer, fixed-point, range, and reseed operations.

### Requirement: Debugging Utilities
The library SHALL provide debugging utilities to print values of different types, break into the debugger, and assert a condition with a message.

#### Scenario: Debug output and assertions
- **WHEN** a program needs debugging support
- **THEN** the library SHALL provide DebugPrint variants, DebugBreak, and Assert operations.

### Requirement: Library Initialization and Cleanup
The library SHALL provide init/cleanup procedures that initialize the trigonometry tables, random number generator, main and frame memory pools, and HAL, and that clean up the pools and HAL.

#### Scenario: Library initialization
- **WHEN** a program starts
- **THEN** initialization SHALL set up trig tables, the random generator, the main and frame memory pools, and the HAL.

#### Scenario: Library cleanup
- **WHEN** a program exits
- **THEN** cleanup SHALL destroy the memory pools and shut down the HAL.

### Requirement: Performance Profiling
The library SHALL provide a profiler for measuring and averaging operation times.

#### Scenario: Profiling
- **WHEN** a program needs performance measurements
- **THEN** the library SHALL provide start, stop, average-time, and reset operations for a profiler.