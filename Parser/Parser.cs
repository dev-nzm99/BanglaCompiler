using BanglaCompiler.AST;
using BanglaCompiler.Errors;
using BanglaCompiler.Lexer;

namespace BanglaCompiler.Parser;


public sealed class Parser
{
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

    // <statement> ::= <declaration> | <assignment> | <if-statement>
    //               | <while-statement> | <print-statement>

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
        Token typeToken = Advance(); 
        Token nameToken = Expect(TokenType.Identifier, "Expected a variable name after the type.");
        Expect(TokenType.Assign, "Expected '=' after the variable name in a declaration.");
        ExpressionNode initializer = ParseExpression();
        Expect(TokenType.Semicolon, "Expected ';' after variable declaration.");

        return new DeclarationNode(typeToken.Lexeme, nameToken.Lexeme, initializer, typeToken.Line, typeToken.Column);
    }

    // <assignment> ::= <identifier> "=" <expression> ";"
    private StatementNode ParseAssignment()
    {
        Token nameToken = Advance();
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

    // Panic-mode error recovery

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
                return; 
            }

            Token consumed = Advance(); 
            if (consumed.Type == TokenType.Semicolon)
            {
                return; 
            }
        }
    }

    // Token stream helpers

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

    private Token Expect(TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw new ParseException(message, Current.Line, Current.Column);
    }
}
