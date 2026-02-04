FROM ubuntu:22.04

# Avoid interactive prompts
ENV DEBIAN_FRONTEND=noninteractive

# Install dependencies
RUN apt-get update && apt-get install -y \
    wget \
    curl \
    tar \
    && rm -rf /var/lib/apt/lists/*

# Set up arguments
ARG MAME_VERSION=mame0284
ARG INSTALL_PATH=/opt/mame

# Create installation directory
RUN mkdir -p ${INSTALL_PATH}

# Download and install MAME
RUN echo "Installing MAME ${MAME_VERSION}..." && \
    MAME_URL="https://github.com/mamedev/mame/releases/download/${MAME_VERSION}/${MAME_VERSION}_linux_x64.tar.gz" && \
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
