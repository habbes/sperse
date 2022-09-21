using Grpc.Core;
using server;
using Lib;

namespace server.Services;

public class RemoteEvaluationService : RemoteEvaluation.RemoteEvaluationBase
{
    private readonly ILogger<RemoteEvaluationService> _logger;
    public RemoteEvaluationService(ILogger<RemoteEvaluationService> logger)
    {
        _logger = logger;
    }

    public override Task<ExprReply> EvaluateExpression(ExprRequest request, ServerCallContext context) {
        Console.WriteLine($"Server received expression '{request.Expr}'");
        MockRemoteEvaluator eval = new();
        var value = eval.Execute(request.Expr);
        return Task.FromResult(new ExprReply
        {
            Value = value.ToString()
        });
    }
}
