#include <algorithm>
#include <chrono>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>

#include "led-matrix.h"
#include "graphics.h"
#include "nlohmann/json.hpp"

using rgb_matrix::Canvas;
using rgb_matrix::Color;
using rgb_matrix::Font;
using rgb_matrix::RGBMatrix;
using json = nlohmann::json;

const std::string ScoreboardJsonPath = "/home/admin/scoreboard/scoreboard.json";
const std::string DisplayConfigPath = "/home/admin/scoreboard/display_config.json";
const std::string PixelLogoRoot = "/home/admin/scoreboard/pixel-logos";

struct Pixel {
    int r;
    int g;
    int b;
};

struct PpmImage {
    int width;
    int height;
    std::vector<Pixel> pixels;
};

struct DisplayConfig {
    std::vector<std::string> teams;
    std::vector<std::string> leagues;
    bool liveOnly = false;
    int rotationSeconds = 10;
};

std::string ToUpper(std::string value) {
    std::transform(value.begin(), value.end(), value.begin(), ::toupper);
    return value;
}

bool StartsWith(const std::string& value, const std::string& prefix) {
    return value.rfind(prefix, 0) == 0;
}

bool Contains(const std::vector<std::string>& items, const std::string& value) {
    std::string upperValue = ToUpper(value);

    for (const auto& item : items) {
        if (ToUpper(item) == upperValue) {
            return true;
        }
    }

    return false;
}

std::string SafeString(const json& obj, const std::string& key, const std::string& fallback = "") {
    if (!obj.contains(key) || obj[key].is_null()) {
        return fallback;
    }

    return obj[key].get<std::string>();
}

int SafeInt(const json& obj, const std::string& key, int fallback = 0) {
    if (!obj.contains(key) || obj[key].is_null()) {
        return fallback;
    }

    return obj[key].get<int>();
}

bool SafeBool(const json& obj, const std::string& key, bool fallback = false) {
    if (!obj.contains(key) || obj[key].is_null()) {
        return fallback;
    }

    return obj[key].get<bool>();
}

std::string ToPiPath(std::string path) {
    std::replace(path.begin(), path.end(), '\\', '/');

    if (path.rfind("scoreboard/", 0) == 0) {
        return "/home/admin/" + path;
    }

    return path;
}

std::string ToPpmPath(std::string path) {
    size_t pos = path.find(".png");
    if (pos != std::string::npos) {
        path.replace(pos, 4, ".ppm");
    }

    return path;
}

bool FileExists(const std::string& path) {
    std::ifstream file(path);
    return file.good();
}

DisplayConfig LoadDisplayConfig() {
    DisplayConfig config;

    std::ifstream file(DisplayConfigPath);

    if (!file.is_open()) {
        std::cerr << "display_config.json not found. Showing all games.\n";
        return config;
    }

    try {
        json data;
        file >> data;

        if (data.contains("teams") && data["teams"].is_array()) {
            for (const auto& team : data["teams"]) {
                config.teams.push_back(ToUpper(team.get<std::string>()));
            }
        }

        if (data.contains("leagues") && data["leagues"].is_array()) {
            for (const auto& league : data["leagues"]) {
                config.leagues.push_back(ToUpper(league.get<std::string>()));
            }
        }

        if (data.contains("liveOnly") && !data["liveOnly"].is_null()) {
            config.liveOnly = data["liveOnly"].get<bool>();
        }

        if (data.contains("rotationSeconds") && !data["rotationSeconds"].is_null()) {
            config.rotationSeconds = data["rotationSeconds"].get<int>();
        }

        if (config.rotationSeconds < 3) {
            config.rotationSeconds = 3;
        }
    }
    catch (const std::exception& ex) {
        std::cerr << "Could not parse display_config.json: " << ex.what() << "\n";
    }

    return config;
}

json LoadScoreboardJson() {
    std::ifstream file(ScoreboardJsonPath);

    if (!file.is_open()) {
        throw std::runtime_error("Could not open scoreboard.json");
    }

    json data;
    file >> data;
    return data;
}

bool IsLiveGame(const json& game) {
    if (!game.contains("Status") || game["Status"].is_null()) {
        return false;
    }

    std::string state = SafeString(game["Status"], "State");
    state = ToUpper(state);

    return state == "IN" || state == "LIVE";
}

bool GameMatchesConfig(const json& game, const DisplayConfig& config) {
    std::string league = SafeString(game, "League");
    std::string awayCode = SafeString(game["Away"], "Code");
    std::string homeCode = SafeString(game["Home"], "Code");

    if (!config.leagues.empty() && !Contains(config.leagues, league)) {
        return false;
    }

    if (!config.teams.empty() &&
        !Contains(config.teams, awayCode) &&
        !Contains(config.teams, homeCode)) {
        return false;
    }

    return true;
}

