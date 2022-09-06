namespace QuickSpike;

abstract class Expression
{
    public abstract object Evaluate(EvaluationContext context);
}

abstract class ReactiveExpression : Expression
{
    public abstract void Update(EvaluationContext context);
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
        if (value is PendingValue)
        {
            context.SymbolTable.SetSymbol(this.id, this.expression);
        }
        else
        {
            context.SymbolTable.SetSymbol(this.id, value);
        }

        return value;
    }
}

class AddExpression : Expression
{
    private Expression left;
    private Expression right;

    public AddExpression(Expression left, Expression right)
    {
        this.left = left;
        this.right = right;
    }

    public override object Evaluate(EvaluationContext context)
    {
        return (int)this.left.Evaluate(context) + (int)this.right.Evaluate(context);
    }
}

class IntExpression : Expression
{
    private int value;

    public IntExpression(int value)
    {
        this.value = value;
    }

    public override object Evaluate(EvaluationContext context)
    {
        return this.value;
    }
}

class VarExpression : Expression
{
    private string id;

    public VarExpression(string id)
    {
        this.id = id;
    }

    public override object Evaluate(EvaluationContext context)
    {
        object value = context.SymbolTable.GetSymbol(this.id);
        if (value is Expression expression)
        {
            return expression.Evaluate(context);
        }

        return value;
    }
}

class FutureValueExpression : ReactiveExpression
{
    private readonly Expression innerExpression;
    public Guid Id { get; } = Guid.NewGuid();
    private object? value = null;
    int status = 0;

    public FutureValueExpression(Expression innerExpression)
    {
        this.innerExpression = innerExpression;
    }
    
    
    public override object Evaluate(EvaluationContext context)
    {
        if (status == 0)
        {
            status = 1;
            context.DelayedTracker.DelayExecute(Id, this, innerExpression);
            status = 1;
            return new PendingValue(Id);
        }
        else if (status == 1)
        {
            return new PendingValue(Id);
        }

        return value;
    }

    public override void Update(EvaluationContext context)
    {
        object value = context.DelayedTracker.GetValue(Id);
        if (value is PendingValue)
        {
            return;
        }

        this.value = value;
        status = 2;
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

record struct PendingValue(Guid Id);

class ErrorValue { }

enum FutureStatus
{
    Pending,
    Error,
    Success
}