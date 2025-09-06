#!/usr/bin/env bash
set -euo pipefail

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"

curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 9.0
bash /tmp/dotnet-install.sh --channel 8.0
rm /tmp/dotnet-install.sh
ln -sfn "$DOTNET_ROOT/dotnet" /usr/local/bin/dotnet
