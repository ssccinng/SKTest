// See https://aka.ms/new-console-template for more information
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SK.Common;
using System.ComponentModel;

var builder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-3.5-turbo-16k", Utils.Key, httpClient: new HttpClient());
builder.Plugins.AddFromType<TimePlugin>();

// 试一下

var kernel = builder.Build();


var request = Console.ReadLine()!;

var prompt =
    $"""
    What isw the intent of this request? {request}
    You can choose between SendEmail, SendMessage, CompleteTask, CreateDocument
    """;

prompt = @$"Instructions: What is the intent of this request?
Choices: SendEmail, SendMessage, CompleteTask, CreateDocument.
User Input: {request}
Intent: ";

prompt = @$"<message role=""system"">Instructions: What is the intent of this request?
If you don't know the intent, don't guess; instead respond with ""Unknown"".
Choices: SendEmail, SendMessage, CompleteTask, CreateDocument, Unknown.
Bonus: You'll get $20 if you get this right.</message>

<message role=""system"">User Input:</message>
<message role=""user""> {request}</message>:
<message role=""system"">Intent: </message>"
;

//var res = await kernel.InvokePromptAsync(prompt);
var chatService = kernel.GetRequiredService<IChatCompletionService>();

ChatHistory history = [];

history.AddSystemMessage(@"Instructions: What is the intent of this request?
If you don't know the intent, don't guess; instead respond with ""Unknown"".
Choices: SendEmail, SendMessage, CompleteTask, CreateDocument, Unknown.
Bonus: You'll get $20 if you get this right.");
history.AddSystemMessage("User Input:");
history.AddUserMessage(request);
history.AddSystemMessage("Intent: ");


OpenAIPromptExecutionSettings Settings = new OpenAIPromptExecutionSettings()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

history = [];

history.AddUserMessage(request);

var res = await chatService.GetChatMessageContentAsync(history, Settings, kernel: kernel);

Console.WriteLine(res.Content);



public class ChatGlmClientHandler : HttpClientHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri.LocalPath == "/v1/chat/completions")
        {
            request.RequestUri = new Uri("http://localhost:8000/v1/chat/completions");
        }
        return base.SendAsync(request, cancellationToken);
    }
}

class ProxyOpenAIHandler : HttpClientHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.Host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase))
        {
            // your proxy url
            request.RequestUri = new Uri($"http://localhost:8000{request.RequestUri.PathAndQuery}");
        }
        return base.SendAsync(request, cancellationToken);
    }
}


public class TimePlugin
{
    [KernelFunction, Description("Get Now Time")]
    public string GetNowTime() => DateTime.Now.ToString();
}


