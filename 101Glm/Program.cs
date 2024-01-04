using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Net;

using Microsoft.SemanticKernel.TemplateEngine;
using System.Reflection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Net.Http.Json;
using System.Text.Json;
using Json.More;
using System.Text.Json.Nodes;
using SK.Common;





var builder = Kernel.CreateBuilder();
HttpClient client = new HttpClient(new ChatGlmClientHandler());
HttpClient client1 = new HttpClient(new TestClientHandler());
//OpenAIClient openAIClient = new OpenAIClient()
//builder.Services.wi
//builder.AddOpenAIChatCompletion("gpt-3.5-turbo-16k", "");
builder.AddOpenAIChatCompletion("gpt-3.5-turbo-16k", Utils.Key, httpClient: client);
builder.Plugins.AddFromType<LightPlugin>();


Kernel kernel = builder.Build();

var _service = kernel.GetRequiredService<IChatCompletionService>();
// Enable auto invocation of kernel functions
OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
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

    [KernelFunction, Description("Changes the state of the light.'")]
    public string ChangeState(bool newState)
    {
        IsOn = newState;
        var state = GetState();

        // Print the state to the console
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($"[Light is now {state}]");
        Console.ResetColor();

        return state;
    }

}// See https://aka.ms/new-console-template for more information

public class TestClientHandler : HttpClientHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var res = await base.SendAsync(request, cancellationToken);

        Console.WriteLine(await res.Content.ReadAsStringAsync());
        return res;

    }
}
public class ChatGlmClientHandler : HttpClientHandler
{
    static JsonObject ChangeToolCall2FunctionCall(JsonObject data)
    {
        var node = data["tools"].AsArray().Select(s => { var res = s["function"]; s.AsObject().Remove("function"); return res; });
        data.Remove("tools");

        JsonArray jsonArray = [.. node];
        data.Add("functions", jsonArray);

        data.Remove("tool_choice");
        data.Add("function_call", "auto");

        var messages = data["messages"].AsArray();
        string functionName = "";
        foreach (JsonObject mNode in messages)
        {
            if (mNode["tool_calls"] != null)
            {
                 var toolCalls = mNode["tool_calls"].AsArray();
                 mNode.Remove("tool_calls");
                    foreach (JsonObject tNode in toolCalls)
                    {
                        if (tNode["function"] == null)
                        {
                            continue;
                        }
                        var fNode = tNode["function"];
                        tNode.Remove("function");
                        tNode.Remove("id");
                        tNode.Remove("type");

                        functionName = fNode["name"].ToString();

                        mNode["function_call"] = fNode;
                        break;
                    }
            }
            if (mNode["role"].ToString() == "tool")
            {
                mNode["role"] = "function";

                mNode["name"] = functionName;

                mNode.Remove("tool_name");
                // mNode.Remove("tool_call_id");


            }
           
        }

        return data;
    }

    static JsonObject ChangeFunctionCall2ToolCall(JsonObject data)
    {
       var cArray = data["choices"].AsArray();
        foreach (var cNode in cArray)
        {
            var mNode = cNode["message"].AsObject();
            var fnode = mNode["function_call"];
            mNode.Remove("function_call");

            JsonObject tnode = new JsonObject();

            tnode["id"] = $"call_{Random.Shared.Next(100000000)}";
            tnode["type"] = "function";
            tnode["function"] = fnode;

            mNode.Add("tool_calls", new JsonArray(tnode));

            if (cNode["finish_reason"].ToString() == "function_call")
            {
                cNode["finish_reason"] = "tool_calls";
            }

        }

        return data;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri.LocalPath == "/v1/chat/completions")
        {
            request.RequestUri = new Uri("http://localhost:8000/v1/chat/completions");
        }
        Console.WriteLine(request.Content.ReadAsStringAsync().Result);
        await Console.Out.WriteLineAsync();
        var data = await request.Content.ReadFromJsonAsync<JsonObject>();
        
        var function_call = ChangeToolCall2FunctionCall(data);


        request.Content = JsonContent.Create(function_call);
        Console.WriteLine(request.Content.ReadAsStringAsync().Result);
        await Console.Out.WriteLineAsync();

        var res = await base.SendAsync(request, cancellationToken);
        Console.WriteLine(await res.Content.ReadAsStringAsync());
        await Console.Out.WriteLineAsync();

        data = await res.Content.ReadFromJsonAsync<JsonObject>();
        var tool_call = ChangeFunctionCall2ToolCall(data);
        
        res.Content = JsonContent.Create(tool_call);
        Console.WriteLine( await res.Content.ReadAsStringAsync());
        await Console.Out.WriteLineAsync();

        return res;
    }
}



