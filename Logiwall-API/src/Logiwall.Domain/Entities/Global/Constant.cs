namespace Logiwall.Domain.Entities.Global;
public class Constant
{
    [Key]
    [Required]
    public short Code { get; set; }

    [Required]
    [MaxLength(256)]
    public string Type { get; set; } = default!;

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = default!;
}
