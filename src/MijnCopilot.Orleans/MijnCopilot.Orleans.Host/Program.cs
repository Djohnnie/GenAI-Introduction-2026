using MijnCopilot.Agents.DependencyInjection;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Orleans.Configuration;
using Orleans.Dashboard;
using System.Net;
using System.Net.Sockets;

static int GetFreePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

var builder = WebApplication.CreateBuilder(args);

var siloPort = GetFreePort();
var gatewayPort = GetFreePort();
var httpPort = GetFreePort();

Console.WriteLine($"HTTP-PORT: {httpPort}");

builder.WebHost.UseUrls($"http://localhost:{httpPort}");

var blobConnectionString = builder.Configuration.GetValue<string>("BLOB_CONNECTION_STRING")!;

builder.UseOrleans(siloBuilder =>
{
    siloBuilder
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "mijn-copilot";
            options.ServiceId = "mijn-copilot";
        })
        .ConfigureEndpoints(siloPort: siloPort, gatewayPort: gatewayPort)
        .UseAzureStorageClustering(options =>
        {
            options.TableServiceClient = new TableServiceClient(blobConnectionString);
        })
        .AddMemoryGrainStorageAsDefault()
        .AddAzureBlobGrainStorage("blob-store", options =>
        {
            options.BlobServiceClient = new BlobServiceClient(blobConnectionString);
        })
        .AddDashboard();
});

builder.Services.AddCopilotServices(builder.Configuration);

var app = builder.Build();

app.MapOrleansDashboard();

await app.RunAsync();