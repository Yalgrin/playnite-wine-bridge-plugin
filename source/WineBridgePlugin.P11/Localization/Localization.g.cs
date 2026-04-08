namespace Playnite;

public static partial class Loc
{

    /// <summary>
    /// Fluent test string
    /// </summary>
    public static string example_string()
    {
        return GetString("example_string");
    }
    /// <summary>
    /// Fluent {$param} string
    /// </summary>
    public static string example_string_param(object param)
    {
        return GetString("example_string_param", ("param", param));
    }
}

public static partial class LocId
{
    public static readonly HashSet<string> StringIds = new()
    {
        "example_string", 
        "example_string_param"
    };

    /// <summary>
    /// Fluent test string
    /// </summary>
    public const string example_string = "example_string";
    /// <summary>
    /// Fluent {$param} string
    /// </summary>
    public const string example_string_param = "example_string_param";
}
