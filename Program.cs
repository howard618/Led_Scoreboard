using LedScoreboard;
using LedScoreboard.Interfaces;
using LedScoreboard.Models;
using LedScoreboard.Services;

var builder = Host.CreateApplicationBuilder(args);

var config = new ScoreboardConfig();
builder.Services.AddSingleton(config);

builder.Services.AddHttpClient<ISportsDataProvider, EspnSportsDataProvider>();
builder.Services.AddHttpClient<LogoDownloaderService>();
builder.Services.AddSingleton<LogoProcessorService>();
builder.Services.AddSingleton<ScoreboardFileWriterService>();

builder.Services.AddSingleton<ScoreboardService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