std::vector<json> GetMatchingGames(const json& data, const DisplayConfig& config) {
    std::vector<json> allGames;
    std::vector<json> liveGames;

    if (!data.contains("Games") || !data["Games"].is_array()) {
        return allGames;
    }

    for (const auto& game : data["Games"]) {
        if (!GameMatchesConfig(game, config)) {
            continue;
        }

        allGames.push_back(game);

        if (IsLiveGame(game)) {
            liveGames.push_back(game);
        }
    }

    if (!liveGames.empty()) {
        return liveGames;
    }

    return allGames;
}

PpmImage LoadPpm(const std::string& filePath) {
    std::ifstream file(filePath, std::ios::binary);

    if (!file.is_open()) {
        throw std::runtime_error("Could not open PPM: " + filePath);
    }

    std::string magic;
    file >> magic;

    if (magic != "P6") {
        throw std::runtime_error("Unsupported PPM format");
    }

    int width;
    int height;
    int maxValue;

    file >> width >> height >> maxValue;
    file.get();

    std::vector<Pixel> pixels;
    pixels.reserve(width * height);

    for (int i = 0; i < width * height; i++) {
        unsigned char rgb[3];
        file.read(reinterpret_cast<char*>(rgb), 3);

        pixels.push_back({
            static_cast<int>(rgb[0]),
            static_cast<int>(rgb[1]),
            static_cast<int>(rgb[2])
        });
    }

    return { width, height, pixels };
}

void DrawPpm(Canvas* canvas, const PpmImage& image, int startX, int startY) {
    for (int y = 0; y < image.height; y++) {
        for (int x = 0; x < image.width; x++) {
            const Pixel& pixel = image.pixels[y * image.width + x];

            canvas->SetPixel(
                startX + x,
                startY + y,
                pixel.r,
                pixel.g,
                pixel.b
            );
        }
    }
}

void DrawBase(Canvas* canvas, int cx, int cy, bool occupied, Color fill, Color outline) {
    Color c = occupied ? fill : outline;

    canvas->SetPixel(cx, cy - 2, c.r, c.g, c.b);
    canvas->SetPixel(cx - 1, cy - 1, c.r, c.g, c.b);
    canvas->SetPixel(cx, cy - 1, c.r, c.g, c.b);
    canvas->SetPixel(cx + 1, cy - 1, c.r, c.g, c.b);
    canvas->SetPixel(cx - 2, cy, c.r, c.g, c.b);
    canvas->SetPixel(cx - 1, cy, c.r, c.g, c.b);
    canvas->SetPixel(cx, cy, c.r, c.g, c.b);
    canvas->SetPixel(cx + 1, cy, c.r, c.g, c.b);
    canvas->SetPixel(cx + 2, cy, c.r, c.g, c.b);
    canvas->SetPixel(cx - 1, cy + 1, c.r, c.g, c.b);
    canvas->SetPixel(cx, cy + 1, c.r, c.g, c.b);
    canvas->SetPixel(cx + 1, cy + 1, c.r, c.g, c.b);
    canvas->SetPixel(cx, cy + 2, c.r, c.g, c.b);
}

std::string GetLogoPath(const json& team, const std::string& league) {
    std::string code = SafeString(team, "Code");
    std::string lowerLeague = ToUpper(league);
    std::transform(lowerLeague.begin(), lowerLeague.end(), lowerLeague.begin(), ::tolower);

    std::string pixelLogo = PixelLogoRoot + "/" + lowerLeague + "/" + ToUpper(code) + ".ppm";

    if (FileExists(pixelLogo)) {
        return pixelLogo;
    }

    std::string processedLogo = SafeString(team, "ProcessedLogoFile");
    processedLogo = ToPpmPath(ToPiPath(processedLogo));

size_t pos = processedLogo.find("_20x20");
if (pos != std::string::npos) {
    processedLogo.replace(pos, 6, "_16x16");
}

processedLogo = ToPpmPath(ToPiPath(processedLogo));

    return processedLogo;
}

void DrawLogoOrFallback(
    Canvas* canvas,
    const json& team,
    const std::string& league,
    int x,
    int y,
    Font& font,
    Color textColor
) {
    std::string code = SafeString(team, "Code", "???");
    std::string logoPath = GetLogoPath(team, league);

    try {
        if (!logoPath.empty()) {
            PpmImage image = LoadPpm(logoPath);
            DrawPpm(canvas, image, x, y);
            return;
        }
    }
    catch (const std::exception& ex) {
        std::cerr << "Logo error for " << code << ": " << ex.what() << "\n";
    }

    rgb_matrix::DrawText(canvas, font, x, y + 12, textColor, nullptr, code.c_str());
}

