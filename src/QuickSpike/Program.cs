// See https://aka.ms/new-console-template for more information

while (true)
{
    string input = GetNextInput();
    if (input == "exit")
    {
        Console.WriteLine("Bye!");
        break;
    }

    var tokens = GetTokens(input);
    foreach (var token in tokens)
    {
        Console.Write(token);
        Console.Write(" ");
    }

    Console.WriteLine();
}

string GetNextInput(string prompt = ">> ")
{
    Console.Write(prompt);
    string input = Console.ReadLine();
    return input;
}

List<Token> GetTokens(string input)
{
    var tokens = input.Split(" ");
    return tokens.Select(t => t.Trim())
        .Where(t => t != "")
        .Select(value => GetToken(value))
        .ToList();
}

Token GetToken(string value)
{
    if (value == "=")
    {
        return new Token(value, TokenType.Assgn);
    }

    if (value == "+")
    {
        return new Token(value, TokenType.Plus)
    }
    
    if (int.TryParse(value, out _))
    {
        return new Token(value, TokenType.IntConst);
    }

    return new Token(value, TokenType.Id);
}

record struct Token(string Value, TokenType Type);

enum TokenType
{
    Id,
    IntConst,
    Assgn,
    Plus,
}

class TokenStream
{
    public TokenStream(List<Token> tokens)
    {
        Tokens = tokens;
        Pos = 0;
    }

    List<Token> Tokens { get; private set }
    int Pos { get; private set; }
    bool Eof => Pos == Tokens.Count - 1;

    int IncrPos()
    {
        if (Eof)
        {
            throw new Exception("Cannot seek past end of stream.");
        }

        return ++Pos;
    }
}