using System.Net.Http.Json;
using System.Text.Json;

namespace asp_webapi_hw.Tests;

public static class TestHelpers
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static object ValidRegisterPayload(
        string email    = "user@example.com",
        string password = "Password1",
        string userName = "john_doe",
        int    age      = 25) => new
    {
        email,
        password,
        confirmPassword = password,
        age,
        userName
    };

    public static object ValidLoginPayload(
        string email    = "user@example.com",
        string password = "Password1") => new { email, password };

    // ── HTTP-хелпери ──────────────────────────────────────────────────────────

    public static Task<HttpResponseMessage> PostAsync(
        HttpClient client, string url, object body) =>
        client.PostAsJsonAsync(url, body);

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
    }

    // ── Сценарій: зареєструвати + отримати токен ─────────────────────────────

    public static async Task<string> RegisterAndGetAccessTokenAsync(
        HttpClient client,
        string email    = "token_user@example.com",
        string password = "Password1",
        string userName = "token_user")
    {
        // Реєстрація
        await PostAsync(client, "/api/auth/register",
            ValidRegisterPayload(email, password, userName));

        // Генерація токена
        var resp = await PostAsync(client, "/api/token/generate",
            ValidLoginPayload(email, password));

        var body = await ReadJsonAsync(resp);
        return body.GetProperty("accessToken").GetString()!;
    }

    public static async Task<(string AccessToken, string RefreshToken)>
        RegisterAndGetTokenPairAsync(
            HttpClient client,
            string email    = "pair_user@example.com",
            string password = "Password1",
            string userName = "pair_user")
    {
        await PostAsync(client, "/api/auth/register",
            ValidRegisterPayload(email, password, userName));

        var resp = await PostAsync(client, "/api/token/generate",
            ValidLoginPayload(email, password));

        var body = await ReadJsonAsync(resp);
        return (
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("refreshToken").GetString()!
        );
    }
}
