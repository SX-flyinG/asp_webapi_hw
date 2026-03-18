using System.ComponentModel.DataAnnotations;

namespace asp_webapi_hw.DTO;

public class RegisterDto : IValidatableObject
{
    [Required(ErrorMessage = "Email є обов'язковим.")]
    [EmailAddress(ErrorMessage = "Невалідний формат email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль є обов'язковим.")]
    [MinLength(8,  ErrorMessage = "Пароль має містити мінімум 8 символів.")]
    [MaxLength(64, ErrorMessage = "Пароль не може перевищувати 64 символи.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтвердження пароля є обов'язковим.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вік є обов'язковим.")]
    [Range(13, 120, ErrorMessage = "Вік має бути від 13 до 120 років.")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Ім'я користувача є обов'язковим.")]
    [MinLength(3,  ErrorMessage = "UserName має містити мінімум 3 символи.")]
    [MaxLength(20, ErrorMessage = "UserName не може перевищувати 20 символів.")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$",
        ErrorMessage = "UserName може містити лише латинські літери, цифри та '_'.")]
    public string UserName { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (Password != ConfirmPassword)
            yield return new ValidationResult(
                "Паролі не збігаються.",
                new[] { nameof(ConfirmPassword) });
    }
}
