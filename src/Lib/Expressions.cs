﻿namespace QuickSpike;

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
    }

    public abstract void VisitAssignment(AssignmentExpression expression);
    public abstract void VisitAdd(AddExpression expression);
    public abstract void VisitIntConst(IntExpression expression);
    public abstract void VisitVar(VarExpression expression);
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
            context.SymbolTable.SetSymbol(this.id, pendingValue);
        }
        else
        {
            context.SymbolTable.SetSymbol(this.id, value);
        }

        return value;
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        context.SymbolTable.SetSymbol(this.id, value);
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
        object value = context.SymbolTable.GetSymbol(this.id);
        return value;
    }

    public override object Update(EvaluationContext context, Guid parent, object value)
    {
        throw new NotImplementedException();
    }
}

class FutureValueExpression : ReactiveExpression
{
    private readonly Expression innerExpression;
    public PendingValue pendingValue;
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
            pendingValue = context.ValueTracker.Add(this);
            status = 1;
            context.DelayedTracker.DelayExecute(pendingValue.Id, this, innerExpression);
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

record struct PendingValue(Guid Id);

class ErrorValue { }

enum FutureStatus
{
    Pending,
    Error,
    Success
}