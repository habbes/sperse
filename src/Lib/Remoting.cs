using QuickSpike;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib;

class MockRemoteEvaluator
{
    private Evaluator eval = new();
    public async Task<object> Execute(string expression)
    {
        await Task.Delay(8000);
        return eval.Execute(expression);
    }
}