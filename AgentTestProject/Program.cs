using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAIChatLibrary;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    // Color scheme for agents
    private static ConsoleColor PMColor = ConsoleColor.Blue;
    private static ConsoleColor DevColor = ConsoleColor.Green;
    private static ConsoleColor ReviewerColor = ConsoleColor.Yellow;
    private static ConsoleColor QAColor = ConsoleColor.Cyan;
    private static ConsoleColor SystemColor = ConsoleColor.White;

    static async Task Main(string[] args)
    {
        // Build configuration to read appsettings.json
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var configuration = builder.Build();

        // Setup DI and add the agent manager
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOpenAIAgents();
        var serviceProvider = services.BuildServiceProvider();

        // Retrieve the agent manager
        var manager = serviceProvider.GetRequiredService<IAgentManager>();

        // Create agents:
        // We'll use 'gpt-3.5-turbo' as a model example, can be replaced with 'gpt-4' if desired.
        // System prompts to define each agent's role and style.
        var pmSystemPrompt = "You are a project manager. You come up with small programming tasks, clearly defining the requirements. Do not give examples of the task in code, write only requirements, clearly list them";
        var devSystemPrompt = "You are a software developer. You implement solutions to the tasks provided by the project manager. You will produce code that meets all requirements. Provide source code only and no other output";
        var reviewerSystemPrompt = "You are a code reviewer. You will check the provided code against the requirements, You will also check for code standards, syntax erors, and logic issues, and either provide detailed feedback or confirm it meets all requirements.";
        var qaSystemPrompt = "You are a QA engineer. You will test the implemented code functionally and verify that all requested features are present and work as intended. If something is wrong, request changes. If all good, confirm completion.";

        // Create agents using the manager
        var pm = manager.CreateAgent("ProjectManager", "o1-mini", pmSystemPrompt);
        var dev = manager.CreateAgent("Developer", "o1-preview", devSystemPrompt);
        var reviewer = manager.CreateAgent("CodeReviewer", "o1-preview", reviewerSystemPrompt);
        var qa = manager.CreateAgent("QA", "o1-preview", qaSystemPrompt);

        // Steps:
        // 1) Ask PM for a small programming task
        LogMessage("SYSTEM", "Asking PM for a small programming task...", SystemColor);
        var taskDescription = await manager.SendMessageAsync("ProjectManager",
            "Please come up with a small programming task (involving writing a simple C# function) and clearly list all requirements.");
        LogMessage("PM", taskDescription, PMColor);

        // Extract the requirements. For simplicity, we assume the PM's response contains all requirements in plain text.
        // We will pass the entire response as 'requirements' to the dev, reviewer, QA since the instructions say full set.

        // 2) Developer implements the solution based on PM's requirements
        LogMessage("SYSTEM", "Developer is implementing the solution...", SystemColor);
        var devResponse = await manager.SendMessageAsync("Developer",
            $"The project manager provided these requirements:\n{taskDescription}\nPlease implement the solution in C#. Provide the code inside ```csharp``` code fences.");
        LogMessage("Developer", devResponse, DevColor);

        // 3) Reviewer reviews the code based on PM requirements
        // We'll send the code and the requirements to the reviewer
        LogMessage("SYSTEM", "Code Reviewer is reviewing the code...", SystemColor);
        var reviewerResponse = await manager.SendMessageAsync("CodeReviewer",
            $"Here are the requirements from the PM:\n{taskDescription}\n\nHere is the code from the Developer:\n{devResponse}\n\nCheck if the code meets all the requirements. If not, provide specific feedback. If everything looks good, say 'Looks good, all requirements met!'");
        LogMessage("Reviewer", reviewerResponse, ReviewerColor);

        // If reviewer suggests changes, go back to dev until "Looks good" is achieved
        while (!reviewerResponse.Contains("Looks good", StringComparison.OrdinalIgnoreCase))
        {
            LogMessage("SYSTEM", "Reviewer requested changes. Asking Developer to fix issues...", SystemColor);
            devResponse = await manager.SendMessageAsync("Developer",
                $"The reviewer had this feedback:\n{reviewerResponse}\nPlease fix the code accordingly and provide the updated code inside ```csharp``` fences.");
            LogMessage("Developer", devResponse, DevColor);

            LogMessage("SYSTEM", "Reviewer is re-checking after fixes...", SystemColor);
            reviewerResponse = await manager.SendMessageAsync("CodeReviewer",
                $"Here are the requirements from the PM:\n{taskDescription}\n\nHere is the updated code:\n{devResponse}\nPlease check again if all requirements are met. If good, say 'Looks good, all requirements met!'");
            LogMessage("Reviewer", reviewerResponse, ReviewerColor);
        }

        // 4) QA checks the final code. If QA requests changes, the dev fixes and QA re-checks until done
        LogMessage("SYSTEM", "QA is testing the code...", SystemColor);
        var qaResponse = await manager.SendMessageAsync("QA",
            $"Here are the requirements from the PM:\n{taskDescription}\nHere is the final code:\n{devResponse}\nPlease verify it works correctly and meets all the requirements. If something is wrong, request changes. If all good, say 'Done, all tests passed!'");
        LogMessage("QA", qaResponse, QAColor);

        while (!qaResponse.Contains("Done, all tests passed", StringComparison.OrdinalIgnoreCase))
        {
            LogMessage("SYSTEM", "QA requested changes. Asking Developer to fix issues...", SystemColor);
            devResponse = await manager.SendMessageAsync("Developer",
                $"The QA had this feedback:\n{qaResponse}\nPlease fix the code accordingly and provide the updated code inside ```csharp``` fences.");
            LogMessage("Developer", devResponse, DevColor);

            LogMessage("SYSTEM", "QA is re-checking after fixes...", SystemColor);
            qaResponse = await manager.SendMessageAsync("QA",
                $"Here are the requirements from the PM:\n{taskDescription}\nHere is the updated code:\n{devResponse}\nCheck again, if all good, say 'Done, all tests passed!'");
            LogMessage("QA", qaResponse, QAColor);
        }

        // All done
        LogMessage("SYSTEM", "All done! Here are the final results:", SystemColor);
        LogMessage("PM Requirements", taskDescription, PMColor);
        LogMessage("Final QA Confirmation", qaResponse, QAColor);
        LogMessage("Final Code", devResponse, DevColor);

        Console.ResetColor();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static void LogMessage(string agent, string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{agent}] {message}");
        Console.ResetColor();
    }
}
