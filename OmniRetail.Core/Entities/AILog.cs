using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniRetail.Core.Entities;

public class AILog : BaseEntity
{
    public string Prompt { get; set; } = string.Empty;

    public string Response { get; set; } = string.Empty;
}
