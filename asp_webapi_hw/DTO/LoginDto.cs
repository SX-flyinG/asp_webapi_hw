using System.ComponentModel.DataAnnotations;

namespace asp_webapi_hw.DTO;

public class LoginDto
{
    [Required(ErrorMessage = "Email є обов'язковим.")]
    [EmailAddress(ErrorMessage = "Невалідний формат email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль є обов'язковим.")]
    public string Password { get; set; } = string.Empty;
}
