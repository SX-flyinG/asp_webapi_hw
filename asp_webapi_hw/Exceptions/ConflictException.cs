namespace asp_webapi_hw.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
