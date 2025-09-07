#!/usr/bin/env bash
set -euo pipefail

# install .NET SDK 9.x and 8.x into user directory
INSTALL_DIR="$HOME/.dotnet"
mkdir -p "$INSTALL_DIR"
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 --install-dir "$INSTALL_DIR"
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir "$INSTALL_DIR"

cat <<'EOP' >> "$HOME/.bashrc"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="\$DOTNET_ROOT:\$PATH"
EOP

echo "Done. Restart shell to use dotnet."
