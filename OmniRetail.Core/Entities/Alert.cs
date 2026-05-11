using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniRetail.Core.Entities;

public class Alert : BaseEntity
{
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
}
