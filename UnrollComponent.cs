using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Unroll
{
    public class UnrollComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public UnrollComponent()
          : base("Unroll", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "R", "reset", GH_ParamAccess.item , false);
            pManager.AddMeshParameter("Mesh", "mesh", "input mesh to find topology", GH_ParamAccess.item);
            pManager.AddCurveParameter("Polyline", "Pl", "Unrolled polyline", GH_ParamAccess.list);
            pManager.AddNumberParameter("gap ", "gap", "gap", GH_ParamAccess.item, 0.2);
            
        }
        

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "Pl", "outputPolyline", GH_ParamAccess.list);
            pManager.AddNumberParameter("average", "avearge", "average ", GH_ParamAccess.item);
            pManager.AddVectorParameter("vector3d", "vector3d", "vector 3d", GH_ParamAccess.tree);
            pManager.AddVectorParameter("helpers", "vector3d", "vector 3d", GH_ParamAccess.tree);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool reset = false;
            DA.GetData(0,ref reset);

            Mesh xx = new Mesh();
            DA.GetData(1, ref xx);

            List<Curve> crvs = new List<Curve>();
            DA.GetDataList(2, crvs);
            List<Polyline> pls = new List<Polyline>();
            for (int i = 0; i< crvs.Count; i++) {

                Polyline testPoly;
                crvs[i].TryGetPolyline(out testPoly);
                pls.Add(testPoly);
            
            }

            double gap = .3;
            DA.GetData(3,ref gap);
            //________________________________________________________________________________________


            if (reset)
            {
                

                faces = new List<List<int>>();
                indices = new List<List<int>>();

                duals = new List<List<int>>();


                edgeCount = neighbours( xx, ref faces, ref indices, ref duals);
                 e = pls;
            }
            //_____________________________________________init moving list

            vv = new List<List<Vector3d>>();
            cc = new List<List<Vector3d>>();
            for (int i = 0; i < pls.Count; i++)
            {
                vv.Add(new List<Vector3d>());
                cc.Add(new List<Vector3d>());
            }
            //______________________________________________________________________________________________
            //fiding the average: 
            double aveDist = 0.2;// average(e);
            //Main loop:

            for (int i = 0; i < edgeCount; i++)
            {
                int index01 = duals[i][0];
                int index02 = duals[i][1];

                //find coreespodent edge for index01;

                Polyline a = e[index01];
                Polyline b = e[index02];
                //
                int edgeIndex01a = faces[index01].IndexOf(index02);
                int edgeIndex02b = faces[index02].IndexOf(index01);

                int edgeindex = indices[index01][edgeIndex01a];
                int edgeindex02 = indices[index02][edgeIndex02b];

                Line aa = a.SegmentAt(edgeindex);
                Line bb = b.SegmentAt(edgeindex02);

                Point3d pointa = aa.PointAt(0.5);
                Point3d pointb = bb.PointAt(0.5);
                Point3d pointc = bb.ClosestPoint(pointa, false);
                //Line ln = new Line(pointa, pointb);
               

                Vector3d direction = (pointa - pointb);
                double nL = 0.5 * (direction.Length - aveDist);

                //direction.Unitize();
                direction *= 0.25;

                double cDistace = pointc.DistanceTo(pointa);
                Vector3d helper = new Vector3d(0, 0, 0);
                
                if (cDistace < gap)
                {
                    helper =  pointa - pointc;
                    helper.Unitize();
                    helper *= cDistace - gap;
                }


                vv[index01].Add(-direction );
                vv[index02].Add(direction  );

                cc[index01].Add(-helper);
                cc[index02].Add(helper);



            }


            //__________________________________________________________
            //moving the panels 
            for (int i = 0; i < vv.Count; i++)
            {
                Vector3d sumV = new Vector3d(0, 0, 0);
                for (int j = 0; j < vv[i].Count; j++)
                {
                    sumV += (vv[i][j] + cc[i][j]);

                }

                Vector3d sumD = (sumV / vv[i].Count);


                Transform ll = Transform.Translation(sumD);
                e[i].Transform(ll);
            }


            DA.SetDataList(0, e);
            DA.SetData(1, edgeCount);
            DA.SetDataTree(2, ListOfListsToTree(vv));
            DA.SetDataTree(3, ListOfListsToTree(cc));

        }

        //Defining golbal variables
        //___________________________________________________

        List<Polyline> e;
        int edgeCount = 0;


        List<List<int>> faces;
        List<List<int>> indices;
        List<List<int>> duals;

        List<List<Vector3d>> vv;
        List<List<Vector3d>> cc;
        //___________________________________________________


        //__________________________
        double average(List<Polyline> e)
        {
            double aveDist = 0;
            double sumDist = 0;
            for (int i = 0; i < edgeCount; i++)
            {
                int index01 = duals[i][0];
                int index02 = duals[i][1];

                //find coreespodent edge for index01;

                Polyline a = e[index01];
                Polyline b = e[index02];
                //
                int edgeIndex01a = faces[index01].IndexOf(index02);
                int edgeIndex02b = faces[index02].IndexOf(index01);

                int edgeindex = indices[index01][edgeIndex01a];
                int edgeindex02 = indices[index02][edgeIndex02b];

                Line aa = a.SegmentAt(edgeindex);
                Line bb = b.SegmentAt(edgeindex02);

                Point3d pointa = aa.PointAt(0.5);
                Point3d pointb = bb.PointAt(0.5);

                double testDist = pointa.DistanceTo(pointb);

                sumDist += testDist;




            }
            return aveDist = sumDist / edgeCount;
        }
