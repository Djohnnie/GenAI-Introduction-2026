using AgentFramework.AzureOpenAiAgent;
using AgentFramework.Common;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var tools = GeneralPlugin.GetTools();
var name = "TimeAgent";
var description = "Agent that knows about the current date and time.";
var instructions = "You should only reply on questions related to the current date and time and never any other questions.";

var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var chatClient = openAIClient.GetChatClient("gpt-5-chat").AsIChatClient();
var agentClient = new ChatClientAgent(chatClient, name: name, description: description, instructions: instructions, tools: tools);

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