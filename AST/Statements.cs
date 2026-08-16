namespace BanglaCompiler.AST;


public sealed class DeclarationNode : StatementNode
{
    public string DeclaredType { get; }   // "সংখ্যা" or "ভগ্নাংশ"
    public string Identifier { get; }
    public ExpressionNode Initializer { get; }

    public DeclarationNode(string declaredType, string identifier, ExpressionNode initializer, int line, int column)
        : base(line, column)
    {
        DeclaredType = declaredType;
        Identifier = identifier;
        Initializer = initializer;
    }
}


public sealed class AssignmentNode : StatementNode
{
    public string Identifier { get; }
    public ExpressionNode Value { get; }

    public AssignmentNode(string identifier, ExpressionNode value, int line, int column) : base(line, column)
    {
        Identifier = identifier;
        Value = value;
    }
}


public sealed class IfNode : StatementNode
{
    public ConditionNode Condition { get; }
    public List<StatementNode> ThenBody { get; }
    public List<StatementNode>? ElseBody { get; }

    public IfNode(ConditionNode condition, List<StatementNode> thenBody, List<StatementNode>? elseBody, int line, int column)
        : base(line, column)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElseBody = elseBody;
    }
}


public sealed class WhileNode : StatementNode
{
    public ConditionNode Condition { get; }
    public List<StatementNode> Body { get; }

    public WhileNode(ConditionNode condition, List<StatementNode> body, int line, int column) : base(line, column)
    {
        Condition = condition;
        Body = body;
    }
}


public sealed class PrintNode : StatementNode
{
    public ExpressionNode Value { get; }

    public PrintNode(ExpressionNode value, int line, int column) : base(line, column)
    {
        Value = value;
    }
}
