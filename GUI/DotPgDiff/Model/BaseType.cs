using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
    class BaseType : Scoped<Database>, IType
    {
        public long? RealTypeOid { get; set; }

        public override string TypeName { get { return null; } }

        public override string SqlCreate
        {
            get { throw new NotImplementedException(); }
        }

        public override string SqlDrop
        {
            get { throw new NotImplementedException(); }
        }
    }
}
