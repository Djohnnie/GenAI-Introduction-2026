using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ClientModel;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var name = "TranslationAgent";
var description = "Agent that knows about translating text between languages.";
var instructions = "You should respond to translation requests.";

var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var chatClient = openAIClient.GetChatClient("gpt-4o").AsIChatClient();
var agentClient = new ChatClientAgent(chatClient, name: name, description: description, instructions: instructions);

var chatSession = await agentClient.CreateSessionAsync();

var textContent = new TextContent("Translate the text in the image to Dutch.");
var urlContent = new UriContent("https://djohnnie.blob.core.windows.net/temp/quote.jpg", "image/jpeg");
var binaryData = await File.ReadAllBytesAsync("quote.jpg");
var inlineImageContent = new DataContent(binaryData, "image/jpeg");

var response1 = await agentClient.RunAsync(new ChatMessage(ChatRole.User, [textContent, urlContent]), chatSession);
var response2 = await agentClient.RunAsync(new ChatMessage(ChatRole.User, [textContent, inlineImageContent]), chatSession);

Console.WriteLine(response1.Text);
Console.WriteLine("--------------------------------------------------");
Console.WriteLine(response2.Text);