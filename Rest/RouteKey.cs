namespace Crovus.Rest;

public readonly record struct RouteKey(HttpMethod Method, string Template, string? MajorParameter = null)
{
    public static RouteKey Get(string template, string? major = null) => new(HttpMethod.Get, template, major);
    public static RouteKey Post(string template, string? major = null) => new(HttpMethod.Post, template, major);
    public static RouteKey Patch(string template, string? major = null) => new(HttpMethod.Patch, template, major);
    public static RouteKey Put(string template, string? major = null) => new(HttpMethod.Put, template, major);
    public static RouteKey Delete(string template, string? major = null) => new(HttpMethod.Delete, template, major);

    public override string ToString() => $"{Method.Method} {Template}:{MajorParameter}";
}
