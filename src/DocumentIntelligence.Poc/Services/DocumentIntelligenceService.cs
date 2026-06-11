using System.Globalization;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using DocumentIntelligence.Poc.Models;

namespace DocumentIntelligence.Poc.Services;

/// <summary>
/// Analyzes an uploaded purchase order document with Azure Document
/// Intelligence and maps the extracted fields onto our <see cref="PurchaseOrder"/>
/// model.
///
/// The flow is:
///   1. Read the temporarily uploaded file from disk.
///   2. Send the bytes to the cloud service with the "prebuilt-invoice"
///      model (a ready-made model trained by Microsoft, no training needed).
///   3. The service returns a list of strongly-typed fields (vendor, items,
///      totals, ...), each with a confidence score.
///   4. Translate those fields into a PurchaseOrder the edit form can show.
/// </summary>
public class DocumentIntelligenceService
{
    // Microsoft ships several prebuilt models (invoice, receipt, id-document,
    // ...). The invoice model is the best match for purchase orders.
    private const string ModelId = "prebuilt-invoice";

    private readonly string? _endpoint;
    private readonly string? _key;
    private readonly ILogger<DocumentIntelligenceService> _logger;

    public DocumentIntelligenceService(IConfiguration configuration, ILogger<DocumentIntelligenceService> logger)
    {
        // IConfiguration merges several sources; environment variables are
        // one of them, so the values from launchSettings.json appear here.
        _endpoint = configuration["DOCUMENT_INTELLIGENCE_ENDPOINT"];
        _key = configuration["DOCUMENT_INTELLIGENCE_KEY"];
        _logger = logger;
    }

    /// <summary>True when a (syntactically) valid endpoint URL is configured.
    /// The UI uses this to show a friendly message instead of failing.</summary>
    public bool IsConfigured => Uri.TryCreate(_endpoint, UriKind.Absolute, out _);

