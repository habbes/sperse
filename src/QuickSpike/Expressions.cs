namespace QuickSpike;

abstract class Expression
{
    public abstract object Evaluate(EvaluationContext context);
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
        context.SymbolTable.SetSymbol(this.id, value);

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
        return context.SymbolTable.GetSymbol(this.id);
    }
}

class FutureValueExpression : Expression
{
    private readonly Expression innerExpression;
    public Guid Id { get; } = Guid.NewGuid();

    public FutureValueExpression(Expression innerExpression)
    {
        this.innerExpression = innerExpression;
    }
    
    
    public override object Evaluate(EvaluationContext context)
    {
        context.DelayedTracker.DelayExecute(Id, innerExpression);
        return new PendingValue(Id);
    }
}

record struct PendingValue(Guid Id);

class ErrorValue { }

enum FutureStatus
{
    Pending,
    Error,
    Success
}