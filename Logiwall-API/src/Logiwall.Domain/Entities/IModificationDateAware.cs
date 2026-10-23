namespace Logiwall.Domain.Entities;
public interface IModificationDateAware
{
    public DateTimeOffset? ModificationDate { get; set; }
}
