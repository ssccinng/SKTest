// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using System.Text;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;

using NAudio.Wave;
using System.Runtime.InteropServices;
using Whisper.net;
using Whisper.net.Ggml;
using System.Diagnostics;

var modelName = "ggml-medium.bin";
//if (!File.Exists(modelName))
//{
//    using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Medium);
//    using var fileWriter = File.OpenWrite(modelName);
//    await modelStream.CopyToAsync(fileWriter);
//}

//using var whisperFactory = WhisperFactory.FromPath("ggml-medium.bin");

//using var processor = whisperFactory.CreateBuilder()
//        .WithLanguage("chinese")
//        .Build();

var builder = new KernelBuilder();
builder.AddOpenAIChatCompletion("chatglm", "---",
        httpClient: new HttpClient(new ChatglmHandler())
        );

builder.Plugins.AddFromObject(new RobotPlugin());

Kernel kernel = builder.Build();

var chat = kernel.GetRequiredService<IChatCompletionService>();

OpenAIPromptExecutionSettings settings = new()
{
    FunctionCallBehavior = FunctionCallBehavior.AutoInvokeKernelFunctions,
    Temperature = 0.8,
    TopP = 0.8
};


string taskSplitPrompt =
"""
You are a very useful task decomposition assistant.
Your task is to break down the dialogue entered by the user into a single task list and return it, with one task per line.
If there are no tasks in this sentence, please return 'No tasks detected'.
You don't need to reply to anything other than the decomposed content.
""";

System.Console.Write("User -> ");
ChatHistory taskSpiltHistory = new();
taskSpiltHistory.AddSystemMessage(taskSplitPrompt);
taskSpiltHistory.AddUserMessage("把水和橘子放到1点");
taskSpiltHistory.AddAssistantMessage("把水放到1点\n把橘子放到1点");

var taskExcutePrompt = """
        You are a very helpful AI assistant for assisting robotic arms in executing tasks,
        Now you need to execute the specified task based on the user's conversation.
        If the user selects multiple items at once, please call the function multiple times separately.
        For non robotic arm tasks, you should reply, 'I'm sorry, I don't seem to understand this task.'
        """;
var taskExcutePrompt1 = """
        You are a very helpful AI assistant for assisting robotic arms in executing tasks,
        Now you need to execute the specified task based on the user's conversation.
        """;


// taskSpiltHistory.AddUserMessage("把扇子和工具盒放到B点");
// taskSpiltHistory.AddAssistantMessage("把水放到B点\n把橘子放到B点");
int idx = 0;
while (true)
{


    // var userMessage = await RecordTest(idx++);
    var userMessage = Console.ReadLine();

    // Console.WriteLine(userMessage);
    taskSpiltHistory.AddUserMessage(userMessage);
    // Console.WriteLine(userMessage);
    var result = await chat.GetChatMessageContentAsync(taskSpiltHistory, settings, kernel);

    taskSpiltHistory.RemoveAt(taskSpiltHistory.Count - 1);

    Console.WriteLine("分解后任务");
    Console.WriteLine(result.Content);

    var taskList = result.Content.Split('\n');

    foreach (var task in taskList)
    {
        ChatHistory chathistory = new();
        chathistory.AddSystemMessage(
            taskExcutePrompt1
        );
        // Console.WriteLine(userMessage);
        //chathistory.AddUserMessage("把水放到A点");
        //chathistory.AddAssistantMessage(
        //    """
        //    {"station": "A", "name": "水"}
        //    """);
        chathistory.AddUserMessage(task);

        result = await chat.GetChatMessageContentAsync(chathistory, settings, kernel);
        System.Console.WriteLine("Assistant -> {0}", result.Content);

        // The destination workstation can only be A, B, C, D
        // Console.ReadKey();
    }


    System.Console.Write("User -> ");
}


Console.InputEncoding = Encoding.UTF8;
System.Console.Write("User -> ");
while (true)
{
    ChatHistory chathistory = new();
    chathistory.AddSystemMessage(
        """
        You are a very helpful AI assistant for assisting robotic arms in executing tasks,
        Now you need to execute the specified task based on the user's conversation.
        If the user selects multiple items at once, please call the function multiple times separately.
        For non robotic arm tasks, you should reply, 'I'm sorry, I don't seem to understand this task.'
        """);
    var userMessage = Console.ReadLine();
    // Console.WriteLine(userMessage);
    chathistory.AddUserMessage(userMessage);
    // Console.WriteLine(userMessage);
    var result = await chat.GetChatMessageContentAsync(chathistory, settings, kernel);
    chathistory.AddAssistantMessage(result.Content);

    System.Console.WriteLine("Assistant -> {0}", result.Content);

    System.Console.Write("User -> ");
}




async Task<string> RecordTest(int i)
{
    var waveIn = new WaveInEvent();
    WaveFileWriter writer = null;
    writer = new WaveFileWriter($"dani{i}.wav", waveIn.WaveFormat);
    waveIn.StartRecording();

    waveIn.DataAvailable += (s, a) =>
    {
        writer.Write(a.Buffer, 0, a.BytesRecorded);
    };
    waveIn.RecordingStopped += (s, a) =>
    {
        int cc = i;
        try
        {
            writer?.Dispose();
            writer = null;

            waveIn.Dispose();
            using (var reader = new WaveFileReader($"dani{cc}.wav"))
            {
                var outFormat = new WaveFormat(16000, reader.WaveFormat.Channels);
                using (var resampler = new MediaFoundationResampler(reader, outFormat))
                {
                    // resampler.ResamplerQuality = 60;
                    WaveFileWriter.CreateWaveFile($"1dani{cc}.wav", resampler);
                    System.Console.WriteLine("success");
                }
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    };



    Console.WriteLine("正在录制....");
    Console.ReadLine();


    waveIn.StopRecording();


    await Task.Delay(1000);
    //Console.ReadKey();

    // var modelName = "ggml-base.bin";
    // if (!File.Exists(modelName))
    // {
    //     using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base);
    //     using var fileWriter = File.OpenWrite(modelName);
    //     await modelStream.CopyToAsync(fileWriter);
    // }


    string wavFileName = $"1dani{i}.wav";
    using var fileStream = File.OpenRead(wavFileName);
    string rr = "";
    Stopwatch stopwatch = Stopwatch.StartNew();

    Console.WriteLine("语音识别结果");

    //await foreach (var result in processor.ProcessAsync(fileStream))
    //{
    //    if (rr == "") rr = result.Text;
    //    System.Console.WriteLine(result.Text);
    //}
    //System.Console.WriteLine("识别耗时: {0}", stopwatch.ElapsedMilliseconds);
    return rr;
}


public class RobotPlugin
{
    [KernelFunction, Description("执行机器人任务")]
    public string GetTask(
         [Description("目标工位")] int station,
         [Description("物品名称")] string name
        //[Description("destination workstation")] string station,
        //[Description("item name")] string name
        )
    {
        System.Console.WriteLine($"-{station}- -{name}-");
        return "完成";
    }


    // [KernelFunction, Description("执行机器人拣选任务")]




}



public class ChatglmHandler : HttpClientHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri.LocalPath == "/v1/chat/completions")
        {
            request.RequestUri = new Uri("http://127.0.0.1:8000/v1/chat/completions");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
