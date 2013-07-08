using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
    class Schema : Scoped<Database>
    {
        public override string FullName { get { return this.Name; } }

        public List<CompositeType> CompositeTypes { get; set; }
        public List<Table> Tables { get; set; }
        public List<View> Views { get; set; }
        public List<Function> Functions { get; set; }
        public List<Operator> Operators { get; set; }

        public override string TypeName { get { return Tag.Schema; } }
		/*
        public override string SqlCreate
        {
            get { return string.Format("CREATE SCHEMA \"{0}\" AUTHORIZATION \"{1}\";", this.Name, this.Owner); }
        }

        public override string SqlDrop
        {
            get { return string.Format("DROP SCHEMA IF EXISTS \"{0}\" CASCADE;", this.Name); }
        }*/
    }
}
