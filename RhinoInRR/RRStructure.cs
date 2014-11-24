using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Dlubal.RSTAB6;

namespace RhinoInRR
{
    /// <summary>
    /// Class for storing Rhino-RSTAB object
    /// </summary>
    public class RRObject
    {
        /// <summary>
        /// Author antklim
        /// </summary>
        private List<RRNode> nds;    // list of nodes
        private List<RREdge> edg;    // list of edges
        private List<RRMtrl> mat;    // list of materials
        private List<RRCrSc> csc;    // list of cross sections

        /// <summary>
        /// Default constructor
        /// </summary>
        public RRObject()
        {
            this.nds = new List<RRNode>();
            this.edg = new List<RREdge>();
            this.mat = new List<RRMtrl>();
            this.csc = new List<RRCrSc>();
        }

        /// <summary>
        /// Getter/Setter for RSTABNode list
        /// </summary>
        public List<RRNode> Nodes
        {
            get { return this.nds; }
            set { this.nds = value; }
        }

        /// <summary>
        /// Getter/Setter for RSTABEdge list
        /// </summary>
        public List<RREdge> Edges
        {
            get { return this.edg; }
            set { this.edg = value; }
        }

        /// <summary>
        /// Getter/Setter for RSTABMtrls list
        /// </summary>
        public List<RRMtrl> Mtrls
        {
            get { return this.mat; }
            set { this.mat = value; }
        }

        /// <summary>
        /// Getter/Setter for RSTABCrCs list
        /// </summary>
        public List<RRCrSc> CrCss
        {
            get { return this.csc; }
            set { this.csc = value; }
        }

        /// <summary>
        /// Adds new node into the list
        /// </summary>
        /// <param name="a">Node to add</param>
        public void AddNode(RRNode a)
        {
            this.nds.Add(a);
        }

        /// <summary>
        /// Adds new edge into the list
        /// </summary>
        /// <param name="a">Edge to add</param>
        public void AddEdge(RREdge a, bool b)
        {
            if (!b)
                this.edg.Add(a);
            else
            {
                if (!haveEdge(this.edg, a))
                {
                    this.edg.Add(a);
                }
            }
        }

        /// <summary>
        /// Adds new material into the list
        /// </summary>
        /// <param name="a">Material to add</param>
        public void AddMtrl(RRMtrl a)
        {
            this.mat.Add(a);
        }

        /// <summary>
        /// Adds new cross section into the list
        /// </summary>
        /// <param name="a">Cross section to add</param>
        public void AddCrCs(RRCrSc a)
        {
            this.csc.Add(a);
        }

        /// <summary>
        /// Represents list of nodes as list of Rhino.Point3d
        /// </summary>
        /// <returns>List of Rhino.Point3d</returns>
        public List<Point3d> RhinoNodes()
        {
            List<Point3d> ps = new List<Point3d>();

            foreach (RRNode n in nds)
            {
                ps.Add(n.GetPoint3d());
            }

            return ps;
        }

        /// <summary>
        /// Returns list of nodes no.
        /// </summary>
        /// <returns>List of nodes no.</returns>
        public List<string> RhinoNodesNo()
        {
            List<string> ls = new List<string>();

            foreach (RRNode n in this.nds)
            {
                ls.Add(n.ID.ToString());
            }

            return ls;
        }

        /// <summary>
        /// Represents list of edges as list of Rhino.Line
        /// </summary>
        /// <returns>List of Rhino.Line</returns>
        public List<Line> RhinoLines()
        {
            List<Line> ls = new List<Line>();

            foreach (RREdge e in edg)
            {
                ls.Add(e.GetLine());
            }

            return ls;
        }

