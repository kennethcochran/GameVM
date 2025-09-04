# GameVM

A virtual machine specifically designed for video games, making retro console development easier by providing modern tools while respecting vintage hardware constraints.

> **Note**: This project is in active development and not yet ready for production use.

## Quick Start

### Prerequisites

GameVM requires:
- .NET 8 SDK (LTS) ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Java Development Kit (JDK) 21 or later (required for ANTLR tooling)
  - Options: [Eclipse Temurin](https://adoptium.net/), [Microsoft Build of OpenJDK](https://www.microsoft.com/openjdk), or [Amazon Corretto](https://aws.amazon.com/corretto/)
- The build process will automatically handle ANTLR tooling

### Build
```pwsh
dotnet build
```

## Documentation

Comprehensive documentation is available in the [docs](docs/) directory:

## Development Status

GameVM is currently in early development. The current focus is on building the core compiler infrastructure.

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md).

## License

This project is released under the [Unlicense](LICENSE).
