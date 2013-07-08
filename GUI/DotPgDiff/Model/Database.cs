using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BO.DotPgDiff.Model
{
    class Database : Base
    {
        public List<Schema> Schemas { get; set; }
        public List<BaseType> BaseTypes { get; set; }

        public Dictionary<string, Dictionary<long, Base>> Elements { get; private set; }

        private Dictionary<long, Base> ToDic<T>(IEnumerable<T> set)
            where T : Base
        {
            return set.ToDictionary(x => x.Oid, x => x as Base);
        }

        public void BuildCache()
        {
            var fs = this.Schemas.SelectMany(x => x.Functions);
            var ops = this.Schemas.SelectMany(s => s.Operators);
            var cts = this.Schemas.SelectMany(s => s.CompositeTypes);
            var tbls = this.Schemas.SelectMany(s => s.Tables);
            var vs = this.Schemas.SelectMany(s => s.Views);

            /*var tps = new List<IType>();
            tps.AddRange(cts.Cast<IType>());
            tps.AddRange(this.BaseTypes.Cast<IType>());*/
			/*
            Func<IType, IType> resolveT = t => {
                if (t.RealTypeOid.HasValue) {
                    try {
                        return vs.Single(x => x.Oid == t.RealTypeOid.Value);
                    } catch {
                        try {
                            return tbls.Single(x => x.Oid == t.RealTypeOid.Value);
                        } catch {
                            return t;
                        }
                    }
                } else {
                    return t;
                }
            };*/

            this.Elements = new Dictionary<string, Dictionary<long, Base>>();
            this.Elements[Tag.Schema] = ToDic(this.Schemas);
            this.Elements[Tag.CompositeType] = ToDic(cts);
            this.Elements[Tag.Table] = ToDic(tbls);
            this.Elements[Tag.View] = ToDic(vs);
            this.Elements[Tag.Function] = ToDic(fs);
            this.Elements[Tag.Operator] = ToDic(ops);
			/*
            this.DropCascadesTo.AddRange(this.Elements.Values.SelectMany(x => x.Values).Cast<IBase>());

            // schemas
            {
                foreach (var s in this.Schemas) {
                    s.DropCascadesTo.AddRange(s.Functions);
                    s.DropCascadesTo.AddRange(s.Operators);
                    s.DropCascadesTo.AddRange(s.CompositeTypes);
                    s.DropCascadesTo.AddRange(s.Tables);
                    s.DropCascadesTo.AddRange(s.Views);
                }
            }
            // functions
            {
                foreach (var f in fs) {
                    f.UsedTypes = tps.Where(x => f.UsedTypeOids.Contains(x.Oid)).Select(resolveT).ToList();
                    foreach (var u in f.UsedTypes)
                        u.DropCascadesTo.Add(f);
                    var rx = new Regex("\\W" + f.FullName + "\\W");
                    foreach (var ff in fs.Where(w => w.Oid != f.Oid && rx.IsMatch(w.SqlCreate)))
                        f.DropCascadesTo.Add(ff);
                }
            }
            // tables
            {
                foreach (var t in tbls) {
                    var rx = new Regex("\\W" + t.FullName + "\\W");
                    foreach (var vv in vs.Where(w => rx.IsMatch(w.SqlCreate)))
                        t.DropCascadesTo.Add(vv);
                }
            }
            // composite types
            {
                Action<IEnumerable<CompositeType>> colsAndFuncs = coll => {
                    foreach (var t in coll) {
                        foreach (var c in t.Columns) {
                            c.Type = resolveT(tps.Single(x => x.Oid == c.TypeOid));
                        }
                        var rx = new Regex("\\W" + t.FullName + "\\W");
                        foreach (var f in fs.Where(w => w.UsedTypeOids.Contains(t.Oid) || rx.IsMatch(w.SqlCreate) || w.UsedTypes.Select(j => j.Oid).Contains(t.Oid)))
                            t.DropCascadesTo.Add(f);
                    }
                };
                colsAndFuncs(cts);
                colsAndFuncs(tbls);
                colsAndFuncs(vs);
            }
            // operators
            {
                foreach (var o in ops) {
                    o.Func = fs.SingleOrDefault(x => x.Oid == o.FuncOid);
                    if (o.Func != null)
                        o.Func.DropCascadesTo.Add(o);

                    o.RetType = resolveT(tps.Single(x => x.Oid == o.RetTypeOid));
                    o.RetType.DropCascadesTo.Add(o);

                    if (o.LeftTypeOid.HasValue) {
                        o.LeftType = resolveT(tps.Single(x => x.Oid == o.LeftTypeOid.Value));
                        if (o.LeftTypeOid != o.RetTypeOid)
                            o.LeftType.DropCascadesTo.Add(o);
                    }
                    if (o.RightTypeOid.HasValue) {
                        o.RightType = resolveT(tps.Single(x => x.Oid == o.RightTypeOid.Value));
                        if (o.RightTypeOid != o.RetTypeOid && (o.LeftTypeOid.HasValue && o.LeftTypeOid != o.RightTypeOid || !o.LeftTypeOid.HasValue))
                            o.RightType.DropCascadesTo.Add(o);
                    }
                    if (o.CommutatorOid.HasValue) {
                        o.Commutator = ops.Single(x => x.Oid == o.CommutatorOid.Value);
                        if (o.CommutatorOid != o.Oid)
                            o.Commutator.DropCascadesTo.Add(o);
                    }
                    if (o.NegatorOid.HasValue) {
                        o.Negator = ops.Single(x => x.Oid == o.NegatorOid.Value);
                        if (o.NegatorOid != o.Oid)
                            o.Negator.DropCascadesTo.Add(o);
                    }
                }
            }*/
        }

        public override string TypeName { get { return Tag.Database; } }

        public override string SqlCreate
        {
            get { return string.Format("CREATE DATABASE \"{0}\" OWNER \"{1}\";", this.Name, this.Owner); }
        }

        public override string SqlDrop
        {
            get { return string.Format("DROP DATABASE \"{0}\";", this.Name); }
        }
    }
}