    public async Task<PurchaseOrder> AnalyzePurchaseOrderAsync(string filePath, string contentType, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Azure Document Intelligence is not configured. " +
                "Set the DOCUMENT_INTELLIGENCE_ENDPOINT and DOCUMENT_INTELLIGENCE_KEY environment variables in launchSettings.json.");
        }

        var client = CreateClient();

        // The document was saved to a temp file by the upload handler; read
        // it back and wrap the bytes (plus MIME type) for the SDK.
        byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var binaryInput = BinaryData.FromBytes(bytes, contentType);

        _logger.LogInformation("Analyzing document {FilePath} ({Size} bytes) with {ModelId}...", filePath, bytes.Length, ModelId);

        // Analysis is a "long-running operation": the service processes the
        // document asynchronously on its side. WaitUntil.Completed makes the
        // SDK poll until the result is ready, so one call does everything.
        Operation<AnalyzeResult> operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            ModelId,
            binaryInput,
            cancellationToken);

        return MapToPurchaseOrder(operation.Value);
    }

    private DocumentIntelligenceClient CreateClient()
    {
        var serviceUri = new Uri(_endpoint!);

        // Two ways to authenticate:
        // - An API key (simplest, what this POC uses).
        // - DefaultAzureCredential: walks through managed identity, Azure CLI
        //   login, Visual Studio login, ... Preferred in production because
        //   there is no secret to manage. Used here when no key is set.
        return string.IsNullOrWhiteSpace(_key)
            ? new DocumentIntelligenceClient(serviceUri, new DefaultAzureCredential())
            : new DocumentIntelligenceClient(serviceUri, new AzureKeyCredential(_key));
    }

    /// <summary>
    /// Translates the generic analysis result into our domain model. The
    /// field names ("VendorName", "Items", ...) are defined by the
    /// prebuilt-invoice model; see the model documentation for the full list.
    /// </summary>
    private PurchaseOrder MapToPurchaseOrder(AnalyzeResult result)
    {
        var order = new PurchaseOrder();

        // One uploaded file can in theory contain multiple documents; the
        // POC simply takes the first one. Fields is a dictionary of
        // field name -> DocumentField (typed value + confidence).
        var fields = result.Documents.FirstOrDefault()?.Fields;
        if (fields is null)
        {
            _logger.LogWarning("Analysis returned no extracted fields.");
            return order;
        }

        order.VendorName = GetString(fields.GetValueOrDefault("VendorName")) ?? string.Empty;
        order.VendorAddress = GetString(fields.GetValueOrDefault("VendorAddress")) ?? string.Empty;

        // Prefer the document's purchase order reference; fall back to the
        // invoice number when there is none.
        order.PoNumber = GetString(fields.GetValueOrDefault("PurchaseOrder"))
                         ?? GetString(fields.GetValueOrDefault("InvoiceId"))
                         ?? string.Empty;

        // There is no "description" on an invoice, so compose something
        // readable out of the document number and the customer name.
        var invoiceId = GetString(fields.GetValueOrDefault("InvoiceId"));
        var customerName = GetString(fields.GetValueOrDefault("CustomerName"));
        order.Description = string.Join(" ", new[]
        {
            invoiceId is null ? null : $"Document {invoiceId}",
            customerName is null ? null : $"for {customerName}"
        }.Where(p => p is not null));

        // "Items" is an array field; each entry is an object field, i.e. a
        // nested dictionary with its own sub-fields per item line.
        if (fields.GetValueOrDefault("Items")?.ValueList is { } items)
        {
            foreach (var item in items)
            {
                var itemFields = item.ValueDictionary;
                if (itemFields is null)
                {
                    continue;
                }

                var quantity = GetDecimal(itemFields.GetValueOrDefault("Quantity")) ?? 1m;
                var amount = GetDecimal(itemFields.GetValueOrDefault("Amount"));

                // Not every document prints a unit price. When it is missing
                // we derive it from the line amount: price = amount / qty.
                var unitPrice = GetDecimal(itemFields.GetValueOrDefault("UnitPrice"))
                                ?? (quantity != 0 ? amount / quantity : null);

                AddOrMergeLine(order.Lines, new PurchaseOrderLine
                {
                    ItemCode = GetString(itemFields.GetValueOrDefault("ProductCode")) ?? string.Empty,
                    Description = GetString(itemFields.GetValueOrDefault("Description")) ?? string.Empty,
                    Quantity = quantity,
                    Price = Math.Round(unitPrice ?? 0m, 2, MidpointRounding.AwayFromZero)
                });
            }
        }

        // The model extracts amounts, not percentages, so the VAT percentage
        // is derived: tax / subtotal * 100. When the document doesn't state
        // both amounts, the model default (21%) is kept.
        var subTotal = GetDecimal(fields.GetValueOrDefault("SubTotal"));
        var totalTax = GetDecimal(fields.GetValueOrDefault("TotalTax"));
        if (subTotal is > 0 && totalTax is not null)
        {
            order.VatPercentage = Math.Round(totalTax.Value / subTotal.Value * 100m, 2, MidpointRounding.AwayFromZero);
        }

        return order;
    }

    /// <summary>
    /// Adds a line unless it duplicates one extracted earlier. Multi-page
    /// documents can repeat the item table on later pages (sometimes with a
    /// more detailed description), which the prebuilt model extracts twice.
    /// </summary>
    private static void AddOrMergeLine(List<PurchaseOrderLine> lines, PurchaseOrderLine line)
    {
        var duplicate = lines.FirstOrDefault(existing => IsDuplicate(existing, line));
        if (duplicate is null)
        {
            lines.Add(line);
            return;
        }

        // Keep the most detailed variant of the duplicated line.
        if (line.Description.Length > duplicate.Description.Length)
        {
            duplicate.Description = line.Description;
        }
        if (string.IsNullOrWhiteSpace(duplicate.ItemCode))
        {
            duplicate.ItemCode = line.ItemCode;
        }
    }

    /// <summary>
    /// Two lines are duplicates when quantity and price match, unless both
    /// carry an item code, in which case the codes decide.
    /// </summary>
    private static bool IsDuplicate(PurchaseOrderLine a, PurchaseOrderLine b)
    {
        if (a.Quantity != b.Quantity || a.Price != b.Price)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(a.ItemCode) && !string.IsNullOrWhiteSpace(b.ItemCode))
        {
            return string.Equals(a.ItemCode.Trim(), b.ItemCode.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        // Without two item codes to compare, matching quantity and price is
        // enough; descriptions of repeated lines vary too much to be useful.
        return true;
    }

    /// <summary>
    /// Safely reads a field as text. Content holds the raw text of the field
    /// as printed on the document, which also works for structured values
    /// such as addresses.
    /// </summary>
    private static string? GetString(DocumentField? field)
    {
        var value = field?.ValueString ?? field?.Content;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Safely reads a field as a decimal. The model returns money as a
    /// currency value, counts as double/int64, and sometimes only raw text —
    /// this helper tries them in that order.
    /// </summary>
    private static decimal? GetDecimal(DocumentField? field)
    {
        if (field is null)
        {
            return null;
        }

        if (field.ValueCurrency is { } currency)
        {
            return (decimal)currency.Amount;
        }

        if (field.ValueDouble is { } d)
        {
            return (decimal)d;
        }

        if (field.ValueInt64 is { } l)
        {
            return l;
        }

        return decimal.TryParse(field.Content, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
