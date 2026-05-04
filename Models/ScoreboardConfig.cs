using System;
using System.Collections.Generic;
using System.Text;

namespace LedScoreboard.Models
{
    public class ScoreboardConfig
    {
        public List<string> SelectedGameIds { get; set; } = new(); // controls which games are dislpayed on the board 
        public string DisplayMode { get; set; } = "rotate"; // controls how they are shown
        public int RefreshSeconds { get; set; } = 15; // rate at which APIs are called (board updates)
        public int RotationSeconds { get; set; } = 10; // rate at which the selected games will cycle across the board
        public string DisplayUrl { get; set; } = "http://localhost:5000/update"; // where data is sent to the Raspberry Pi
    }
}
