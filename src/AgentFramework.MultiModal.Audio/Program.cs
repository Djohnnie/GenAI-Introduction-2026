using Azure.AI.OpenAI;
using System.ClientModel;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var openAIClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var audioClient = openAIClient.GetAudioClient("whisper");

using (var binaryData1 = File.OpenRead(@"dag-vriendjes.mp3"))
{
    var response1 = await audioClient.TranscribeAudioAsync(binaryData1, "dag-vriendjes.mp3");
    Console.WriteLine(response1.Value.Text);
}

Console.WriteLine("--------------------------------------------------");

using (var binaryData2 = File.OpenRead(@"dag-vriendjes.mp3"))
{
    var response2 = await audioClient.TranslateAudioAsync(binaryData2, "dag-vriendjes.mp3");
    Console.WriteLine(response2.Value.Text);
}