using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.SemanticFunctions;
using System.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using System.Text.Json;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;

var config =
    new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", true)
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConfiguration(config.GetSection("Logging"))
        .ClearProviders()
        .AddConsole();
});


var kernel = Kernel.Builder
    .WithConfiguration(new KernelConfig()
        .AddAzureTextCompletionService(
            deploymentName: config["OpenAI:TextCompletion:DeploymentName"]!,
            endpoint: config["OpenAI:Endpoint"]!,
            apiKey: config["OpenAI:ApiKey"]!
        )
        .AddAzureChatCompletionService(
            deploymentName: config["OpenAI:ChatCompletion:DeploymentName"]!,
            endpoint: config["OpenAI:Endpoint"]!,
            apiKey: config["OpenAI:ApiKey"]!
        )
        .AddAzureTextEmbeddingGenerationService(
            deploymentName: config["OpenAI:TextEmbedding:DeploymentName"]!,
            endpoint: config["OpenAI:Endpoint"]!,
            apiKey: config["OpenAI:ApiKey"]!
        )
    )
    .WithLogger(loggerFactory.CreateLogger<Kernel>())
    .WithMemoryStorage(new VolatileMemoryStore())
    .Build();


// === CreateSemanticFunction ===
// var function01 = kernel.CreateSemanticFunction("{{$input}}");
// Console.WriteLine(await function01.InvokeAsync("2+2=")); // "2+2=" -> "4"


// === RegisterSemanticFunction ===
// var promptTemplateConfig = new PromptTemplateConfig();
// var promptTemplate = new PromptTemplate("({{$input}})^2=", promptTemplateConfig, kernel);
// var functionConfig = new SemanticFunctionConfig(promptTemplateConfig, promptTemplate);
// var function02 = kernel.RegisterSemanticFunction("TestFunction", functionConfig);
// Console.WriteLine(await function02.InvokeAsync("3+3")); // "(3+3)^2=" -> "36"


// === SemanticFunction Chaining ===
// Console.WriteLine(await kernel.RunAsync("4+4=", function01, function02)); // "(4+4)^2=" -> "64"


// === ContextVariables ===
// const string prompt = @"
// {{$history}}
// Human: {{$human_input}}
// AI:";
// var chatFunction = kernel.CreateSemanticFunction(prompt, "Chat", "ChatBot");
// var context = new ContextVariables { ["history"] = "" };
// Func<string, Task<string>> chat = async (human_input) =>
// {
//     context["human_input"] = human_input;
//     var ai_answer = await kernel.RunAsync(context, chatFunction);
//     context["history"] += $@"
// Human: {context["human_input"]}
// AI: {ai_answer}";
//     return ai_answer.ToString();
// };
// Console.WriteLine(await chat("6+6=")); // 12
// Console.WriteLine(await chat("<previous>-2=")); // 10
// Console.WriteLine(await chat("<previous>*3=")); // 30


// === SequentialPlanner ===
// var mathSkill = kernel.ImportSemanticSkillFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Skills"), "Math"); // {"Power": ..., "Add": ...}
// var functionsView = kernel.Skills.GetFunctionsView(); // NativeFunction: {}, SemanticFunctions: {"Math": {"Power": ..., "Add": ...}}
// var planner = new SequentialPlanner(kernel);
// var prompt = "I need to add 2 to 3 and then raise the result to power of 2";
// var plan = await planner.CreatePlanAsync(prompt); // Math.Add(3, 2) -> Math.Power(5, 2)
// Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
// Console.WriteLine(await kernel.RunAsync(plan)); // 25


// === Embeddings / Memory / TextMemorySkill ===
// const string MemoryCollectionName = "variables";
// await kernel.Memory.SaveInformationAsync(MemoryCollectionName, id: "info1", text: "x=123");
// await kernel.Memory.SaveInformationAsync(MemoryCollectionName, id: "info2", text: "y=321");
// await foreach (var memoryQueryResult in kernel.Memory.SearchAsync(MemoryCollectionName, query: "x", limit: 2))
// {
//     Console.WriteLine(JsonSerializer.Serialize(memoryQueryResult, new JsonSerializerOptions { WriteIndented = true }));
// }
// kernel.ImportSkill(new TextMemorySkill());
// const string prompt = @"
// {{recall $a}}
// {{recall $b}}
// {{$a}}+{{$b}}=";
// var context = new ContextVariables
// {
//     ["a"] = "x",
//     ["b"] = "y",
//     [TextMemorySkill.CollectionParam] = MemoryCollectionName
// };
// var function = kernel.CreateSemanticFunction(prompt);
// Console.WriteLine(await kernel.RunAsync(context, function)); // "123+321=" -> "444"


// === ChatGPT ===
var chatCompletion = kernel.GetService<IChatCompletion>();
var chat = (OpenAIChatHistory)chatCompletion.CreateNewChat();
chat.AddUserMessage("2+2=");
chat.AddAssistantMessage(await chatCompletion.GenerateMessageAsync(chat));
Console.WriteLine(chat.Messages[^1].Content);


// TODO: ActionPlanner
// TODO: ISemanticTextMemory


Debugger.Break();
