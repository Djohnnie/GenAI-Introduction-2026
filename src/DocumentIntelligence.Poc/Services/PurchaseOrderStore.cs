using DocumentIntelligence.Poc.Models;

namespace DocumentIntelligence.Poc.Services;

/// <summary>
/// In-memory storage for the POC. Registered as a SINGLETON in Program.cs,
/// so there is one instance for the whole application: every user and every
/// browser tab works against the same list. Data lives until the application
/// stops — in a real application this class would be replaced by a database
/// repository with the same public methods.
/// </summary>
public class PurchaseOrderStore
{
    // Multiple users (circuits) can call the store at the same time, so all
    // access to the list is protected with a lock. System.Threading.Lock is
    // the dedicated lock type introduced in .NET 9.
    private readonly Lock _lock = new();
    private readonly List<PurchaseOrder> _orders = [];

    /// <summary>
    /// Returns a snapshot of all orders. Note that the orders are CLONED:
    /// callers can do whatever they want with the result without affecting
    /// the stored data (no accidental shared references).
    /// </summary>
    public IReadOnlyList<PurchaseOrder> GetAll()
    {
        lock (_lock)
        {
            return _orders.Select(o => o.Clone()).ToList();
        }
    }

    /// <summary>Returns a clone of one order, or null when the id is unknown.</summary>
    public PurchaseOrder? Get(Guid id)
    {
        lock (_lock)
        {
            return _orders.FirstOrDefault(o => o.Id == id)?.Clone();
        }
    }

    /// <summary>
    /// Inserts or updates ("upserts") an order: remove any stored version
    /// with the same id, then add a clone of the incoming one.
    /// </summary>
    public void Save(PurchaseOrder order)
    {
        lock (_lock)
        {
            _orders.RemoveAll(o => o.Id == order.Id);
            _orders.Add(order.Clone());
        }
    }

    public void Delete(Guid id)
    {
        lock (_lock)
        {
            _orders.RemoveAll(o => o.Id == id);
        }
    }
}
