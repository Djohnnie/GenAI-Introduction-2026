using AgentFramework.Common;
using AgentFramework.OllamaAgent;
using Microsoft.Agents.AI;
using OllamaSharp;

var endpoint = "http://localhost:11434/";
var model = "llama3.2:3b";

var tools = GeneralPlugin.GetTools();
var name = "TimeAgent";
var description = "Agent that knows about the current date and time.";
var instructions = "You should only reply on questions related to the current date and time and never any other questions.";

var ollamaClient = new OllamaApiClient(new Uri(endpoint), model);
var agentClient = new ChatClientAgent(ollamaClient, name: name, description: description, instructions: instructions, tools: tools);

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