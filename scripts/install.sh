#!/bin/bash
set -e

echo "Installing LED Scoreboard..."

sudo apt update
sudo apt install -y git curl wget imagemagick g++ make

echo "Creating runtime folders..."
mkdir -p /home/admin/scoreboard
mkdir -p /home/admin/scoreboard/pixel-logos/nfl
mkdir -p /home/admin/scoreboard/pixel-logos/mlb
mkdir -p /home/admin/scoreboard/pixel-logos/nhl

echo "Installing .NET 8 if needed..."
if ! command -v dotnet >/dev/null 2>&1; then
    cd /home/admin
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x dotnet-install.sh
    ./dotnet-install.sh --channel 8.0

    echo 'export DOTNET_ROOT=$HOME/.dotnet' >> /home/admin/.bashrc
    echo 'export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools' >> /home/admin/.bashrc

    export DOTNET_ROOT=/home/admin/.dotnet
    export PATH=$PATH:/home/admin/.dotnet:/home/admin/.dotnet/tools
fi

echo "Restoring backend..."
cd /home/admin/Led_Scoreboard
dotnet restore

echo "Compiling renderer..."
g++ renderer/scoreboard_renderer.cpp \
-I/home/admin/rpi-rgb-led-matrix/include \
-L/home/admin/rpi-rgb-led-matrix/lib \
-lrgbmatrix \
-lrt \
-lm \
-lpthread \
-o /home/admin/scoreboard_renderer

echo "Creating backend start script..."
cat > /home/admin/start_scoreboard_backend.sh << 'EOF'
#!/bin/bash
cd /home/admin/Led_Scoreboard
dotnet run
EOF
chmod +x /home/admin/start_scoreboard_backend.sh

echo "Creating renderer start script..."
cat > /home/admin/start_scoreboard_renderer.sh << 'EOF'
#!/bin/bash
/home/admin/scoreboard_renderer
EOF
chmod +x /home/admin/start_scoreboard_renderer.sh

echo "Creating backend systemd service..."
sudo tee /etc/systemd/system/scoreboard-backend.service > /dev/null << 'EOF'
[Unit]
Description=LED Scoreboard Backend
After=network.target

[Service]
Type=simple
User=admin
WorkingDirectory=/home/admin/Led_Scoreboard
ExecStart=/home/admin/start_scoreboard_backend.sh
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

echo "Creating renderer systemd service..."
sudo tee /etc/systemd/system/scoreboard-renderer.service > /dev/null << 'EOF'
[Unit]
Description=LED Scoreboard Renderer
After=network.target scoreboard-backend.service
Requires=scoreboard-backend.service

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

echo "Enabling services..."
sudo systemctl daemon-reload
sudo systemctl enable scoreboard-backend.service
sudo systemctl enable scoreboard-renderer.service

echo "Starting services..."
sudo systemctl restart scoreboard-backend.service
sudo systemctl restart scoreboard-renderer.service

echo "Install complete."
echo "Check services with:"
echo "sudo systemctl status scoreboard-backend.service"
echo "sudo systemctl status scoreboard-renderer.service"
