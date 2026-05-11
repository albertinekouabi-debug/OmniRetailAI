using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniRetail.Core.Entities;

public class Sale : BaseEntity
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public decimal TotalAmount { get; set; }

    public ICollection<SaleItem> Items { get; set; }
        = new List<SaleItem>();
}
