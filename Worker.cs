using LedScoreboard.Models;
using LedScoreboard.Services;

namespace LedScoreboard
{
 public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ScoreboardService _scoreboardService;
        private readonly ScoreboardConfig _config;
        private readonly LogoDownloaderService _logoDownloaderService;
        private readonly LogoProcessorService _logoProcessorService;
        private readonly ScoreboardFileWriterService _scoreboardFileWriterService;


        public Worker(ILogger<Worker> logger, ScoreboardService scoreboardService, ScoreboardConfig config,
            LogoDownloaderService logoDownloader, LogoProcessorService logoProcessorService, ScoreboardFileWriterService scoreboardFileWriterService)
        {
            _logger = logger;
            _scoreboardService = scoreboardService;
            _config = config;
            _logoDownloaderService = logoDownloader;
            _logoProcessorService = logoProcessorService;
            _scoreboardFileWriterService = scoreboardFileWriterService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scoreboard service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var update = await _scoreboardService.BuildUpdateAsync();
                    await _logoDownloaderService.DownloadLogosAsync(update.Games);
                    await _logoProcessorService.ProcessLogosAsync(update.Games);
                    await _scoreboardFileWriterService.WriteAsync(update);

                    foreach (var game in update.Games)
                    {
                        string statusText = string.IsNullOrWhiteSpace(game.Status.Clock)
                         ? game.Status.Period
                         : $"{game.Status.Period} {game.Status.Clock}";

                        _logger.LogInformation(
                            "{League}: {Away} {AwayScore} - {Home} {HomeScore} ({Status})",
                            game.League,
                            game.Away.Code,
                            game.Away.Score,
                            game.Home.Code,
                            game.Home.Score,
                            statusText
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error building scoreboard update");
                }
                await Task.Delay(TimeSpan.FromSeconds(_config.RefreshSeconds), stoppingToken);
            }


        }
    }
}
