// See https://aka.ms/new-console-template for more information
using Azure.Core;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};


var kernel = Kernel.CreateBuilder()
           .AddOpenAIChatCompletion(
               "chatglm-3", "----", httpClient: new HttpClient(new ProxyOpenAIHandler()))
           .Build();


Console.Write("Your request: ");
string request = Console.ReadLine()!;
var prompt = @$"What is the intent of this request? {request}
You can choose between SendEmail, SendMessage, CompleteTask, CreateDocument.";

prompt = @$"Instructions: What is the intent of this request?
Choices: SendEmail, SendMessage, CompleteTask, CreateDocument.
User Input: {request}
Intent: ";

prompt = @$"## Instructions
Provide the intent of the request using the following format:

```json
{{
    ""intent"": {{intent}}
}}
```

## Choices
You can choose between the following intents:

```json
[""SendEmail"", ""SendMessage"", ""CompleteTask"", ""CreateDocument""]
```

## User Input
The user input is:

```json
{{
    ""request"": ""{request}""
}}
```

## Intent";

var history = @"<message role=""user"">I hate sending emails, no one ever reads them.</message>
<message role=""assistant"">I'm sorry to hear that. Messages may be a better way to communicate.</message>";

prompt = @$"<message role=""system"">Instructions: What is the intent of this request?
If you don't know the intent, don't guess; instead respond with ""Unknown"".
Choices: SendEmail, SendMessage, CompleteTask, CreateDocument, Unknown.</message>

<message role=""user"">Can you send a very quick approval to the marketing team?</message>
<message role=""system"">Intent:</message>
<message role=""assistant"">SendMessage</message>

<message role=""user"">Can you send the full update to the marketing team?</message>
<message role=""system"">Intent:</message>
<message role=""assistant"">SendEmail</message>

{history}
<message role=""user"">{request}</message>
<message role=""system"">Intent:</message>";


var res = await kernel.InvokePromptAsync(prompt);

Console.WriteLine(res.ToString());

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