namespace BanglaCompiler.Semantic;

public sealed class Symbol
{
    public string Name { get; }
    public DataType Type { get; }
    public int DeclarationLine { get; }
    public int DeclarationColumn { get; }

    public Symbol(string name, DataType type, int declarationLine, int declarationColumn)
    {
        Name = name;
        Type = type;
        DeclarationLine = declarationLine;
        DeclarationColumn = declarationColumn;
    }
}
