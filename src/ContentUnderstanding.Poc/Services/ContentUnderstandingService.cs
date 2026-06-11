using System.Globalization;
using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Identity;
using ContentUnderstanding.Poc.Models;

namespace ContentUnderstanding.Poc.Services;

/// <summary>
/// Analyzes an uploaded purchase order document with Azure Content
/// Understanding and maps the extracted fields onto our <see cref="PurchaseOrder"/>
/// model.
///
/// Content Understanding is the successor of Document Intelligence, hosted on
/// an Azure AI Foundry resource (endpoint looks like
/// https://&lt;name&gt;.services.ai.azure.com/). It handles documents, images,
/// audio and video with one API, and uses generative models under the hood.
///
/// The flow is:
///   1. Read the temporarily uploaded file from disk.
///   2. Send the bytes to the cloud service with the "prebuilt-invoice"
///      analyzer (a ready-made analyzer from Microsoft, no training needed).
///   3. The service returns the content with a dictionary of typed fields
///      (vendor, items, totals, ...), each with a confidence score.
///   4. Translate those fields into a PurchaseOrder the edit form can show.
/// </summary>
public class ContentUnderstandingService
{
    // Microsoft ships several prebuilt analyzers (invoice, receipt, ...).
    // The invoice analyzer is the best match for purchase orders.
    private const string AnalyzerId = "prebuilt-invoice";

    private readonly string? _endpoint;
    private readonly string? _key;
    private readonly ILogger<ContentUnderstandingService> _logger;

