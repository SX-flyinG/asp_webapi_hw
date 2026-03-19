using System.Net;
using FluentAssertions;
using Xunit;

namespace asp_webapi_hw.Tests;

public class ProblemDetailsFormatTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProblemDetailsFormatTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ValidationError_ContainsAllRequiredProblemDetailsFields()
    {
        // Act — невалідний email → 400
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register",
            new
            {
                email           = "bad-email",
                password        = "Password1",
                confirmPassword = "Password1",
                age             = 25,
                userName        = "user_fmt"
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/problem+json");

        var body = await TestHelpers.ReadJsonAsync(response);
        body.TryGetProperty("type",     out _).Should().BeTrue("має бути поле type");
        body.TryGetProperty("title",    out _).Should().BeTrue("має бути поле title");
        body.TryGetProperty("status",   out _).Should().BeTrue("має бути поле status");
        body.TryGetProperty("instance", out _).Should().BeTrue("має бути поле instance");
        body.TryGetProperty("traceId",  out _).Should().BeTrue("має бути поле traceId");
        body.TryGetProperty("errors",   out _).Should().BeTrue("ValidationProblemDetails має мати errors");
    }

    [Fact]
    public async Task BusinessError_409_ContainsAllRequiredProblemDetailsFields()
    {
        // Arrange — реєструємо двічі → 409
        var payload = TestHelpers.ValidRegisterPayload(
            email: "fmt_conflict@example.com", userName: "fmt_conflict");
        await TestHelpers.PostAsync(_client, "/api/auth/register", payload);
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("type").GetString().Should()
            .StartWith("https://httpstatuses.com/");
        body.GetProperty("title").GetString().Should().Be("Conflict");
        body.GetProperty("status").GetInt32().Should().Be(409);
        body.TryGetProperty("detail",   out _).Should().BeTrue();
        body.TryGetProperty("instance", out _).Should().BeTrue();
        body.TryGetProperty("traceId",  out _).Should().BeTrue();
    }

    [Fact]
    public async Task BusinessError_401_ContainsAllRequiredProblemDetailsFields()
    {
        // Act — невірний пароль → 401
        var response = await TestHelpers.PostAsync(_client, "/api/auth/login",
            TestHelpers.ValidLoginPayload("nobody@example.com", "WrongPass"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("title").GetString().Should().Be("Unauthorized");
        body.GetProperty("status").GetInt32().Should().Be(401);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task BusinessError_404_ContainsAllRequiredProblemDetailsFields()
    {
        // Act — revoke-all неіснуючого юзера → 404
        var response = await TestHelpers.PostAsync(
            _client, "/api/token/revoke-all/88888", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("title").GetString().Should().Be("Not Found");
        body.GetProperty("status").GetInt32().Should().Be(404);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AllErrorResponses_HaveCorrectContentType()
    {
        // Перевіряємо Content-Type для різних типів помилок
        var cases = new (string Url, object Body, int Status)[]
 {
    ("/api/auth/register",      new { email = "bad" },                             400),
    ("/api/auth/login",         new { email = "x@x.com", password = "WrongPass" }, 401),
    ("/api/token/revoke",       new { refreshToken = "fake" },                     404),
    ("/api/token/revoke-all/0", new { },                                           404),
 };

        foreach (var (url, body, _) in cases)
        {
            var response = await TestHelpers.PostAsync(_client, url, body);
            response.Content.Headers.ContentType?.MediaType
                .Should().Be("application/problem+json",
                    $"URL {url} має повертати application/problem+json");
        }
    }
}
