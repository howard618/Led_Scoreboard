using LedScoreboard.Interfaces;
using LedScoreboard.Models;
using System.Text.Json;

namespace LedScoreboard.Services
{
    public class EspnSportsDataProvider : ISportsDataProvider
    {
        private readonly HttpClient _httpClient;

        public EspnSportsDataProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<GameState>> GetGamesAsync()
        {
            List<GameState> games = new();

            games.AddRange(await GetLeagueGamesAsync(
                "NFL",
                "https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard"
            ));

            games.AddRange(await GetLeagueGamesAsync(
                "MLB",
                "https://site.api.espn.com/apis/site/v2/sports/baseball/mlb/scoreboard"
            ));

            games.AddRange(await GetLeagueGamesAsync(
                "NHL",
                "https://site.api.espn.com/apis/site/v2/sports/hockey/nhl/scoreboard"
            ));

            return games;
        }

        private async Task<List<GameState>> GetLeagueGamesAsync(string league, string url)
        {
            List<GameState> games = new();

            string json = await _httpClient.GetStringAsync(url);

            using JsonDocument document = JsonDocument.Parse(json);

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("events", out JsonElement events))
            {
                return games;
            }

            foreach (JsonElement gameEvent in events.EnumerateArray())
            {
                string gameId = gameEvent.GetProperty("id").GetString() ?? "";

                JsonElement competition = gameEvent
                    .GetProperty("competitions")[0];

                JsonElement status = gameEvent.GetProperty("status");

                JsonElement statusType = status.GetProperty("type");

                string state = statusType.GetProperty("state").GetString() ?? "";
                string period = status.GetProperty("period").GetInt32().ToString();

                string clock = "";

                if (status.TryGetProperty("displayClock", out JsonElement displayClock))
                {
                    clock = displayClock.GetString() ?? "";
                }

                JsonElement competitors = competition.GetProperty("competitors");

                TeamState homeTeam = new();
                TeamState awayTeam = new();

                foreach (JsonElement competitor in competitors.EnumerateArray())
                {
                    string homeAway = competitor.GetProperty("homeAway").GetString() ?? "";

                    JsonElement team = competitor.GetProperty("team");

                    string teamCode = team.GetProperty("abbreviation").GetString() ?? "";

                    string logoUrl = league switch
                    {
                        "NFL" => $"https://a.espncdn.com/i/teamlogos/nfl/500/{teamCode.ToLower()}.png",
                        "MLB" => $"https://a.espncdn.com/i/teamlogos/mlb/500/{teamCode.ToLower()}.png",
                        "NHL" => $"https://a.espncdn.com/i/teamlogos/nhl/500/{teamCode.ToLower()}.png",
                        _ => ""
                    };

                  
                    Console.WriteLine($"TEAM: {teamCode}");
                    Console.WriteLine($"LOGO URL: {logoUrl}");

                    TeamState teamState = new TeamState
                    {
                        Code = team.GetProperty("abbreviation").GetString() ?? "",
                        Name = team.GetProperty("displayName").GetString() ?? "",
                        Score = int.TryParse(
                            competitor.GetProperty("score").GetString(),
                            out int score
                        ) ? score : 0,

                        LogoUrl = logoUrl,

                        LogoFile = $"/scoreboard/logos/{league.ToLower()}/{teamCode}.png"
                    };

                    if (homeAway == "home")
                    {
                        homeTeam = teamState;
                    }
                    else if (homeAway == "away")
                    {
                        awayTeam = teamState;
                    }
                }

                games.Add(new GameState
                {
                    League = league,
                    GameId = $"{league.ToLower()}-{gameId}",
                    Home = homeTeam,
                    Away = awayTeam,
                    Status = new GameStatus
                    {
                        Period = FormatPeriod(league, period, state),
                        Clock = ShouldShowClock(league, state) ? clock : "",
                        State = state.ToUpper()
                    },
                    LastUpdatedUtc = DateTime.UtcNow
                });
            }

            return games;
        }

        private static string FormatPeriod(string league, string period, string state)
        {
            // Normalize state (ESPN gives lowercase)
            state = state.ToLower();

            if (state == "pre")
            {
                return "PRE";
            }

            if (state == "post")
            {
                return "FINAL";
            }

            // Live game formatting
            return league switch
            {
                "NFL" => $"Q{period}",

                "NHL" => period switch
                {
                    "1" => "1ST",
                    "2" => "2ND",
                    "3" => "3RD",
                    _ => period
                },

                "MLB" => int.TryParse(period, out int inning) && inning > 0
                    ? $"INN {inning}"
                    : "LIVE",

                _ => period
            };


        }

        private static bool ShouldShowClock(string league, string state)
        {
            state = state.ToLower();

            if (state != "in")
            {
                return false;
            }

            return league == "NFL" || league == "NHL";
        }
    }
}