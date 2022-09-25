using Lib;

namespace QuickSpike;

public class Evaluator
{
    EvaluationContext context;

    public Evaluator(RemoteManager remoteManager)
    {
        this.context = new(remoteManager);
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

    public RemoteManager RemoteManager { get; private set; }

    public EvaluationContext(RemoteManager remoteManager)
    {
        this.DelayedTracker = new(this);
        this.ValueTracker = new(this);
        RemoteManager = remoteManager;
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
    
    public async Task ExecuteRemote(Guid id, Expression expression, string? tag)
    {
        if (this.context.RemoteManager == null)
        {
            throw new Exception("Remote connection not established.");
        }

        try
        {
            var serializer = new ExpressionSerializer(this.context);
            serializer.Visit(expression);
            var serialized = serializer.GetSerializedExpression();
            // Console.WriteLine($"Serialized expression to '{serialized}'");

            object value = await this.context.RemoteManager.Execute(serialized, tag);

            Console.WriteLine($"Expression Id {id} completed. Value = {value}");
            this.context.ValueTracker.Update(id, value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }
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
        if (TryGetSymbolAndLevel(name, out var value, out _))
        {
            return value;
        }

        throw new Exception($"Unknown variable '{name}'");
    }

    public bool TryGetSymbolAndLevel(string name, out object value, out int level)
    {
        level = scopes.Count - 1;
        while (level >= 0)
        {
            var scope = scopes[level];
            if (scope.TryGetSymbol(name, out value))
            {
                return true;
            }

            level--;
        }

        value = null;
        return false;
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