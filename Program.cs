using System.Text;

namespace BanglaCompiler;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        string sourcePath = args[0];
        string? outputPath = null;
        bool showTokens = false;
        bool showAst = false;
        bool checkOnly = false;
        bool runAfterCompile = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: -o requires an output file path.");
                        return 1;
                    }
                    outputPath = args[++i];
                    break;

                case "--tokens":
                    showTokens = true;
                    break;

                case "--ast":
                    showAst = true;
                    break;

                case "--check":
                    checkOnly = true;
                    break;

                case "--run":
                    runAfterCompile = true;
                    break;

                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;

                default:
                    Console.Error.WriteLine($"Warning: unrecognized option '{args[i]}' ignored.");
                    break;
            }
        }

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Error: source file not found: {sourcePath}");
            return 1;
        }

        outputPath ??= Path.ChangeExtension(sourcePath, ".py");

        string source;
        try
        {
            source = File.ReadAllText(sourcePath, Encoding.UTF8);
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Error: could not read source file: {ex.Message}");
            return 1;
        }

        return Compile(source, sourcePath, outputPath, showTokens, showAst, checkOnly, runAfterCompile);
    }

  
    private static int Compile(
        string source,
        string sourcePath,
        string outputPath,
        bool showTokens,
        bool showAst,
        bool checkOnly,
        bool runAfterCompile)
    {
       
    }

    private static void PrintUsage()
    {
        Console.WriteLine("BanglaCompiler — Sohoj (সহজ) to Python compiler");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run -- <source.bl> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o <file>   Write generated Python to <file> (default: <source>.py)");
        Console.WriteLine("  --tokens    Print the token stream produced by the lexer");
        Console.WriteLine("  --ast       Print the abstract syntax tree produced by the parser");
        Console.WriteLine("  --check     Run lexing/parsing/semantic analysis only; do not generate code");
        Console.WriteLine("  --run       After a successful compile, run the generated Python with python3/python");
        Console.WriteLine("  -h, --help  Show this help text");
    }
}
