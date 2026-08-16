namespace BanglaCompiler.Lexer;

/// <summary>
/// Every distinct kind of lexeme the Sohoj lexer can produce.
///
/// Compiler theory note: a "token type" (also called a token class) groups
/// together all lexemes that the parser should treat identically from a
/// grammar point of view. For example every integer the programmer writes
/// (10, 42, 0, 999...) is a different *lexeme* but the same *token type*
/// (IntegerLiteral) — the parser's grammar rules only ever reason about
/// token types, never about specific lexeme text (except for keywords,
/// where the lexeme literally decides the type — see Lexer.cs).
/// </summary>
public enum TokenType
{
    // --- Literals & identifiers ---
    Identifier,
    IntegerLiteral,
    FloatLiteral,

    // --- Keywords (each Sohoj keyword gets its own token type so the
    //     parser can dispatch on TokenType directly instead of comparing
    //     lexeme strings everywhere; conceptually these are still exactly
    //     the "Keyword" token category described in the language spec) ---
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
    Invalid,       // an unrecognized character/word; lexer reports an error
                    // but still emits this token so parsing/error-recovery
                    // can continue instead of the compiler crashing.
    EndOfFile,
}
