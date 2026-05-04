using System;
using System.Collections.Generic;
using System.Text;

namespace LedScoreboard.Models
{
    public class ScoreboardUpdate
    {
        public string DisplayMode { get; set; } = "rotate";  //Tells the raspberry pi how to display (rotate, single, ticker)
        public List<GameState> Games { get; set; } // allows the user to select available games to display 
        public DateTime GeneratedAtUtc { get; set; } // helps debug timing and sync issues
    }
}
