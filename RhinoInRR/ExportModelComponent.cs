using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Diagnostics;
using RhinoInRR.Properties;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace RhinoInRR
{
    public class ExportModelComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ExportModelComponent()
            : base("ExportModel", "xM", "Export Curves to Rstab", "rhino-in", "ЯR")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //http://wiki.mcneel.com/developer/rhinocommonsamples/closestpoint
            /**
             * One length for each curve. If the number of lengths is less than the one of curves, 
             * length values are repeated in pattern.
             * If there are no lengths, then the physical 
             * length of the curves is computed.
             */
            pManager.AddCurveParameter("Curves", "C", "Curves to export", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Tolerance", "t", "Set tolerances for merge points/lines", GH_ParamAccess.item);
            pManager.AddPointParameter("Support Points", "SP", "Set support points", GH_ParamAccess.list);
            pManager.AddTextParameter("Cross Section tree", "CS", "Set cross sections (tree)", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Export Model", "xM", "set true to export file to RSTAB", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Delete Structural Data", "rM", "set false to keep existing Structural Data", GH_ParamAccess.item, true);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Info", "I", "Information message", GH_ParamAccess.item);
            pManager.AddTextParameter("Output", "O", "Output information", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Curve> cr = new GH_Structure<GH_Curve>(); // curves tree
            List<Point3d> ap = new List<Point3d>(); // anchor points
            List<string> cl = new List<string>();  // cross section list
            double tl = new Double();        // tolerance
            bool xm = false;               // export model into RSTAB
            bool rm = true;                // remove structural data

            #region checking input params
            if (!DA.GetDataTree(0, out cr)) { return; }
            if (!DA.GetData(1, ref tl)) { tl = -1; };
            if (tl <= 0) tl = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            DA.GetDataList(2, ap);
            DA.GetDataList(3, cl);

            if (!DA.GetData(4, ref xm)) { return; }
            if (xm == false) return;

            if (!DA.GetData(5, ref rm)) { return; }
            #endregion

            var w = Stopwatch.StartNew();

            #region graph creation
            int pid = 1, bid = 1;
            RRObject ro = new RRObject();
            RRMtrl mt = new RRMtrl();
            ro.AddMtrl(mt);

            foreach (List<GH_Curve> lc in cr.Branches)
            {
                string dsc = "IPE 100";
                if (cl.Count >= bid) dsc = cl[bid - 1];

                ro.AddCrCs(new RRCrSc { ID = bid, Material = mt, Description = dsc });

                // check any curve and build the graph from the points
                foreach (GH_Curve curve in lc)
                {
                    RRNode p1, p2;
                    bool b1, b2;

                    p1 = p2 = null;
                    b1 = b2 = true;

                    foreach (RRNode rn in ro.Nodes)
                    {
                        Point3d p = rn.GetPoint3d();

                        if (b1)
                        {
                            if (curve.Value.PointAtStart.DistanceTo(p) <= tl)
                            {
                                b1 = false;
                                p1 = rn;
                            }
                        }

                        if (b2)
                        {
                            if (curve.Value.PointAtEnd.DistanceTo(p) <= tl)
                            {
                                b2 = false;
                                p2 = rn;
                            }
                        }

                        if (!b1 && !b2) break;
                    }

                    if (b1)
                    {
                        p1 = new RRNode(pid++, curve.Value.PointAtStart);
                        ro.AddNode(p1);
                    }

                    if (b2)
                    {
                        p2 = new RRNode(pid++, curve.Value.PointAtEnd);
                        ro.AddNode(p2);
                    }

                    RREdge re = new RREdge { StartNode = p1, EndNode = p2, CrossSection = ro.CrCss[bid - 1] };
                    ro.AddEdge(re, true);
                }

                bid++;
            }
            #endregion

            RRConverter rrc = new RRConverter(ro);
            rrc.Rhino2RSTAB(ap, tl, rm);

            w.Stop();

            if (rrc.ErrorCode == 0)
            {
                DA.SetData(1, "No Errors occured\nExport time " + (w.Elapsed.TotalMilliseconds / 1000).ToString("~0.00 sec"));
            }
            else
            {
                DA.SetData(1, rrc.ErrorCode + " " + rrc.ErrorMessage);
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return Resources.export24; 
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{d9128af3-4cd6-4a69-9247-db6e33980269}"); }
        }
    }
}