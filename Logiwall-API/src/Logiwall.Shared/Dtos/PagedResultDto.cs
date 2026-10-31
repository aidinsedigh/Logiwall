namespace Logiwall.Shared.Dtos;

public record PagedResult<T>(T[] Items, long TotalCount);
