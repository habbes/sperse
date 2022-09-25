using Google.Protobuf.WellKnownTypes;
using QuickSpike;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib;

internal class ExpressionSerializer : ExpressionVisitor
{
    private readonly EvaluationContext context;
    private readonly Stack<string> valueStack = new();
    private readonly Stack<FunctionValue> functionStack = new();

    public ExpressionSerializer(EvaluationContext context)
    {
        this.context = context;
    }
    public string GetSerializedExpression()
    {
        if (valueStack.Count == 0)
        {
            throw new Exception("No serialized expression.");
        }

        Debug.Assert(valueStack.Count == 1);

        string body = valueStack.Pop();
        // hack: serialize as a block in case body contains multiple expressions
        // currently the parser can only handle multiple expressions if it's a block
        string block = $"{{\n{body}\n}}";
        return block;
    }

    public override void VisitAdd(AddExpression expression)
    {
        expression.Left.Accept(this);
        expression.Right.Accept(this);
        object rightValue = this.valueStack.Pop();
        object leftValue = this.valueStack.Pop();
        valueStack.Push($"{leftValue}+{rightValue}");
    }

    public override void VisitAssignment(AssignmentExpression expression)
    {
        throw new NotImplementedException();
    }

    public override void VisitIntConst(IntExpression expression)
    {
        this.valueStack.Push(expression.Value.ToString());
    }

    public override void VisitVar(VarExpression expression)
    {
        // hack: get the var's value if global variable, otherwise
        // assume variable is from function args or define inside the block
        if (this.functionStack.TryPeek(out FunctionValue function) && function.Args.Contains(expression.Identifier))
        {
            this.valueStack.Push(expression.Identifier);
            
        }
        else
        {
            object value = this.context.Scope.GetSymbol(expression.Identifier);
            this.valueStack.Push(value.ToString());
        }
    }

    public override void VisitBlock(BlockExpression expression)
    {
        List<string> statements = new();
        int initialStackLength = this.valueStack.Count;
        foreach (var expr in expression.Expressions)
        {
            expr.Accept(this);
            if (this.valueStack.Count > initialStackLength)
            {
                statements.Add(this.valueStack.Pop());
            }
        }
        string body = string.Join('\n', statements);
        string block = $"{{\n{body}\n}}";

        this.valueStack.Push(block);
    }

    public override void VisitFunctionCall(FunctionCallExpression expression)
    {
        FunctionValue function = this.context.Scope.GetSymbol(expression.Identifier) as FunctionValue;
        if (function == null)
        {
            throw new Exception($"'{expression.Identifier}' is not a function.");
        }

        // function def
        this.functionStack.Push(function);
        function.Body.Accept(this);
        string functionBody = this.valueStack.Pop();
        string functionSig = $"{function.Identifier}({string.Join(',', function.Args)})";
        string functionDef = $"def {functionSig}{functionBody}";

        List<string> serializedParams = new();
        foreach (var param in expression.Parameters)
        {
            param.Accept(this);
            serializedParams.Add(this.valueStack.Pop());
        }

        string functionCall = $"{expression.Identifier}({string.Join(',', serializedParams)})";

        string serialized = $"{functionDef}\n{functionCall}";

        this.functionStack.Pop();
        this.valueStack.Push(serialized);    }

    public override void VisitFunctionDef(FunctionDefExpression expression)
    {
        throw new NotImplementedException();
    }
}
