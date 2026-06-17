using AgentFramework.Common;
using AgentFramework.PluginsAndFunctions;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var chatClient = client.GetChatClient("gpt-4o");
var tools = GeneralPlugin.GetTools();
var agentClient = chatClient.AsAIAgent(name: "Chat", description: "Just a chat", tools: tools);

var chatSession = await agentClient.CreateSessionAsync();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("User > ");
    Console.ForegroundColor = ConsoleColor.White;
    var request = Console.ReadLine();

    Console.ForegroundColor = ConsoleColor.Cyan;

    var response = await agentClient.RunAsync(request!, chatSession);
    foreach (var message in response.Messages)
    {
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            Console.Write("Assistant > ");
            Console.WriteLine(message.Text);
        }
    }

    Console.WriteLine();

    chatSession.Debug();
}