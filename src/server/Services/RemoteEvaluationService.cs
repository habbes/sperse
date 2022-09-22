using Grpc.Core;
using server;
using Lib;

namespace server.Services;

class RemoteEvaluationService : RemoteEvaluation.RemoteEvaluationBase
{
    private readonly ILogger<RemoteEvaluationService> _logger;
    private readonly TaskTracker _tracker;
    public RemoteEvaluationService(ILogger<RemoteEvaluationService> logger, TaskTracker tracker)
    {
        _logger = logger;
        _tracker = tracker;
    }

    public override Task<ExprReply> EvaluateExpression(ExprRequest request, ServerCallContext context) {
        Console.WriteLine($"Server received expression '{request.Expr}'");
        var data = _tracker.AddNew(request.Expr);
        MockRemoteEvaluator eval = new();
        _tracker.Start(data.Id);
        var value = eval.Execute(request.Expr);
        _tracker.Complete(data.Id, value);
        return Task.FromResult(new ExprReply
        {
            Value = value.ToString()
        });
    }
}
