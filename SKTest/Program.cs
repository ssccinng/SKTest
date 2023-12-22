using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using System.ComponentModel;
using System.Net;

using Microsoft.SemanticKernel.TemplateEngine;
using System.Reflection;





KernelBuilder builder = new();
HttpClient client = new HttpClient(new ChatGlmClientHandler());
//OpenAIClient openAIClient = new OpenAIClient()
//builder.Services.wi
builder.AddOpenAIChatCompletion("gpt", "xxx", httpClient: client);
builder.Plugins.AddFromType<TestPlugin>();


Kernel kernel = builder.Build();
 
var _service = kernel.GetRequiredService<IChatCompletionService>();
// Enable auto invocation of kernel functions
OpenAIPromptExecutionSettings settings = new()
{
    FunctionCallBehavior = FunctionCallBehavior.AutoInvokeKernelFunctions
};


 
settings.Temperature = 0.8;
settings.TopP = 0.8;
//settings.ChatSystemPrompt = "你是一个ai助手，在你回复的每一句话后面都要加上洛托";

ChatHistory chatMessageContents = new ChatHistory();

chatMessageContents.AddSystemMessage("你是一个ai助手，在你回复的每一句话的最后都要加上洛托");
//chatMessageContents.AddAssistantMessage("阿罗拉~, 我是洛托姆图鉴。有什么能帮到你的洛?");
//Console.WriteLine("Assistant > " + chatMessageContents[1].Content);
// Start a chat session
while (true)
{
    // Get the user's message
    Console.Write("User > ");
    var userMessage = Console.ReadLine()!;
    // Invoke the kernel
    //var results = await kernel.InvokePromptAsync(userMessage, new(settings));
    chatMessageContents.AddUserMessage(userMessage);


    var result = await _service.GetChatMessageContentAsync(chatMessageContents, settings, kernel);

    
    //var result = await kernel.InvokePromptAsync(
    //chatMessageContents.Select(s => s.),
    //arguments: new(settings) {

    //    //{ "messages", chatMessageContents }, 
    //});

    chatMessageContents.AddAssistantMessage(result.Content);
    //kernel.InvokePromptAsync(chatMessageContents., new(settings));
    // Print the results
    Console.WriteLine($"Assistant > {result}");
}


public class TestPlugin
{
    [KernelFunction, Description("获取现在时间")]
    public string GetTime()
    {
        return DateTime.Now.ToString("F");
    }

    [KernelFunction, Description("随机切换风格")]
    public string ChangeConsole()
    {
        Console.BackgroundColor = Random.Shared.GetItems(Enum.GetValues<ConsoleColor>(), 1)[0];
        return Console.BackgroundColor.ToString();
    }
}


public class LightPlugin
{
    public bool IsOn { get; set; }

    [KernelFunction, Description("Gets the state of the light.")]
    public string GetState() => IsOn ? "on" : "off";

    //[KernelFunction, Description("Changes the state of the light.'")]
    //public string ChangeState(bool newState)
    //{
    //    IsOn = newState;
    //    var state = GetState();

    //    // Print the state to the console
    //    Console.ForegroundColor = ConsoleColor.DarkBlue;
    //    Console.WriteLine($"[Light is now {state}]");
    //    Console.ResetColor();

    //    return state;
    //}

}// See https://aka.ms/new-console-template for more information


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