using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using RhinoInRR.Properties;
using System.Diagnostics;

namespace RhinoInRR
{
    public class ImportModelComponent : GH_Component
    {
        public ImportModelComponent()
            : base("ImportModel", "iM", "Import Lines to Rhino", "rhino-in", "ЯR")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Import Model", "iM", "set true to import model to Rhino", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Import Selected Region", "srM", "set true to import only selected region", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Import Dummy Members", "idM", "set false to import dummy members", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("RSTAB Nodes", "Nodes", "Nodes avalible in RSTAB", GH_ParamAccess.list);
            pManager.AddTextParameter("RSTAB Nodes No.", "Nodes No.", "Nodes No. avalible in RSTAB", GH_ParamAccess.list);
            pManager.AddCurveParameter("RSTAB Members", "Members", "Members avalible in RSTAB", GH_ParamAccess.tree);
            pManager.AddTextParameter("RSTAB Cross Sections", "Cross Sections No.", "Cross Sections avalible in RSTAB", GH_ParamAccess.list);
            pManager.AddTextParameter("RSTAB Members No.", "Members No.", "Members No. avalible in RSTAB", GH_ParamAccess.tree);
            pManager.AddTextParameter("Output", "O", "Output information", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool bc = false;
            bool bs = false;
            bool bd = false;

            #region cheking input params
            if (!DA.GetData(0, ref bc)) { return; }
            if (!bc) return;

            DA.GetData(1, ref bs);
            DA.GetData(2, ref bd);
            #endregion

            var w = Stopwatch.StartNew();

            RRConverter rrc = new RRConverter();
            rrc.RSTAB2Rhino(bs, bd);
            if (rrc.ErrorCode == 0)
            {
                DA.SetDataList(0, rrc.RRObject.RhinoNodes());
                DA.SetDataList(1, rrc.RRObject.RhinoNodesNo());
                DA.SetDataTree(2, rrc.RRObject.RhinoLinesTree());
                DA.SetDataList(3, rrc.RRObject.RhinoCrSec());
                DA.SetDataTree(4, rrc.RRObject.RhinoMembersTree());
                w.Stop();
                DA.SetData(5, "No Errors occured\nImport time " + (w.Elapsed.TotalMilliseconds / 1000).ToString("~0.00 sec"));
            }
            else
            {
                w.Stop();
                DA.SetDataList(0, null);
                DA.SetDataList(1, null);
                DA.SetDataList(2, null);
                DA.SetDataList(3, null);
                DA.SetDataList(4, null);
                DA.SetData(5, rrc.ErrorCode + " " + rrc.ErrorMessage);
            }
        }

        protected override Bitmap Internal_Icon_24x24
        {
            get { return Resources.import24; }
        }

        public override Guid ComponentGuid
        {
            // Online Guid Generator:
            //http://www.newguid.com/

            get { return new Guid("6f8e41f2-ec97-4066-bc7c-7638e5804c67"); }
        }
    }
}