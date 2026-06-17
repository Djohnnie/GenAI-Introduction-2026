using AgentFramework.Common;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.ClientModel;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;
var googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_KEY") ?? string.Empty;

var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var chatClient = openAIClient.GetChatClient("gpt-5-chat").AsIChatClient();

await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "GMaps",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-google-maps"],
    EnvironmentVariables = new Dictionary<string, string>
    {
        { "GOOGLE_MAPS_API_KEY", googleApiKey }
    }
}));

var name = "GMapsAgent";
var description = "Agent that helps to get directions using Google Maps";
var instructions = "You should provide travel directions between two locations.";
var tools = await mcpClient.ListToolsAsync();

var agentClient = new ChatClientAgent(chatClient, name: name, description: description, instructions: instructions, tools: tools.Cast<AITool>().ToList());
var chatSession = await agentClient.CreateSessionAsync();

foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name}: {tool.Description}");
}

Console.WriteLine();

var prompt = "Get directions between Veldkant 35C, Kontich, Belgium and Jaarbeurs Utrecht";
await foreach (var response in agentClient.RunStreamingAsync(prompt, chatSession))
{
    Console.Write(response.Text);
}

Console.WriteLine();

chatSession.Debug();
