namespace QuickSpike;

abstract class Expression
{
    public abstract object Evaluate(EvaluationContext context);
    public abstract object Update(EvaluationContext context, Guid parent, object value);

    public virtual void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}

interface IExpressionVisitor
{
    void Visit(Expression expression);
    void VisitAssignment(AssignmentExpression expression);
    void VisitAdd(AddExpression expression);
    void VisitIntConst(IntExpression expression);
    void VisitVar(VarExpression expression);
}

abstract class ExpressionVisitor : IExpressionVisitor
{
    public virtual void Visit(Expression expression)
    {
        if (expression is AddExpression addExpression)
        {
            this.VisitAdd(addExpression);
        }
        else if (expression is IntExpression intExpression)
        {
            this.VisitIntConst(intExpression);
        }
        else if (expression is VarExpression varExpression)
        {
            this.VisitVar(varExpression);
        }
        else if (expression is AssignmentExpression assignmentExpression)
        {
            this.VisitAssignment(assignmentExpression);
        }
        else if (expression is BlockExpression block)
        {
            this.VisitBlock(block);
        }
        else if (expression is FunctionCallExpression functionCall)
        {
            this.VisitFunctionCall(functionCall);
        }
        else if (expression is FunctionDefExpression functionDef)
        {
            this.VisitFunctionDef(functionDef);
        }
        else
        {
            throw new Exception("Unsupported expression!");
        }
    }

    public abstract void VisitAssignment(AssignmentExpression expression);
    public abstract void VisitAdd(AddExpression expression);
    public abstract void VisitIntConst(IntExpression expression);
    public abstract void VisitVar(VarExpression expression);
    public abstract void VisitBlock(BlockExpression expression);
    public abstract void VisitFunctionCall(FunctionCallExpression expression);
    public abstract void VisitFunctionDef(FunctionDefExpression expression);
}

abstract class ReactiveExpression : Expression
{
}

class AssignmentExpression : Expression
{
    private string id;
    private Expression expression;

    public AssignmentExpression(string id, Expression value)
    {
        this.id = id;
        this.expression = value;
    }

    public override object Evaluate(EvaluationContext context)
    {
        var value = this.expression.Evaluate(context);
        if (value is PendingValue parentPendingValue)
        {
            PendingValue pendingValue = context.ValueTracker.AddDependency(parentPendingValue.Id, this);
            context.Scope.SetSymbol(this.id, pendingValue);
        }
        else
        {
            context.Scope.SetSymbol(this.id, value);
        }

        return value;
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        context.Scope.SetSymbol(this.id, value);
        return value;
    }
}

class AddExpression : Expression
{
    private Expression left;
    private Expression right;
    private PendingValue leftPendingValue;
    private PendingValue rightPendingValue;
    private PendingValue temp;
    private int? leftValue;
    private int? rightValue;

    public AddExpression(Expression left, Expression right)
    {
        this.left = left;
        this.right = right;
    }

    public Expression Left => left;
    public Expression Right => right;

    public override object Evaluate(EvaluationContext context)
    {
        object leftValue = this.left.Evaluate(context);
        object rightValue = this.right.Evaluate(context);
        Guid id = Guid.NewGuid();
        PendingValue pendingValue = default;

        if (leftValue is PendingValue leftPendingValue)
        {
            this.leftPendingValue = leftPendingValue;
            pendingValue = context.ValueTracker.AddDependency(leftPendingValue.Id, id, this);
        }
        else
        {
            this.leftValue = (int)leftValue;
        }

        if (rightValue is PendingValue rightPendingValue)
        {
            this.rightPendingValue = rightPendingValue;
            pendingValue = context.ValueTracker.AddDependency(rightPendingValue.Id, id, this);
        }
        else
        {
            this.rightValue = (int)rightValue;
        }

        if (pendingValue.Id != Guid.Empty)
        {
            temp = pendingValue;
            return pendingValue;
        }

        return (int)this.left.Evaluate(context) + (int)this.right.Evaluate(context);
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        if (this.leftPendingValue.Id == parent)
        {
            this.leftValue = (int)value;
        }

        if (this.rightPendingValue.Id == parent)
        {
            this.rightValue = (int)value;
        }

        if (this.leftValue != null && this.rightValue != null)
        {
            return leftValue + rightValue;
        }

        return temp;
    }
}

class IntExpression : Expression
{
    private int value;

    public int Value => value;

    public IntExpression(int value)
    {
        this.value = value;
    }

    public override object Evaluate(EvaluationContext context)
    {
        return this.value;
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        return value;
    }
}

class VarExpression : Expression
{
    private string id;

    public VarExpression(string id)
    {
        this.id = id;
    }

    public string Identifier => id;

    public override object Evaluate(EvaluationContext context)
    {
        object value = context.Scope.GetSymbol(this.id);
        return value;
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        throw new NotImplementedException();
    }
}

