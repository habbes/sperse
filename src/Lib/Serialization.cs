using QuickSpike;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib;

internal class ExpressionSerializer : ExpressionVisitor
{
    private readonly EvaluationContext context;
    private readonly Stack<object> valueStack = new();
    private StringBuilder exprWriter = new();

    public ExpressionSerializer(EvaluationContext context)
    {
        this.context = context;
    }
    public override void VisitAdd(AddExpression expression)
    {
        expression.Left.Accept(this);
        expression.Right.Accept(this);
        object rightValue = this.valueStack.Pop();
        object leftValue = this.valueStack.Pop();
        exprWriter.Append($"{leftValue}+{rightValue}");
    }

    public override void VisitAssignment(AssignmentExpression expression)
    {
        throw new NotImplementedException();
    }

    public override void VisitIntConst(IntExpression expression)
    {
        this.valueStack.Push(expression.Value);
    }

    public override void VisitVar(VarExpression expression)
    {
        this.valueStack.Push(context.Scope.GetSymbol(expression.Identifier));
    }

    public string GetSerializedExpression()
    {
        return exprWriter.ToString();
    }
}
