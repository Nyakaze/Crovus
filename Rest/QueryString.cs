namespace Crovus.Rest;

internal struct QueryString()
{
    private List<string>? _parameters;

    public void Add(string name, object? value)
    {
        if (value is null)
            return;

        (_parameters ??= new List<string>(4)).Add($"{name}={value}");
    }

    public void AddEscaped(string name, string? value)
    {
        if (value is null)
            return;

        Add(name, Uri.EscapeDataString(value));
    }

    public readonly string AppendTo(string path) =>
        _parameters is { Count: > 0 } ? $"{path}?{string.Join('&', _parameters)}" : path;
}
