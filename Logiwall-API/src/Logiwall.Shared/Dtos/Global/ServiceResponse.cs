namespace Logiwall.Shared.Dtos.Global;

public record ServiceResponse<T>
{
    public T? Data { get; set; } = default;

    public bool Succuss { get; set; } = true;

    public string Message { get; set; } = string.Empty;
}
