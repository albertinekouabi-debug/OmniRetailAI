namespace OmniRetail.Core.Enums;

/// <summary>
/// Types de mouvements de stock.
/// </summary>
public enum InventoryTransactionType
{
    Entry = 1,
    Exit = 2,
    Adjustment = 3,
    Sale = 4,
    Return = 5
}