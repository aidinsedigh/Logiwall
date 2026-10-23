using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logiwall.Domain.Entities;
public interface ICreationDateAware
{
    public DateTimeOffset CreationDate { get => DateTimeOffset.Now; set { } }
}
