using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Model
{
    interface IBase
    {
		long Oid { get; set; }
		string Name { get; set; }
		string Owner { get; set; }
		string FullName { get; set; }
        string DisplayName { get; }

        string TypeName { get; }

        //List<IBase> DropCascadesTo { get; set; }
    }

    interface IType : IBase
    {
		long? RealTypeOid { get; set; }
    }

    abstract class Base : IBase
    {
        protected Base()
        {
            //this.DropCascadesTo = new List<IBase>();
        }

		public long Oid { get; set; }
		public string Name { get; set; }
		public string Owner { get; set; }

        public virtual string FullName { get; set; }
        public virtual string DisplayName { get { return this.FullName; } }

        public abstract string TypeName { get; }
		public virtual string SqlCreate { get; internal set; }
		public virtual string SqlDrop { get; internal set; }

        //public List<IBase> DropCascadesTo { get; set; }

        public override bool Equals(object obj)
        {
            try {
                var o = (IBase)obj;
                return 
                    o.GetType() == this.GetType()
                    && o.Oid == this.Oid;
            } catch {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return (this.GetType().ToString() + " " + this.Oid).GetHashCode();
        }
    }

    interface IScoped : IBase
    {
        string ParentName { get; }
    }

    abstract class Scoped<ParentType> : Base, IScoped
        where ParentType : Base
    {
        public ParentType Parent { get; internal set; }
        public string ParentName { get { return this.Parent.Name; } }
    }
}
