#!/usr/bin/env bash
set -euo pipefail

# update existing .NET SDK installations
INSTALL_DIR="$HOME/.dotnet"
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir "$INSTALL_DIR" --no-cdn
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir "$INSTALL_DIR" --no-cdn
"$INSTALL_DIR/dotnet" workload update || true

echo "Maintenance completed."
