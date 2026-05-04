using LedScoreboard.Models;
using LedScoreboard.Services;

namespace LedScoreboard
{
 public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ScoreboardService _scoreboardService;
        private readonly ScoreboardConfig _config;


        public Worker(ILogger<Worker> logger, ScoreboardService scoreboardService, ScoreboardConfig config)
        {
            _logger = logger;
            _scoreboardService = scoreboardService;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scoreboard service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var update = await _scoreboardService.BuildUpdateAsync();

                    foreach(var game in update.Games)
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
