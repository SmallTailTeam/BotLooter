namespace BotLooter;

public static class FlowUtils
{
    public static void AbortWithError(string error)
    {
        Console.WriteLine(error);
        Console.WriteLine("Нажмите любую клавишу для выхода.");
        Console.ReadKey();
        Environment.Exit(0);
    }

    public static void WaitForApproval(string message)
    {
        Console.WriteLine(message);
        Console.WriteLine("Нажмите любую клавишу для продолжения.");
        Console.ReadKey();
        Console.CursorLeft = 0;
    }

    public static void WaitForExit(string message)
    {
        Console.WriteLine(message);
        Console.WriteLine("Нажмите 'ctrl + c' для выхода.");
        
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
} 