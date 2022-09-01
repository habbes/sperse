// See https://aka.ms/new-console-template for more information

Evaluator eval = new Evaluator();

while (true)
{
    string input = GetNextInput();
    if (input == "exit")
    {
        Console.WriteLine("Bye!");
        break;
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

class Lexer
{
    public TokenStream Tokenize(string input)
    {
        return new TokenStream(GetTokens(input));
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
            return new Token(value, TokenType.Plus);
        }
        
        if (int.TryParse(value, out _))
        {
            return new Token(value, TokenType.IntConst);
        }

        return new Token(value, TokenType.Id);
    }
}

record struct Token(string Value, TokenType Type);

enum TokenType
{
    Id,
    IntConst,
    Assgn,
    Plus,
}

class Evaluator
{
    EvaluationContext context = new();

    public object Execute(string input)
    {
        Lexer lexer = new Lexer();
        TokenStream tokens = lexer.Tokenize(input);
        Parser parser = new Parser(tokens);
        Expression expression = parser.ParseExpression();
        return expression.Evaluate(context);
    }
}


class Parser
{
    private TokenStream tokens;

    public Parser(TokenStream tokens)
    {
        this.tokens = tokens;
    }

    public Expression ParseExpression ()
    {
        Token token;
        if (this.tokens.TryPeekAfterNext(out token))
        {
            if (token.Type == TokenType.Assgn)
            {
                return this.ParseAssignment();
            }
            else if (token.Type == TokenType.Plus)
            {
                return this.ParseAdd();
            }
        }

        token = this.tokens.Consume();
        if (token.Type == TokenType.Id)
        {
            return new VarExpression(token.Value);
        }

        if (token.Type == TokenType.IntConst)
        {
            return new IntExpression(int.Parse(token.Value));
        }

        throw new Exception(string.Format("Unexpected token {0}", token));
    }

    private Expression ParseAssignment()
    {
        Token idToken = this.tokens.ConsumeType(TokenType.Id);
        this.tokens.ConsumeType(TokenType.Assgn);
        Expression expression = this.ParseExpression();

        return new AssignmentExpression(idToken.Value, expression);
    }

    private Expression ParseAdd()
    {
        Expression left = this.ParseOperandExpression();
        this.tokens.ConsumeType(TokenType.Plus);
        Expression right = this.ParseExpression();
        
        return new AddExpression(left, right);
    }

    private Expression ParseOperandExpression()
    {
        Token token = this.tokens.Consume();
        if (token.Type == TokenType.Id)
        {
            return new VarExpression(token.Value);
        }

        if (token.Type == TokenType.IntConst)
        {
            return new IntExpression(int.Parse(token.Value));
        }

        throw new Exception($"Unexpected operand token {token}");
    }

    
}

class TokenStream
{
    public TokenStream(List<Token> tokens)
    {
        Tokens = tokens;
        Pos = 0;
    }

    public IReadOnlyList<Token> Tokens { get; private set; }
    public int Pos { get; private set; }
    public bool Eof => Pos == Tokens.Count;

    public Token Consume()
    {
        if (Eof)
        {
            throw new Exception("Cannot seek past end of stream.");
        }

        return this.Tokens[Pos++];
    }

    public Token ConsumeType(TokenType type)
    {
        Token token = Consume();
        if (token.Type != type)
        {
            throw new Exception($"Expected {type} but go {token}");
        }

        return token;
    }

    public bool TryConsume(out Token token)
    {
        token = default;

        if (Eof)
        {
            return false;
        }

        token = this.Tokens[Pos++];
        return true;
    }

    public bool TryPeekAfterNext(out Token token)
    {
        token = default;

        if (Eof)
        {
            return false;
        }

        if (Pos + 1 >= Tokens.Count)
        {
            return false;
        }

        token = this.Tokens[Pos + 1];
        return true;
    }
}

abstract class Expression
{
    public abstract object Evaluate(EvaluationContext context);
}

class AssignmentExpression : Expression
{
    private string id;
    private Expression expression;

    public AssignmentExpression(string id, Expression value)
    {
        this.id = id;
        this.expression = value;
    }

    public override object Evaluate(EvaluationContext context)
    {
        var value = this.expression.Evaluate(context);
        context.SymbolTable.SetSymbol(this.id, value);

        return value;
    }
}

class AddExpression : Expression
{
    private Expression left;
    private Expression right;

    public AddExpression(Expression left, Expression right)
    {
        this.left = left;
        this.right = right;
    }

    public override object Evaluate(EvaluationContext context)
    {
        return (int)this.left.Evaluate(context) + (int)this.right.Evaluate(context);
    }
}

class IntExpression : Expression
{
    private int value;

    public IntExpression(int value)
    {
        this.value = value;
    }

    public override object Evaluate(EvaluationContext context)
    {
        return this.value;
    }
}

class VarExpression : Expression
{
    private string id;

    public VarExpression(string id)
    {
        this.id = id;
    }

    public override object Evaluate(EvaluationContext context)
    {
        return context.SymbolTable.GetSymbol(this.id);
    }
}

class EvaluationContext
{
    public SymbolTable SymbolTable { get; private set; } = new();
}

class SymbolTable
{
    Dictionary<string, object> symbols = new();

    public void SetSymbol(string id, object value)
    {
        symbols[id] = value;
    }

    public object GetSymbol(string id)
    {
        return symbols[id];
    }
}