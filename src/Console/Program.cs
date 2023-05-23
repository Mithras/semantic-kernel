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
using Microsoft.SemanticKernel.Reliability;
using Microsoft.SemanticKernelTest.Skills;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticKernel.AI.TextCompletion;

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


var kernelBuilder = new KernelBuilder()
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
    // DefaultHttpRetryHandlerFactory -> DefaultHttpRetryHandler(HttpRetryConfig)
    //  .SetDefaultHttpRetryConfig(new HttpRetryConfig { ... })
    //  .SetHttpRetryHandlerFactory(...)
    )
    .WithLogger(loggerFactory.CreateLogger<Kernel>())
    .WithMemoryStorage(new VolatileMemoryStore());
var kernel = kernelBuilder.Build();


// === CreateSemanticFunction ===
// var function01 = kernel.CreateSemanticFunction("{{$input}}", "function01");
// Console.WriteLine(await function01.InvokeAsync("2+2=")); // "2+2=" -> "4"


// === RegisterSemanticFunction ===
// var promptTemplateConfig = new PromptTemplateConfig();
// var promptTemplate = new PromptTemplate("({{$input}})^2=", promptTemplateConfig, kernel);
// var functionConfig = new SemanticFunctionConfig(promptTemplateConfig, promptTemplate);
// var function02 = kernel.RegisterSemanticFunction("MySkill", "function02", functionConfig);
// Console.WriteLine(await function02.InvokeAsync("3+3")); // "(3+3)^2=" -> "36"


// === SemanticFunction Chaining ===
// function01 = kernel.Func("_GLOBAL_FUNCTIONS_", "function01");
// function02 = kernel.Func("MySkill", "function02");
// Console.WriteLine(await kernel.RunAsync("4+4=", function01, function02)); // "(4+4)^2=" -> "64"


// === ContextVariables ===
// const string prompt = @"
// {{$history}}
// Human: {{$input}}
// AI:";
// var chatFunction = kernel.CreateSemanticFunction(prompt, "Chat", "ChatBot");
// var context = new ContextVariables { ["history"] = "" };
// Func<string, Task<string>> chat = async (input) =>
// {
//     context.Update(input);
//     var ai_answer = await kernel.RunAsync(context, chatFunction);
//     context["history"] += $@"
// Human: {context.Input}
// AI: {ai_answer}";
//     return ai_answer.ToString();
// };
// Console.WriteLine(await chat("6+6=")); // 12
// Console.WriteLine(await chat("<previous>-2=")); // 10
// Console.WriteLine(await chat("<previous>*3=")); // 30


// === ActionPlanner ===
// var nativeMathSkill = kernel.ImportSkill(new NativeMath(), "NativeMath"); // {"Add": ..., "Subtract": ...}
// var semanticMathSkill = kernel.ImportSemanticSkillFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Skills"), "SemanticMath"); // {"Power": ... }
// var functionsView = kernel.Skills.GetFunctionsView(); // NativeFunction: {"NativeMath": {"Add": ..., "Subtract": ...}}, SemanticFunctions: {"SemanticMath": {"Power": ...}}
// var context = new ContextVariables();
// var planner = new ActionPlanner(kernel);
// var prompt1 = "I need to add 2 to 3";
// var plan1 = await planner.CreatePlanAsync(prompt1); // Math.Add(3, 2)
// Console.WriteLine(await kernel.RunAsync(context, plan1)); // 5
// var prompt2 = "I need to raise {{$input}} to power of 2";
// var plan2 = await planner.CreatePlanAsync(prompt2); // Math.Power(5, 2)
// Console.WriteLine(await plan2.InvokeAsync(new SKContext(context))); // 25


// === SequentialPlanner ===
// var nativeMathSkill = kernel.ImportSkill(new NativeMath(), "NativeMath"); // {"Add": ..., "Subtract": ...}
// var semanticMathSkill = kernel.ImportSemanticSkillFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Skills"), "SemanticMath"); // {"Power": ... }
// var functionsView = kernel.Skills.GetFunctionsView(); // NativeFunction: {"NativeMath": {"Add": ..., "Subtract": ...}}, SemanticFunctions: {"SemanticMath": {"Power": ...}}
// var planner = new SequentialPlanner(kernel);
// var prompt = "I need to add 2 to 3 and then raise the result to power of 2";
// var plan = await planner.CreatePlanAsync(prompt); // Math.Add(3, 2) -> Math.Power(5, 2)
// Console.WriteLine(await kernel.RunAsync(plan)); // 25
// // or
// // while (plan.HasNextStep)
// // {
// //     await kernel.StepAsync(plan);
// //     // or
// //     // await plan.InvokeNextStepAsync(kernel.CreateNewContext());
// //     Console.WriteLine(plan.State.Input);
// // }
// Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));


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
// var chatCompletion = kernel.GetService<IChatCompletion>();
// var chat = (OpenAIChatHistory)chatCompletion.CreateNewChat();
// chat.AddUserMessage("2+2=");
// chat.AddAssistantMessage(await chatCompletion.GenerateMessageAsync(chat));
// Console.WriteLine(chat.Messages[^1].Content);


// === PromptTemplateEngine ===
// kernel.ImportSkill(new TimeSkill(), "Time");
// var promptRenderer = new PromptTemplateEngine();
// var prompt = @"{{time.now}}";
// var renderedPrompt = await promptRenderer.RenderAsync(prompt, kernel.CreateNewContext());


// === DallE ===
// throw new NotImplementedException();


// === ITextCompletion.CompleteStreamAsync() ===
// var textCompletion = kernel.GetService<ITextCompletion>();
// await foreach (string chunk in textCompletion.CompleteStreamAsync("Count from 1 to 10", new()))
// {
//     Console.Write(chunk);
// }


// === IChatCompletion.GenerateMessageStreamAsync() ===
// var chatCompletion = kernel.GetService<IChatCompletion>();
// var chat = (OpenAIChatHistory)chatCompletion.CreateNewChat();
// chat.AddUserMessage("Count from 1 to 10");
// chat.AddAssistantMessage("");
// await foreach (var chunk in chatCompletion.GenerateMessageStreamAsync(chat))
// {
//     chat.Messages[^1].Content += chunk;
// }
// Console.WriteLine(chat.Messages[^1].Content);


// === TEST ===


Debugger.Break();
