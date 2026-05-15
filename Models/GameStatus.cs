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
        public string DetailLine { get; set; } = ""; // extra sport specific text 
        public int Balls { get; set; } // MLB Specific 
        public int Strikes { get; set; } // MLB Specific 
        public int Outs { get; set; } // MLB Specific 
        public bool RunnerOnFirst { get; set; } //light up first base diamond 
        public bool RunnerOnSecond { get; set; } //light up second base diamond 
        public bool RunnerOnThird { get; set; } //light up third base diamond 
    }
}
