using BanglaCompiler.AST;
using BanglaCompiler.Errors;
using BanglaCompiler.Lexer;

namespace BanglaCompiler.Parser;

/// <summary>
/// Hand-written Recursive Descent Parser for Sohoj.
///
/// Compiler theory note: a recursive descent parser implements a grammar
/// with one method per non-terminal — literally "recursive" because rules
/// like &lt;expression&gt; refer to &lt;term&gt; which refers back to
/// &lt;expression&gt; (via parenthesized sub-expressions), and the C# call
/// stack mirrors that recursion directly. It's the most direct, most
/// teachable way to turn a grammar into working code, which is exactly why
/// it's the right choice for a compiler-construction course project: every
/// method below can be read side-by-side with the matching rule in
/// docs/grammar.md.
///
/// Operator precedence (docs/grammar.md) is implemented structurally, not
/// with precedence numbers or tables: ParseExpression calls ParseTerm which
/// calls ParseFactor, so "*" and "/" are always parsed one level deeper
/// (tighter-binding) than "+" and "-" simply because of which method calls
/// which. This is the standard "precedence climbing via grammar layering"
/// technique.
///
/// PART 6 UPDATE: syntax errors are now reported through the shared
/// <see cref="ErrorReporter"/> instead of the Parser's own ParseError/
/// ParseResult types used in Part 4. Parse() now returns a ProgramNode
/// directly; callers check errorReporter.HasErrors to decide whether to
/// proceed to semantic analysis / code generation. The panic-mode recovery
/// behavior (Synchronize) is unchanged from Part 4.
/// </summary>
public sealed class Parser
{
    /// <summary>Internal control-flow exception used to unwind out of a broken statement/expression
    /// so the enclosing statement-list loop can record the error and resynchronize. This never
    /// escapes the Parser class — Parse() always returns a ProgramNode, never throws.</summary>
    private sealed class ParseException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public ParseException(string message, int line, int column) : base(message)
        {
            Line = line;
            Column = column;
        }
    }

    private readonly List<Token> _tokens;
    private readonly ErrorReporter _errorReporter;
    private int _pos;

    private static readonly HashSet<TokenType> ComparisonOperators = new()
    {
        TokenType.Greater, TokenType.Less, TokenType.GreaterEqual,
        TokenType.LessEqual, TokenType.Equal, TokenType.NotEqual,
    };

    public Parser(List<Token> tokens, ErrorReporter errorReporter)
    {
        _tokens = tokens;
        _errorReporter = errorReporter;
    }

    /// <summary>Parses the full token stream produced by the Lexer into a ProgramNode. Syntax
    /// errors are reported into the ErrorReporter passed to the constructor; parsing never throws
    /// out of this method — it always returns the best ProgramNode it could recover.</summary>
    public ProgramNode Parse()
    {
        var statements = new List<StatementNode>();

        while (!IsAtEnd())
        {
            try
            {
                statements.Add(ParseStatement());
            }
            catch (ParseException ex)
            {
                _errorReporter.ReportSyntax(ex.Message, ex.Line, ex.Column);
                Synchronize(insideBlock: false);
            }
        }

        return new ProgramNode(statements);
    }

    // -----------------------------------------------------------------
    // <statement> ::= <declaration> | <assignment> | <if-statement>
    //               | <while-statement> | <print-statement>
    // -----------------------------------------------------------------
    private StatementNode ParseStatement()
    {
        return Current.Type switch
        {
            TokenType.KeywordSongkha or TokenType.KeywordVognangsho => ParseDeclaration(),
            TokenType.Identifier => ParseAssignment(),
            TokenType.KeywordJodi => ParseIfStatement(),
            TokenType.KeywordJotokhon => ParseWhileStatement(),
            TokenType.KeywordDekhao => ParsePrintStatement(),
            _ => throw new ParseException(
                    $"Unexpected token '{Current.Lexeme}'. Expected a declaration (সংখ্যা/ভগ্নাংশ), " +
                    $"an assignment, যদি, যতক্ষণ, or দেখাও.",
                    Current.Line, Current.Column),
        };
    }

    // <declaration> ::= <type> <identifier> "=" <expression> ";"
    private StatementNode ParseDeclaration()
    {
        Token typeToken = Advance(); // KeywordSongkha or KeywordVognangsho, guaranteed by ParseStatement's dispatch
        Token nameToken = Expect(TokenType.Identifier, "Expected a variable name after the type.");
        Expect(TokenType.Assign, "Expected '=' after the variable name in a declaration.");
        ExpressionNode initializer = ParseExpression();
        Expect(TokenType.Semicolon, "Expected ';' after variable declaration.");

        return new DeclarationNode(typeToken.Lexeme, nameToken.Lexeme, initializer, typeToken.Line, typeToken.Column);
    }

    // <assignment> ::= <identifier> "=" <expression> ";"
    private StatementNode ParseAssignment()
    {
        Token nameToken = Advance(); // Identifier, guaranteed by ParseStatement's dispatch
        Expect(TokenType.Assign, "Expected '=' after identifier in assignment.");
        ExpressionNode value = ParseExpression();
        Expect(TokenType.Semicolon, "Expected ';' after assignment.");

        return new AssignmentNode(nameToken.Lexeme, value, nameToken.Line, nameToken.Column);
    }

    // <if-statement> ::= "যদি" "(" <condition> ")" <block> [ "অন্যথায়" <block> ]
    private StatementNode ParseIfStatement()
    {
        Token ifToken = Advance(); // KeywordJodi
        Expect(TokenType.LeftParen, "Expected '(' after যদি.");
        ConditionNode condition = ParseCondition();
        Expect(TokenType.RightParen, "Expected ')' after যদি condition.");

        List<StatementNode> thenBody = ParseBlock();
        List<StatementNode>? elseBody = null;

        if (Check(TokenType.KeywordOnnothay))
        {
            Advance();
            elseBody = ParseBlock();
        }

        return new IfNode(condition, thenBody, elseBody, ifToken.Line, ifToken.Column);
    }

    // <while-statement> ::= "যতক্ষণ" "(" <condition> ")" <block>
    private StatementNode ParseWhileStatement()
    {
        Token whileToken = Advance(); // KeywordJotokhon
        Expect(TokenType.LeftParen, "Expected '(' after যতক্ষণ.");
        ConditionNode condition = ParseCondition();
        Expect(TokenType.RightParen, "Expected ')' after যতক্ষণ condition.");

        List<StatementNode> body = ParseBlock();

        return new WhileNode(condition, body, whileToken.Line, whileToken.Column);
    }

    // <print-statement> ::= "দেখাও" "(" <expression> ")" ";"
    private StatementNode ParsePrintStatement()
    {
        Token printToken = Advance(); // KeywordDekhao
        Expect(TokenType.LeftParen, "Expected '(' after দেখাও.");
        ExpressionNode value = ParseExpression();
        Expect(TokenType.RightParen, "Expected ')' after দেখাও argument.");
        Expect(TokenType.Semicolon, "Expected ';' after দেখাও statement.");

        return new PrintNode(value, printToken.Line, printToken.Column);
    }

    // <block> ::= "{" <statement>* "}"
    private List<StatementNode> ParseBlock()
    {
        Expect(TokenType.LeftBrace, "Expected '{' to start a block.");

        var statements = new List<StatementNode>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            try
            {
                statements.Add(ParseStatement());
            }
            catch (ParseException ex)
            {
                _errorReporter.ReportSyntax(ex.Message, ex.Line, ex.Column);
                Synchronize(insideBlock: true);
            }
        }

        Expect(TokenType.RightBrace, "Expected '}' to close a block.");
        return statements;
    }

    // <condition> ::= <expression> <comparison-op> <expression>
    private ConditionNode ParseCondition()
    {
        ExpressionNode left = ParseExpression();

        if (!ComparisonOperators.Contains(Current.Type))
        {
            throw new ParseException(
                "Expected a comparison operator (>, <, >=, <=, ==, !=) in condition.",
                Current.Line, Current.Column);
        }

        Token opToken = Advance();
        ExpressionNode right = ParseExpression();

        return new ConditionNode(left, opToken.Type, right, left.Line, left.Column);
    }

    // <expression> ::= <term> ( ( "+" | "-" ) <term> )*
    private ExpressionNode ParseExpression()
    {
        ExpressionNode expr = ParseTerm();

        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            Token op = Advance();
            ExpressionNode right = ParseTerm();
            expr = new BinaryExpressionNode(expr, op.Type, right, expr.Line, expr.Column);
        }

        return expr;
    }

    // <term> ::= <factor> ( ( "*" | "/" ) <factor> )*
    private ExpressionNode ParseTerm()
    {
        ExpressionNode expr = ParseFactor();

        while (Check(TokenType.Multiply) || Check(TokenType.Divide))
        {
            Token op = Advance();
            ExpressionNode right = ParseFactor();
            expr = new BinaryExpressionNode(expr, op.Type, right, expr.Line, expr.Column);
        }

        return expr;
    }

    // <factor> ::= <integer-literal> | <float-literal> | <identifier>
    //            | "(" <expression> ")" | "-" <factor>
    private ExpressionNode ParseFactor()
    {
        if (Check(TokenType.Minus))
        {
            Token op = Advance();
            ExpressionNode operand = ParseFactor(); // right-associative unary minus, e.g. "--x" would nest (not that lexer permits "--" anyway)
            return new UnaryExpressionNode(op.Type, operand, op.Line, op.Column);
        }

        if (Check(TokenType.IntegerLiteral))
        {
            Token t = Advance();
            return new LiteralNode(t.Lexeme, isFloat: false, t.Line, t.Column);
        }

        if (Check(TokenType.FloatLiteral))
        {
            Token t = Advance();
            return new LiteralNode(t.Lexeme, isFloat: true, t.Line, t.Column);
        }

        if (Check(TokenType.Identifier))
        {
            Token t = Advance();
            return new IdentifierNode(t.Lexeme, t.Line, t.Column);
        }

        if (Check(TokenType.LeftParen))
        {
            Advance();
            ExpressionNode expr = ParseExpression();
            Expect(TokenType.RightParen, "Expected ')' after expression.");
            return expr;
        }

        throw new ParseException(
            $"Unexpected token '{Current.Lexeme}' in expression. Expected a number, identifier, or '('.",
            Current.Line, Current.Column);
    }

    // -----------------------------------------------------------------
    // Panic-mode error recovery
    // -----------------------------------------------------------------

    /// <summary>
    /// Compiler theory note: "panic mode" recovery discards tokens after a
    /// syntax error until it finds a position it's confident is a safe
    /// place to resume parsing — here, either just after a semicolon (the
    /// end of whatever statement was broken) or at a token that clearly
    /// starts a new statement (a keyword). This is what satisfies "error
    /// recovery should skip to semicolon or end of line where appropriate":
    /// we skip to the nearest statement boundary rather than aborting the
    /// whole compilation on the first mistake.
    ///
    /// Two correctness properties matter a lot more here than they might
    /// first appear, and both are covered by tests in Part 9:
    ///
    /// 1. NEVER discard a token that's already a safe resync point. A very
    ///    common failure case is a missing semicolon, e.g. "সংখ্যা x = 10"
    ///    followed immediately by "সংখ্যা y = 20;" — the error is raised
    ///    with Current already sitting on the KeywordSongkha that starts
    ///    the next, perfectly valid declaration. So every stop condition
    ///    below is checked BEFORE consuming anything.
    ///
    /// 2. ALWAYS make forward progress, or return only via a check against
    ///    the CURRENT token — never resume based on stale state (like "the
    ///    previously consumed token happened to be a semicolon", which may
    ///    have belonged to a completely unrelated, already-finished
    ///    statement). An earlier draft of this method checked
    ///    Previous().Type == Semicolon on entry, which could be stale in
    ///    exactly this way — for a stray/unmatched '}' encountered outside
    ///    any block, it caused Synchronize to return immediately, forever,
    ///    without ever consuming the offending brace, hanging the compiler
    ///    in an infinite loop. The loop below instead only recognizes a
    ///    semicolon-boundary immediately AFTER consuming it in THIS call,
    ///    guaranteeing every iteration either returns from checking Current
    ///    (no state risk) or performs an Advance() (guaranteed progress).
    ///
    /// <paramref name="insideBlock"/> distinguishes the two places this is
    /// called from: inside ParseBlock, a '}' is this block's own closing
    /// brace and should be left for ParseBlock's Expect(RightBrace) to
    /// consume; at the top level (Parse()), there is no enclosing block to
    /// consume a '}', so a stray one there must be treated as garbage and
    /// skipped instead — otherwise it can never be resolved.
    /// </summary>
    private void Synchronize(bool insideBlock)
    {
        while (!IsAtEnd())
        {
            switch (Current.Type)
            {
                case TokenType.KeywordSongkha:
                case TokenType.KeywordVognangsho:
                case TokenType.KeywordJodi:
                case TokenType.KeywordJotokhon:
                case TokenType.KeywordDekhao:
                    return; // next token clearly starts a new statement
            }

            if (insideBlock && Current.Type == TokenType.RightBrace)
            {
                return; // this block's closing brace — leave it for ParseBlock to consume
            }

            Token consumed = Advance(); // not yet at a safe point — discard and keep looking; guarantees progress
            if (consumed.Type == TokenType.Semicolon)
            {
                return; // just consumed a statement-ending semicolon — safe to resume right after it
            }
        }
    }

    // -----------------------------------------------------------------
    // Token stream helpers
    // -----------------------------------------------------------------

    private Token Current => _tokens[_pos];

    private Token Previous() => _tokens[_pos - 1];

    private bool IsAtEnd() => Current.Type == TokenType.EndOfFile;

    private bool Check(TokenType type) => Current.Type == type;

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            _pos++;
        }
        return Previous();
    }

    /// <summary>Consumes the current token if it matches <paramref name="type"/>, otherwise
    /// throws a ParseException with the given message — this is how every grammar rule
    /// enforces required tokens (closing parens, semicolons, etc.).</summary>
    private Token Expect(TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw new ParseException(message, Current.Line, Current.Column);
    }
}
