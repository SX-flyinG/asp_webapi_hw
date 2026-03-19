

namespace asp_webapi_hw.Models;

public static class UserRepository
{
    private static readonly List<User> _users = new();
    private static int _nextId = 1;

    public static User? FindByEmail(string email) =>
        _users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public static User? GetById(int id) =>
        _users.FirstOrDefault(u => u.Id == id);

    public static User Add(User user)
    {
        user.Id = _nextId++;
        _users.Add(user);
        return user;
    }
}
