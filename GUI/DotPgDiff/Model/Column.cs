using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
    class Column : Scoped<CompositeType>
    {
        public long TypeOid { get; set; }
        public int? TypeLen { get; set; }
        public IType Type { get; set; }
        public string Default { get; set; }
        public bool IsNullable { get; set; }

        public override string DisplayName
        {
            get
            {
                return string.Format("{0} ({1}, {2})", 
                    this.Name, 
                    this.TypeLen.HasValue ? string.Format("{0}({1})", this.Type.FullName, this.TypeLen.Value) : this.Type.FullName,
                    this.IsNullable ? "null" : "not null");
            }
        }

        public override string TypeName { get { return Tag.Column; } }

        public override string SqlCreate
        {
            get
            {
                if (this.Parent is View) {
                    return String.Empty;
                } else if (this.Parent is Table) {
                    return string.Format("ALTER TABLE {0} ADD COLUMN {1} {2};", this.Parent.FullName, this.Name, this.Type.FullName);
                } else if (this.Parent is CompositeType) {
                    return string.Format("ALTER TYPE {0} ADD ATTRIBUTE {1} {2};", this.Parent.FullName, this.Name, this.Type.FullName);
                } else {
                    return String.Empty;
                }
            }
        }

        public override string SqlDrop
        {
            get
            {
                if (this.Parent is View) {
                    return String.Empty;
                } else if (this.Parent is Table) {
                    return string.Format("ALTER TABLE {0} DROP COLUMN IF EXISTS {1} CASCADE;", this.Parent.FullName, this.Name);
                } else if (this.Parent is CompositeType) {
                    return string.Format("ALTER TYPE {0} DROP ATTRIBUTE IF EXISTS {1} CASCADE;", this.Parent.FullName, this.Name);
                } else {
                    return String.Empty;
                }
            }
        }
    }
}
