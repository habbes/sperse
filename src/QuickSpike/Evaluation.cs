using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSpike;

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


class EvaluationContext
{
    public SymbolTable SymbolTable { get; private set; } = new();
    public DelayedOperationTracker DelayedTracker { get; private set; }

    public EvaluationContext()
    {
        this.DelayedTracker = new(this);
    }
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

class DelayedOperationTracker
{
    Queue<Entry> entries = new Queue<Entry>();
    EvaluationContext context;

    public DelayedOperationTracker(EvaluationContext context)
    {
        this.context = context;
    }
    
    public async Task DelayExecute(Guid id, Expression expression)
    {
        await Task.Delay(2000);
        object value = expression.Evaluate(this.context);

        Console.WriteLine($"Expression Id {id} completed. Value = {value}");
        // propagate values
    }

    public void Work()
    {
        while (entries.Count > 0)
        {

        }
    }

    struct Entry
    {
        public Expression Expression { get; set; }
    }
}
