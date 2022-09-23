using Lib;

namespace QuickSpike;

public class Evaluator
{
    EvaluationContext context;

    public Evaluator()
    {
        this.context = new();
    }

    public Evaluator(RemoteConnector connector)
    {
        this.context = new(connector);
    }

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
    public HierarchicalScope Scope { get; private set; } = new();
    public RemoteOperationTracker DelayedTracker { get; private set; }
    public ValueTracker ValueTracker { get; private set; }

    public RemoteConnector? RemoteConnector { get; private set; }

    public EvaluationContext(RemoteConnector? remoteConnector = null)
    {
        this.DelayedTracker = new(this);
        this.ValueTracker = new(this);
        RemoteConnector = remoteConnector;
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
        if (!symbols.TryGetValue(id, out var value))
        {
            throw new Exception($"Unknown variable '{id}'");
        }

        return value;
    }
}

class RemoteOperationTracker
{
    EvaluationContext context;

    public RemoteOperationTracker(EvaluationContext context)
    {
        this.context = context;
    }
    
    public async Task ExecuteRemote(Guid id, Expression expression)
    {
        if (this.context.RemoteConnector == null)
        {
            throw new Exception("Remote connection not established.");
        }

        var serializer = new ExpressionSerializer(this.context);
        serializer.Visit(expression);
        var serialized = serializer.GetSerializedExpression();
        // Console.WriteLine($"Serialized expression to '{serialized}'");
        
        object value = await this.context.RemoteConnector.Execute(serialized);

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

interface IScope
{
    public object GetSymbol(string name);
    public void SetSymbol(string name, object value);
}

class LocalScope : IScope
{
    private Dictionary<string, object> symbols = new();

    public void SetSymbol(string id, object value)
    {
        symbols[id] = value;
    }

    public object GetSymbol(string id)
    {
        if (!symbols.TryGetValue(id, out var value))
        {
            throw new Exception($"Unknown variable '{id}'");
        }

        return value;
    }

    public bool TryGetSymbol(string id, out object value)
    {
        return symbols.TryGetValue(id, out value);
    }
}

class HierarchicalScope : IScope
{
    List<LocalScope> scopes;

    public HierarchicalScope()
    {
        scopes = new();
        StartScope();
    }

    private IScope CurrentScope => scopes[scopes.Count - 1];

    public object GetSymbol(string name)
    {
        int level = scopes.Count - 1;
        while (level >= 0)
        {
            var scope = scopes[level];
            if (scope.TryGetSymbol(name, out var value))
            {
                return value;
            }
        }

        throw new Exception($"Unknown variable '{name}'");
    }

    public void SetSymbol(string name, object value)
    {
        CurrentScope.SetSymbol(name, value);
    }

    public void StartScope()
    {
        scopes.Add(new LocalScope());
    }

    public void EndScope()
    {
        scopes.RemoveAt(scopes.Count - 1);
    }
}