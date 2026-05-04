using LedScoreboard.Interfaces;
using LedScoreboard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LedScoreboard.Services
{
    public class ScoreboardService
    {
        private readonly ISportsDataProvider _dataProvider;
        private readonly ScoreboardConfig _config;

        public ScoreboardService(ISportsDataProvider dataProvider, ScoreboardConfig config)
        {
            _dataProvider = dataProvider;
            _config = config;
        }


        public async Task<ScoreboardUpdate> BuildUpdateAsync()
        {
            var games = await _dataProvider.GetGamesAsync();

            if (_config.SelectedGameIds.Any())
            {
                games = games
                    .Where(g => _config.SelectedGameIds.Contains(g.GameId)).ToList();
            }

            return new ScoreboardUpdate
            {
                DisplayMode = _config.DisplayMode,
                Games = games,
                GeneratedAtUtc = DateTime.UtcNow,
            };
        }
    }
}
