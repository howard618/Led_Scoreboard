using System;
using System.Collections.Generic;
using System.Text;

namespace LedScoreboard.Models
{
    public class GameState
    {
        public string League { get; set; } // idenifies the sport >> NBA, MLB, NHL 
        public string GameId { get; set; } // for tracking updates 
        public TeamState Home { get; set; } = new(); // home team data 
        public TeamState Away { get; set; } = new(); // away team data 
        public GameStatus Status { get; set; } // contains game timing/state
        public DateTime LastUpdatedUtc { get; set; } // last update 
    }
}
