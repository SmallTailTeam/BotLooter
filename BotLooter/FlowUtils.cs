using Serilog;
using Serilog.Core;

namespace BotLooter;

public static class FlowUtils
{
    public static bool AskForApproval { get; set; }
    public static bool AskForExit { get; set; }
    
    public static void AbortWithError(string error)
    {
        Log.Logger.Error(error);
        Log.Logger.Information("Нажмите любую клавишу для выхода.");
        Console.ReadKey();
        Environment.Exit(0);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    public static void WaitForApproval(string messageTemplate, params object[] args)
    {
        Log.Logger.Information(messageTemplate, args);

        if (AskForApproval)
        {
            Log.Logger.Information("Нажмите любую клавишу для продолжения.");
            Console.ReadKey();
            Console.CursorLeft = 0;
        }
        else
        {
            Log.Logger.Information("Ожидаю 5 секунд до продолжения.");
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Console.CursorLeft = 0;
        }
    }

    public static void WaitForExit(string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            Log.Logger.Information(message);
        }

        if (AskForExit)
        {
            Log.Logger.Information("Нажмите '{Keys}' для выхода.", "ctrl + c");

            Console.TreatControlCAsInput = true;

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key is { Key: ConsoleKey.C, Modifiers: ConsoleModifiers.Control })
                {
                    break;
                }
            }

            Environment.Exit(0);
        }
        else
        {
            Log.Logger.Information("Выхожу через 5 секунд.");
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Environment.Exit(0);
        }
    }
} 