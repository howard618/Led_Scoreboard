using System;
using System.Collections.Generic;
using System.Text;

namespace LedScoreboard.Models
{
    public class GameStatus
    {
        public string Period { get; set; } // how game time is broken down 
        public string Clock { get; set; } //how much time is left on the game clock 
        public string State { get; set; } // status of the game >> PRE, LIVE, POST 
    }
}
