using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Logiwall.Domain.Entities.People;
public class Person : IArchiveAware, ICreationDateAware, IModificationDateAware
{
    public int Id { get; set; }

    [Required, MaxLength(15)]
    public string PhoneNumber { get; set; } = default!;

    [Required, MaxLength(100)]
    [PersonalData]
    public string Name { get; set; } = default!;

    [MaxLength(100)]
    [PersonalData]
    public string LastName { get; set; } = default!;

    [MaxLength(210)]
    [PersonalData]
    public string? FullName => $"{Name} {LastName}";

    public DateTimeOffset? ModificationDate { get; set; }

    public int UserId { get; set; }

    //todo: I add this properties without implementation again because
    //When Entity Framework is mapping entities to database tables, it ignores interface members
    //that are not explicitly implemented in the concrete class
    public bool IsArchive { get; set; }

    public DateTimeOffset CreationDate { get; set; }

    public Customer Customer { get; set; } = default!;

    public PersonalInfo? PersonalInfo { get; set; } = default;
}
