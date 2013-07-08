using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
    class View : CompositeType
    {
        public override string TypeName { get { return Tag.View; } }
    }
}
