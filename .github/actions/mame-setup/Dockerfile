FROM ubuntu:22.04

# Avoid interactive prompts
ENV DEBIAN_FRONTEND=noninteractive

# Install dependencies
RUN apt-get update && apt-get install -y \
    wget \
    curl \
    tar \
    jq \
    && rm -rf /var/lib/apt/lists/*

# Set up arguments
ARG MAME_VERSION=mame0284
ARG INSTALL_PATH=/opt/mame

# Create installation directory
RUN mkdir -p ${INSTALL_PATH}

# Download and install MAME using the same API as original code
RUN echo "Installing MAME ${MAME_VERSION}..." && \
    # Get latest release info from GitHub API (same as original)
    LATEST_RELEASE_URL="https://api.github.com/repos/mamedev/mame/releases/latest" && \
    echo "Fetching release info from: ${LATEST_RELEASE_URL}" && \
    RELEASE_JSON=$(wget -qO- "${LATEST_RELEASE_URL}") && \
    echo "Finding Linux asset..." && \
    # Find Linux asset (add Linux support to original logic)
    MAME_URL=$(echo "${RELEASE_JSON}" | jq -r '.assets[] | select(.name | contains("linux_x64") and endswith(".tar.gz")) | .browser_download_url' | head -1) && \
    if [ -z "${MAME_URL}" ] || [ "${MAME_URL}" = "null" ]; then \
        echo "ERROR: No Linux MAME asset found in release"; \
        echo "Available assets:"; \
        echo "${RELEASE_JSON}" | jq -r '.assets[].name'; \
        exit 1; \
    fi && \
    echo "Downloading from: ${MAME_URL}" && \
    wget --timeout=300 --tries=3 "${MAME_URL}" -O /tmp/mame.tar.gz && \
    echo "Extracting MAME..." && \
    tar -xzf /tmp/mame.tar.gz -C ${INSTALL_PATH} --strip-components=1 && \
    rm /tmp/mame.tar.gz && \
    chmod +x ${INSTALL_PATH}/mame && \
    echo "MAME installed at ${INSTALL_PATH}/mame"

# Add to PATH
ENV PATH="${INSTALL_PATH}:${PATH}"

# Set working directory
WORKDIR /github/workspace

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Set entrypoint
ENTRYPOINT ["/entrypoint.sh"]