void DrawGame(Canvas* canvas, Font& font, const json& game) {
    canvas->Clear();

    std::string league = SafeString(game, "League");
    std::string awayCode = SafeString(game["Away"], "Code", "AWAY");
    std::string homeCode = SafeString(game["Home"], "Code", "HOME");

    int awayScore = SafeInt(game["Away"], "Score");
    int homeScore = SafeInt(game["Home"], "Score");

    std::string period = SafeString(game["Status"], "Period");
    std::string clock = SafeString(game["Status"], "Clock");

    bool isMlb = ToUpper(league) == "MLB";
    bool awayBatting = isMlb && StartsWith(period, "Top");
    bool homeBatting = isMlb && StartsWith(period, "Bot");

    Color white(255, 255, 255);
    Color yellow(255, 210, 0);
    Color dim(55, 55, 55);

    DrawLogoOrFallback(canvas, game["Away"], league, 0, 0, font, white);
    DrawLogoOrFallback(canvas, game["Home"], league, 0, 14, font, white);

    std::string awayText = (awayBatting ? "*" : "") + awayCode + " " + std::to_string(awayScore);
    std::string homeText = (homeBatting ? "*" : "") + homeCode + " " + std::to_string(homeScore);

    rgb_matrix::DrawText(canvas, font, 20, 12, white, nullptr, awayText.c_str());
    rgb_matrix::DrawText(canvas, font, 20, 29, white, nullptr, homeText.c_str());

    if (isMlb) {
        int outs = SafeInt(game["Status"], "Outs");

        bool runnerOnFirst = SafeBool(game["Status"], "RunnerOnFirst");
        bool runnerOnSecond = SafeBool(game["Status"], "RunnerOnSecond");
        bool runnerOnThird = SafeBool(game["Status"], "RunnerOnThird");

        rgb_matrix::DrawText(canvas, font, 78, 10, yellow, nullptr, period.c_str());

        if (ToUpper(period) == "PRE") {
            rgb_matrix::DrawText(canvas, font, 98, 24, white, nullptr, "MLB");
        }
        else {
            std::string outsText = std::to_string(outs) + " OUT";
            if (outs != 1) {
                outsText += "S";
            }
	    Color red(255,40,40);
            DrawBase(canvas, 72, 10, runnerOnSecond, red, dim);
            DrawBase(canvas, 63, 17, runnerOnThird, red, dim);
            DrawBase(canvas, 81, 17, runnerOnFirst, red, dim);
            DrawBase(canvas, 72, 24, false, white, dim);

            rgb_matrix::DrawText(canvas, font, 94, 24, white, nullptr, outsText.c_str());
        }
    }
    else {
        rgb_matrix::DrawText(canvas, font, 86, 10, yellow, nullptr, period.c_str());

        if (!clock.empty() && clock != "0:00") {
            rgb_matrix::DrawText(canvas, font, 78, 24, white, nullptr, clock.c_str());
        }
        else {
            rgb_matrix::DrawText(canvas, font, 86, 24, white, nullptr, league.c_str());
        }
    }

    std::cout << "Displaying "
              << league << ": "
              << awayCode << " " << awayScore
              << " - "
              << homeCode << " " << homeScore
              << " "
              << period << "\n";
}

void DrawMessage(Canvas* canvas, Font& font, const std::string& line1, const std::string& line2) {
    canvas->Clear();

    Color white(255, 255, 255);
    Color yellow(255, 210, 0);

    rgb_matrix::DrawText(canvas, font, 2, 12, yellow, nullptr, line1.c_str());
    rgb_matrix::DrawText(canvas, font, 2, 27, white, nullptr, line2.c_str());
}

int main() {
    RGBMatrix::Options options;
    options.rows = 32;
    options.cols = 64;
    options.chain_length = 2;
    options.hardware_mapping = "regular";
    options.brightness = 80;

    rgb_matrix::RuntimeOptions runtime;
    runtime.drop_privileges = 0;

    RGBMatrix* matrix = RGBMatrix::CreateFromOptions(options, runtime);

    if (matrix == nullptr) {
        std::cerr << "Could not create RGB matrix\n";
        return 1;
    }

    Canvas* canvas = matrix;

    Font font;
    if (!font.LoadFont("/home/admin/rpi-rgb-led-matrix/fonts/7x13.bdf")) {
        std::cerr << "Could not load font\n";
        delete matrix;
        return 1;
    }

    int gameIndex = 0;

    while (true) {
        try {
            DisplayConfig config = LoadDisplayConfig();
            json data = LoadScoreboardJson();
            std::vector<json> games = GetMatchingGames(data, config);

            if (games.empty()) {
                DrawMessage(canvas, font, "NO GAMES", "MATCH CONFIG");
                std::this_thread::sleep_for(std::chrono::seconds(5));
                continue;
            }

            if (gameIndex >= static_cast<int>(games.size())) {
                gameIndex = 0;
            }

            DrawGame(canvas, font, games[gameIndex]);

            gameIndex++;
            if (gameIndex >= static_cast<int>(games.size())) {
                gameIndex = 0;
            }

            std::this_thread::sleep_for(std::chrono::seconds(config.rotationSeconds));
        }
        catch (const std::exception& ex) {
            std::cerr << "Renderer error: " << ex.what() << "\n";
            DrawMessage(canvas, font, "RENDER ERR", "CHECK LOG");
            std::this_thread::sleep_for(std::chrono::seconds(5));
        }
    }

    delete matrix;
    return 0;
}
