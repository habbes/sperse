using QuickSpike;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;

namespace Lib;

class MockRemoteEvaluator
{
    private Evaluator eval = new();
    GrpcChannel channel;
    RemoteEvaluation.RemoteEvaluationClient client;

    public MockRemoteEvaluator()
    {
        this.channel = GrpcChannel.ForAddress("http://localhost:5041");
        this.client = new RemoteEvaluation.RemoteEvaluationClient(channel);
    }

    public async Task<object> Execute(string expression)
    {
        // send dummy request to grpc server to test connectivity
        var response = await client.EvaluateExpressionAsync(new ExprRequest { Expr = "World" });
        // write the response
        Console.WriteLine(response.Value);
        await Task.Delay(8000);
        return eval.Execute(expression);
    }
}