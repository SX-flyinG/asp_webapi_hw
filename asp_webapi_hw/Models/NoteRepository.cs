

namespace asp_webapi_hw.Models;

public static class NoteRepository
{
    private static readonly List<Note> _notes = new();
    private static int _nextId = 1;

    public static IEnumerable<Note> GetAll() =>
        _notes.AsReadOnly();

    public static IEnumerable<Note> GetByUserId(int userId) =>
        _notes.Where(n => n.UserId == userId).ToList();

    public static Note? GetById(int id) =>
        _notes.FirstOrDefault(n => n.Id == id);

    public static Note Add(Note note)
    {
        note.Id        = _nextId++;
        note.CreatedAt = DateTime.UtcNow;
        _notes.Add(note);
        return note;
    }
}