        /// <summary>
        /// Represents list of edges as tree of Rhino.Line
        /// </summary>
        /// <returns>Tree of Rhino.Line</returns>
        public GH_Structure<GH_Curve> RhinoLinesTree()
        {
            GH_Structure<GH_Curve> ls = new GH_Structure<GH_Curve>();
            List<int> cs = new List<int>();
            int i = 0;

            List<RREdge> le = new List<RREdge>();

            foreach (RREdge e in edg)
            {
                if (e.CrossSection != null)
                {
                    i = cs.IndexOf(e.CrossSection.ID);
                    if (i == -1)
                    {
                        cs.Add(e.CrossSection.ID);
                        i = cs.Count - 1;
                    }

                    ls.Append(e.GetGHCurve(), new GH_Path(i));
                }
                else
                {
                    le.Add(e);
                }
            }

            GH_Path zp = new GH_Path(cs.Count - 1);
            foreach (RREdge e in le)
            {
                ls.Append(e.GetGHCurve(), zp);
            }

            return ls;
        }

        /// <summary>
        /// Represents list of edges as tree of Member No.
        /// </summary>
        /// <returns>Tree of Members No.</returns>
        public DataTree<string> RhinoMembersTree()
        {
            DataTree<string> ls = new DataTree<string>();
            List<int> cs = new List<int>();
            int i = 0;

            List<RREdge> le = new List<RREdge>();

            foreach (RREdge e in edg)
            {
                if (e.CrossSection != null)
                {
                    i = cs.IndexOf(e.CrossSection.ID);
                    if (i == -1)
                    {
                        cs.Add(e.CrossSection.ID);
                        i = cs.Count - 1;
                    }

                    ls.Add(e.ID.ToString(), new GH_Path(i));
                }
                else
                {
                    le.Add(e);
                }
            }

            GH_Path zp = new GH_Path(cs.Count - 1);
            foreach (RREdge e in le)
            {
                ls.Add(e.ID.ToString(), zp);
            }

            return ls;
        }

        /// <summary>
        /// Returns Cross section list
        /// </summary>
        /// <returns>Cross section list</returns>
        public List<string> RhinoCrSec()
        {
            List<string> ls = new List<string>();

            foreach (RRCrSc cs in this.csc)
            {
                ls.Add(cs.Description);
            }

            return ls;
        }

        /// <summary>
        /// Returns tree of members divided by cross sections
        /// </summary>
        /// <returns>Tree of members</returns>
        public DataTree<string> RhinoCrSecTree()
        {
            int i = 0;
            DataTree<string> t = new DataTree<string>();

            foreach (RRCrSc cs in this.csc)
            {
                GH_Path path = new GH_Path(i++);
                List<string> ls = new List<string>();
                ls.Add(cs.ID + "_" + cs.Description);

                foreach (RREdge re in this.edg)
                {
                    if (re.CrossSection.Description.Equals(cs.Description)) ls.Add(re.ID.ToString());
                }
                t.AddRange(ls, path);
            }

            return t;
        }

        /// <summary>
        /// Finds Cross Section by ID
        /// </summary>
        /// <param name="a">ID of the cross section</param>
        /// <returns>Cross section with ID or null</returns>
        public RRCrSc GetCrCsByNo(int a)
        {
            foreach (RRCrSc c in this.csc)
            {
                if (c.ID == a) return c;
            }
            return null;
        }

