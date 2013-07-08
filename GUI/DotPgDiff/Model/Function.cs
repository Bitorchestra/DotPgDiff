using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
    class Function : Scoped<Schema>
    {
        public string Signature { get; set; }
        public List<long> UsedTypeOids { get; set; }

        public override string TypeName { get { return Tag.Function; } }

        public override string DisplayName
        {
            //get { return string.Format("{0}({1})", this.FullName, this.Signature); }
			get { return this.Signature; }
        }
    }
}
