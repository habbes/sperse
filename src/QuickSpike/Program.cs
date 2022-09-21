// See https://aka.ms/new-console-template for more information

using Lib;
using QuickSpike;

RemoteConnector remoteConnector = new();
Evaluator eval = new Evaluator(remoteConnector);

while (true)
{
    string input = GetNextInput().Trim();
    if (input == "exit")
    {
        Console.WriteLine("Bye!");
        break;
    }

    if (input == "")
    {
        continue;
    }

    try
    {
        object value = eval.Execute(input);
        Console.WriteLine(value);
    }
    catch (Exception e)
    {
        Console.WriteLine($"ERROR: {e.Message}");
    }
    
}

string GetNextInput(string prompt = ">> ")
{
    Console.Write(prompt);
    string? input = Console.ReadLine();
    if (input == null)
    {
        throw new Exception("Read null");
    }

    return input;
}


