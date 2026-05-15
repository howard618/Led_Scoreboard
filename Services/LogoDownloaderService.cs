using LedScoreboard.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace LedScoreboard.Services
{
    public class LogoDownloaderService
    {

        private readonly HttpClient _httpClient;
        private readonly ILogger<LogoDownloaderService> _logger;
        private readonly ScoreboardConfig _config;


        public LogoDownloaderService(HttpClient httpClient, ILogger<LogoDownloaderService> logger, ScoreboardConfig config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
        }

        public async Task DownloadLogosAsync(List<GameState> games)
        {
            foreach (GameState game in games)
            {
                await DownloadTeamLogoAsync(game.League, game.Home);
                await DownloadTeamLogoAsync(game.League, game.Away);
            }
        }

        private async Task DownloadTeamLogoAsync(string league, TeamState team)
        {
            if (string.IsNullOrWhiteSpace(team.LogoUrl))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(team.Code))
            {
                return;
            }

            string folderPath = Path.Combine(_config.DataFolder, _config.LogoFolder, league.ToLower());

            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, $"{team.Code}.png");

            team.LogoFile = filePath;

            if (File.Exists(filePath))
            {
                return;
            }
            try
            {
                byte[] imageBytes = await _httpClient.GetByteArrayAsync(team.LogoUrl);

                await File.WriteAllBytesAsync(filePath, imageBytes);

                _logger.LogInformation("Downloaded logo for {League} {TeamCode} to {FilePath}", league, team.Code, filePath);
            }

            catch(Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not download logo for {League} {TeamCode}",
                    league,
                    team.Code
                );
            }
        }
    }
}
