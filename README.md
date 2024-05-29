# GameVM

## A virtual machine specifically designed for video games

The goal of GameVM is to make developing video games for game consoles easier. It's still in the early stages of development but some of the features I intend to implement are:

- [Runs on at least 2nd through 5th generation game consoles]
- [Dynamically Generated VM]
- [Support for mutltiple forms of dispatching]
- [Support for familiar high level languages]
- [Library based hardware abstraction layer]
- [Seamless interop between bytecode and machine code]
- [Fine grained compile time control over what is compiled to bytecode and what is compiled to machine code]
- [JIT compiling for consoles that have enough RAM to support it]

## Runs on at least 2nd through 5th generation game consoles

My end goal is that a game that targets one console can be ported to another console by a simple recompile provided it uses the [HAL](#library-based-hardware-abstraction-layer) and stays within the target's ROM and RAM contraints. With this in mind GameVM will initially target the following consoles:

- 2nd Generation Consoles
  - Fairchild Channel F (Fairchild F8)
  - Atari VSC/2600 (MOS 6507)
  - Atari 5200 (MOS 6502C)
  - Bally Astrocade (Zilog Z80)
  - Magnavox Odyssey 2 (Intel 8048)
  - Intellivision (General Instrument CP1610)
  - ColecoVision (Zilog Z80)
  - Vectrex (Motorola MC68A09)
  - Arcadia 2001 (Signetics 2650/2650A)
- 3rd Generation Consoles
  - Nintendo Entertainment System (NES) (Ricoh 2A03)
  - Sega Master System (Zilog Z80)
  - Atari 7800 (Atari SALLY)
  - Commodore 64 Games System (MOS Technology 6510)
  - Amstrad GX4000 (Zilog Z80)
  - Sega SG-1000 Mark III (Zilog Z80)
  - NEC PC Engine/TurboGrafx-16 (Hudson Soft HuC6280)
  - Mattel Intellivision II (General Instrument CP1610)
  - Atari XEGS (Atari SALLY)
  - Casio PV-1000 (Zilog Z80)
- 4th Generation Consoles
  - Super Nintendo Entertainment System (SNES) (Ricoh 5A22)
  - Sega Genesis/Mega Drive (Motorola 68000)
  - TurboGrafx-16/PC Engine (HuC6280)
  - Neo Geo AES (Motorola 68000)
  - Philips CD-i (MIPS RISC)
  - Commodore Amiga CD32 (Motorola 68EC020)
  - Atari Jaguar (Tom and Jerry custom chips)
  - 3DO Interactive Multiplayer (ARM60)
  - Sega Game Gear (Zilog Z80)
  - Sega Pico (Motorola 68000)
  - Pioneer LaserActive (Mitsubishi M37702)
  - Amstrad Mega PC (Intel 8086)
  - NEC PC-FX (NEC V810)
  - SNK Neo Geo CD (Motorola 68000)
  - FM Towns Marty (Intel 80386)
  - Bandai Playdia (Renesas H83002)
  - Casio Loopy (Mitsubishi M74050)
  - Apple Pippin (PowerPC 603)
  - Tiger R-Zone (Toshiba TMP91C242F)
  - Handhelds
    - Nintendo Game Boy (Sharp LR35902)
    - Sega Game Gear (Zilog Z80)
    - Atari Lynx (2x Custom CMOS)
    - NEC TurboExpress/PC Engine GT (HuC6280)  
- 5th Generation Consoles
  - Sony PlayStation (MIPS R3000)
  - Sega Saturn (2x Hitachi SH-2)
  - Nintendo 64 (MIPS R4300i)
  - 3DO Interactive Multiplayer (ARM60)
  - Atari Jaguar (Tom and Jerry custom chips)
  - Bandai Playdia (Renesas H83002)
  - NEC PC-FX (NEC V810)
  - Sega Pico (Sega Genesis/Mega Drive compatible processor)
  - Apple Bandai Pippin (PowerPC 603e)
  - Handhelds
    - Nintendo Game Boy Color (Zilog Z80)
    - Neo Geo Pocket/Neo Geo Pocket Color (Toshiba TLCS-900H)
    - Bandai WonderSwan/WonderSwan Color (NEC V30MZ)
    - SNK Neo Geo Pocket Color (Toshiba TLCS-900H)

Why not 1st generation consoles? 1st generation consoles did not have interchangable games, used custom hardware and were produced in limited quantities. Because of this there has been little interest in creating homebrew games for these consoles. I may consider supporting these consoles if there is enough interest though.

## Support for familiar high-level languages

GameVM aims to provide support for familiar high-level programming languages, enabling game developers to write games or game engines using their preferred languages. Here is a list of general-purpose programming languages commonly used in game development, ordered by the difficulty of implementing a compiler for each language (from easiest to most difficult):

- Python
- Lua
- JavaScript
- C#
- Ruby
- Java
- Haxe
- Go
- Rust
- Pascal
- Basic
- C++
- C

## Dynamically Generated VM

Most VMs provide a static instruction set, which includes numerous instructions that may not be used by applications at runtime. GameVM takes a different approach. It dynamically generates an interpreter specific to the game during compilation. This optimized interpreter supports only the instructions required by the game.

GameVM utilizes a technique similar to optimizing compilers. Instructions are mapped to virtual op codes dynamically, tailored to the specific game being compiled. This customization not only saves space but can also enhance the VM's performance.

In addition, GameVM empowers game developers to create explicit superinstructions. These superinstructions are combinations of primitive instructions embedded in the VM itself. By leveraging superinstructions, developers can avoid runtime decoding of virtual instructions into native instructions.

By adopting this dynamic generation approach and supporting superinstructions, GameVM streamlines the execution of games and facilitates easier portability across different consoles.

## Support for mutltiple forms of dispatching

Dispatching is a crucial aspect of VMs, determining how instructions are executed. GameVM supports multiple forms of dispatching, each with its own characteristics. Here's a summary:

- Native code: Instructions are executed sequentially by the CPU. This is traditional compiled applications or sometimes Ahead Of Time (AOT) compilation. This is usually the fastest approach, although it may result in larger code size.
- Subroutine threaded code (STC): Functions are used to encapsulate low-level CPU operations, reducing memory footprint but incurring function call overhead.
- Direct threaded code (DTC): Addresses are used instead of functions, stored in an ordered array. An instruction pointer variable points to the current address, resulting in efficient execution with minimal overhead.
- Indirect threaded code (ITC): Adds an extra layer of indirection to DTC, using a table of addresses to addresses. This approach offers potential benefits, such as architecture independence, but may have performance costs.
- Token threaded code: Uses a table of tokens instead of addresses. This approach can significantly reduce memory usage (especially if the token size is limited to a byte) and is portable across different platforms. However, it generally incurs a performance penalty and is often used in environments with limited RAM.
- Line Interpreter: Deploys a game as minified source code along with a line interpreter. This option comes with significant trade-offs. A line interpreter is much slower than a threaded code interpreter. Additionally, while high level source code is often more compact than bytecode or machine code, a line interpreter is larger than a threaded code interpreter due to the inclusion of a parser and tokenizer. So depending on the size of your game this option may actually result in a larger overall size despite the conpactness of the source code.

These different forms of dispatching provide flexibility in optimizing the VM's performance and memory usage based on the specific requirements of the game and the target console.

## Library based hardware abstraction layer

While the processor is abstracted by the VM itself, support for all other hardware is provided by optional modular libraries. This includes virtual memory, video and audio controllers, game controllers, and persistent storage (if available). The goal of these libraries is to provide a consistent interface to these subsystems to programmers. This may not always be ideal, especially on the more resource constrained game consoles. For those situations the libraries can act as well documented example code for how to interact directly with the hardware of a specific system.

## Licensing

TBD
