using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dlubal.RSTAB6;
using Rhino.Geometry;
using System.Runtime.InteropServices;
using System.Reflection;

namespace RhinoInRR
{
    /// <summary>
    /// This class is for converting objects from Rhino to RSTAB and from RSTAB to Rhino
    /// </summary>
    class RRConverter
    {
        /// <summary>
        /// Author antklim
        /// </summary>

        #region Errors
        private enum ErrCode
        {
            NONE = 0,           // 0 - no errors, from 1 to 99 common error codes
            RSTABDATA = 100,    // from 100 to 999 RSTAB application errors
            RSTABSTRC,
            RSTABMTRL,
            RSTABCRSC,
            RSTABNODE,
            RSTABSUPP,
            RSTABMMBR,
            RSTABSLCN,
            RSTABNDCN,
            RSTABCRCN,
            RSTABMBCN,
            RHINONODE,
            RHINOLINE,
            MARSH = 1000,   // from 1000 to 1099 marshall errors 
            MARSH_COM = 1099
        };

        private static Dictionary<ErrCode, string> ErrMesg = new Dictionary<ErrCode, string>()
        {
            {ErrCode.NONE, "No errors"},
            {ErrCode.RSTABDATA, "RSTAB IrsStructuralData initialization error"},
            {ErrCode.RSTABSTRC, "RSTAB IrsStructure initialization error"},
            {ErrCode.RSTABMTRL, "RSTAB material creation error"},
            {ErrCode.RSTABCRSC, "RSTAB cross section creation/getting error"},
            {ErrCode.RSTABNODE, "RSTAB RS_NODE creation/getting error"},
            {ErrCode.RSTABSUPP, "RSTAB RS_NODE_SUPPORT creation error"},
            {ErrCode.RSTABMMBR, "RSTAB RS_MEMBER creation/getting error"},
            {ErrCode.RSTABSLCN, "RSTAB selection count getting error"},
            {ErrCode.RSTABNDCN, "RSTAB node count getting error"},
            {ErrCode.RSTABCRCN, "RSTAB cross section count getting error"},
            {ErrCode.RSTABMBCN, "RSTAB member count getting error"},
            {ErrCode.RHINONODE, "Rhino point creation error"},
            {ErrCode.RHINOLINE, "Rhino line creation error"},
            {ErrCode.MARSH    , "System.Marshall error"},
            {ErrCode.MARSH_COM, "System.Marshal COM error"}
        };
        #endregion

