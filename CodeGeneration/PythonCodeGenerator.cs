using System.Text;
using BanglaCompiler.AST;
using BanglaCompiler.Lexer;

namespace BanglaCompiler.CodeGeneration;

public sealed class PythonCodeGenerator
{
    private const int PrecedenceAddSub = 1;
    private const int PrecedenceMulDiv = 2;
    private const int PrecedenceUnary = 3;
    private const int PrecedenceAtomic = int.MaxValue; 

    private const string IndentUnit = "    ";

    private readonly StringBuilder _output = new();
    private int _indentLevel;

    public string Generate(ProgramNode program)
    {
        _output.Clear();
        _indentLevel = 0;

        if (program.Statements.Count == 0)
        {

            EmitLine("# (empty program)");
            return _output.ToString();
        }

        foreach (StatementNode statement in program.Statements)
        {
            EmitStatement(statement);
        }

        return _output.ToString();
    }

    // Statements

    private void EmitStatement(StatementNode statement)
    {
        switch (statement)
        {
            case DeclarationNode decl:
                EmitDeclaration(decl);
                break;
            case AssignmentNode assign:
                EmitAssignment(assign);
                break;
            case IfNode ifNode:
                EmitIf(ifNode);
                break;
            case WhileNode whileNode:
                EmitWhile(whileNode);
                break;
            case PrintNode print:
                EmitPrint(print);
                break;
            default:
                throw new InvalidOperationException($"Unhandled statement node type: {statement.GetType().Name}");
        }
    }

    private void EmitDeclaration(DeclarationNode decl)
    {
        (string text, _) = EmitExpr(decl.Initializer);
        EmitLine($"{decl.Identifier} = {text}");
    }

    private void EmitAssignment(AssignmentNode assign)
    {
        (string text, _) = EmitExpr(assign.Value);
        EmitLine($"{assign.Identifier} = {text}");
    }

    private void EmitIf(IfNode ifNode)
    {
        EmitLine($"if {EmitConditionText(ifNode.Condition)}:");
        EmitBlock(ifNode.ThenBody);

        if (ifNode.ElseBody is not null)
        {
            EmitLine("else:");
            EmitBlock(ifNode.ElseBody);
        }
    }

    private void EmitWhile(WhileNode whileNode)
    {
        EmitLine($"while {EmitConditionText(whileNode.Condition)}:");
        EmitBlock(whileNode.Body);
    }

    private void EmitPrint(PrintNode print)
    {
        (string text, _) = EmitExpr(print.Value);
        EmitLine($"print({text})");
    }

    private void EmitBlock(List<StatementNode> statements)
    {
        _indentLevel++;

        if (statements.Count == 0)
        {
            EmitLine("pass");
        }
        else
        {
            foreach (StatementNode statement in statements)
            {
                EmitStatement(statement);
            }
        }

        _indentLevel--;
    }

    private void EmitLine(string text)
    {
        _output.Append(' ', _indentLevel * IndentUnit.Length);
        _output.Append(text);
        _output.Append('\n');
    }

    // Expressions & conditions

    private string EmitConditionText(ConditionNode condition)
    {
        (string leftText, _) = EmitExpr(condition.Left);
        (string rightText, _) = EmitExpr(condition.Right);
        return $"{leftText} {PythonOperator(condition.Operator)} {rightText}";
    }

    private (string Text, int Precedence) EmitExpr(ExpressionNode expression)
    {
        switch (expression)
        {
            case LiteralNode literal:
                return (literal.RawLexeme, PrecedenceAtomic);

            case IdentifierNode identifier:
                return (identifier.Name, PrecedenceAtomic);

            case UnaryExpressionNode unary:
                {
                    (string operandText, int operandPrecedence) = EmitExpr(unary.Operand);
                    if (operandPrecedence < PrecedenceUnary)
                    {
                        operandText = $"({operandText})";
                    }
                    return ($"-{operandText}", PrecedenceUnary);
                }

            case BinaryExpressionNode binary:
                {
                    int opPrecedence = (binary.Operator == TokenType.Multiply || binary.Operator == TokenType.Divide)
                        ? PrecedenceMulDiv
                        : PrecedenceAddSub;

                    (string leftText, int leftPrecedence) = EmitExpr(binary.Left);
                    (string rightText, int rightPrecedence) = EmitExpr(binary.Right);

                    if (leftPrecedence < opPrecedence)
                    {
                        leftText = $"({leftText})";
                    }
                    if (rightPrecedence <= opPrecedence)
                    {
                        rightText = $"({rightText})";
                    }

                    return ($"{leftText} {PythonOperator(binary.Operator)} {rightText}", opPrecedence);
                }

            default:
                throw new InvalidOperationException($"Unhandled expression node type: {expression.GetType().Name}");
        }
    }

    private static string PythonOperator(TokenType op) => op switch
    {
        TokenType.Plus => "+",
        TokenType.Minus => "-",
        TokenType.Multiply => "*",
        TokenType.Divide => "/",
        TokenType.Greater => ">",
        TokenType.Less => "<",
        TokenType.GreaterEqual => ">=",
        TokenType.LessEqual => "<=",
        TokenType.Equal => "==",
        TokenType.NotEqual => "!=",
        _ => throw new InvalidOperationException($"No Python operator mapping exists for token type {op}."),
    };
}
