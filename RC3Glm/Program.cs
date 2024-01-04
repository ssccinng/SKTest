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
builder.Plugins.AddFromType<LightPlugin>();


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

while (true)
{
    // Get the user's message
    Console.Write("User > ");
    var userMessage = Console.ReadLine()!;
    chatMessageContents.AddUserMessage(userMessage);


    var result = await _service.GetChatMessageContentAsync(chatMessageContents, settings, kernel);

    chatMessageContents.AddAssistantMessage(result.Content);

    Console.WriteLine($"Assistant > {result}");
}



public class LightPlugin
{
    public bool IsOn { get; set; }

    [KernelFunction, Description("Gets the state of the light.")]
    public string GetState() => IsOn ? "on" : "off";

}// See https://aka.ms/new-console-template for more information


public class ChatGlmClientHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri.LocalPath == "/v1/chat/completions")
        {
            request.RequestUri = new Uri("http://localhost:8000/v1/chat/completions");
        }
        Console.WriteLine(await request.Content.ReadAsStringAsync());
        var res = await base.SendAsync(request, cancellationToken);
        await Console.Out.WriteLineAsync(await res.Content.ReadAsStringAsync());
        return res;
    }
}