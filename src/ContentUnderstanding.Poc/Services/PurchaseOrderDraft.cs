using ContentUnderstanding.Poc.Models;

namespace ContentUnderstanding.Poc.Services;

/// <summary>
/// Carries a purchase order from the document-analysis flow (Home page) to
/// the edit form (PurchaseOrderEdit page) across a navigation.
///
/// Why is this needed? Navigating to another page creates new component
/// instances, so component fields don't survive. A small SCOPED service does:
/// in Blazor Server "scoped" means one instance per circuit (browser tab),
/// so the draft of one user can never show up for another user.
/// </summary>
public class PurchaseOrderDraft
{
    private PurchaseOrder? _draft;

    /// <summary>Called by the Home page after a successful analysis.</summary>
    public void Set(PurchaseOrder order) => _draft = order;

    /// <summary>
    /// Called by the edit form when it opens in "new" mode. Take semantics:
    /// the draft is returned AND cleared, so refreshing the form afterwards
    /// starts with an empty order instead of re-loading an old draft.
    /// </summary>
    public PurchaseOrder? Take()
    {
        var draft = _draft;
        _draft = null;
        return draft;
    }
}
