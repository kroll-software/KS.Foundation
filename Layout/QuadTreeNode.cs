using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

// found in this project under CBOL license
// http://www.codeproject.com/Articles/224231/Word-Cloud-Tag-Cloud-Generator-Control-for-NET-Win
// Thank you for sharing this great work!

namespace KS.Foundation
{
    public class QuadTreeNode
    {
		private readonly Stack<ILayoutItem> m_Contents = new Stack<ILayoutItem>();

        private QuadTreeNode[] m_Nodes = new QuadTreeNode[0];

        public QuadTreeNode(RectangleF bounds)
        {
            Bounds = bounds;
        }

        public bool IsEmpty
        {
            get { return Bounds.IsEmpty || m_Nodes.Length == 0; }
        }

		public RectangleF Bounds { get; set; }
        
        public int Count
        {
            get
            {
                int count = 0;

                foreach (QuadTreeNode node in m_Nodes)
                    count += node.Count;

                count += this.Contents.Count;

                return count;
            }
        }

		public IEnumerable<ILayoutItem> SubTreeContents
        {
            get
            {
				IEnumerable<ILayoutItem> results = new ILayoutItem[0];

                foreach (QuadTreeNode node in m_Nodes)
                    results = results.Concat(node.SubTreeContents);

                results = results.Concat(this.Contents);
                return results;
            }
        }

		public Stack<ILayoutItem> Contents
        {
            get { return m_Contents; }
        }


        public bool HasContent(RectangleF queryArea)
        {
			IEnumerable<ILayoutItem> queryResult = Query(queryArea);
            return IsEmptyEnumerable(queryResult);
        }

		private static bool IsEmptyEnumerable(IEnumerable<ILayoutItem> queryResult)
        {
            using (var enumerator = queryResult.GetEnumerator())
            {
                return enumerator.MoveNext();
            }
        }

        /// <summary>
        ///   Query the QuadTree for items that are in the given area
        /// </summary>
        /// <param name = "queryArea">
        ///   <returns></returns></param>
		public IEnumerable<ILayoutItem> Query(RectangleF queryArea)
        {
            // this quad contains items that are not entirely contained by
            // it's four sub-quads. Iterate through the items in this quad 
            // to see if they intersect.
			foreach (ILayoutItem item in this.Contents)
            {
                if (queryArea.IntersectsWith(item.Bounds))
                    yield return item;
            }

            foreach (QuadTreeNode node in m_Nodes)
            {
                if (node.IsEmpty)
                    continue;

                // Case 1: search area completely contained by sub-quad
                // if a node completely contains the query area, go down that branch
                // and skip the remaining nodes (break this loop)
                if (node.Bounds.Contains(queryArea))
                {
					IEnumerable<ILayoutItem> subResults = node.Query(queryArea);
                    foreach (var subResult in subResults)
                    {
                        yield return subResult;
                    }
                    break;
                }

                // Case 2: Sub-quad completely contained by search area 
                // if the query area completely contains a sub-quad,
                // just add all the contents of that quad and it's children 
                // to the result set. You need to continue the loop to test 
                // the other quads
                if (queryArea.Contains(node.Bounds))
                {
					IEnumerable<ILayoutItem> subResults = node.SubTreeContents;
                    foreach (var subResult in subResults)
                    {
                        yield return subResult;
                    }
                    continue;
                }

                // Case 3: search area intersects with sub-quad
                // traverse into this quad, continue the loop to search other
                // quads
                if (node.Bounds.IntersectsWith(queryArea))
                {
					IEnumerable<ILayoutItem> subResults = node.Query(queryArea);
                    foreach (var subResult in subResults)
                    {
                        yield return subResult;
                    }
                }
            }
			yield break;
        }


		public void Insert(ILayoutItem item)
        {
            // if the item is not contained in this quad, there's a problem            
            
            // *** This is an important bug-fix by kroll            
            if (!Bounds.IntersectsWith(item.Bounds))
            //if (!m_Bounds.Contains(item.Bounds))
            {                
                //Trace.TraceWarning("feature is out of the bounds of this quadtree node");
                return;
            }

            // if the subnodes are null create them. may not be sucessfull: see below
            // we may be at the smallest allowed size in which case the subnodes will not be created
            if (m_Nodes.Length == 0)
                CreateSubNodes();

            // for each subnode:
            // if the node contains the item, add the item to that node and return
            // this recurses into the node that is just large enough to fit this item
            foreach (QuadTreeNode node in m_Nodes)
            {
                if (node.Bounds.Contains(item.Bounds))
                {
                    node.Insert(item);
                    return;
                }
            }

            // if we make it to here, either
            // 1) none of the subnodes completely contained the item. or
            // 2) we're at the smallest subnode size allowed 
            // add the item to this node's contents.
            this.Contents.Push(item);
        }

        public void ForEach(QuadTree.QuadTreeAction action)
        {
            action(this);

            // draw the child quads
            foreach (QuadTreeNode node in this.m_Nodes)
                node.ForEach(action);
        }

        private void CreateSubNodes()
        {
            // the smallest subnode has an area 
            if ((Bounds.Height * Bounds.Width) <= 10)
                return;

            float halfWidth = (Bounds.Width / 2f);
            float halfHeight = (Bounds.Height / 2f);

            m_Nodes = new QuadTreeNode[4];
            m_Nodes[0] = (new QuadTreeNode(new RectangleF(Bounds.Location, new SizeF(halfWidth, halfHeight))));
            m_Nodes[1] = (new QuadTreeNode(new RectangleF(new PointF(Bounds.Left, Bounds.Top + halfHeight), new SizeF(halfWidth, halfHeight))));
            m_Nodes[2] = (new QuadTreeNode(new RectangleF(new PointF(Bounds.Left + halfWidth, Bounds.Top), new SizeF(halfWidth, halfHeight))));
            m_Nodes[3] = (new QuadTreeNode(new RectangleF(new PointF(Bounds.Left + halfWidth, Bounds.Top + halfHeight), new SizeF(halfWidth, halfHeight))));
        }
    }
}