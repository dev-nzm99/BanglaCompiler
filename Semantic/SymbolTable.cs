namespace BanglaCompiler.Semantic;


public sealed class SymbolTable
{
    private readonly Dictionary<string, Symbol> _symbols = new();


    public bool Contains(string name) => _symbols.ContainsKey(name);


    public bool TryDeclare(Symbol symbol)
    {
        if (_symbols.ContainsKey(symbol.Name))
        {
            return false;
        }

        _symbols[symbol.Name] = symbol;
        return true;
    }

    public bool TryLookup(string name, out Symbol? symbol) => _symbols.TryGetValue(name, out symbol);

    public IReadOnlyCollection<Symbol> AllSymbols => _symbols.Values;
}
