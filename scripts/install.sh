#!/bin/bash

set -e

echo "======================================="
echo " LED Scoreboard Installer"
echo "======================================="

REPO_DIR="/home/admin/Led_Scoreboard"

echo ""
echo "Updating system packages..."
sudo apt update
sudo apt install -y \
    git \
    g++ \
    make \
    cmake \
    python3 \
    python3-pip \
    python3-pil \
    libgraphicsmagick++-dev \
    libwebp-dev \
    libpng-dev \
    libjpeg-dev \
    fonts-dejavu-core \
    jq

echo ""
echo "Installing .NET 8..."

if [ ! -d "/home/admin/.dotnet" ]; then
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
fi

export DOTNET_ROOT=/home/admin/.dotnet
export PATH=$PATH:/home/admin/.dotnet

echo ""
echo "Cloning RGB matrix library..."

if [ ! -d "/home/admin/rpi-rgb-led-matrix" ]; then
    git clone https://github.com/hzeller/rpi-rgb-led-matrix.git /home/admin/rpi-rgb-led-matrix
fi

echo ""
echo "Building RGB matrix library..."

cd /home/admin/rpi-rgb-led-matrix
make -j$(nproc)

echo ""
echo "Creating scoreboard directories..."

mkdir -p /home/admin/scoreboard
mkdir -p /home/admin/scoreboard/newlogos
mkdir -p /home/admin/scoreboard/pixel-logos

echo ""
echo "Building C# backend..."

cd "$REPO_DIR"

export PATH=$PATH:/home/admin/.dotnet

/home/admin/.dotnet/dotnet restore
/home/admin/.dotnet/dotnet build

echo ""
echo "Compiling scoreboard renderer..."

g++ renderer/scoreboard_renderer.cpp \
-I/home/admin/rpi-rgb-led-matrix/include \
-L/home/admin/rpi-rgb-led-matrix/lib \
-lrgbmatrix \
-lrt \
-lm \
-lpthread \
-o /home/admin/scoreboard_renderer

echo ""
echo "Creating backend launcher..."

cat << 'EOF' > /home/admin/start_scoreboard_backend.sh
#!/bin/bash

export DOTNET_ROOT=/home/admin/.dotnet
export PATH=$PATH:/home/admin/.dotnet

cd /home/admin/Led_Scoreboard

exec /home/admin/.dotnet/dotnet run
EOF

chmod +x /home/admin/start_scoreboard_backend.sh

echo ""
echo "Creating renderer launcher..."

cat << 'EOF' > /home/admin/start_scoreboard_renderer.sh
#!/bin/bash

exec /home/admin/scoreboard_renderer
EOF

chmod +x /home/admin/start_scoreboard_renderer.sh

echo ""
echo "Installing backend systemd service..."

sudo tee /etc/systemd/system/scoreboard-backend.service > /dev/null << 'EOF'
[Unit]
Description=LED Scoreboard Backend
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=admin
WorkingDirectory=/home/admin/Led_Scoreboard
ExecStart=/home/admin/start_scoreboard_backend.sh
Restart=always
RestartSec=5

Environment=DOTNET_ROOT=/home/admin/.dotnet
Environment=PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/home/admin/.dotnet

[Install]
WantedBy=multi-user.target
EOF

echo ""
echo "Installing renderer systemd service..."

sudo tee /etc/systemd/system/scoreboard-renderer.service > /dev/null << 'EOF'
[Unit]
Description=LED Scoreboard Renderer
After=network-online.target scoreboard-backend.service
Wants=network-online.target

[Service]
Type=simple
User=root
WorkingDirectory=/home/admin
ExecStart=/home/admin/start_scoreboard_renderer.sh
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

echo ""
echo "Reloading systemd..."

sudo systemctl daemon-reload

echo ""
echo "Enabling services..."

sudo systemctl enable scoreboard-backend.service
sudo systemctl enable scoreboard-renderer.service

echo ""
echo "Starting services..."

sudo systemctl restart scoreboard-backend.service
sudo systemctl restart scoreboard-renderer.service

echo ""
echo "======================================="
echo " INSTALL COMPLETE"
echo "======================================="
echo ""
echo "Backend status:"
sudo systemctl --no-pager --full status scoreboard-backend.service || true

echo ""
echo "Renderer status:"
sudo systemctl --no-pager --full status scoreboard-renderer.service || true