class BlockExpression : Expression
{
    private IReadOnlyCollection<Expression> expressions;

    public BlockExpression(IReadOnlyCollection<Expression> expressions)
    {
        this.expressions = expressions;
    }

    public IReadOnlyCollection<Expression> Expressions => expressions;

    public override object Evaluate(EvaluationContext context)
    {
        context.Scope.StartScope();
        // TODO: pending dependency tracking
        foreach (var expr in expressions.Take(expressions.Count - 1))
        {
            expr.Evaluate(context);
        }

        object value = expressions.Last().Evaluate(context);
        context.Scope.EndScope();

        return value;
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        foreach (var expr in expressions.Take(expressions.Count - 1))
        {
            expr.Update(context, parent, value);
        }

        return expressions.Last().Update(context, parent, value);
    }
}

class FunctionDefExpression : Expression
{
    private string identifier;
    private Expression body;
    private IReadOnlyList<string> args;

    public FunctionDefExpression(string identifier, IReadOnlyList<string> args, Expression body)
    {
        this.identifier = identifier;
        this.body = body;
        this.args = args;
    }

    public string Identifier => identifier;
    public Expression Body => body;
    public IReadOnlyList<string> Args => args;

    public override object Evaluate(EvaluationContext context)
    {
        // TODO: pending dependency tracking
        var value = new FunctionValue(this.identifier, this.args, this.body);
        context.Scope.SetSymbol(identifier, value);
        return value;
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        throw new NotImplementedException();
    }
}

class FunctionCallExpression : Expression
{
    private string identifier;
    private IReadOnlyList<Expression> parameters;

    public FunctionCallExpression(string identifier, IReadOnlyList<Expression> parameters)
    {
        this.identifier = identifier;
        this.parameters = parameters;
    }

    public string Identifier => identifier;
    public IReadOnlyList<Expression> Parameters => parameters;

    public override object Evaluate(EvaluationContext context)
    {
        FunctionValue? func = context.Scope.GetSymbol(this.identifier) as FunctionValue;
        if (func == null)
        {
            throw new Exception($"'{identifier}' is not a function.");
        }

        if (parameters.Count != func.Args.Count)
        {
            throw new Exception($"Function '{identifier}' expects {func.Args.Count} parameters but was called with {parameters.Count}.");
        }

        // TODO: track pending dependencies
        List<object> paramValues = new List<object>();
        foreach (var param in parameters)
        {
            paramValues.Add(param.Evaluate(context));
        }

        context.Scope.StartScope();
        
        for (int i = 0; i < parameters.Count; i++)
        {
            context.Scope.SetSymbol(func.Args[i], paramValues[i]);
        }

        object value = func.Body.Evaluate(context);
        context.Scope.EndScope();
        return value;
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        throw new NotImplementedException();
    }
}

class RemoteExpression : Expression
{
    private readonly Expression innerExpression;
    public PendingValue pendingValue;
    private object? value = null;
    private string? tag = null;
    int status = 0;

    public RemoteExpression(Expression innerExpression, string? tag = null)
    {
        this.innerExpression = innerExpression;
        this.tag = tag;
    }
    
    
    public override object Evaluate(EvaluationContext context)
    {
        if (status == 0)
        {
            pendingValue = context.ValueTracker.Add(this);
            status = 1;
            context.DelayedTracker.ExecuteRemote(pendingValue.Id, innerExpression, this.tag);
            status = 1;
            return pendingValue;
        }
        else if (status == 1)
        {
            return pendingValue;
        }

        return value;
    }

    public override object Update(EvaluationContext context, Guid id, object value)
    {
        if (value is PendingValue)
        {
            return value;
        }

        this.value = value;
        status = 2;
        return value;
    }
}

//class PendingValueExpression : ReactiveExpression
//{
//    private readonly FutureValueExpression innerExpression;
//    public Guid Id { get; } = Guid.NewGuid();
//    int status = 0;
//    object? value = null;

//    public PendingValueExpression(FutureValueExpression expression)
//    {
//        this.innerExpression = expression;
//    }

//    public override object Evaluate(EvaluationContext context)
//    {
//        if (status == 0)
//        {
//            status = 1;
//            context.DelayedTracker.AddDependent(innerExpression.Id, Id, this);
//            status = 1;
//            return new PendingValue(Id);
//        }
//        else if (status == 1)
//        {
//            return new PendingValue(Id);
//        }

//        return value;
//    }

//    public override void Update(EvaluationContext context)
//    {
//        object value = context.DelayedTracker.GetValue(Id);
//        if (value is PendingValue)
//        {
//            return;
//        }

//        this.value = value;
//        status = 2;
//    }
//}



class ErrorValue { }

enum FutureStatus
{
    Pending,
    Error,
    Success
}