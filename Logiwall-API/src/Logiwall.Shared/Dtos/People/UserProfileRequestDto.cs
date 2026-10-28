namespace Logiwall.Shared.Dtos.People;
public record UserProfileRequestDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }
}
