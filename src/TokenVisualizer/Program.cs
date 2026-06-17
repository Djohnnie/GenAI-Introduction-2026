using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using Spectre.Console;

// -- Colour palette cycling across tokens
var palette = new[]
{
    "yellow", "cyan", "green", "magenta", "dodgerblue2",
    "orange1", "mediumpurple1", "chartreuse2", "hotpink", "gold1",
    "aquamarine1", "deeppink2", "greenyellow", "cornflowerblue", "lightsalmon1"
};

// -- Banner
AnsiConsole.Write(new FigletText("Token Visualizer").Centered().Color(Color.Cyan1));
AnsiConsole.Write(new Rule("[bold deepskyblue1]Powered by Microsoft.Extensions.AI  ·  Azure OpenAI  ·  GPT-4o tokenizer[/]")
    .RuleStyle("grey23").Centered());
AnsiConsole.WriteLine();

// -- Read configuration from environment variables
var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");
var key      = Environment.GetEnvironmentVariable("OPENAI_KEY");
var model    = Environment.GetEnvironmentVariable("OPENAI_MODEL");

if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model))
{
    AnsiConsole.MarkupLine("[bold red]Missing configuration.[/] Please set the following environment variables:");
    AnsiConsole.MarkupLine("  [yellow]OPENAI_ENDPOINT[/]  -- Azure OpenAI endpoint URL");
    AnsiConsole.MarkupLine("  [yellow]OPENAI_KEY[/]       -- Azure OpenAI API key");
    AnsiConsole.MarkupLine("  [yellow]OPENAI_MODEL[/]     -- Deployment/model name");
    return;
}

// -- Build IChatClient via Microsoft.Extensions.AI
IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key))
    .GetChatClient(model)
    .AsIChatClient();

// -- Tokenizer (GPT-4o uses o200k_base)
var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");

// -- Conversation history
var history = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant. Give clear, concise answers.")
};

AnsiConsole.MarkupLine("[grey]Chat mode active -- conversation history is maintained across turns.[/]");
AnsiConsole.MarkupLine("[grey]Commands: [bold]quit[/] to exit · [bold]clear[/] to reset history.[/]");
AnsiConsole.WriteLine();

// -- Main chat loop
while (true)
{
    var input = AnsiConsole.Ask<string>("[bold yellow]You[/] [grey]>[/]");
    AnsiConsole.WriteLine();

    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        history.RemoveAll(m => m.Role != ChatRole.System);
        AnsiConsole.MarkupLine("[grey]Conversation history cleared.[/]");
        AnsiConsole.WriteLine();
        continue;
    }

    if (string.IsNullOrWhiteSpace(input))
        continue;

    // -- Visualize INPUT tokens (current message only, for colour display)
    AnsiConsole.Write(new Rule("[bold yellow] Your Message — Token Stream [/]").RuleStyle("yellow").Centered());
    AnsiConsole.WriteLine();
    VisualizeTokens(tokenizer, input, palette);

    // -- Count message-only tokens before adding to history
    var localMessageCount = tokenizer.CountTokens(input);

    // -- Call Azure OpenAI
    history.Add(new ChatMessage(ChatRole.User, input));

    // -- Count tokens across the full history (what is actually sent to the API)
    var localHistoryCount = CountHistoryTokens(tokenizer, history);

    ChatResponse? response = null;

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots2)
        .SpinnerStyle(Style.Parse("cyan"))
        .StartAsync("[cyan]Waiting for AI response...[/]", async ctx =>
        {
            response = await chatClient.GetResponseAsync(history);
        });

    var responseText = response!.Text ?? string.Empty;
    history.Add(new ChatMessage(ChatRole.Assistant, responseText));

    // -- Display AI response
    AnsiConsole.Write(new Rule("[bold green] AI Response [/]").RuleStyle("green").Centered());
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Panel(new Markup(Markup.Escape(responseText)))
        .BorderStyle(Style.Parse("green"))
        .Expand());
    AnsiConsole.WriteLine();

    // -- Visualize OUTPUT tokens
    AnsiConsole.Write(new Rule("[bold green] AI Response — Token Stream [/]").RuleStyle("green").Centered());
    AnsiConsole.WriteLine();
    VisualizeTokens(tokenizer, responseText, palette);
    var localOutputCount = tokenizer.CountTokens(responseText);

    // -- Comparison: local tokenizer (full history) vs API reported usage
    var usage = response.Usage;
    ShowComparison(localMessageCount, localHistoryCount, localOutputCount, usage);

    AnsiConsole.Write(new Rule().RuleStyle("grey23"));
    AnsiConsole.WriteLine();
}

