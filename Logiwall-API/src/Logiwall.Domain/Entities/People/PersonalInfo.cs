using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Domain.Entities.People;
public class PersonalInfo : ICreationDateAware, IModificationDateAware
{
    public int Id { get; set; }

    public PersonKind Kind { get; set; }

    public Gender? Gender { get; set; }

    public DateTimeOffset? BirthDate { get; set; }

    [Required, StringLength(11)]
    public string? NationalCode { get; set; }

    public int PersonId { get; set; }

    public Person Person { get; set; } = default!;

    public DateTimeOffset CreationDate { get; set; }


    public DateTimeOffset? ModificationDate { get; set; } = DateTimeOffset.Now;
}
