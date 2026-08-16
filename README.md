# Language Specification — সহজ (Sohoj)

**Sohoj** (সহজ, Bangla for "simple") is a small, original, statically-typed
imperative programming language whose source is written using Bangla-script
keywords. The Sohoj compiler (this project, `BanglaCompiler`) translates
`.bl` source files into equivalent, runnable Python 3 source code.

## 1. File Extension

Sohoj source files use the extension **`.bl`** (e.g. `hello.bl`, `loop.bl`).

## 2. Character Set

Source files are UTF-8 encoded. Keywords are Bangla-script Unicode sequences.
Identifiers use ASCII letters (`a`-`z`, `A`-`Z`), digits, and underscore,
starting with a letter or underscore. (Restricting identifiers to ASCII keeps
the lexer's identifier-vs-keyword disambiguation simple and unambiguous for a
first compiler project, while keywords themselves remain authentically
Bangla.)

## 3. Data Types

Sohoj has exactly two built-in data types:

| Keyword     | Meaning         | Maps to Python | Example literal |
|-------------|-----------------|-----------------|------------------|
| `সংখ্যা`    | integer (`int`) | `int`           | `10`, `-3`, `0`  |
| `ভগ্নাংশ`   | float (`double`)| `float`         | `5.5`, `-2.0`    |

Both types support the four arithmetic operators and the six comparison
operators. Mixed `সংখ্যা`/`ভগ্নাংশ` arithmetic is allowed and always
produces `ভগ্নাংশ` (matching Python's own int/float promotion rules), exactly
like C, Java, or Python.

## 4. Keywords

| Keyword       | Role                          |
|---------------|--------------------------------|
| `সংখ্যা`      | integer type                  |
| `ভগ্নাংশ`     | float type                    |
| `যদি`         | `if`                           |
| `অন্যথায়`    | `else`                         |
| `যতক্ষণ`      | `while`                        |
| `দেখাও`       | `print`                        |

Keywords are reserved words; they cannot be used as identifiers.

## 5. Statements

### 5.1 Declaration

```
সংখ্যা x = 10;
ভগ্নাংশ y = 5.5;
```

A declaration introduces a new variable with a fixed type. Re-declaring an
already-declared name in the same scope is a semantic error.

### 5.2 Assignment

```
x = 20;
x = x + 5;
```

Assigning to an undeclared variable is a semantic error. Assigning a
`ভগ্নাংশ` expression to a `সংখ্যা` variable is a type error (narrowing is
disallowed); assigning a `সংখ্যা` expression to a `ভগ্নাংশ` variable is
allowed (widening, as in most statically typed languages).

### 5.3 `if` / `else`

```
যদি (x > 5) {
    দেখাও(x);
} অন্যথায় {
    দেখাও(0);
}
```

The `অন্যথায়` branch is optional.

### 5.4 `while`

```
যতক্ষণ (x <= 5) {
    দেখাও(x);
    x = x + 1;
}
```

### 5.5 `print`

```
দেখাও(x);
দেখাও(x + y);
```

`দেখাও(...)` takes exactly one expression argument.

## 6. Expressions

Arithmetic expressions support `+ - * /` with standard precedence
(multiplication/division bind tighter than addition/subtraction) and
parentheses for grouping. See `grammar.md` for the full precedence table.

Comparison expressions (`> < >= <= == !=`) are only valid as the condition of
an `if`/`while` statement.

## 7. Comments

A comment begins with `//` and runs to the end of the line:

```
সংখ্যা x = 10; // this is a comment
```

## 8. Example Program

```
সংখ্যা x = 10;
সংখ্যা y = 20;

সংখ্যা result = x + y * 2;

দেখাও(result);
```

Generated Python:

```python
x = 10
y = 20
result = x + y * 2
print(result)
```

Output: `50`

## 9. Compiler Pipeline

```
.bl source file
      │
      ▼
Lexical Analysis   (Lexer.cs)          →  stream of Tokens
      │
      ▼
Parsing            (Parser.cs)         →  Abstract Syntax Tree
      │
      ▼
Semantic Analysis  (SemanticAnalyzer.cs) →  type-checked AST + Symbol Table
      │
      ▼
Code Generation    (PythonCodeGenerator.cs) → Python 3 source text
      │
      ▼
output.py
```

## 10. Error Handling Philosophy

The compiler never crashes on invalid input. Lexical, syntax, and semantic
errors are collected into a unified `CompilerError` list with line/column
information and reported together where possible (panic-mode recovery for
syntax errors — see `docs/error-handling.md`, added in Part 6).
