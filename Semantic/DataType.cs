namespace BanglaCompiler.Semantic;


public enum DataType
{
    Integer,  
    Float,    
    Unknown,  
}

public static class DataTypeExtensions
{
    public static DataType FromKeyword(string keywordLexeme) => keywordLexeme switch
    {
        "সংখ্যা" => DataType.Integer,
        "ভগ্নাংশ" => DataType.Float,
        _ => DataType.Unknown, 
    };

    public static string ToDisplayName(this DataType type) => type switch
    {
        DataType.Integer => "সংখ্যা (integer)",
        DataType.Float => "ভগ্নাংশ (float)",
        _ => "unknown",
    };
}
