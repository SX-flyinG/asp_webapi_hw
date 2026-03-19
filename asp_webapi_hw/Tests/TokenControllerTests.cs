using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace asp_webapi_hw.Tests;

public class TokenControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TokenControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // POST /api/token/generate
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Generate_ValidCredentials_Returns200WithTokenPair()
    {
        // Arrange
        await TestHelpers.PostAsync(_client, "/api/auth/register",
            TestHelpers.ValidRegisterPayload(
                email: "gen_ok@example.com", userName: "gen_ok"));

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/generate",
            TestHelpers.ValidLoginPayload("gen_ok@example.com", "Password1"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("tokenType").GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task Generate_WrongPassword_Returns401()
    {
        // Arrange
        await TestHelpers.PostAsync(_client, "/api/auth/register",
            TestHelpers.ValidRegisterPayload(
                email: "gen_wrong@example.com", userName: "gen_wrong"));

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/generate",
            TestHelpers.ValidLoginPayload("gen_wrong@example.com", "WrongPass!"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(401);
    }

    [Fact]
    public async Task Generate_UnknownEmail_Returns401()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/generate",
            TestHelpers.ValidLoginPayload("nobody@example.com", "Password1"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Generate_MissingEmail_Returns400()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/generate",
            new { password = "Password1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("Email", out _).Should().BeTrue();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // POST /api/token/refresh
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Refresh_ValidRefreshToken_Returns200WithNewPair()
    {
        // Arrange
        var (_, refreshToken) = await TestHelpers.RegisterAndGetTokenPairAsync(
            _client, "refresh_ok@example.com", userName: "refresh_ok");

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/refresh",
            new { refreshToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        // Rotation — новий refresh token має відрізнятися від старого
        body.GetProperty("refreshToken").GetString().Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task Refresh_TokenRotation_OldTokenIsRevoked()
    {
        // Arrange
        var (_, oldRefresh) = await TestHelpers.RegisterAndGetTokenPairAsync(
            _client, "rotation@example.com", userName: "rotation_user");

        // Act — перше оновлення (повинно пройти)
        await TestHelpers.PostAsync(_client, "/api/token/refresh",
            new { refreshToken = oldRefresh });

        // Act — повторне використання старого токена (Rotation — має бути відкликаний)
        var secondResponse = await TestHelpers.PostAsync(_client, "/api/token/refresh",
            new { refreshToken = oldRefresh });

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/refresh",
            new { refreshToken = "completely-invalid-token" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(401);
    }

    [Fact]
    public async Task Refresh_EmptyToken_Returns400()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/refresh",
            new { refreshToken = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("RefreshToken", out _).Should().BeTrue();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // POST /api/token/revoke
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Revoke_ValidToken_Returns204()
    {
        // Arrange
        var (_, refreshToken) = await TestHelpers.RegisterAndGetTokenPairAsync(
            _client, "revoke_ok@example.com", userName: "revoke_ok");

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/revoke",
            new { refreshToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_AfterRevoke_TokenCannotBeUsedForRefresh()
    {
        // Arrange
        var (_, refreshToken) = await TestHelpers.RegisterAndGetTokenPairAsync(
            _client, "revoke_check@example.com", userName: "revoke_check");

        await TestHelpers.PostAsync(_client, "/api/token/revoke",
            new { refreshToken });

        // Act — спроба оновити відкликаний токен
        var response = await TestHelpers.PostAsync(_client, "/api/token/refresh",
            new { refreshToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_NonExistentToken_Returns404()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/revoke",
            new { refreshToken = "non-existent-token-xyz" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task Revoke_EmptyToken_Returns400()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/revoke",
            new { refreshToken = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // POST /api/token/revoke-all/{userId}
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RevokeAll_ExistingUser_Returns204AndInvalidatesAllTokens()
    {
        // Arrange — реєструємо та отримуємо два refresh токени
        var email    = "revokeall@example.com";
        var password = "Password1";
        var userName = "revokeall_user";

        var registerResp = await TestHelpers.PostAsync(_client, "/api/auth/register",
            TestHelpers.ValidRegisterPayload(email, password, userName));
        var registerBody = await TestHelpers.ReadJsonAsync(registerResp);
        var userId = registerBody.GetProperty("id").GetInt32();

        var genResp1 = await TestHelpers.PostAsync(_client, "/api/token/generate",
            TestHelpers.ValidLoginPayload(email, password));
        var refresh1 = (await TestHelpers.ReadJsonAsync(genResp1))
    .GetProperty("refreshToken").GetString()!;

       

        var genResp2 = await TestHelpers.PostAsync(_client, "/api/token/generate",
            TestHelpers.ValidLoginPayload(email, password));
        var refresh2 = (await TestHelpers.ReadJsonAsync(genResp2))
           .GetProperty("refreshToken").GetString()!;

        // Act
        var revokeAllResp = await TestHelpers.PostAsync(
            _client, $"/api/token/revoke-all/{userId}", new { });

        // Assert — 204
        revokeAllResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Обидва токени мають бути відкликані
        var r1 = await TestHelpers.PostAsync(_client, "/api/token/refresh",
            new { refreshToken = refresh1 });
        var r2 = await TestHelpers.PostAsync(_client, "/api/token/refresh",
            new { refreshToken = refresh2 });

        r1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        r2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeAll_NonExistentUser_Returns404()
    {
        // Act
        var response = await TestHelpers.PostAsync(
            _client, "/api/token/revoke-all/99999", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(404);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // POST /api/token/validate
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Validate_ValidAccessToken_Returns200WithUserId()
    {
        // Arrange
        var (accessToken, _) = await TestHelpers.RegisterAndGetTokenPairAsync(
            _client, "validate_ok@example.com", userName: "validate_ok");

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/validate",
            new { accessToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("valid").GetBoolean().Should().BeTrue();
        body.GetProperty("userId").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Validate_InvalidToken_Returns401()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/validate",
            new { accessToken = "totally.invalid.token" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(401);
        body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Validate_EmptyToken_Returns400()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/token/validate",
            new { accessToken = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("AccessToken", out _).Should().BeTrue();
    }
}
