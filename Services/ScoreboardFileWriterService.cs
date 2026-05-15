using System;
using System.Collections.Generic;
using System.Text;
using LedScoreboard.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LedScoreboard.Services
{
    public class ScoreboardFileWriterService
    {
        private readonly ILogger<ScoreboardFileWriterService> _logger;
        private readonly ScoreboardConfig _config;


        public ScoreboardFileWriterService(ILogger<ScoreboardFileWriterService> logger, ScoreboardConfig config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task WriteAsync(ScoreboardUpdate update)
        {
            string filePath = Path.Combine(_config.DataFolder, _config.ScoreboardJsonFile);


            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            string json = JsonSerializer.Serialize(update, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Wrote scoreboard data to {FilePath}", filePath);

        }





    }
}
