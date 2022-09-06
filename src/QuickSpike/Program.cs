// See https://aka.ms/new-console-template for more information

using QuickSpike;

Evaluator eval = new Evaluator();

while (true)
{
    string input = GetNextInput();
    if (input == "exit")
    {
        Console.WriteLine("Bye!");
        break;
    }

    if (input == "")
    {
        continue;
    }

    object value = eval.Execute(input);
    Console.WriteLine(value);
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


