using System;
using System.Collections.Generic;
using System.Text;

namespace LedScoreboard.Models
{
    public class TeamState
    {
        public string LogoUrl { get; set; } = ""; //stores ESPN image URL so logos can be downloaded
        public string LogoFile { get; set; } = ""; // tells the display render which local image to use
        public string Code { get; set; } // shorhand for team names for scoreboard : Detroit Lions >> DET 
        public string Name { get; set; } //full team name for debugging 
        public int Score { get; set; } // game score 
        public bool HasPossession { get; set; } // for NFL >> Who has the ball.
        public bool IsWinner { get; set; } // when game is over this indicates the winner 
        public string ProcessedLogoFile { get; set; } = ""; //small matrix-ready logo file used by the display renderer
    }
}
