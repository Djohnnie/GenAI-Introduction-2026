using AgentFramework.Common;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var chatClient = client.GetChatClient("gpt-4o");
var agentClient = chatClient.AsAIAgent(name: "Chat", description: "Just a chat");

var chatSession = await agentClient.CreateSessionAsync();
var request = "Generate 5 random employees with full name, age and function title.";
var response = await agentClient.RunAsync<List<Employee>>(request!, chatSession);

foreach (var employee in response.Result)
{
    Console.WriteLine(JsonSerializer.Serialize(employee, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine();
}

Console.WriteLine();

chatSession.Debug();
public record Employee(string FullName, int Age, string FunctionTitle);