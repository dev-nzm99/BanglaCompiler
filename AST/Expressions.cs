using BanglaCompiler.Lexer;

namespace BanglaCompiler.AST;


public sealed class LiteralNode : ExpressionNode
{
    public bool IsFloat { get; }
    public string RawLexeme { get; }

    public LiteralNode(string rawLexeme, bool isFloat, int line, int column) : base(line, column)
    {
        RawLexeme = rawLexeme;
        IsFloat = isFloat;
    }
}

public sealed class IdentifierNode : ExpressionNode
{
    public string Name { get; }

    public IdentifierNode(string name, int line, int column) : base(line, column)
    {
        Name = name;
    }
}

public sealed class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public TokenType Operator { get; } // Plus, Minus, Multiply, or Divide
    public ExpressionNode Right { get; }

    public BinaryExpressionNode(ExpressionNode left, TokenType op, ExpressionNode right, int line, int column)
        : base(line, column)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}


public sealed class UnaryExpressionNode : ExpressionNode
{
    public TokenType Operator { get; } // always Minus
    public ExpressionNode Operand { get; }

    public UnaryExpressionNode(TokenType op, ExpressionNode operand, int line, int column) : base(line, column)
    {
        Operator = op;
        Operand = operand;
    }
}


public sealed class ConditionNode : AstNode
{
    public ExpressionNode Left { get; }
    public TokenType Operator { get; } // Greater, Less, GreaterEqual, LessEqual, Equal, NotEqual
    public ExpressionNode Right { get; }

    public ConditionNode(ExpressionNode left, TokenType op, ExpressionNode right, int line, int column)
        : base(line, column)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
}
