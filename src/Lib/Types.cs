using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSpike;

record struct PendingValue(Guid Id);

record FunctionValue(string Identifier, IReadOnlyCollection<string> Args, Expression Body);