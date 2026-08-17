using System.Text;
using BanglaCompiler.Errors;

namespace BanglaCompiler.Lexer;

public sealed class Lexer
{
    private readonly string _source;
    private readonly ErrorReporter _errorReporter;
    private readonly List<Token> _tokens = new();

    private int _pos;      // index into _source of the next unread character
    private int _line = 1; // 1-based current line
    private int _column = 1; // 1-based current column

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["সংখ্যা"] = TokenType.KeywordSongkha,
        ["ভগ্নাংশ"] = TokenType.KeywordVognangsho,
        ["যদি"] = TokenType.KeywordJodi,
        ["অন্যথায়"] = TokenType.KeywordOnnothay,
        ["যতক্ষণ"] = TokenType.KeywordJotokhon,
        ["দেখাও"] = TokenType.KeywordDekhao,
    };

    public Lexer(string source, ErrorReporter errorReporter)
    {
        _source = source;
        _errorReporter = errorReporter;
    }

    public List<Token> Tokenize()
    {
        while (!IsAtEnd())
        {
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EndOfFile, string.Empty, _line, _column));
        return _tokens;
    }

    private void ScanToken()
    {
        char c = Peek();

        // Whitespace
        if (c == ' ' || c == '\t' || c == '\r')
        {
            Advance();
            return;
        }

        if (c == '\n')
        {
            Advance();
            _line++;
            _column = 1;
            return;
        }

        // Comments: // ... end of line
        if (c == '/' && PeekNext() == '/')
        {
            while (!IsAtEnd() && Peek() != '\n')
            {
                Advance();
            }
            return;
        }


        if (IsAsciiDigit(c))
        {
            ScanNumber();
            return;
        }

        if (IsAsciiIdentifierStart(c))
        {
            ScanIdentifier();
            return;
        }

        if (IsBanglaChar(c))
        {
            ScanBanglaWord();
            return;
        }

 
        switch (c)
        {
            case '+': AddSingle(TokenType.Plus); return;
            case '-': AddSingle(TokenType.Minus); return;
            case '*': AddSingle(TokenType.Multiply); return;
            case '/': AddSingle(TokenType.Divide); return;
            case '(': AddSingle(TokenType.LeftParen); return;
            case ')': AddSingle(TokenType.RightParen); return;
            case '{': AddSingle(TokenType.LeftBrace); return;
            case '}': AddSingle(TokenType.RightBrace); return;
            case ';': AddSingle(TokenType.Semicolon); return;

            case '=':
                if (PeekNext() == '=') AddDouble(TokenType.Equal);
                else AddSingle(TokenType.Assign);
                return;

            case '!':
                if (PeekNext() == '=') { AddDouble(TokenType.NotEqual); return; }
                ReportInvalidCharacter(c);
                return;

            case '>':
                if (PeekNext() == '=') AddDouble(TokenType.GreaterEqual);
                else AddSingle(TokenType.Greater);
                return;

            case '<':
                if (PeekNext() == '=') AddDouble(TokenType.LessEqual);
                else AddSingle(TokenType.Less);
                return;

            default:
                ReportInvalidCharacter(c);
                return;
        }
    }

    private void ScanNumber()
    {
        int startLine = _line;
        int startColumn = _column;
        var sb = new StringBuilder();
        bool isFloat = false;

        while (!IsAtEnd() && IsAsciiDigit(Peek()))
        {
            sb.Append(Advance());
        }

        if (!IsAtEnd() && Peek() == '.' && IsAsciiDigit(PeekNext()))
        {
            isFloat = true;
            sb.Append(Advance()); // consume '.'
            while (!IsAtEnd() && IsAsciiDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        _tokens.Add(new Token(
            isFloat ? TokenType.FloatLiteral : TokenType.IntegerLiteral,
            sb.ToString(),
            startLine,
            startColumn));
    }

    private void ScanIdentifier()
    {
        int startLine = _line;
        int startColumn = _column;
        var sb = new StringBuilder();

        while (!IsAtEnd() && IsAsciiIdentifierPart(Peek()))
        {
            sb.Append(Advance());
        }

        _tokens.Add(new Token(TokenType.Identifier, sb.ToString(), startLine, startColumn));
    }

    private void ScanBanglaWord()
    {
        int startLine = _line;
        int startColumn = _column;
        var sb = new StringBuilder();

        while (!IsAtEnd() && IsBanglaChar(Peek()))
        {
            sb.Append(Advance());
        }

        string word = sb.ToString();
        if (Keywords.TryGetValue(word, out TokenType type))
        {
            _tokens.Add(new Token(type, word, startLine, startColumn));
        }
        else
        {
 
            _errorReporter.ReportLexical(
                $"Unrecognized word '{word}'. Expected a keyword (সংখ্যা, ভগ্নাংশ, যদি, অন্যথায়, যতক্ষণ, দেখাও) — identifiers must use ASCII letters.",
                startLine,
                startColumn,
                word);
            _tokens.Add(new Token(TokenType.Invalid, word, startLine, startColumn));
        }
    }


    private void ReportInvalidCharacter(char c)
    {
        _errorReporter.ReportLexical($"Unexpected character '{c}'.", _line, _column, c.ToString());
        _tokens.Add(new Token(TokenType.Invalid, c.ToString(), _line, _column));
        Advance();
    }

    private void AddSingle(TokenType type)
    {
        int line = _line;
        int column = _column;
        char c = Advance();
        _tokens.Add(new Token(type, c.ToString(), line, column));
    }

    private void AddDouble(TokenType type)
    {
        int line = _line;
        int column = _column;
        char first = Advance();
        char second = Advance();
        _tokens.Add(new Token(type, $"{first}{second}", line, column));
    }

    private bool IsAtEnd() => _pos >= _source.Length;

    private char Peek() => IsAtEnd() ? '\0' : _source[_pos];

    private char PeekNext() => (_pos + 1 >= _source.Length) ? '\0' : _source[_pos + 1];

    private char Advance()
    {
        char c = _source[_pos];
        _pos++;
        _column++;
        return c;
    }

    private static bool IsAsciiDigit(char c) => c is >= '0' and <= '9';

    private static bool IsAsciiLetter(char c) => (c is >= 'a' and <= 'z') || (c is >= 'A' and <= 'Z');

    private static bool IsAsciiIdentifierStart(char c) => IsAsciiLetter(c) || c == '_';

    private static bool IsAsciiIdentifierPart(char c) => IsAsciiLetter(c) || IsAsciiDigit(c) || c == '_';


    private static bool IsBanglaChar(char c) => c is >= '\u0980' and <= '\u09FF';
}