//duals is an output list for each line which refer to neighbour faces
//edgeFace 

int neighbours( Mesh x, ref List<List<int>> facess, ref List<List<int>> edgeFace, ref List<List<int>> duals)
        {
            for (int i = 0; i < x.Faces.Count; i++)
            {
                facess.Add(new List<int>());
                edgeFace.Add(new List<int>());
               

            }
            int edgeCounter = 0;
            int edgeCount = x.TopologyEdges.Count;
            for (int i = 0; i < edgeCount; i++)
            {
                int[] test = x.TopologyEdges.GetConnectedFaces(i);
                if (test.Length == 2)
                {
                    edgeCounter++;
                    List<int> dd = new List<int>() { test[0], test[1] };
                    duals.Add(dd);
                    facess[test[0]].Add(test[1]);
                    facess[test[1]].Add(test[0]);
                    //find the edge id for test[0]
                    Line[] lines = FaceEdges(x, test[0]);
                    Line[] lines2 = FaceEdges(x, test[1]);
                    for (int k = 0; k < 3; k++)
                    {


                        Line a = lines[k];
                        Point3d aa = a.PointAt(0.5);

                        
                        for (int q = 0; q < 3; q++)
                        {
                            Line b = lines2[q];
                            Point3d bb = b.PointAt(0.5);
                            if (aa.DistanceTo(bb) < 0.01)
                            {
                                edgeFace[test[0]].Add(k);

                                edgeFace[test[1]].Add(q);
                         

                            }
                        }



                    }


                }


            }
            return edgeCounter;
        }




        public DataTree<Vector3d> ListOfListsToTree(List<List<Vector3d>> list)
        {



            DataTree<Vector3d> tree = new DataTree<Vector3d>();


            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list[i].Count; j++)

                    tree.Add(list[i][j], new GH_Path(i));
            }


            return tree;
        }

        Line[] FaceEdges(Mesh x ,int indexx )
        {
            Line[] lines = new Line[3];

            lines[0] = new Line(x.Vertices[x.Faces[indexx].A], x.Vertices[x.Faces[indexx].B]);
            lines[1] = new Line(x.Vertices[x.Faces[indexx].B], x.Vertices[x.Faces[indexx].C]);
            lines[2] = new Line(x.Vertices[x.Faces[indexx].C], x.Vertices[x.Faces[indexx].A]);
            return lines;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("20c55809-c123-4148-9342-e43b972d063a"); }
        }
    }
}
