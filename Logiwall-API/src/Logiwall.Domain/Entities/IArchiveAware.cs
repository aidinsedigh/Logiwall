namespace Logiwall.Domain.Entities;
public interface IArchiveAware
{
    public bool IsArchive { get => false; set { } }
}
