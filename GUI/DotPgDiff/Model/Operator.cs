using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
    class Operator : Scoped<Schema>
    {
        public override string TypeName { get { return Tag.Operator; } }
    }
}
