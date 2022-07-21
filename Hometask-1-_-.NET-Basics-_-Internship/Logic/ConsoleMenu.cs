using Hometask_1___.NET_Basics___Internship.Logic.Interfaces;

namespace Hometask_1___.NET_Basics___Internship.Logic;

public class ConsoleMenu : IMenu
{
    private string? MenuOptions()
    {
        Console.Clear();
        Console.WriteLine("1. Start");
        Console.WriteLine("2. Reset");
        Console.WriteLine("3. Stop");
        Console.WriteLine("4. Show meta.log");
        Console.WriteLine("5. Exit");

        Console.WriteLine();

        Console.WriteLine("Service is currently offline");

        Console.WriteLine();

        Console.Write("Enter command: ");

        return Console.ReadLine();
    }

    private bool CommandsHandler(string? command)
    {
        switch (command)
        {
            case "1":
                CommandStart();
                break;
            case "2":
                CommandReset();
                break;
            case "3":
                CommandStop();
                break;
            case "4":
                CommandShowMetaLog();
                break;
            case "5":
                Console.WriteLine("Exit");
                Console.ReadLine();
                return false;
        }

        return true;
    }

    private void CommandStart()
    {
        Console.WriteLine("CommandStart");
    }

    private void CommandReset()
    {
        Console.WriteLine("CommandReset");
    }

    private void CommandStop()
    {
        Console.WriteLine("CommandStop");
    }

    private void CommandShowMetaLog()
    {
        Console.WriteLine("CommandShowMetaLog");
    }

    public void Show()
    {
        var run = true;
        while (run)
        {
            var command = MenuOptions();
            run = CommandsHandler(command);
        }
    }
}