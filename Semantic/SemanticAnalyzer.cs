using BanglaCompiler.AST;
using BanglaCompiler.Errors;
using BanglaCompiler.Lexer;

namespace BanglaCompiler.Semantic;


public sealed class SemanticAnalyzer
{
    private readonly ErrorReporter _errorReporter;
    private readonly SymbolTable _symbols = new();

    public SemanticAnalyzer(ErrorReporter errorReporter)
    {
        _errorReporter = errorReporter;
    }



    public SymbolTable Analyze(ProgramNode program)
    {
        foreach (StatementNode statement in program.Statements)
        {
            AnalyzeStatement(statement);
        }

        return _symbols;
    }


    // Statements

    private void AnalyzeStatement(StatementNode statement)
    {
        switch (statement)
        {
            case DeclarationNode decl:
                AnalyzeDeclaration(decl);
                break;
            case AssignmentNode assign:
                AnalyzeAssignment(assign);
                break;
            case IfNode ifNode:
                AnalyzeIf(ifNode);
                break;
            case WhileNode whileNode:
                AnalyzeWhile(whileNode);
                break;
            case PrintNode print:
                InferType(print.Value); // walked purely to catch undefined-variable errors inside it
                break;
            default:
                throw new InvalidOperationException($"Unhandled statement node type: {statement.GetType().Name}");
        }
    }



    private void AnalyzeDeclaration(DeclarationNode decl)
    {
        DataType declaredType = DataTypeExtensions.FromKeyword(decl.DeclaredType);
        DataType initializerType = InferType(decl.Initializer);

        CheckAssignmentCompatibility(declaredType, initializerType, decl.Identifier, decl.Line, decl.Column);

        var symbol = new Symbol(decl.Identifier, declaredType, decl.Line, decl.Column);
        if (!_symbols.TryDeclare(symbol))
        {
            _errorReporter.ReportSemantic(
                $"Variable '{decl.Identifier}' is already declared.",
                decl.Line, decl.Column, decl.Identifier);
        }
    }

    private void AnalyzeAssignment(AssignmentNode assign)
    {
        DataType valueType = InferType(assign.Value);

        if (!_symbols.TryLookup(assign.Identifier, out Symbol? symbol))
        {
            _errorReporter.ReportSemantic(
                $"Undefined variable '{assign.Identifier}'. Variables must be declared with সংখ্যা or ভগ্নাংশ before use.",
                assign.Line, assign.Column, assign.Identifier);
            return; 
        }

        CheckAssignmentCompatibility(symbol!.Type, valueType, assign.Identifier, assign.Line, assign.Column);
    }


    private void AnalyzeIf(IfNode ifNode)
    {
        AnalyzeCondition(ifNode.Condition);

        foreach (StatementNode statement in ifNode.ThenBody)
        {
            AnalyzeStatement(statement);
        }

        if (ifNode.ElseBody is not null)
        {
            foreach (StatementNode statement in ifNode.ElseBody)
            {
                AnalyzeStatement(statement);
            }
        }
    }

    private void AnalyzeWhile(WhileNode whileNode)
    {
        AnalyzeCondition(whileNode.Condition);

        foreach (StatementNode statement in whileNode.Body)
        {
            AnalyzeStatement(statement);
        }
    }


    private void AnalyzeCondition(ConditionNode condition)
    {
        InferType(condition.Left);
        InferType(condition.Right);
    }

    // Expressions

    private DataType InferType(ExpressionNode expression)
    {
        switch (expression)
        {
            case LiteralNode literal:
                return literal.IsFloat ? DataType.Float : DataType.Integer;

            case IdentifierNode identifier:
                if (_symbols.TryLookup(identifier.Name, out Symbol? symbol))
                {
                    return symbol!.Type;
                }

                _errorReporter.ReportSemantic(
                    $"Undefined variable '{identifier.Name}'. Variables must be declared with সংখ্যা or ভগ্নাংশ before use.",
                    identifier.Line, identifier.Column, identifier.Name);
                return DataType.Unknown;

            case UnaryExpressionNode unary:
                // Unary minus never changes the operand's numeric type (-x is
                // সংখ্যা if x is সংখ্যা, ভগ্নাংশ if x is ভগ্নাংশ).
                return InferType(unary.Operand);

            case BinaryExpressionNode binary:
                return InferBinaryType(binary);

            default:
                throw new InvalidOperationException($"Unhandled expression node type: {expression.GetType().Name}");
        }
    }



    private DataType InferBinaryType(BinaryExpressionNode binary)
    {
        DataType leftType = InferType(binary.Left);
        DataType rightType = InferType(binary.Right);

        if (binary.Operator == TokenType.Divide && IsZeroLiteral(binary.Right))
        {
            _errorReporter.ReportSemantic(
                "Division by zero: the right-hand side of '/' is a literal 0. " +
                "(This is only caught here because it's a literal; দেখাও(x / y) where y turns out " +
                "to be zero at runtime cannot be detected at compile time and will fail when the " +
                "generated Python program runs.)",
                binary.Line, binary.Column);
        }

        if (leftType == DataType.Unknown || rightType == DataType.Unknown)
        {
            return DataType.Unknown; // an error was already reported for the sub-expression; don't cascade
        }

        return (leftType == DataType.Float || rightType == DataType.Float) ? DataType.Float : DataType.Integer;
    }

    private static bool IsZeroLiteral(ExpressionNode expression)
    {
        return expression is LiteralNode literal
            && double.TryParse(literal.RawLexeme, System.Globalization.CultureInfo.InvariantCulture, out double value)
            && value == 0.0;
    }


    private void CheckAssignmentCompatibility(DataType targetType, DataType valueType, string variableName, int line, int column)
    {
        if (targetType == DataType.Unknown || valueType == DataType.Unknown)
        {
            return;
        }

        if (targetType == DataType.Integer && valueType == DataType.Float)
        {
            _errorReporter.ReportSemantic(
                $"Type error: cannot assign a {valueType.ToDisplayName()} value to '{variableName}', " +
                $"which is declared as {targetType.ToDisplayName()}. Assigning a fractional value to an " +
                $"integer variable would lose precision (narrowing is not allowed); declare '{variableName}' " +
                $"as ভগ্নাংশ instead, or convert the value explicitly.",
                line, column, variableName);
        }
    }
}
