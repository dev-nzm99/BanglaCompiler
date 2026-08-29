using System.Diagnostics;
using System.Text;
using BanglaCompiler.CodeGeneration;
using BanglaCompiler.Errors;
using BanglaCompiler.Semantic;
using BanglaCompiler.Utils;
using SohojLexer = BanglaCompiler.Lexer.Lexer;
using SohojParser = BanglaCompiler.Parser.Parser;
using SohojToken = BanglaCompiler.Lexer.Token;

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
        Console.WriteLine($"Compiling: {sourcePath}");
        Console.WriteLine();

        var reporter = new ErrorReporter();

        // ---- Phase 1: Lexical analysis --------------------------------
        int before = reporter.Count;
        List<SohojToken> tokens = new SohojLexer(source, reporter).Tokenize();
        if (showTokens)
        {
            PrintTokens(tokens);
        }
        PrintPhaseStatus(1, "Lexical analysis", reporter, before);

        // ---- Phase 2: Parsing ------------------------------------------
        before = reporter.Count;
        var ast = new SohojParser(tokens, reporter).Parse();
        if (showAst)
        {
            Console.WriteLine();
            AstPrinter.Print(ast, Console.Out);
            Console.WriteLine();
        }
        PrintPhaseStatus(2, "Parsing", reporter, before);

        // ---- Phase 3: Semantic analysis ---------------------------------
        before = reporter.Count;
        SymbolTable symbols = new SemanticAnalyzer(reporter).Analyze(ast);
        PrintPhaseStatus(3, "Semantic analysis", reporter, before);
        _ = symbols; 

        if (reporter.HasErrors)
        {
            Console.WriteLine();
            Console.WriteLine($"Compilation failed: {reporter.Count} error(s) found.");
            Console.WriteLine();
            reporter.PrintAll(Console.Out);
            return 1;
        }

        if (checkOnly)
        {
            Console.WriteLine();
            Console.WriteLine("Check completed successfully: no lexical, syntax, or semantic errors found.");
            return 0;
        }

        // ---- Phase 4: Python code generation -----------------------------
        string pythonCode = new PythonCodeGenerator().Generate(ast);
        Console.WriteLine("[4/5] Python code generated.");

        // ---- Phase 5: Write output ----------------------------------------
        try
        {
            File.WriteAllText(outputPath, pythonCode, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Error: could not write output file: {ex.Message}");
            return 1;
        }

        Console.WriteLine("[5/5] Compilation successful.");
        Console.WriteLine();
        Console.WriteLine($"Output: {outputPath}");

        if (runAfterCompile)
        {
            RunGeneratedPython(outputPath);
        }

        return 0;
    }

    private static void PrintPhaseStatus(int step, string phaseName, ErrorReporter reporter, int errorCountBefore)
    {
        int newErrors = reporter.Count - errorCountBefore;
        string suffix = newErrors > 0 ? $" ({newErrors} error(s) found)" : "";
        Console.WriteLine($"[{step}/5] {phaseName} completed{suffix}.");
    }

    private static void PrintTokens(List<SohojToken> tokens)
    {
        Console.WriteLine();
        Console.WriteLine("--- Tokens ---");
        foreach (SohojToken token in tokens)
        {
            Console.WriteLine(token);
        }
        Console.WriteLine();
    }

    private static void RunGeneratedPython(string outputPath)
    {
        Console.WriteLine();
        Console.WriteLine($"Running: python {outputPath}");
        Console.WriteLine("--- program output ---");

        foreach (string candidate in new[] { "python3", "python" })
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = candidate,
                    UseShellExecute = false,
                };
                startInfo.ArgumentList.Add(outputPath);

                using Process? process = Process.Start(startInfo);
                process?.WaitForExit();
                return; 
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // This candidate executable isn't installed/on PATH 
            }
        }

        Console.WriteLine();
        Console.WriteLine("Python runtime not found.");
        Console.WriteLine("Generated code was still written successfully.");
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
