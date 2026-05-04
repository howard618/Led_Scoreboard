using LedScoreboard.Interfaces;
using LedScoreboard.Models;
using System;
using System.Collections.Generic;
using System.Text;

// This calss is strictly dummy data for testing pre API calls for real time data. Only implement for debugging with dummy data. 

namespace LedScoreboard.Services
{
    public class FakeSportsDataProvider : ISportsDataProvider
    {
        public Task<List<GameState>> GetGamesAsync()
        {
            List<GameState> games = new();
            games.Add(new GameState
            {
                League = "NFL",
                GameId = "nfl-det-gb",

                Home = new TeamState
                {
                    Code = "DET",
                    Name = "Detroit Lions",
                    Score = 24,
                },

                Away = new TeamState
                {
                    Code = "Gb",
                    Name = "Green Bay Packers",
                    Score = 17,
                },

                Status = new GameStatus
                {
                    Period = "Q3",
                    Clock = "08:22",
                    State = "Live",
                },

                LastUpdatedUtc = DateTime.UtcNow
            });

            return Task.FromResult(games);
        }
    }
}
