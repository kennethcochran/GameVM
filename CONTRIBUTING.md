# Contributing to GameVM

Thank you for your interest in contributing to GameVM! We appreciate your time and effort. This guide will help you get started with contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Environment](#development-environment)
- [Building the Project](#building-the-project)
- [Making Changes](#making-changes)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Reporting Issues](#reporting-issues)
- [License](#license)

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report any unacceptable behavior to the project maintainers.

## Getting Started

1. **Fork** the repository on GitHub
2. **Clone** your fork locally
   ```bash
   git clone https://github.com/your-username/GameVM.git
   cd GameVM
   ```
3. Add the upstream repository as a remote
   ```bash
   git remote add upstream https://github.com/kennethcochran/GameVM.git
   ```

## Development Environment

> **JDK Requirement Note**: The JDK is only required because ANTLR (used for parsing) is implemented in Java. The JDK is only used during the build process to generate parser code - the runtime does not require Java.

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [JDK 21 or later](https://adoptium.net/) (required for ANTLR tooling only)
- [Git](https://git-scm.com/)
- A code editor (Visual Studio, VS Code, JetBrains Rider, etc.)

## Building the Project

1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

## Making Changes

1. Create a new branch for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b bugfix/issue-number-description
   ```

2. Make your changes following the project's coding standards

3. Run tests to ensure nothing is broken:
   ```bash
   dotnet test
   ```

4. Commit your changes with a clear and descriptive commit message:
   ```bash
   git commit -m "Add feature X"
   ```

5. Push your changes to your fork:
   ```bash
   git push origin your-branch-name
   ```

## Submitting a Pull Request

1. Go to the [GameVM repository](https://github.com/kennethcochran/GameVM)
2. Click on "Pull Request" and then "New Pull Request"
3. Click on "compare across forks"
4. Select your fork and branch
5. Fill in the PR template with all relevant details
6. Submit the pull request

## Reporting Issues

Before creating a new issue, please check if a similar issue already exists in the [issue tracker](https://github.com/kennethcochran/GameVM/issues).

When creating a new issue, please include:
- A clear and descriptive title
- Steps to reproduce the issue
- Expected vs. actual behavior
- Any relevant error messages or logs
- Your environment details (OS, .NET version, etc.)

## License

By contributing to GameVM, you agree that your contributions will be licensed under the [Unlicense](LICENSE).
