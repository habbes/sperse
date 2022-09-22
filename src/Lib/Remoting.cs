using QuickSpike;
using Grpc.Net.Client;

namespace Lib;

public class MockRemoteEvaluator
{
    private Evaluator eval = new();

    public object Execute(string expression)
    {
        return eval.Execute(expression);
    }
}

public class RemoteConnector
{
    RemoteEvaluation.RemoteEvaluationClient client;
    GrpcChannel channel;

    public RemoteConnector(string address)
    {
        this.channel = GrpcChannel.ForAddress(address);
        this.client = new RemoteEvaluation.RemoteEvaluationClient(channel);
    }

    public async Task<object> Execute(string expression)
    {
        try
        {
            Console.WriteLine($"Sending expression to server '{expression}'");
            // artificial delay to make requests take longer for demonstration purposes
            await Task.Delay(5000);
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
}