        /// <summary>
        /// Check if RSTAB edge in the list
        /// </summary>
        /// <param name="le">list of RSTAB edges</param>
        /// <param name="re">RSTAB edge to find in the list</param>
        /// <returns>True if the edge in the list, false if there is no such edges in the list</returns>
        private bool haveEdge(List<RREdge> a, RREdge b)
        {
            foreach (RREdge e in a)
            {
                if (e.Equals(b)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Class for storing RR node
    /// </summary>
    public class RRNode
    {
        /// <summary>
        /// Author antklim
        /// </summary>

        const double POINT_TOLERANCE = 0.001d;
        private int id;     // node ID
        private double x;   // node x coordinate
        private double y;   // node y coordinate
        private double z;   // node z coordinate

        public RRNode()
        {
        }

        /// <summary>
        /// Constructor using int as ID and Rhino.Point3d
        /// </summary>
        /// <param name="a">ID</param>
        /// <param name="b">Rhino.Point3d for getting the coordinates</param>
        public RRNode(int a, Point3d b)
        {
            this.id = a;
            this.x = b.X;
            this.y = b.Y;
            this.z = b.Z;
        }

        /// <summary>
        /// Constructor using RSTAB node
        /// </summary>
        /// <param name="a">RSTAB node for getting the ID and coordinates</param>
        public RRNode(RS_NODE a)
        {
            this.id = a.iNo;
            this.x = a.x;
            this.y = a.y;
            this.z = a.z;
        }

        /// <summary>
        /// Getter/setter for ID
        /// </summary>
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Getter/setter for x coordinate
        /// </summary>
        public double X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// Getter/setter for y coordinate
        /// </summary>
        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// Getter/setter for z coordinate
        /// </summary>
        public double Z
        {
            get { return z; }
            set { z = value; }
        }

        /// <summary>
        /// Represents node as Rhino.Point3d
        /// </summary>
        /// <returns>Rhino.Point3d</returns>
        public Point3d GetPoint3d()
        {
            return new Point3d { X = this.x, Y = this.y, Z = this.z };
        }

        /// <summary>
        /// Represents node as RSTAB node
        /// </summary>
        /// <returns>RSTAB Node</returns>
        public RS_NODE GetRSNode()
        {
            return new RS_NODE { iNo = this.id, csType = RS_CS_TYPE.CS_CARTESIAN, x = this.x, y = this.y, z = this.z };
        }

        public override bool Equals(object obj)
        {
            if (this.GetPoint3d().DistanceTo(((RRNode)obj).GetPoint3d()) <= POINT_TOLERANCE) return true;
            return false;
        }

        public override string ToString()
        {
            return "Node X: " + this.x + ", Y: " + this.y + ", Z: " + this.z;
        }
    }

    /// <summary>
    /// Class for storing RR edge
    /// </summary>
    public class RREdge
    {
        /// <summary>
        /// Author antklim
        /// </summary>
        private bool or = false;    // orientation flag
        private int id = 0;         // member id (need for import)
        private RRNode sn;          // start node of the edge
        private RRNode en;          // end node of the edge
        private RRCrSc cs;          // cross section of the edge

        /// <summary>
        /// Default constructor
        /// </summary>
        public RREdge() : base() { }

        /// <summary>
        /// Constructor from a Grasshopper curve
        /// </summary>
        /// <param name="a">Grasshopper curve</param>
        public RREdge(GH_Curve a)
        {
            this.or = false;
            this.sn = new RRNode(0, a.Value.PointAtStart);
            this.en = new RRNode(0, a.Value.PointAtEnd);
            this.cs = null;
        }

        /// <summary>
        /// Constructor from two nodes
        /// </summary>
        /// <param name="a">First node</param>
        /// <param name="b">Second node</param>
        public RREdge(RRNode a, RRNode b)
        {
            this.or = false;
            this.sn = a;
            this.en = b;
            this.cs = null;
        }

        /// <summary>
        /// Getter/setter for ID
        /// </summary>
        public int ID
        {
            get { return this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Getter/setter for start node
        /// </summary>
        public RRNode StartNode
        {
            get { return sn; }
            set { sn = value; }
        }

        /// <summary>
        /// Getter/setter for end node
        /// </summary>
        public RRNode EndNode
        {
            get { return en; }
            set { en = value; }
        }

        /// <summary>
        /// Getter/setter for cross section
        /// </summary>
        public RRCrSc CrossSection
        {
            get { return cs; }
            set { cs = value; }
        }

        /// <summary>
        /// Represents edge as Rhino.Line
        /// </summary>
        /// <returns>Rhino.Line</returns>
        public Line GetLine()
        {
            return new Line { From = sn.GetPoint3d(), To = en.GetPoint3d() };
        }

        /// <summary>
        /// Represents edge as Rhino.Curve
        /// </summary>
        /// <returns>Rhino.Curve</returns>
        public Curve GetCurve()
        {
            Line l = this.GetLine();
            return l.ToNurbsCurve();
        }

        /// <summary>
        /// Represents edge as Grasshoper GH_Curve
        /// </summary>
        /// <returns>Rhino.GH_Curve</returns>
        public GH_Curve GetGHCurve()
        {
            return new GH_Curve { Value = this.GetCurve() };
        }

        /// <summary>
        /// Represents edge as RSTAB member
        /// </summary>
        /// <param name="a">RSTAB member ID</param>
        /// <returns>RSTAB Member</returns>
        public RS_MEMBER GetRSMember(int a)
        {
            return new RS_MEMBER
            {
                iNo = a,
                ID = (a).ToString(),
                type = RS_MEMBER_TYPE.MT_BEAM,
                iStartNodeNo = this.sn.ID,
                iEndNodeNo = this.en.ID,
                iStartCrossSectionNo = this.cs.ID,
                iEndCrossSectionNo = this.cs.ID
            };
        }

        public override bool Equals(Object a)
        {
            if (or)
            {
                if (sn.Equals(((RREdge)a).StartNode) && en.Equals(((RREdge)a).EndNode)) return true;
            }
            else
            {
                if ((sn.Equals(((RREdge)a).StartNode) && en.Equals(((RREdge)a).EndNode)) ||
                    (en.Equals(((RREdge)a).StartNode) && sn.Equals(((RREdge)a).EndNode)))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Class for storing RR cross section
    /// </summary>
    public class RRCrSc
    {
        /// <summary>
        /// Author antklim
        /// </summary>
        private int id;      // cross section ID
        private RRMtrl mt;   // cross section material
        private string ds;   // cross section description

        /// <summary>
        /// Default constructor
        /// </summary>
        public RRCrSc()
        {
            this.id = 1;
            this.mt = new RRMtrl();
            this.ds = "IPE 100";
        }

        /// <summary>
        /// Constructor from int, material and cross section description
        /// </summary>
        /// <param name="id">Cross section ID</param>
        /// <param name="mi">Material</param>
        /// <param name="ds">Cross section description</param>
        public RRCrSc(int id, RRMtrl mi, string ds)
        {
            this.id = id;
            this.mt = mi;
            this.ds = ds;
        }

        /// <summary>
        /// Constructor from RSTAB Cross section
        /// </summary>
        /// <param name="cs">RSTAB Cross section to convert</param>
        public RRCrSc(RS_CROSS_SECTION cs)
        {
            this.id = cs.iNo;
            this.mt = null;
            this.ds = cs.strDescription;
        }

        /// <summary>
        /// Getter/setter from ID
        /// </summary>
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Getter/setter for material
        /// </summary>
        public RRMtrl Material
        {
            get { return mt; }
            set { mt = value; }
        }

        /// <summary>
        /// Getter/setter for description
        /// </summary>
        public string Description
        {
            get { return ds; }
            set { ds = value; }
        }
    }

    /// <summary>
    /// Class for storing RR material
    /// </summary>
    public class RRMtrl
    {
        /// <summary>
        /// Author antklim
        /// </summary>
        private int id;     // material ID
        private string ds;  // material description

        /// <summary>
        /// Default constructor
        /// </summary>
        public RRMtrl()
        {
            this.id = 1;
            this.ds = "Steel St 37";
        }

        /// <summary>
        /// Constructor from int and description
        /// </summary>
        /// <param name="id">Material ID</param>
        /// <param name="ds">Material description</param>
        public RRMtrl(int id, string ds)
        {
            this.id = id;
            this.ds = ds;
        }

        /// <summary>
        /// Getter/setter for ID
        /// </summary>
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Getter/setter for description
        /// </summary>
        public string Description
        {
            get { return ds; }
            set { ds = value; }
        }
    }

    /// <summary>
    /// Edge
    /// </summary>
    class Edge
    {
        /// <summary>
        /// Start Point of an edge
        /// </summary>
        public int Start;

        /// <summary>
        /// End Point of an edge
        /// </summary>
        public int End;
    }

    /// <summary>
    /// Cycle presents as a set of edges
    /// </summary>
    class RRCycle
    {
        /// <summary>
        /// List of Edges
        /// </summary>
        public List<Edge> Edges = new List<Edge>();

        public override bool Equals(object obj)
        {
            RRCycle right = (RRCycle)obj;

            //in case od different number of edges, cycles  are not equals
            if (right.Edges.Count != this.Edges.Count)
            {
                return false;
            }

            //check: algorithm shouldn't take same way twice
            foreach (Edge e in this.Edges)
            {
                if (!right.Edges.Contains(e))
                {
                    return false;
                }
            }


            return true;
        }
    }
}
