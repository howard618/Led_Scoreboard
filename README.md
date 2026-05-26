# LED Scoreboard

A live multi-sport Raspberry Pi LED scoreboard using:

- Raspberry Pi
- HUB75 RGB LED matrix panels
- C# / .NET 8 backend
- C++ renderer
- ESPN scoreboard APIs
- hzeller/rpi-rgb-led-matrix

Supported leagues:

- NFL
- MLB
- NHL

---

# Quick Installation

## Hardware Needed

- Raspberry Pi 4 or newer
- Two HUB75 RGB LED matrix panels
- RGB Matrix HAT
- 5V, 15Amp power supply
- Raspberry Pi OS
- Internet connection

---

## Clone the Repo

git clone https://github.com/howard618/Led_Scoreboard.git
cd Led_Scoreboard

---

## Run Installer

chmod +x scripts/install.sh
./scripts/install.sh

The installer automatically:

- installs dependencies
- installs .NET 8
- installs rpi-rgb-led-matrix
- creates runtime folders
- compiles the renderer
- creates systemd services
- starts the scoreboard automatically

After installation, the scoreboard will automatically start every time the Raspberry Pi powers on. If threre are no changes powercycle the Raspberry Pi. 

---

# Managing Services

Check backend:

sudo systemctl status scoreboard-backend.service

Check renderer:

sudo systemctl status scoreboard-renderer.service

Restart backend:

sudo systemctl restart scoreboard-backend.service

Restart renderer:

sudo systemctl restart scoreboard-renderer.service

---

# Project Overview

The project is split into two systems:

C# Backend
↓
scoreboard.json
↓
C++ LED Renderer

## Backend (.NET 8)

The backend:

- polls ESPN APIs
- parses live game data
- downloads/processes logos
- writes normalized JSON data to:

/home/admin/scoreboard/scoreboard.json

Main backend files:

- Program.cs → application setup/configuration
- Worker.cs → continuous update loop
- EspnSportsDataProvider.cs → ESPN parsing logic

---

## Renderer (C++)

The renderer:

- reads scoreboard.json
- reads display_config.json
- filters games
- renders graphics to the LED matrix

Renderer source:

renderer/scoreboard_renderer.cpp

Compiled output:

/home/admin/scoreboard_renderer

---

# Display Configuration

Runtime config file:

/home/admin/scoreboard/display_config.json

Example:

{
  "mode": "teams",
  "teams": [],
  "leagues": ["MLB"],
  "liveOnly": false,
  "rotationSeconds": 10
}

---

# MLB Features

- Top/Bottom inning display
- Outs display
- Base runner indicators
- Batting team indicator

---

# Recompiling the Renderer

If you modify:

renderer/scoreboard_renderer.cpp

recompile with:

g++ renderer/scoreboard_renderer.cpp \
-I/home/admin/rpi-rgb-led-matrix/include \
-L/home/admin/rpi-rgb-led-matrix/lib \
-lrgbmatrix \
-lrt \
-lm \
-lpthread \
-o /home/admin/scoreboard_renderer

Then restart:

sudo systemctl restart scoreboard-renderer.service

---

# Updating From GitHub

cd /home/admin/Led_Scoreboard
git pull

Then restart services if needed.

---

# Future Goals

- phone/web control UI
- ticker mode
- WiFi setup mode
- OBS/browser output
- additional sports support