        #region Members
        /// <summary>
        /// Object for convertion
        /// </summary>
        private RRObject obj;
        /// <summary>
        /// Error message
        /// </summary>
        private string ems;
        /// <summary>
        /// Error code
        /// </summary>
        private ErrCode ecd;
        /// <summary>
        /// Error source in code
        /// </summary>
        private string epl;
        /// <summary>
        /// /RSTAB structure
        /// </summary>
        private IrsStructure IStructure = null;
        /// <summary>
        /// RSTAB data interface
        /// </summary>
        private IrsStructuralData IData = null;
        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="a">Rhino object</param>
        public RRConverter(RRObject a)
        {
            this.obj = a;
            SetErr();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RRConverter()
        {
            this.obj = new RRObject();
            SetErr();
        }

        private void SetErr()
        {
            this.ems = "";
            this.epl = "";
            this.ecd = ErrCode.NONE;
        }

        /// <summary>
        /// Returns error message
        /// </summary>
        public string ErrorMessage
        {
            get { return epl + " " + ErrMesg[ecd] + ": " + ems; }
        }

        /// <summary>
        /// Returns error code. 0 if no errors occured
        /// </summary>
        public int ErrorCode
        {
            get { return (int)ecd; }
        }

        /// <summary>
        /// Returns RRObject from converter
        /// </summary>
        public RRObject RRObject
        {
            get { return this.obj; }
        }

        /// <summary>
        /// Converter from Rhino object into RSTAB
        /// </summary>
        /// <param name="ap">List of anchor points</param>
        /// <param name="tol">Minimal distance to distant point</param>
        /// <param name="dd">Flag for deletion RSTAB data structure</param>
        public void Rhino2RSTAB(List<Point3d> ap, double tol, bool dd)
        {
            PrepRSTABApp(dd, false);
            if (ecd != ErrCode.NONE) goto l99;

            SetRSTABMaterial();
            if (ecd != ErrCode.NONE) goto l99;

            SetRSTABCrossSection();
            if (ecd != ErrCode.NONE) goto l99;

            foreach (RRNode rn in this.obj.Nodes)
            {
                SetRSTABNode(rn);
                if (ecd != ErrCode.NONE) goto l99;
            }

            int i = 1, j = 1;
            if (ap != null)
            {
                foreach (RRNode rn in this.obj.Nodes)
                {
                    SetRSTABSupport(rn, ref i, ap, tol);
                    if (ecd != ErrCode.NONE) goto l99;
                }
            }

            foreach (RREdge re in this.obj.Edges)
            {
                SetRSTABMember(re, ref j);
                if (ecd != ErrCode.NONE) goto l99;
            }

            SetRSTABModelUnit();

        l99: FreeRSTAB(true);
        }

        /// <summary>
        /// Converter from RSTAB into Rhino
        /// </summary>
        /// <param name="a">If true then convert only selected things</param>
        /// <param name="b">If true then also convert dummy objects</param>
        public void RSTAB2Rhino(bool a, bool b)
        {
            PrepRSTABApp(false, true);
            if (ecd != ErrCode.NONE) goto l99;

            GetRSTABSelection(a);
            if (ecd != ErrCode.NONE) goto l99;

            int nc = GetRSTABNodeCount();
            if (ecd != ErrCode.NONE) goto l99;
            if (nc <= 0)
            {
                ecd = ErrCode.RSTABNDCN;
                ems = "Incorrect node count: " + nc;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
                goto l99;
            }

            GetRSTABNode(nc);
            if (ecd != ErrCode.NONE) goto l99;

            GetRSTABCrossSection();
            if (ecd != ErrCode.NONE) goto l99;

            GetRSTABMember(b);
            if (ecd != ErrCode.NONE) goto l99;

        l99: FreeRSTAB(false);
        }

        /// <summary>
        /// Preparing RSTAB application
        /// </summary>
        /// <param name="dd">Flag for deletion RSTAB data structure</param>
        /// <param name="rd">Reading flag</param>
        private void PrepRSTABApp(bool dd, bool rd)
        {
            try
            {
                IStructure = Marshal.GetActiveObject("RSTAB6.Structure") as IrsStructure;
            }
            catch (COMException)
            {
                try
                {
                    IStructure = Marshal.GetActiveObject("RSTAB6DEMO.Structure") as IrsStructure;
                }
                catch (COMException ce)
                {
                    ecd = ErrCode.MARSH_COM;
                    ems = ce.Message;
                    epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name + "()";
                }
                catch (Exception e)
                {
                    ecd = ErrCode.MARSH;
                    ems = e.Message;
                    epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
                }
            }
            catch (Exception e)
            {
                ecd = ErrCode.MARSH;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
            if (ecd != ErrCode.NONE) return;

            try
            {
                IStructure.rsGetApplication().rsLockLicence();
                IData = IStructure.rsGetStructuralData();
                if (!rd)
                {
                    IData.rsPrepareModification();
                    if (dd) IData.rsDeleteStructuralData();
                }
            }
            catch (Exception e)
            {
                ecd = ErrCode.MARSH;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }

        /// <summary>
        /// Frees RSTAB COM object
        /// </summary>
        /// <param name="a">True if data was modified</param>
        private void FreeRSTAB(bool a)
        {
            if (IData != null)
            {
                try
                {
                    if (a) IData.rsFinishModification();
                }
                catch (Exception e)
                {
                    ecd = ErrCode.RSTABDATA;
                    ems = e.Message;
                    epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
                }
                finally
                {
                    IData = null;
                }
            }

            if (IStructure != null)
            {
                try
                {
                    IStructure.rsGetApplication().rsUnlockLicence();

                    // Releases COM object
                    Marshal.ReleaseComObject(IStructure);
                }
                catch (Exception e)
                {
                    ecd = ErrCode.RSTABSTRC;
                    ems = e.Message;
                    epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
                }
                finally
                {
                    IStructure = null;
                }
            }

            // Cleans Garbage Collector for releasing all COM interfaces and objects.
            System.GC.Collect();
        }

        /// <summary>
        /// Sets RSTAB materials
        /// </summary>
        private void SetRSTABMaterial()
        {
            try
            {
                if (this.obj.Mtrls == null)
                {
                    IData.rsSetMaterial(new RS_MATERIAL { iNo = 1, strDescription = "Steel St 37" });
                }
                else
                {
                    if (this.obj.Mtrls.Count == 0)
                    {
                        IData.rsSetMaterial(new RS_MATERIAL { iNo = 1, strDescription = "Steel St 37" });
                    }
                    else
                    {
                        foreach (RRMtrl mt in this.obj.Mtrls)
                        {
                            IData.rsSetMaterial(new RS_MATERIAL { iNo = mt.ID, strDescription = mt.Description });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABMTRL;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }

        /// <summary>
        /// Sets RSTAB cross sections
        /// </summary>
        private void SetRSTABCrossSection()
        {
            try
            {
                if (this.obj.CrCss == null)
                {
                    IData.rsSetCrossSection(new RS_CROSS_SECTION { iNo = 1, iMaterialNo = 1, strDescription = "IPE 100" });
                }
                else
                {
                    if (this.obj.CrCss.Count == 0)
                    {
                        IData.rsSetCrossSection(new RS_CROSS_SECTION { iNo = 1, iMaterialNo = 1, strDescription = "IPE 100" });
                    }
                    else
                    {
                        foreach (RRCrSc cs in this.obj.CrCss)
                        {
                            IData.rsSetCrossSection(new RS_CROSS_SECTION { iNo = cs.ID, iMaterialNo = cs.Material.ID, strDescription = cs.Description });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABCRSC;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }

        /// <summary>
        /// Sets model unit in RSTAB
        /// </summary>
        private void SetRSTABModelUnit()
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Sets node in RSTAB
        /// </summary>
        /// <param name="a">Node to create in RSTAB</param>
        private void SetRSTABNode(RRNode a)
        {
            try
            {
                IData.rsSetNode(a.GetRSNode());
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABNODE;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }

        /// <summary>
        /// Sets supports in RSTAB
        /// </summary>
        /// <param name="a">Node for supporting</param>
        /// <param name="i">Support ID</param>
        /// <param name="b">Anchor points list</param>
        /// <param name="c">Tolerance</param>
        private void SetRSTABSupport(RRNode a, ref int i, List<Point3d> b, double c)
        {
            Point3d k = a.GetPoint3d();
            foreach (Point3d p in b)
            {
                try
                {
                    if (p.DistanceTo(k) <= c)
                    {
                        IData.rsSetNodeSupport(new RS_NODE_SUPPORT
                        {
                            ID = (i).ToString(),
                            iNo = i++,
                            rotationSequence = 0,
                            fuX = -1,
                            fuY = -1,
                            fuZ = -1,
                            fPhiX = 0,
                            fPhiY = 0,
                            fPhiZ = -1,
                            strNodeList = (a.ID).ToString()
                        });

                        break;
                    }
                }
                catch (Exception e)
                {
                    ecd = ErrCode.RSTABSUPP;
                    ems = e.Message;
                    epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
                }
                if (ecd != ErrCode.NONE) return;
            }
        }

        /// <summary>
        /// Sets member in RSTAB
        /// </summary>
        /// <param name="a">Edge to create in RSTAB</param>
        /// <param name="i">Edge ID</param>
        private void SetRSTABMember(RREdge a, ref int i)
        {
            try
            {
                IData.rsSetMember(a.GetRSMember(i++));
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABMMBR;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }

        /// <summary>
        /// Sets selection in RSTAB
        /// </summary>
        /// <param name="a">Selection flag</param>
        private void GetRSTABSelection(bool a)
        {
            try
            {
                IData.rsEnableSelections(a);
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABSLCN;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }

        /// <summary>
        /// Gets nodes from RSTAB
        /// </summary>
        /// <param name="a">Node count to read</param>
        private void GetRSTABNode(int a)
        {
            try
            {
                RS_NODE[] ns = new RS_NODE[a];
                IData.rsGetNodeArr(a, ns);

                foreach (RS_NODE n in ns)
                {
                    this.obj.AddNode(new RRNode(n));
                }
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABNODE;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }

        /// <summary>
        /// Returns node count in RSTAB
        /// </summary>
        /// <returns>Node count</returns>
        private int GetRSTABNodeCount()
        {
            int a = 0;
            try
            {
                a = IData.rsGetNodeCount();
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABNDCN;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }

            return a;
        }

        /// <summary>
        /// Returns RSTAB Cross sections
        /// </summary>
        private void GetRSTABCrossSection()
        {
            int cc = 0;
            try
            {
                cc = IData.rsGetCrossSectionCount();
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABCRCN;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
                return;
            }

            try
            {
                RS_CROSS_SECTION[] cs = new RS_CROSS_SECTION[cc];
                IData.rsGetCrossSectionArr(cc, cs);

                foreach (RS_CROSS_SECTION c in cs)
                {
                    this.obj.AddCrCs(new RRCrSc(c));
                }
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABCRSC;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }

        /// <summary>
        /// Gets members from RSTAB
        /// </summary>
        /// <param name="a">If true the read dummy members</param>
        private void GetRSTABMember(bool a)
        {
            int mc = 0;

            try
            {
                mc = IData.rsGetMemberCount();
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABMBCN;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
                return;
            }

            try
            {
                RS_MEMBER[] ms = new RS_MEMBER[mc];
                IData.rsGetMemberArr(mc, ms);

                foreach (RS_MEMBER m in ms)
                {
                    RRNode sn, en;
                    sn = new RRNode(IData.rsGetNode(m.iStartNodeNo, ITEM_AT.AT_NO).rsGetData());
                    en = new RRNode(IData.rsGetNode(m.iEndNodeNo, ITEM_AT.AT_NO).rsGetData());
                    RREdge ed = new RREdge
                    {
                        ID = m.iNo,
                        StartNode = sn,
                        EndNode = en,
                        CrossSection = this.obj.GetCrCsByNo(m.iStartCrossSectionNo)
                    };

                    //zero node
                    if (a)
                    {
                        if (m.type.GetHashCode() != 7) this.obj.AddEdge(ed, false);
                    }
                    else
                    {
                        this.obj.AddEdge(ed, false);
                    }
                }
            }
            catch (Exception e)
            {
                ecd = ErrCode.RSTABMMBR;
                ems = e.Message;
                epl = this.GetType().Name + "->" + MethodBase.GetCurrentMethod().Name;
            }
        }
    }
}
