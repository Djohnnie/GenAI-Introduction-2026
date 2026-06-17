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
    Name = "GitHub",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-github"]
}));

var name = "GitHubAgent";
var description = "Agent that helps to get information from public GitHub repositories.";
var instructions = "You should provide information about public GitHub repositories.";
var tools = await mcpClient.ListToolsAsync();

var agentClient = new ChatClientAgent(chatClient, name: name, description: description, instructions: instructions, tools: tools.Cast<AITool>().ToList());
var chatSession = await agentClient.CreateSessionAsync();

foreach (var tool in tools)
{
    Console.WriteLine($"{tool.Name}: {tool.Description}");
}

Console.WriteLine();

var prompt = "Summarize the last 5 commits on the Djohnnie/MijnThuis repository.";
await foreach (var response in agentClient.RunStreamingAsync(prompt, chatSession))
{
    Console.Write(response.Text);
}

Console.WriteLine();

chatSession.Debug();