namespace AgentScope.Extensions.Document;

/// <summary>
/// 读取器输入。对标 Java ReaderInput。
/// </summary>
public sealed record ReaderInput
{
    public InputType Type { get; init; }
    public string Content { get; init; } = "";
    public string? FilePath { get; init; }

    public static ReaderInput FromString(string text) =>
        new() { Type = InputType.String, Content = text };

    public static ReaderInput FromFile(string path) =>
        new() { Type = InputType.File, FilePath = path, Content = File.ReadAllText(path) };

    public static ReaderInput FromUrl(string url) =>
        new() { Type = InputType.Url, Content = url };

    public enum InputType { String, File, Url }
}
