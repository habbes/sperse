using Grpc.Core;
using server;

namespace server.Services;

public class RemoteEvaluationService : RemoteEvaluation.RemoteEvaluationBase
{
    private readonly ILogger<RemoteEvaluationService> _logger;
    public RemoteEvaluationService(ILogger<RemoteEvaluationService> logger)
    {
        _logger = logger;
    }

    public override Task<ExprReply> EvaluateExpression(ExprRequest request, ServerCallContext context) {
        return Task.FromResult(new ExprReply 
        {
            Value = request.Expr
        });
    }
}
