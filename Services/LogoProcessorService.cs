using System;
using System.Collections.Generic;
using System.Text;
using LedScoreboard.Models;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;


namespace LedScoreboard.Services
{
    public class LogoProcessorService
    {
        private readonly ILogger<LogoProcessorService> _logger;
        private readonly ScoreboardConfig _config;
        private const int LogoSize = 16;

        public LogoProcessorService(ILogger<LogoProcessorService> logger, ScoreboardConfig config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task ProcessLogosAsync(List<GameState> games)
        {
            _logger.LogInformation("Starting logo processing...");
            foreach (GameState game in games)
            {
                await ProcessTeamLogoAsync(game.League, game.Home);
                await ProcessTeamLogoAsync(game.League, game.Away);

            }
        }

        private async Task ProcessTeamLogoAsync(string league, TeamState team)
        {
            if (string.IsNullOrWhiteSpace(team.LogoFile))
            {
                return;
            }
            if (!File.Exists(team.LogoFile))
            {
                return;
            }

            string processedFolder = Path.Combine(_config.DataFolder, _config.LogoFolder,league.ToLower(),"processed");

            Directory.CreateDirectory(processedFolder);

            string processedFile = Path.Combine(processedFolder, $"{team.Code}_{LogoSize}x{LogoSize}.png");

            team.ProcessedLogoFile = processedFile;
            _logger.LogInformation("Processing logo file: {FilePath}",team.LogoFile);

            if (File.Exists(processedFile))
            {
                return;
            }


            try
            {
                using Image image = await Image.LoadAsync(team.LogoFile);

                image.Mutate(x =>
                {
                    x.Resize(new ResizeOptions
                    {
                        Size = new Size(LogoSize, LogoSize),
                        Mode = ResizeMode.Pad


                    });
                });


                await image.SaveAsPngAsync(processedFile);

                _logger.LogInformation(
                    "Processed logo for {League} {TeamCode} to {FilePath}",
                    league,
                    team.Code,
                    processedFile
                );
            }

            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Could not process logo for {League} {TeamCode}", league, team.Code);

            }
        }
    }
}
