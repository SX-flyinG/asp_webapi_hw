using Microsoft.AspNetCore.Mvc;
using asp_webapi_hw.DTO;
using asp_webapi_hw.Models;

namespace asp_webapi_hw.Controllers;

[ApiController]
[Route("api/notes")]
[Produces("application/json")]
public class NotesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NoteDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<NoteDto>> GetAll()
    {
        var notes = NoteRepository.GetAll()
            .Select(ToDto);

        return Ok(notes);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<NoteDto> GetById(int id)
    {
        var note = NoteRepository.GetById(id);

        if (note is null)
            throw new KeyNotFoundException($"Нотатку з ID {id} не знайдено.");

        return Ok(ToDto(note));
    }

    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(IEnumerable<NoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<NoteDto>> GetByUser(int userId)
    {
        var user = UserRepository.GetById(userId);

        if (user is null)
            throw new KeyNotFoundException($"Користувача з ID {userId} не знайдено.");

        var notes = NoteRepository.GetByUserId(userId)
            .Select(ToDto);

        return Ok(notes);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<NoteDto> Create([FromBody] CreateNoteDto dto)
    {
        var user = UserRepository.GetById(dto.UserId);

        if (user is null)
            throw new KeyNotFoundException($"Користувача з ID {dto.UserId} не знайдено.");

        var note = NoteRepository.Add(new Note
        {
            Title   = dto.Title,
            Content = dto.Content,
            UserId  = dto.UserId,
        });

        var result = ToDto(note);
        return CreatedAtAction(nameof(GetById), new { id = note.Id }, result);
    }

    private static NoteDto ToDto(Note note) => new()
    {
        Id        = note.Id,
        Title     = note.Title,
        Content   = note.Content,
        UserId    = note.UserId,
        CreatedAt = note.CreatedAt,
    };
}
