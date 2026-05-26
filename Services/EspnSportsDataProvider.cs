using System.Text.Json;
using LedScoreboard.Interfaces;
using LedScoreboard.Models;

namespace LedScoreboard.Services;

public class EspnSportsDataProvider : ISportsDataProvider
{
    private readonly HttpClient _httpClient;

    public EspnSportsDataProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<GameState>> GetGamesAsync()
    {
        var games = new List<GameState>();

        games.AddRange(await GetLeagueGamesAsync(
            "NFL",
            "https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard"));

        games.AddRange(await GetLeagueGamesAsync(
            "MLB",
            "https://site.api.espn.com/apis/site/v2/sports/baseball/mlb/scoreboard"));

        games.AddRange(await GetLeagueGamesAsync(
            "NHL",
            "https://site.api.espn.com/apis/site/v2/sports/hockey/nhl/scoreboard"));

        return games;
    }

    private async Task<List<GameState>> GetLeagueGamesAsync(string league, string url)
    {
        var games = new List<GameState>();

        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();

        using JsonDocument doc = await JsonDocument.ParseAsync(stream);

        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("events", out JsonElement events))
        {
            return games;
        }

        foreach (JsonElement game in events.EnumerateArray())
        {
            string gameId = game.GetProperty("id").GetString() ?? "";

            JsonElement competition =
                game.GetProperty("competitions")[0];

            JsonElement competitors =
                competition.GetProperty("competitors");

            JsonElement home =
                competitors.EnumerateArray()
                    .First(c => c.GetProperty("homeAway").GetString() == "home");

            JsonElement away =
                competitors.EnumerateArray()
                    .First(c => c.GetProperty("homeAway").GetString() == "away");

            JsonElement status =
                competition.GetProperty("status");

            JsonElement statusType =
                status.GetProperty("type");

            string state =
                statusType.GetProperty("state").GetString() ?? "";

            string period =
                status.GetProperty("period").GetInt32().ToString();

            string detailLine = "";

            if (statusType.TryGetProperty("shortDetail", out JsonElement shortDetail))
            {
                detailLine = shortDetail.GetString() ?? "";
            }

            string clock = "";

            if (status.TryGetProperty("displayClock", out JsonElement displayClock))
            {
                clock = displayClock.GetString() ?? "";
            }

            var homeTeam = BuildTeamState(league, home);
            var awayTeam = BuildTeamState(league, away);

            games.Add(new GameState
            {
                League = league,
                GameId = $"{league.ToLower()}-{gameId}",

                Home = homeTeam,
                Away = awayTeam,

                Status = new GameStatus
                {
                    Period = ((league == "MLB" || league == "NFL")
			&& state.Equals("pre", StringComparison.OrdinalIgnoreCase)
 			&& !string.IsNullOrWhiteSpace(detailLine))
                        ? detailLine
                        : FormatPeriod(league, period, state),

                    Clock = ShouldShowClock(league, state) ? clock : "",

                    State = state.ToUpper(),

                    Balls = GetSituationInt(competition, "balls"),
                    Strikes = GetSituationInt(competition, "strikes"),
                    Outs = GetSituationInt(competition, "outs"),

                    RunnerOnFirst = GetSituationBool(competition, "onFirst"),
                    RunnerOnSecond = GetSituationBool(competition, "onSecond"),
                    RunnerOnThird = GetSituationBool(competition, "onThird")
                },

                LastUpdatedUtc = DateTime.UtcNow
            });
        }

        return games;
    }

    private static TeamState BuildTeamState(string league, JsonElement competitor)
    {
        JsonElement team =
            competitor.GetProperty("team");

        string code =
            team.GetProperty("abbreviation").GetString() ?? "";

        Console.WriteLine($"TEAM: {code}");

        string logoUrl = league switch
        {
            "NFL" => $"https://a.espncdn.com/i/teamlogos/nfl/500/{code.ToLower()}.png",
            "MLB" => $"https://a.espncdn.com/i/teamlogos/mlb/500/{code.ToLower()}.png",
            "NHL" => $"https://a.espncdn.com/i/teamlogos/nhl/500/{code.ToLower()}.png",
            _ => ""
        };

        Console.WriteLine($"LOGO URL: {logoUrl}");

        int score = 0;

        if (competitor.TryGetProperty("score", out JsonElement scoreElement))
        {
            int.TryParse(scoreElement.GetString(), out score);
        }

        return new TeamState
        {
            Code = code,
            Name = team.GetProperty("displayName").GetString() ?? "",
            Score = score,
            LogoUrl = logoUrl
        };
    }

    private static string FormatPeriod(string league, string period, string state)
    {
        state = state.ToLower();

        if (state == "pre")
        {
	    if ((league == "MLB" || league == "NFL")
	    && !string.IsNullOrWhiteSpace(period))
	    {
		return period;
	    }
            return "PRE";
        }

        if (state == "post")
        {
            return "FINAL";
        }

        if (league == "NFL")
        {
            return $"Q{period}";
        }

        if (league == "NBA")
        {
            return $"Q{period}";
        }

        if (league == "NHL")
        {
            return $"P{period}";
        }

        if (league == "MLB")
        {
            return $"INN {period}";
        }

        return period;
    }

    private static bool ShouldShowClock(string league, string state)
    {
        state = state.ToLower();

        return state == "in";
    }

    private static int GetSituationInt(JsonElement competition, string propertyName)
    {
        if (!competition.TryGetProperty("situation", out JsonElement situation))
        {
            return 0;
        }

        if (!situation.TryGetProperty(propertyName, out JsonElement value))
        {
            return 0;
        }

        return value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : 0;
    }

    private static bool GetSituationBool(JsonElement competition, string propertyName)
    {
        if (!competition.TryGetProperty("situation", out JsonElement situation))
        {
            return false;
        }

        if (!situation.TryGetProperty(propertyName, out JsonElement value))
        {
            return false;
        }

        return value.ValueKind == JsonValueKind.True;
    }
}
