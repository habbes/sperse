using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

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
    GrpcChannel channel;
    Greeter.GreeterClient client;

    Dictionary<Guid, Entry> entries = new();
    EvaluationContext context;

    public DelayedOperationTracker(EvaluationContext context)
    {
        this.context = context;
        this.channel = GrpcChannel.ForAddress("http://localhost:5041");
        this.client = new Greeter.GreeterClient(channel);
    }
    
    public async Task DelayExecute(Guid id, ReactiveExpression wrapper, Expression expression)
    {

        // grpc piece
        Console.WriteLine("Getting ready to make gRPC call");
        var response = await client.SayHelloAsync(new HelloRequest { Name = "World" });
        Console.WriteLine(response.Message);
        // end grpc piece

        Entry entry = new(id, expression);
        this.entries.Add(id, entry);
        await Task.Delay(4000);
        object value = expression.Evaluate(this.context);
        entry.Value = value;

        Console.WriteLine($"Expression Id {id} completed. Value = {value}");
        wrapper.Update(this.context);
        // propagate values
    }

    public void AddDependent(Guid parentId, Guid childId, Expression childExpression)
    {
        Entry entry = new(childId, childExpression);
        this.entries.Add(childId, entry);
        Entry parent = this.entries[parentId];
        parent.Dependents.Add(childId);
    }

    public object GetValue(Guid id)
    {
        Entry entry = this.entries[id];
        return entry.Value;
    }

    public void Work()
    {
        while (entries.Count > 0)
        {

        }
    }

    class Entry
    {
        public Entry(Guid id, Expression expression)
        {
            Id = id;
            Expression = expression;
            Value = new PendingValue(id);
        }
        public Guid Id { get; set; }
        public Expression Expression { get; set; }
        public List<Guid> Dependents { get; } = new();
        public Status Status { get; set; } = Status.Pending;
        public object Value { get; set; }

    }

    public enum Status
    {
        Pending,
        Success
    }
}
