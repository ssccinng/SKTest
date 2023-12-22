// See https://aka.ms/new-console-template for more information
using System.Reflection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using System.ComponentModel;
using Microsoft.SemanticKernel.PromptTemplate.Handlebars;

KernelBuilder builder = new();
builder.AddOpenAIChatCompletion("gpt", "xxx", httpClient: new HttpClient(new ChatGlmClientHandler()));
Kernel kernel = builder.Build();

OpenAIPromptExecutionSettings settings = new()
{
    FunctionCallBehavior = FunctionCallBehavior.AutoInvokeKernelFunctions
};



settings.Temperature = 0.8;
settings.TopP = 0.8;
ChatHistory chatMessages = new ChatHistory();

//using StreamReader reader = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("prompts.Chat.yaml")!);


//KernelFunction prompt = kernel.CreateFunctionFromPromptYaml(
//    File.ReadAllText("prompts.Chat.yaml"), promptTemplateFactory: new HandlebarsPromptTemplateFactory()
//); ;

var aaa = """
    Bot: How can I help you?
    User: {{$input}}

    ---------------------------------------------

    The intent of the user in 5 words or less:
    """;

chatMessages.AddUserMessage("你好");
string userMessage = "你好";

var result = await kernel.InvokePromptAsync(@$"
        <message role=""system"">You are a helpful assistant.</message>
{{{{#each messages}}}}
  <message role=""{{{{Role}}}}"">{{{{~Content~}}}}</message>
{{{{/each}}}}",
    new(settings)
    {
        {"messages", chatMessages }
    }
);
//这种初始化方法是什么

//var aa = kernel.InvokeStreamingAsync<StreamingChatMessageContent>(prompt, new() { { "messages", chatMessages} }).GetAsyncEnumerator();
//while (await aa.MoveNextAsync())
//{
//    Console.WriteLine(aa.Current);
//}
//Console.WriteLine(aa);

//Console.ReadLine();
////await foreach (var message in );)
//{
//    Console.WriteLine(message);
//}

//var ada = new KernelArguments() { { "1", 1 } };
Console.WriteLine(result.ToString());
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