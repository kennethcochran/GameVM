#!/bin/bash

# MAME Setup Action Entry Point
set -e

MAME_VERSION="${1:-mame0284}"
INSTALL_PATH="${2:-/opt/mame}"

echo "Setting up MAME ${MAME_VERSION} at ${INSTALL_PATH}"

# Verify MAME installation
if [ -f "${INSTALL_PATH}/mame" ]; then
    echo "✅ MAME is available at ${INSTALL_PATH}/mame"
    "${INSTALL_PATH}/mame" -version | head -3
    
    # Set output for GitHub Actions
    echo "mame-path=${INSTALL_PATH}/mame" >> $GITHUB_OUTPUT
    
    # Export environment variables
    echo "GAMEVM_WHICH_PATH=${INSTALL_PATH}/mame" >> $GITHUB_ENV
    echo "GAMEVM_FLATPAK_PATH=" >> $GITHUB_ENV
    
    echo "✅ MAME setup completed successfully"
else
    echo "❌ MAME installation failed"
    exit 1
fi

# Keep container running if needed
exec "$@"
