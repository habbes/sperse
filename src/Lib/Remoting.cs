using QuickSpike;
using Grpc.Net.Client;

namespace Lib;

public class MockRemoteEvaluator
{
    private Evaluator eval = new(new RemoteManager());

    public object Execute(string expression)
    {
        return eval.Execute(expression);
    }
}

public class RemoteConnector
{
    RemoteEvaluation.RemoteEvaluationClient client;
    GrpcChannel channel;
    IReadOnlySet<string> tags;
    int id;

    public RemoteConnector(string address, IReadOnlySet<string> tags, int id)
    {
        this.channel = GrpcChannel.ForAddress(address);
        this.client = new RemoteEvaluation.RemoteEvaluationClient(channel);
        this.tags = tags;
        this.id = id;
    }

    public async Task<object> Execute(string expression)
    {
        try
        {
            //Console.WriteLine($"Sending expression to server '{expression}'");
            // artificial delay to make requests take longer for demonstration purposes
            await Task.Delay(4000);
            var response = await client.EvaluateExpressionAsync(new ExprRequest { Expr = expression });
            // we expect all responses to be numbers for now
            int value = int.Parse(response.Value);
            return value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }
    }

    public IReadOnlySet<string> Tags => tags;
    public int Id => id;
}

public class RemoteManager
{
    List<RemoteConnector> connectors = new();
    int nextId = 1;

    public void Add(string address, IReadOnlySet<string> tags)
    {
        int id = nextId++;
        connectors.Add(new RemoteConnector(address, tags, id));
        Console.WriteLine($"Adding connection {id} with address '{address}' and tags {string.Join(',', tags)}");
    }

    public Task<object> Execute(string code)
    {
        var remote = connectors.FirstOrDefault();

        if (remote == null)
        {
            throw new Exception("No remote machine available");
        }

        Console.WriteLine($"Picked remote {remote.Id}");
        return remote.Execute(code);
    }

    public Task<object> Execute(string code, string? tag)
    {
        if (tag == null)
        {
            return this.Execute(code);
        }

        var connector = connectors.FirstOrDefault(c => c.Tags.Contains(tag));
        if (connector == null)
        {
            throw new Exception($"No remote machine matches the specified tag '{tag}'");
        }

        Console.WriteLine($"Picked remote {connector.Id}");
        return connector.Execute(code);
    }
}