    public ContentUnderstandingService(IConfiguration configuration, ILogger<ContentUnderstandingService> logger)
    {
        // IConfiguration merges several sources; environment variables are
        // one of them, so the values from launchSettings.json appear here.
        _endpoint = configuration["CONTENT_UNDERSTANDING_ENDPOINT"];
        _key = configuration["CONTENT_UNDERSTANDING_KEY"];
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
                "Azure Content Understanding is not configured. " +
                "Set the CONTENT_UNDERSTANDING_ENDPOINT and CONTENT_UNDERSTANDING_KEY environment variables in launchSettings.json.");
        }

        var client = CreateClient();

        // The document was saved to a temp file by the upload handler; read
        // it back and wrap the bytes (plus MIME type) for the SDK.
        byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var binaryInput = BinaryData.FromBytes(bytes, contentType);

        _logger.LogInformation("Analyzing document {FilePath} ({Size} bytes) with {AnalyzerId}...", filePath, bytes.Length, AnalyzerId);

        // Analysis is a "long-running operation": the service processes the
        // document asynchronously on its side. WaitUntil.Completed makes the
        // SDK poll until the result is ready, so one call does everything.
        Operation<AnalysisResult> operation = await client.AnalyzeBinaryAsync(
            WaitUntil.Completed,
            AnalyzerId,
            binaryInput,
            contentType: contentType,
            cancellationToken: cancellationToken);

        // Debug aid: log the raw service response so mapping issues can be
        // diagnosed by comparing it with what ends up in the PurchaseOrder.
        // Enabled through the log level in appsettings.Development.json.
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Raw analysis result:\n{Json}",
                System.ClientModel.Primitives.ModelReaderWriter.Write(operation.Value).ToString());
        }

        return MapToPurchaseOrder(operation.Value);
    }

    private ContentUnderstandingClient CreateClient()
    {
        var serviceUri = new Uri(_endpoint!);

        // Pin the API version so behavior doesn't change under our feet.
        var clientOptions = new ContentUnderstandingClientOptions(ContentUnderstandingClientOptions.ServiceVersion.V2025_11_01);

        // Two ways to authenticate:
        // - An API key (simplest, what this POC uses).
        // - DefaultAzureCredential: walks through managed identity, Azure CLI
        //   login, Visual Studio login, ... Preferred in production because
        //   there is no secret to manage. Used here when no key is set.
        return string.IsNullOrWhiteSpace(_key)
            ? new ContentUnderstandingClient(serviceUri, new DefaultAzureCredential(), clientOptions)
            : new ContentUnderstandingClient(serviceUri, new AzureKeyCredential(_key), clientOptions);
    }

    /// <summary>
    /// Translates the generic analysis result into our domain model. The
    /// field names ("VendorName", "Items", ...) are defined by the
    /// prebuilt-invoice analyzer; see the analyzer documentation for the
    /// full list.
    /// </summary>
    private PurchaseOrder MapToPurchaseOrder(AnalysisResult result)
    {
        var order = new PurchaseOrder();

        // One uploaded file can contain multiple contents; the POC simply
        // takes the first one. Fields is a dictionary of
        // field name -> ContentField (typed value + confidence).
        var fields = result.Contents.FirstOrDefault()?.Fields;
        if (fields is null)
        {
            _logger.LogWarning("Analysis returned no extracted fields.");
            return order;
        }

        order.VendorName = GetString(fields.GetFieldOrDefault("VendorName")) ?? string.Empty;
        order.VendorAddress = GetString(fields.GetFieldOrDefault("VendorAddress")) ?? string.Empty;

        // Prefer the document's purchase order reference; fall back to the
        // invoice number when there is none. Note: the Content Understanding
        // invoice schema differs from Document Intelligence — here the field
        // is called "PONumber" (DI calls it "PurchaseOrder").
        order.PoNumber = GetString(fields.GetFieldOrDefault("PONumber"))
                         ?? GetString(fields.GetFieldOrDefault("InvoiceId"))
                         ?? string.Empty;

        // There is no "description" on an invoice, so compose something
        // readable out of the document number and the customer name.
        var invoiceId = GetString(fields.GetFieldOrDefault("InvoiceId"));
        var customerName = GetString(fields.GetFieldOrDefault("CustomerName"));
        order.Description = string.Join(" ", new[]
        {
            invoiceId is null ? null : $"Document {invoiceId}",
            customerName is null ? null : $"for {customerName}"
        }.Where(p => p is not null));

        // "LineItems" is an array field (DI calls it "Items"); each entry is
        // an object field, i.e. a nested dictionary with its own sub-fields
        // per item line. The money sub-fields (UnitPrice, TotalAmount) are
        // objects too: { Amount: number, CurrencyCode: string }.
        if (fields.GetFieldOrDefault("LineItems") is ContentArrayField { Value: { } items })
        {
            foreach (var item in items.OfType<ContentObjectField>())
            {
                var itemFields = item.Value;
                if (itemFields is null)
                {
                    continue;
                }

                var quantity = GetDecimal(itemFields.GetFieldOrDefault("Quantity")) ?? 1m;
                var amount = GetDecimal(itemFields.GetFieldOrDefault("TotalAmount"));

                // Not every document prints a unit price. When it is missing
                // we derive it from the line amount: price = amount / qty.
                var unitPrice = GetDecimal(itemFields.GetFieldOrDefault("UnitPrice"))
                                ?? (quantity != 0 ? amount / quantity : null);

                var itemCode = GetString(itemFields.GetFieldOrDefault("ProductCode")) ?? string.Empty;
                var description = GetString(itemFields.GetFieldOrDefault("Description")) ?? string.Empty;

                AddOrMergeLine(order.Lines, new PurchaseOrderLine
                {
                    ItemCode = itemCode,
                    Description = description,
                    Quantity = quantity,
                    Price = Math.Round(unitPrice ?? 0m, 2, MidpointRounding.AwayFromZero)
                });
            }
        }

        // The analyzer extracts amounts, not percentages, so the VAT
        // percentage is derived: tax / subtotal * 100. When the document
        // doesn't state both amounts, the model default (21%) is kept.
        // (DI names: "SubTotal" / "TotalTax".)
        var subTotal = GetDecimal(fields.GetFieldOrDefault("SubtotalAmount"));
        var totalTax = GetDecimal(fields.GetFieldOrDefault("TotalTaxAmount"));
        if (subTotal is > 0 && totalTax is not null)
        {
            order.VatPercentage = Math.Round(totalTax.Value / subTotal.Value * 100m, 2, MidpointRounding.AwayFromZero);
        }

        return order;
    }

    /// <summary>
    /// Adds a line unless it duplicates one extracted earlier. Multi-page
    /// documents can repeat the item table on later pages (sometimes with a
    /// more detailed description), which the prebuilt analyzer extracts twice.
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
    /// Safely reads a field as text. ContentField is an abstract base class;
    /// pattern matching on the concrete type tells us how to read the value.
    /// Object fields (such as addresses) are flattened by joining their
    /// sub-values.
    /// </summary>
    private static string? GetString(ContentField? field)
    {
        var value = field switch
        {
            null => null,
            ContentStringField s => s.Value,
            ContentObjectField { Value: { } subFields } =>
                string.Join(", ", subFields.Values.Select(GetString).Where(v => !string.IsNullOrWhiteSpace(v))),
            _ => field.Value?.ToString()
        };

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Safely reads a field as a decimal. The analyzer returns numbers as
    /// number/integer fields, currency amounts sometimes as an object with an
    /// "Amount" sub-field, and occasionally only raw text — this helper
    /// handles each shape.
    /// </summary>
    private static decimal? GetDecimal(ContentField? field)
    {
        return field switch
        {
            null => null,
            ContentNumberField { Value: { } d } => (decimal)d,
            ContentIntegerField { Value: { } l } => l,
            ContentStringField s => decimal.TryParse(s.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null,
            ContentObjectField o => GetDecimal(o.Value?.GetFieldOrDefault("Amount") ?? o.Value?.GetFieldOrDefault("amount")),
            _ => null
        };
    }
}
