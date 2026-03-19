using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace asp_webapi_hw.Tests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // POST /api/auth/register
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Register_ValidData_Returns201WithUserInfo()
    {
        // Arrange
        var payload = TestHelpers.ValidRegisterPayload(
            email: "register_ok@example.com", userName: "register_ok");

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("email").GetString().Should().Be("register_ok@example.com");
        body.GetProperty("userName").GetString().Should().Be("register_ok");
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400WithErrors()
    {
        // Arrange
        var payload = TestHelpers.ValidRegisterPayload(
            email: "not-an-email", userName: "user_inv_email");

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(400);
        body.GetProperty("errors").TryGetProperty("Email", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400WithErrors()
    {
        // Arrange
        var payload = new
        {
            email           = "short_pass@example.com",
            password        = "123",
            confirmPassword = "123",
            age             = 25,
            userName        = "short_user"
        };

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("Password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Register_PasswordTooLong_Returns400WithErrors()
    {
        // Arrange
        var longPassword = new string('A', 65);
        var payload = new
        {
            email           = "long_pass@example.com",
            password        = longPassword,
            confirmPassword = longPassword,
            age             = 25,
            userName        = "long_user"
        };

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("Password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Register_PasswordMismatch_Returns400WithConfirmPasswordError()
    {
        // Arrange
        var payload = new
        {
            email           = "mismatch@example.com",
            password        = "Password1",
            confirmPassword = "Password2",
            age             = 25,
            userName        = "mismatch_user"
        };

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("ConfirmPassword", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(12)]   // молодший мінімального
    [InlineData(121)]  // старший максимального
    [InlineData(0)]    // нуль
    public async Task Register_InvalidAge_Returns400(int age)
    {
        // Arrange
        var payload = new
        {
            email           = $"age_test_{age}@example.com",
            password        = "Password1",
            confirmPassword = "Password1",
            age,
            userName        = $"age_user_{age}"
        };

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("Age", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("ab")]              // коротше 3
    [InlineData("abcdefghijklmnopqrstu")]  // довше 20
    [InlineData("user name")]       // пробіл
    [InlineData("user-name")]       // дефіс
    [InlineData("user@name")]       // спецсимвол
    public async Task Register_InvalidUserName_Returns400(string userName)
    {
        // Arrange
        var payload = new
        {
            email           = $"un_{userName.Length}@example.com",
            password        = "Password1",
            confirmPassword = "Password1",
            age             = 25,
            userName
        };

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("UserName", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        // Arrange — перша реєстрація
        var payload = TestHelpers.ValidRegisterPayload(
            email: "duplicate@example.com", userName: "duplicate_user");
        await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Act — повторна реєстрація з тим самим email
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(409);
        body.GetProperty("title").GetString().Should().Be("Conflict");
        body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_MissingRequiredFields_Returns400()
    {
        // Arrange — порожнє тіло
        var response = await TestHelpers.PostAsync(_client, "/api/auth/register", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // POST /api/auth/login
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAccessToken()
    {
        // Arrange — реєстрація
        await TestHelpers.PostAsync(_client, "/api/auth/register",
            TestHelpers.ValidRegisterPayload(
                email: "login_ok@example.com", userName: "login_ok"));

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/login",
            TestHelpers.ValidLoginPayload("login_ok@example.com", "Password1"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("expiresIn").GetInt32().Should().Be(3600);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        await TestHelpers.PostAsync(_client, "/api/auth/register",
            TestHelpers.ValidRegisterPayload(
                email: "login_wrong@example.com", userName: "login_wrong"));

        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/login",
            TestHelpers.ValidLoginPayload("login_wrong@example.com", "WrongPass!"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(401);
        body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/login",
            TestHelpers.ValidLoginPayload("ghost@example.com", "Password1"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/login",
            new { email = "bad-email", password = "Password1" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("Email", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        // Act
        var response = await TestHelpers.PostAsync(_client, "/api/auth/login",
            new { email = "test@example.com", password = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await TestHelpers.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("Password", out _).Should().BeTrue();
    }
}
