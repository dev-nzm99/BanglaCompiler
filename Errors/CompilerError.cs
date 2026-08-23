namespace BanglaCompiler.Errors;

public enum ErrorType
{
    Lexical,
    Syntax,
    Semantic,
}


public sealed class CompilerError
{
    public ErrorType Type { get; }
    public string Message { get; }
    public int Line { get; }
    public int Column { get; }
    public string? Lexeme { get; }

    public CompilerError(ErrorType type, string message, int line, int column, string? lexeme = null)
    {
        Type = type;
        Message = message;
        Line = line;
        Column = column;
        Lexeme = lexeme;
    }

    public override string ToString() => $"[{Type} Error] Line {Line}, Column {Column}:\n  {Message}";
}
