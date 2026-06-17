using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var chatClient = client.GetChatClient("gpt-4o");
var agentClient = chatClient.AsAIAgent(name: "Chat", description: "Just a chat");

var agentSession = await agentClient.CreateSessionAsync();
await agentClient.RunAsync("Start", agentSession);
var inMemoryState = agentSession.StateBag.GetValue<InMemoryChatHistoryProvider.State>(nameof(InMemoryChatHistoryProvider));
inMemoryState.Messages.Add(new ChatMessage(ChatRole.System, "You should answer as a 10-year old child."));

int age = 10;

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("User > ");
    Console.ForegroundColor = ConsoleColor.White;
    var request = Console.ReadLine();
    inMemoryState.Messages.Add(new ChatMessage(ChatRole.User, request!));

    string fullMessage = "";
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Assistant > ");

    await foreach (var response in agentClient.RunStreamingAsync(agentSession))
    {
        fullMessage += response.Text;
        Console.Write(response.Text);
    }

    Console.WriteLine();

    age -= 2;
    inMemoryState.Messages.Add(new ChatMessage(ChatRole.System, $"You should answer as a {age}-year old child and tell us your age after every response."));
}