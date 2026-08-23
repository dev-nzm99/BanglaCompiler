using System.Linq;

namespace BanglaCompiler.Errors;


public sealed class ErrorReporter
{
    private readonly List<CompilerError> _errors = new();

    public IReadOnlyList<CompilerError> Errors => _errors;

    public bool HasErrors => _errors.Count > 0;

    public int Count => _errors.Count;

    public void Report(CompilerError error) => _errors.Add(error);

    public void ReportLexical(string message, int line, int column, string? lexeme = null) =>
        Report(new CompilerError(ErrorType.Lexical, message, line, column, lexeme));

    public void ReportSyntax(string message, int line, int column, string? lexeme = null) =>
        Report(new CompilerError(ErrorType.Syntax, message, line, column, lexeme));

    public void ReportSemantic(string message, int line, int column, string? lexeme = null) =>
        Report(new CompilerError(ErrorType.Semantic, message, line, column, lexeme));

    public IEnumerable<CompilerError> InSourceOrder() =>
        _errors.OrderBy(e => e.Line).ThenBy(e => e.Column);

    public void PrintAll(TextWriter writer)
    {
        foreach (CompilerError error in InSourceOrder())
        {
            writer.WriteLine(error.ToString());
            writer.WriteLine();
        }
    }
}
