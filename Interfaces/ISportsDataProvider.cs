using LedScoreboard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LedScoreboard.Interfaces
{
    public interface ISportsDataProvider
    {
        Task<List<GameState>> GetGamesAsync();  //creates a list of games from API calls in 
    }
}
