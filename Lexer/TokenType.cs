namespace BanglaCompiler.Lexer;

public enum TokenType
{
    // --- Literals & identifiers ---
    Identifier,
    IntegerLiteral,
    FloatLiteral,

    KeywordSongkha,      // সংখ্যা   - int type
    KeywordVognangsho,   // ভগ্নাংশ  - float type
    KeywordJodi,         // যদি      - if
    KeywordOnnothay,     // অন্যথায় - else
    KeywordJotokhon,     // যতক্ষণ   - while
    KeywordDekhao,       // দেখাও    - print

    // --- Arithmetic operators ---
    Plus,       // +
    Minus,      // -
    Multiply,   // *
    Divide,     // /

    // --- Assignment & comparison operators ---
    Assign,        // =
    Equal,         // ==
    NotEqual,      // !=
    Greater,       // >
    Less,          // <
    GreaterEqual,  // >=
    LessEqual,     // <=

    // --- Grouping & punctuation ---
    LeftParen,     // (
    RightParen,    // )
    LeftBrace,     // {
    RightBrace,    // }
    Semicolon,     // ;

    // --- Special ---
    Invalid,       

    EndOfFile,
}
