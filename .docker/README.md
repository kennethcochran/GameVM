# GameVM MAME Container

This directory contains the Docker configuration for building a Windows container with MAME pre-installed for GameVM CI/CD.

## 🐳 Container Overview

The `kennethcochran/gamevm-mame:latest` container includes:
- Windows Server 2022 base image
- .NET SDK 8.0
- MAME 0.284 pre-installed at `C:\mame\mame.exe`
- Optimized for GitHub Actions CI/CD

## 🚀 Usage

### GitHub Actions
```yaml
jobs:
  build:
    runs-on: windows-latest
    container:
      image: kennethcochran/gamevm-mame:latest
      options: --platform windows/amd64
    
    env:
      GAMEVM_WHICH_PATH: C:\mame\mame.exe
      GAMEVM_FLATPAK_PATH: ""
```

### Local Development
```bash
# Build locally
docker build -f .docker/Dockerfile.mame -t gamevm-mame:latest .

# Run with volume mount
docker run --rm -v $(pwd):/workspace gamevm-mame:latest powershell
```

## 📦 Container Benefits

1. **No IP Bans**: MAME downloaded once during container build, not during CI
2. **Faster Builds**: No download time during CI runs
3. **Reliability**: Consistent environment across all builds
4. **Version Control**: Specific MAME version (0.284) locked in container
5. **Security**: Scanned and cached container image

## 🔧 Container Build Process

1. **Base Image**: Windows Server 2022 with .NET SDK
2. **MAME Installation**: Downloads and extracts MAME 0.284
3. **Path Configuration**: Adds MAME to PATH and sets environment variables
4. **Verification**: Tests MAME installation
5. **Optimization**: Cleans up temporary files

## 📋 Environment Variables

- `GAMEVM_WHICH_PATH=C:\mame\mame.exe` - Path to MAME executable
- `GAMEVM_FLATPAK_PATH=""` - Empty (Windows container)

## 🔄 Automated Updates

The container is automatically rebuilt:
- Weekly on Sunday at 2 AM UTC
- When Dockerfile changes
- Manual trigger via workflow_dispatch

## 🛡️ Security

- Container built from official Microsoft base images
- MAME downloaded from official GitHub releases
- SBOM generated for each build
- Image scanned for vulnerabilities

## 🐛 Troubleshooting

### Container Not Found
If the container doesn't exist, trigger the [container build workflow](../actions/workflows/container-build.yml) manually.

### MAME Not Working
Check that `C:\mame\mame.exe` exists in the container:
```powershell
Test-Path "C:\mame\mame.exe"
```

### Permission Issues
Ensure the container has proper permissions to access workspace files.

## 📞 Support

For issues with the container:
1. Check the container build logs
2. Verify MAME installation in container
3. Test locally with Docker commands
4. Check GitHub Actions runner compatibility
