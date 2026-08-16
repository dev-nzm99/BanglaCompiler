namespace BanglaCompiler.Lexer;

/// <summary>
/// A single token produced by the lexer: a token type, the raw source text
/// that produced it (the "lexeme"), and its position in the source file
/// (1-based line and column) so later stages — parser, semantic analyzer —
/// can report precise error locations.
/// </summary>
public sealed class Token
{
    public TokenType Type { get; }
    public string Lexeme { get; }
    public int Line { get; }
    public int Column { get; }

    public Token(TokenType type, string lexeme, int line, int column)
    {
        Type = type;
        Lexeme = lexeme;
        Line = line;
        Column = column;
    }

    public override string ToString() => $"{Type}('{Lexeme}') @ {Line}:{Column}";
}
