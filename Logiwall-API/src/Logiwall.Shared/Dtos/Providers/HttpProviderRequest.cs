namespace Logiwall.Shared.Dtos.Providers;
public sealed record HttpProviderRequest<TBody, TRequest>
{
    public required TRequest? Request { get; init; }

    public required string? Uri { get; set; }

    public string? BaseAddress { get; set; }

    public IReadOnlyList<(string Key, string Value)>? HeaderParameters { get; set; }

    public TBody? Body { get; set; }

    /// <summary>
    /// Gets or sets Timeout in seconds.
    /// </summary>
    public int? Timeout { get; set; }
}
