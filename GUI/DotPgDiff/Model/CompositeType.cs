using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
	class CompositeType : Scoped<Schema>
	{
		public override string TypeName { get { return Tag.CompositeType; } }
		//public List<Column> Columns { get; set; }
	}
}
