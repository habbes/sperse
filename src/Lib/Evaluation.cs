using Lib;

namespace QuickSpike;

public class Evaluator
{
    EvaluationContext context = new();

    public object Execute(string input)
    {
        Lexer lexer = new Lexer(input);
        using TokenStream tokens = lexer.Tokenize();
        Parser parser = new Parser(tokens);
        Expression expression = parser.ParseExpression();
        return expression.Evaluate(context);
    }
}


class EvaluationContext
{
    public SymbolTable SymbolTable { get; private set; } = new();
    public DelayedOperationTracker DelayedTracker { get; private set; }
    public ValueTracker ValueTracker { get; private set; }

    public EvaluationContext()
    {
        this.DelayedTracker = new(this);
        this.ValueTracker = new(this);
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
    EvaluationContext context;

    public DelayedOperationTracker(EvaluationContext context)
    {
        this.context = context;
    }
    
    public async Task DelayExecute(Guid id, ReactiveExpression wrapper, Expression expression)
    {
        var serializer = new ExpressionSerializer(this.context);
        serializer.Visit(expression);
        var serialized = serializer.GetSerializedExpression();
        Console.WriteLine($"Serialized expression to '{serialized}'");
        var remoteExecutor = new MockRemoteEvaluator();
        object value = await remoteExecutor.Execute(serialized);

        Console.WriteLine($"Expression Id {id} completed. Value = {value}");
        this.context.ValueTracker.Update(id, value);
    }

    public enum Status
    {
        Pending,
        Success
    }
}

class ValueTracker
{
    private readonly EvaluationContext context;
    Dictionary<Guid, Expression> expressions = new();
    Dictionary<Guid, List<Guid>> dependencies = new();

    public ValueTracker(EvaluationContext context)
    {
        this.context = context;
    }

    public PendingValue AddDependency(Guid parent, Expression expression)
    {
        var childId = Guid.NewGuid();
        return this.AddDependency(parent, childId, expression);
    }

    public PendingValue AddDependency(Guid parent, Guid childId, Expression expression)
    {
        expressions.TryAdd(childId, expression);
        List<Guid> deps;
        if (dependencies.TryGetValue(parent, out deps))
        {
            deps.Add(childId);
        }
        else
        {
            deps = new List<Guid>();
            deps.Add(childId);
            dependencies.Add(parent, deps);
        }

        return new PendingValue(childId);
    }

    public PendingValue Add(Expression expression)
    {
        var id = Guid.NewGuid();
        expressions.Add(id, expression);
        return new PendingValue(id);
    }

    public void Update(Guid id, object value)
    {
        //var expression = this.expressions[id];
        if (this.dependencies.TryGetValue(id, out var deps))
        {
            
            foreach (var dep in deps)
            {
                Expression child = this.expressions[dep];
                object childValue = child.Update(this.context, id, value);
                if (!(childValue is PendingValue))
                {
                    // propagate value to children
                    this.Update(dep, childValue);
                }
            }
        }

        this.expressions.Remove(id);
        this.dependencies.Remove(id);
    }
}
