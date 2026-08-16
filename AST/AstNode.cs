namespace BanglaCompiler.AST;


public abstract class AstNode
{
    public int Line { get; }
    public int Column { get; }

    protected AstNode(int line, int column)
    {
        Line = line;
        Column = column;
    }
}

public abstract class StatementNode : AstNode
{
    protected StatementNode(int line, int column) : base(line, column) { }
}

public abstract class ExpressionNode : AstNode
{
    protected ExpressionNode(int line, int column) : base(line, column) { }
}


public sealed class ProgramNode : AstNode
{
    public List<StatementNode> Statements { get; }

    public ProgramNode(List<StatementNode> statements, int line = 1, int column = 1) : base(line, column)
    {
        Statements = statements;
    }
}
