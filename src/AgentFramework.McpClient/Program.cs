using AgentFramework.Common;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using System.ClientModel;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var chatClient = openAIClient.GetChatClient("gpt-5-chat").AsIChatClient();

await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "Clock",
    Command = "docker",
    Arguments = ["run", "-i", "--rm", "djohnnie/clockmcp"]
}));

var name = "TimeAgent";
var description = "Agent that knows about the current date and time.";
var instructions = "You should only reply on questions related to the current date and time and never any other questions.";
var tools = await mcpClient.ListToolsAsync();

var agentClient = new ChatClientAgent(chatClient, name: name, description: description, instructions: instructions, tools: tools.Cast<AITool>().ToList());
var chatSession = await agentClient.CreateSessionAsync();

foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name}: {tool.Description}");
}

Console.WriteLine();

var prompt = "What is the current date and time?";
await foreach (var response in agentClient.RunStreamingAsync(prompt, chatSession))
{
    Console.Write(response.Text);
}

Console.WriteLine();

chatSession.Debug();

Console.ReadKey();