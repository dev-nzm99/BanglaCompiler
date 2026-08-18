using BanglaCompiler.AST;

namespace BanglaCompiler.Utils;

public static class AstPrinter
{
    private const string IndentUnit = "  ";

    public static void Print(ProgramNode program, TextWriter writer)
    {
        writer.WriteLine("Program");
        foreach (StatementNode statement in program.Statements)
        {
            PrintStatement(statement, writer, 1);
        }
    }

    private static void PrintStatement(StatementNode statement, TextWriter writer, int level)
    {
        switch (statement)
        {
            case DeclarationNode decl:
                WriteLine(writer, level, $"Declaration ({decl.DeclaredType} {decl.Identifier})");
                PrintExpression(decl.Initializer, writer, level + 1);
                break;

            case AssignmentNode assign:
                WriteLine(writer, level, $"Assignment ({assign.Identifier} =)");
                PrintExpression(assign.Value, writer, level + 1);
                break;

            case IfNode ifNode:
                WriteLine(writer, level, "If");
                PrintCondition(ifNode.Condition, writer, level + 1);
                WriteLine(writer, level + 1, "Then:");
                PrintStatements(ifNode.ThenBody, writer, level + 2);
                if (ifNode.ElseBody is not null)
                {
                    WriteLine(writer, level + 1, "Else:");
                    PrintStatements(ifNode.ElseBody, writer, level + 2);
                }
                break;

            case WhileNode whileNode:
                WriteLine(writer, level, "While");
                PrintCondition(whileNode.Condition, writer, level + 1);
                WriteLine(writer, level + 1, "Body:");
                PrintStatements(whileNode.Body, writer, level + 2);
                break;

            case PrintNode print:
                WriteLine(writer, level, "Print");
                PrintExpression(print.Value, writer, level + 1);
                break;

            default:
                WriteLine(writer, level, $"<unknown statement: {statement.GetType().Name}>");
                break;
        }
    }

    private static void PrintStatements(List<StatementNode> statements, TextWriter writer, int level)
    {
        if (statements.Count == 0)
        {
            WriteLine(writer, level, "(empty)");
            return;
        }

        foreach (StatementNode statement in statements)
        {
            PrintStatement(statement, writer, level);
        }
    }

    private static void PrintCondition(ConditionNode condition, TextWriter writer, int level)
    {
        WriteLine(writer, level, $"Condition ({condition.Operator})");
        PrintExpression(condition.Left, writer, level + 1);
        PrintExpression(condition.Right, writer, level + 1);
    }

    private static void PrintExpression(ExpressionNode expression, TextWriter writer, int level)
    {
        switch (expression)
        {
            case LiteralNode literal:
                WriteLine(writer, level, $"Literal {literal.RawLexeme} ({(literal.IsFloat ? "ভগ্নাংশ" : "সংখ্যা")})");
                break;

            case IdentifierNode identifier:
                WriteLine(writer, level, $"Identifier {identifier.Name}");
                break;

            case UnaryExpressionNode unary:
                WriteLine(writer, level, $"Unary ({unary.Operator})");
                PrintExpression(unary.Operand, writer, level + 1);
                break;

            case BinaryExpressionNode binary:
                WriteLine(writer, level, $"Binary ({binary.Operator})");
                PrintExpression(binary.Left, writer, level + 1);
                PrintExpression(binary.Right, writer, level + 1);
                break;

            default:
                WriteLine(writer, level, $"<unknown expression: {expression.GetType().Name}>");
                break;
        }
    }

    private static void WriteLine(TextWriter writer, int level, string text)
    {
        for (int i = 0; i < level; i++)
        {
            writer.Write(IndentUnit);
        }
        writer.WriteLine(text);
    }
}
