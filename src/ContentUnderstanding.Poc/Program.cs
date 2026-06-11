using ContentUnderstanding.Poc.Components;
using ContentUnderstanding.Poc.Services;
using MudBlazor.Services;

// Program.cs is the entry point of an ASP.NET Core application. It does two
// things: register services in the dependency injection (DI) container, and
// configure the HTTP request pipeline (middleware).
var builder = WebApplication.CreateBuilder(args);

// --- Service registration (dependency injection) ---

// Enable Razor components with the "Interactive Server" render mode:
// the UI runs on the server, and a SignalR (websocket) connection keeps the
// browser in sync. Every user gets their own "circuit" (connection + state).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor needs a few services of its own (dialogs, snackbars, popovers).
builder.Services.AddMudServices();

// DI lifetimes matter here:
// - Singleton: ONE instance for the whole application, shared by all users.
//   Perfect for the in-memory store, so every browser tab sees the same data.
builder.Services.AddSingleton<PurchaseOrderStore>();

// - Singleton is fine for the analysis service too: it is stateless (it only
//   reads configuration once and creates an Azure client per call).
builder.Services.AddSingleton<ContentUnderstandingService>();

// - Scoped: in Blazor Server this means ONE instance per circuit (browser tab).
//   The draft must not leak between users, so it is scoped, not singleton.
builder.Services.AddScoped<PurchaseOrderDraft>();

var app = builder.Build();

// --- HTTP request pipeline (middleware runs top to bottom) ---

if (!app.Environment.IsDevelopment())
{
    // In production, show a friendly error page instead of a stack trace.
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Protects interactive form posts against cross-site request forgery.
app.UseAntiforgery();

// Serves the files from wwwroot (css, js) with fingerprinted URLs.
app.MapStaticAssets();

// Wire up the Blazor component endpoints, with App.razor as the root.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
