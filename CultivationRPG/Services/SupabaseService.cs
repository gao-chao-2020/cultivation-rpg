using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using CultivationRPG.Models;

namespace CultivationRPG.Services;

public sealed class SupabaseService
{
    private readonly HttpClient _http;
    private const string Url = "https://xpjrsuernrdsrsmgpzgh.supabase.co";
    private const string Key = "sb_publishable_Fm80KM_APRlvTMReUqvAUg_kx8hieUM";
    private string? _myRowId;

    public SupabaseService()
    {
        _http = new HttpClient { BaseAddress = new Uri(Url) };
        _http.DefaultRequestHeaders.Add("apikey", Key);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {Key}");
    }

    public async Task<List<LeaderboardEntry>> FetchAsync(string? date = null, int limit = 20)
    {
        var url = $"/rest/v1/leaderboard?select=*&order=xp.desc&limit={limit}";
        if (date != null) url += $"&date=eq.{date}";
        var resp = await _http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var rows = await resp.Content.ReadFromJsonAsync<List<Row>>() ?? [];
        int rank = 0;
        return rows.Select(r => new LeaderboardEntry
        {
            Rank = ++rank, PlayerName = r.player_name, Realm = r.realm,
            Xp = r.xp, Spirit = r.spirit, Agility = r.agility
        }).ToList();
    }

    public async Task UpsertAsync(Player player)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var payload = new { device_id = player.Id, player_name = player.Name, realm = player.RealmName, xp = player.Xp, spirit = player.Spirit, agility = (long)player.Agility, wisdom = player.Wisdom, alchemy = player.Alchemy, date = today };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        if (_myRowId != null)
        {
            var patch = new HttpRequestMessage(HttpMethod.Patch, $"/rest/v1/leaderboard?id=eq.{_myRowId}")
            { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            await _http.SendAsync(patch);
        }
        else
        {
            var lookup = await _http.GetAsync($"/rest/v1/leaderboard?select=id&device_id=eq.{Uri.EscapeDataString(player.Id)}");
            if (lookup.IsSuccessStatusCode)
            {
                var rows = await lookup.Content.ReadFromJsonAsync<List<IdRow>>();
                if (rows?.Count > 0)
                {
                    _myRowId = rows[0].id;
                    var patch = new HttpRequestMessage(HttpMethod.Patch, $"/rest/v1/leaderboard?id=eq.{_myRowId}")
                    { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                    await _http.SendAsync(patch);
                    return;
                }
            }
            var req = new HttpRequestMessage(HttpMethod.Post, "/rest/v1/leaderboard")
            { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            req.Headers.Add("Prefer", "return=representation");
            var resp = await _http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                var created = await resp.Content.ReadFromJsonAsync<List<IdRow>>();
                if (created?.Count > 0) _myRowId = created[0].id;
            }
            else
            {
                var err = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Supabase POST failed: {resp.StatusCode} - {err}");
            }
        }
    }

    private class IdRow { public string id { get; set; } = ""; }
    private class Row { public string player_name { get; set; } = ""; public string realm { get; set; } = ""; public long xp { get; set; } public long spirit { get; set; } public long agility { get; set; } }
}
