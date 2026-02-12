# Supported Platforms

This directory contains documentation about platforms supported by GameVM.

## Overview

GameVM is designed to be a multi-target compiler system capable of generating code for various retro gaming platforms and modern systems.

## Platform Categories

### Retro Gaming Consoles
- **Atari 2600** - Atari 2600 VCS
- **Atari 5200** - Atari 5200 SuperSystem
- **Atari 7800** - Atari 7800 ProSystem
- **Atari Jaguar** - Atari 64-bit multimedia system
- **Atari Lynx** - Atari Lynx handheld

### Nintendo Systems
- **NES** - Nintendo Entertainment System
- **SNES** - Super Nintendo Entertainment System
- **Game Boy** - Nintendo Game Boy
- **Game Boy Color** - Nintendo Game Boy Color
- **Nintendo 64** - Nintendo 64

### Sega Systems
- **Master System** - Sega Master System
- **Genesis** - Sega Genesis/Mega Drive
- **Game Gear** - Sega Game Gear
- **Sega Saturn** - Sega Saturn
- **Sega Nomad** - Sega Nomad handheld

### Other Retro Systems
- **Neo Geo** - SNK Neo Geo
- **Neo Geo Pocket** - SNK Neo Geo Pocket
- **Neo Geo Pocket Color** - SNK Neo Geo Pocket Color
- **TurboGrafx-16** - NEC TurboGrafx-16/PC Engine
- **TurboExpress** - NEC TurboExpress handheld
- **PC-FX** - NEC PC-FX
- **3DO** - 3DO Interactive Multiplayer
- **Philips CD-i** - Philips CD-i
- **Commodore CDTV** - Commodore CDTV

### Handheld Systems
- **WonderSwan** - Bandai WonderSwan
- **WonderSwan Color** - Bandai WonderSwan Color
- **Game Gear** - Sega Game Gear
- **TurboExpress** - NEC TurboExpress
- **Game Boy** - Nintendo Game Boy
- **Game Boy Color** - Nintendo Game Boy Color
- **Neo Geo Pocket** - SNK Neo Geo Pocket
- **Neo Geo Pocket Color** - SNK Neo Geo Pocket Color
- **Atari Lynx** - Atari Lynx
- **Sega Nomad** - Sega Nomad
- **TurboExpress** - NEC TurboExpress
- **Game Boy** - Nintendo Game Boy
- **Game Boy Color** - Nintendo Game Boy Color

### Obscure and Rare Systems
- **Channel F** - Fairchild Channel F
- **Emerson Arcadia 2001** - Emerson Arcadia 2001
- **Hartung Game Master** - Hartung Game Master
- **Entex Adventure Vision** - Entex Adventure Vision
- **Bally Astrocade** - Bally Astrocade
- **RCA Studio II** - RCA Studio II
- **Epoch Cassette Vision** - Epoch Cassette Vision
- **Epoch Super Cassette Vision** - Epoch Super Cassette Vision
- **Casio PV-1000** - Casio PV-1000
- **Amstrad GX4000** - Amstrad GX4000
- **Bitcorp Gamate** - Bitcorp Gamate
- **Watara Supervision** - Watara Supervision
- **Mega Duck** - Mega Duck
- **Casio Loopy** - Casio Loopy
- **Apple Pippin** - Apple Pippin
- **Neo Geo Pocket** - Neo Geo Pocket
- **Neo Geo Pocket Color** - Neo Geo Pocket Color

## Platform Documentation

Each platform has detailed documentation in the `specs/` subdirectory:

- **specs/** - Individual platform specifications
- **CapabilityProfiles.md** - Platform capability comparison
- **CapabilityProfilesReport.md** - Detailed capability analysis

## Platform Status

Platforms are categorized by implementation status:

- **Implemented** - Full support available
- **In Development** - Partial support, work in progress
- **Planned** - Support planned but not yet started
- **Research** - Feasibility study phase

## Adding New Platforms

To add support for a new platform:

1. Create a specification document in `specs/`
2. Define the capability profile
3. Implement the backend
4. Add tests and documentation
5. Update the capability profiles

See the [Compiler Architecture](../compiler/README.md) for details on backend implementation.

## Resources

- [Compiler Architecture](../compiler/README.md)
- [API Documentation](../api/README.md)
- [Architecture Overview](../architecture/README.md)
