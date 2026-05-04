using LedScoreboard.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LedScoreboard.Interfaces
{
    internal interface IDisplayClient
    {
        Task SendUpdateAsync(ScoreboardUpdate update);
    }
}
