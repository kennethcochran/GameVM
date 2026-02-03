# GameVM

A virtual machine specifically designed for video games, making retro console development easier by providing modern tools while respecting vintage hardware constraints.

> **Note**: This project is in active development and not yet ready for production use.

## Quick Start

### Prerequisites

GameVM requires:
- .NET 10 SDK ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- Java Development Kit (JDK) 21 or later (required for ANTLR tooling)
  - Options: [Eclipse Temurin](https://adoptium.net/), [Microsoft Build of OpenJDK](https://www.microsoft.com/openjdk), or [Amazon Corretto](https://aws.amazon.com/corretto/)
- The build process will automatically handle ANTLR tooling

## Testing

GameVM uses several testing strategies to ensure correctness:

- **Unit Tests**: NUnit tests for individual components (found in `src/UnitTests`, `src/GameVM.Compiler.Core.Tests`, etc.).
- **BDD Tests**: Scenario-based end-to-end tests using Reqnroll (found in `src/GameVM.Compiler.Specs`). These tests cover language features, backend code generation, and behavior validation via MAME execution.

### Build
```pwsh
dotnet build
```

## Documentation

Comprehensive documentation is available in the [docs](docs/) directory:
- [Compiler Design](docs/compiler/) - Toolchain, IRs, and implementation
- [Standard Library & HAL](docs/api/) - Developer APIs and libraries
- [System References](docs/platforms/specs/) - Hardware specs and platforms
- [Architecture](docs/architecture/) - Strategy, testing, and lifecycle

For an overview of the project architecture and design principles, start with the [Architecture Overview](docs/architecture/ArchitectureOverview.md).

## Development Status

GameVM is currently in early development. The current focus is on building the core compiler infrastructure.

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## Code of Conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md).

## License

This project is released under the [Unlicense](LICENSE).
