using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.SemanticFunctions;
using System.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;

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
    .Build();


// === CreateSemanticFunction ===
// var function01 = kernel.CreateSemanticFunction("{{$input}}");
// Console.WriteLine(await function01.InvokeAsync("2+2=")); // 4


// === RegisterSemanticFunction ===
// var promptTemplateConfig = new PromptTemplateConfig();
// var promptTemplate = new PromptTemplate("({{$input}})^2=", promptTemplateConfig, kernel);
// var functionConfig = new SemanticFunctionConfig(promptTemplateConfig, promptTemplate);
// var function02 = kernel.RegisterSemanticFunction("TestFunction", functionConfig);
// Console.WriteLine(await function02.InvokeAsync("3+3")); // 36


// === SemanticFunction Chaining ===
// Console.WriteLine(await kernel.RunAsync("4+4=", function01, function02)); // 64


// === ImportSemanticSkillFromDirectory ===
// var skills = kernel.ImportSemanticSkillFromDirectory(Directory.GetCurrentDirectory(), "Skills");
// Console.WriteLine(await kernel.RunAsync("5+5", skills["Power"])); // 100


// === ContextVariables ===
const string skPrompt = @"
{{$history}}
Human: {{$human_input}}
AI:";
var chatFunction = kernel.CreateSemanticFunction(skPrompt, "Chat", "ChatBot");
var context = new ContextVariables { ["history"] = "" };
Func<string, Task<string>> chat = async (human_input) =>
{
    context["human_input"] = human_input;
    var ai_answer = await kernel.RunAsync(context, chatFunction);
    context["history"] += $@"
Human: {context["human_input"]}
AI: {ai_answer}";
    return ai_answer.ToString();
};
Console.WriteLine(await chat("6+6=")); // 12
Console.WriteLine(await chat("<previous>-2=")); // 10
Console.WriteLine(await chat("<previous>*3=")); // 30


// === TODO: Planner ===
// === TODO: Embedding ===
// === TODO: DALL-E ===
// === TODO: ChatGPT ===


Debugger.Break();
