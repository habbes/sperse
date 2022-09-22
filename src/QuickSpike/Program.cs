// See https://aka.ms/new-console-template for more information

using Lib;
using QuickSpike;

string remoteAddress = "http://localhost:8585";
if (args.Length > 0)
{
    remoteAddress = args[0];
}

RemoteConnector remoteConnector = new(remoteAddress);
Evaluator eval = new Evaluator(remoteConnector);

Console.WriteLine($"Remote address {remoteAddress}");

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


