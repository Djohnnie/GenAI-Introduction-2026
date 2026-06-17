using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;

var tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? string.Empty;
var clientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? string.Empty;
var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? string.Empty;
var foundryUri = Environment.GetEnvironmentVariable("FOUNDRY_URI") ?? string.Empty;

var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
string[] scopes = ["https://cognitiveservices.azure.com/.default"];
AccessToken token = credential.GetToken(new TokenRequestContext(scopes));

var client = new PersistentAgentsClient(foundryUri, credential);
var agentClient = await client.GetAIAgentAsync("asst_SjmvVnJodiYE3ZlLSFNBWDLb");

var chatSession = await agentClient.CreateSessionAsync();
var response = await agentClient.RunAsync("What are your capabilities?", chatSession);
Console.WriteLine(response.Text);

ChatClientAgentSession typedSession = (ChatClientAgentSession)chatSession;
Console.WriteLine(typedSession.ConversationId);


while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("User > ");
    Console.ForegroundColor = ConsoleColor.White;
    var request = Console.ReadLine();

    Console.ForegroundColor = ConsoleColor.Cyan;

    response = await agentClient.RunAsync(request!, chatSession);
    foreach (var message in response.Messages)
    {
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            Console.Write("Assistant > ");
            Console.WriteLine(message.Text);
        }
    }

    Console.WriteLine();
}