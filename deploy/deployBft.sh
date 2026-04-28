#!/bin/bash
set -e

PROJECT_DIR=~/projects/bft
PUBLISH_DIR=~/published-apps/bft
CSPROJ_DIR="$PROJECT_DIR/BrownFlannelTavernStore"
SERVICE=BrownFlannelTavern

echo "==> Stopping $SERVICE"
sudo systemctl stop "$SERVICE" || true

echo "==> Pulling latest from git"
cd "$PROJECT_DIR"
git pull

echo "==> Publishing to $PUBLISH_DIR"
cd "$CSPROJ_DIR"
dotnet publish -c Release -o "$PUBLISH_DIR"

echo "==> Restoring appsettings.Production.json (if present)"
# Production settings live outside the repo — keep a copy at ~/secrets/bft/
if [ -f ~/secrets/bft/appsettings.Production.json ]; then
    cp ~/secrets/bft/appsettings.Production.json "$PUBLISH_DIR/appsettings.Production.json"
    echo "    copied production settings"
else
    echo "    WARNING: ~/secrets/bft/appsettings.Production.json not found — service will fail to start"
fi

echo "==> Starting $SERVICE"
sudo systemctl start "$SERVICE"

echo "==> Status:"
sudo systemctl status "$SERVICE" --no-pager -n 20
