using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniRetail.Core.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int CurrentStock { get; set; }

    public int CriticalStock { get; set; }

    // Keep as DateTime? intentionally. If DTOs use timestamps, conversion is handled during mapping.
    public DateTime? ExpirationDate { get; set; }

    public bool IsSensitive { get; set; }
}
