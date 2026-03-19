using System.ComponentModel.DataAnnotations;

namespace asp_webapi_hw.DTO;

public class TokenResponseDto
{
    public string AccessToken  { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int    ExpiresIn    { get; set; }        
    public string TokenType    { get; set; } = "Bearer";
}

public class RefreshTokenDto
{
    [Required(ErrorMessage = "RefreshToken є обов'язковим.")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RevokeTokenDto
{
    [Required(ErrorMessage = "RefreshToken є обов'язковим.")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ValidateTokenDto
{
    [Required(ErrorMessage = "AccessToken є обов'язковим.")]
    public string AccessToken { get; set; } = string.Empty;
}
