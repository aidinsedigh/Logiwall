namespace Logiwall.Shared.Dtos.People;
public class UserProfileRequestDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }
}
