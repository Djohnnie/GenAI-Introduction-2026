namespace DocumentIntelligence.Poc.Models;

/// <summary>
/// The domain model for a purchase order. This is a plain C# class (POCO):
/// the UI binds directly to its properties, and the analysis service fills
/// it from the fields extracted out of an uploaded document.
/// </summary>
public class PurchaseOrder
{
    /// <summary>Unique identifier, generated client-side so the model never
    /// depends on a database to exist.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // --- Vendor data ---

    public string VendorName { get; set; } = string.Empty;
    public string VendorAddress { get; set; } = string.Empty;

    // --- Order data ---

    public string PoNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>The item lines of the order. Mutated directly by the edit
    /// form (add/remove buttons).</summary>
    public List<PurchaseOrderLine> Lines { get; set; } = [];

    /// <summary>VAT percentage as a whole number (21 means 21%). Defaults to
    /// the standard Belgian VAT rate.</summary>
    public decimal VatPercentage { get; set; } = 21m;

    // --- Totals ---
    // These are computed properties (=> instead of { get; set; }): they are
    // recalculated on every read, so the UI always shows up-to-date totals
    // while the user edits quantities and prices. Nothing to keep in sync.

    /// <summary>Sum of all line totals, excluding VAT.</summary>
    public decimal TotalAmount => Lines.Sum(l => l.TotalPrice);

    /// <summary>VAT amount, rounded to 2 decimals. AwayFromZero is the
    /// "commercial" rounding people expect (0.005 becomes 0.01), unlike the
    /// default banker's rounding.</summary>
    public decimal VatAmount => Math.Round(TotalAmount * VatPercentage / 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>Grand total including VAT.</summary>
    public decimal TotalAmountIncludingVat => TotalAmount + VatAmount;

    /// <summary>
    /// Creates a deep copy (the lines are copied too). The store hands out
    /// clones so that editing a form never changes stored data until Save.
    /// </summary>
    public PurchaseOrder Clone() => new()
    {
        Id = Id,
        VendorName = VendorName,
        VendorAddress = VendorAddress,
        PoNumber = PoNumber,
        Description = Description,
        VatPercentage = VatPercentage,
        Lines = Lines.Select(l => l.Clone()).ToList()
    };
}

/// <summary>
/// A single item line on a purchase order.
/// </summary>
public class PurchaseOrderLine
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Quantity is a decimal (not int) so fractional quantities
    /// such as 2.5 kg are possible.</summary>
    public decimal Quantity { get; set; } = 1m;

    /// <summary>Unit price, excluding VAT.</summary>
    public decimal Price { get; set; }

    /// <summary>Line total, computed on the fly just like the order totals.</summary>
    public decimal TotalPrice => Math.Round(Quantity * Price, 2, MidpointRounding.AwayFromZero);

    public PurchaseOrderLine Clone() => new()
    {
        ItemCode = ItemCode,
        Description = Description,
        Quantity = Quantity,
        Price = Price
    };
}
