using System.ComponentModel.DataAnnotations;

namespace asp_webapi_hw.DTO;

public class NoteDto
{
    public int      Id        { get; set; }
    public string   Title     { get; set; } = string.Empty;
    public string   Content   { get; set; } = string.Empty;
    public int      UserId    { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNoteDto
{
    [Required(ErrorMessage = "Заголовок є обов'язковим.")]
    [MaxLength(200, ErrorMessage = "Заголовок не може перевищувати 200 символів.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Зміст нотатки є обов'язковим.")]
    [MaxLength(10000, ErrorMessage = "Зміст не може перевищувати 10 000 символів.")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "UserId є обов'язковим.")]
    [Range(1, int.MaxValue, ErrorMessage = "UserId має бути більше 0.")]
    public int UserId { get; set; }
}