AnsiConsole.MarkupLine("[bold green] Goodbye! [/]");

// -- Token visualization: colours only, no return value needed
static void VisualizeTokens(TiktokenTokenizer tokenizer, string text, string[] palette)
{
    var tokens = tokenizer.EncodeToTokens(text, out _);

    var line = new Paragraph();
    for (var i = 0; i < tokens.Count; i++)
    {
        var color = Color.FromConsoleColor((ConsoleColor)(1 + i % 14));
        line.Append(tokens[i].Value, new Style(color, Color.Grey11));
    }

    AnsiConsole.Write(line);
    AnsiConsole.WriteLine();
    AnsiConsole.WriteLine();
}

// -- Sum token counts for every message in the conversation history
static int CountHistoryTokens(TiktokenTokenizer tokenizer, IList<ChatMessage> history)
{
    // Each message in the OpenAI chat format carries ~3 overhead tokens for role markers.
    // We add that overhead here so the comparison is as accurate as possible.
    const int OverheadPerMessage = 3;
    const int ReplyPrimer        = 3; // tokens appended by the API before the assistant replies

    var contentTokens = history.Sum(m => tokenizer.CountTokens(m.Text ?? string.Empty));
    return contentTokens + (history.Count * OverheadPerMessage) + ReplyPrimer;
}

// -- Comparison table: local tokenizer counts vs API-reported usage
static void ShowComparison(int messageOnly, int localInput, int localOutput, UsageDetails? usage)
{
    AnsiConsole.Write(new Rule("[bold deepskyblue1] Token Count Comparison [/]").RuleStyle("deepskyblue1").Centered());
    AnsiConsole.WriteLine();

    var apiInput  = (long?)usage?.InputTokenCount;
    var apiOutput = (long?)usage?.OutputTokenCount;
    var apiTotal  = (long?)usage?.TotalTokenCount;

    var table = new Table()
        .Border(TableBorder.Rounded)
        .BorderStyle(Style.Parse("deepskyblue1"))
        .Expand()
        .AddColumn(new TableColumn("[bold]Metric[/]"))
        .AddColumn(new TableColumn("[bold orange1]Local (message)[/]").Centered())
        .AddColumn(new TableColumn("[bold yellow]Local (full history)[/]").Centered())
        .AddColumn(new TableColumn("[bold cyan]Azure OpenAI API[/]").Centered())
        .AddColumn(new TableColumn("[bold]Delta[/]").Centered());

    // Input row: message-only, full history, API
    table.AddRow(
        "[white]Input tokens[/]",
        $"[orange1]{messageOnly}[/]",
        $"[yellow]{localInput}[/]",
        apiInput.HasValue ? $"[cyan]{apiInput}[/]" : "[grey]n/a[/]",
        apiInput.HasValue ? FormatDelta(localInput, apiInput.Value) : "[grey]—[/]"
    );

    // Output row: message-only and full history are both just the response text
    table.AddRow(
        "[white]Output tokens[/]",
        $"[orange1]{localOutput}[/]",
        $"[yellow]{localOutput}[/]",
        apiOutput.HasValue ? $"[cyan]{apiOutput}[/]" : "[grey]n/a[/]",
        apiOutput.HasValue ? FormatDelta(localOutput, apiOutput.Value) : "[grey]—[/]"
    );

    // Total row
    var localTotal = localInput + localOutput;
    table.AddRow(
        "[grey]Total tokens[/]",
        $"[grey]{messageOnly + localOutput}[/]",
        $"[yellow]{localTotal}[/]",
        apiTotal.HasValue ? $"[cyan]{apiTotal}[/]" : "[grey]n/a[/]",
        apiTotal.HasValue ? FormatDelta(localTotal, apiTotal.Value) : "[grey]—[/]"
    );

    AnsiConsole.Write(table);
    AnsiConsole.MarkupLine("[grey italic]  Local (full history) = content tokens for all messages + ~3 overhead per message + 3 reply-primer tokens.[/]");
    AnsiConsole.WriteLine();
}

static string FormatDelta(long local, long api)
{
    var delta = api - local;
    return delta switch
    {
        0     => "[bold green]= 0[/]",
        > 0   => $"[bold orange1]+ {delta}[/]",
        _     => $"[bold red]{delta}[/]"
    };
}
