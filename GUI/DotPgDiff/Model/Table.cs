using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
    class Table : CompositeType
    {
        public override string TypeName { get { return Tag.Table; } }
    }
}
