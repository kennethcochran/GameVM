# Platform Specifications

This directory contains detailed specifications for each platform supported by GameVM.

## Specification Format

Each platform specification follows a standardized format:

### Required Sections
- **Overview** - Platform introduction and key features
- **Hardware** - CPU, memory, graphics, audio specifications
- **Capabilities** - Development capabilities and limitations
- **Development** - Development tools and environment
- **References** - External documentation and resources

### Optional Sections
- **Emulation Notes** - Specific emulation considerations
- **Performance** - Performance characteristics
- **Compatibility** - Compatibility information

## Platform List

### Console Platforms
- [3do.md](3do.md) - 3DO Interactive Multiplayer
- [apple_pippin.md](apple_pippin.md) - Apple Pippin
- [atari_2600.md](atari_2600.md) - Atari 2600 VCS
- [atari_5200.md](atari_5200.md) - Atari 5200 SuperSystem
- [atari_7800.md](atari_7800.md) - Atari 7800 ProSystem
- [atari_jaguar.md](atari_jaguar.md) - Atari Jaguar
- [atarijaguar.md](atarijaguar.md) - Atari Jaguar (alternate)
- [atari_lynx.md](atari_lynx.md) - Atari Lynx
- [bally_astrocade.md](bally_astrocade.md) - Bally Astrocade
- [channel_f.md](channel_f.md) - Fairchild Channel F
- [colecovision.md](colecovision.md) - ColecoVision
- [commodore_cdtv.md](commodore_cdtv.md) - Commodore CDTV
- [emerson_arcadia_2001.md](emerson_arcadia_2001.md) - Emerson Arcadia 2001
- [entex_adventure_vision.md](entex_adventure_vision.md) - Entex Adventure Vision
- [epoch_cassette_vision.md](epoch_cassette_vision.md) - Epoch Cassette Vision
- [epoch_super_cassette_vision.md](epoch_super_cassette_vision.md) - Epoch Super Cassette Vision
- [fm_towns_marty.md](fm_towns_marty.md) - FM Towns Marty
- [genesis.md](genesis.md) - Sega Genesis/Mega Drive
- [hartung_game_master.md](hartung_game_master.md) - Hartung Game Master
- [intellivision.md](intellivision.md) - Mattel Intellivision
- [mega_duck.md](mega_duck.md) - Mega Duck
- [neogeo.md](neogeo.md) - SNK Neo Geo
- [neo_geo.md](neo_geo.md) - SNK Neo Geo (alternate)
- [nes.md](nes.md) - Nintendo Entertainment System
- [nintendo_64.md](nintendo_64.md) - Nintendo 64
- [odyssey_2.md](odyssey_2.md) - Magnavox Odyssey 2
- [pc_fx.md](pc_fx.md) - NEC PC-FX
- [philips_cdi.md](philips_cdi.md) - Philips CD-i
- [snes.md](snes.md) - Super Nintendo Entertainment System
- [sony_playstation.md](sony_playstation.md) - Sony PlayStation
- [turbografx16.md](turbografx16.md) - TurboGrafx-16/PC Engine
- [turbografx_16.md](turbografx_16.md) - TurboGrafx-16/PC Engine (alternate)

### Handheld Platforms
- [game_boy.md](game_boy.md) - Nintendo Game Boy
- [game_boy_color.md](game_boy_color.md) - Nintendo Game Boy Color
- [game_gear.md](game_gear.md) - Sega Game Gear
- [neo_geo_pocket.md](neo_geo_pocket.md) - SNK Neo Geo Pocket
- [neo_geo_pocket_color.md](neo_geo_pocket_color.md) - SNK Neo Geo Pocket Color
- [sega_nomad.md](sega_nomad.md) - Sega Nomad
- [turboexpress.md](turboexpress.md) - NEC TurboExpress
- [wonderswan.md](wonderswan.md) - Bandai WonderSwan
- [wonderswan_color.md](wonderswan_color.md) - Bandai WonderSwan Color

### Template
- [TEMPLATE.md](TEMPLATE.md) - Template for new platform specifications

## Specification Standards

### Hardware Specifications
- **CPU**: Type, speed, architecture
- **Memory**: RAM, ROM, video memory sizes
- **Graphics**: Resolution, color depth, sprite capabilities
- **Audio**: Sound channels, sample rates
- **Storage**: Cartridge, disc, tape formats

### Development Information
- **Development Tools**: Official and third-party tools
- **Programming Languages**: Supported languages
- **Limitations**: Memory, performance, display constraints
- **Special Features**: Unique hardware capabilities

### References
- **Official Documentation** - Manufacturer documentation
- **Community Resources** - Development forums, wikis
- **Technical References** - Hardware manuals, datasheets

## Contributing

When adding a new platform specification:

1. Copy [TEMPLATE.md](TEMPLATE.md) as a starting point
2. Fill in all required sections
3. Include accurate technical specifications
4. Add relevant references and resources
5. Test all links and references
6. Update the platform list above

## Quality Assurance

- All specifications should be factually accurate
- Links should be verified and working
- Technical specifications should be complete
- References should be authoritative when possible

## Related Documentation

- [Platform Overview](../README.md) - General platform information
- [Capability Profiles](../CapabilityProfiles.md) - Platform capabilities comparison
- [Capability Report](../CapabilityProfilesReport.md) - Detailed capability analysis
- [Compiler Architecture](../../compiler/README.md) - Compiler implementation details
