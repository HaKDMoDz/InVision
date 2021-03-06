﻿/*
 * C# / XNA  port of Bullet (c) 2011 Mark Neale <xexuxjy@hotmail.com>
 *
 * Bullet Continuous Collision Detection and Physics Library
 * Copyright (c) 2003-2008 Erwin Coumans  http://www.bulletphysics.com/
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose, 
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using InVision.Bullet.Collision.BroadphaseCollision;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Collision.CollisionShapes
{
    public class OptimizedBvh : QuantizedBvh
    {
        public OptimizedBvh()
        {
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }

	    public void	Build(StridingMeshInterface triangles,bool useQuantizedAabbCompression, ref Vector3 bvhAabbMin, ref Vector3 bvhAabbMax)
        {
	        int numLeafNodes = 0;
            m_useQuantization = useQuantizedAabbCompression;

        	
	        if (m_useQuantization)
	        {

		        //initialize quantization values
		        SetQuantizationValues(ref bvhAabbMin,ref bvhAabbMax);

		        QuantizedNodeTriangleCallback callback = new QuantizedNodeTriangleCallback(m_quantizedLeafNodes,this);
        	
		        triangles.InternalProcessAllTriangles(callback,ref m_bvhAabbMin,ref m_bvhAabbMax);

		        //now we have an array of leafnodes in m_leafNodes
		        numLeafNodes = m_quantizedLeafNodes.Count;

                //m_quantizedContiguousNodes.resize(2*numLeafNodes);
                m_quantizedContiguousNodes.Capacity = 2 * numLeafNodes;
	        } 
            else
	        {
		        NodeTriangleCallback callback = new NodeTriangleCallback(m_leafNodes);

		        Vector3 aabbMin = MathUtil.MIN_VECTOR;
		        Vector3 aabbMax = MathUtil.MAX_VECTOR;

		        triangles.InternalProcessAllTriangles(callback,ref aabbMin,ref aabbMax);

		        //now we have an array of leafnodes in m_leafNodes
		        numLeafNodes = m_leafNodes.Count;

                //m_contiguousNodes.resize(2*numLeafNodes);
                m_contiguousNodes.Capacity = 2 * numLeafNodes;
	        }

	        m_curNodeIndex = 0;

	        BuildTree(0,numLeafNodes);

            for (int i = 0; i < m_quantizedContiguousNodes.Count; ++i)
            {
                QuantizedBvhNode bvhn = m_quantizedContiguousNodes[i];
            }


	        ///if the entire tree is small then subtree size, we need to create a header info for the tree
	        if(m_useQuantization && m_SubtreeHeaders.Count == 0)
	        {

                BvhSubtreeInfo subtree = new BvhSubtreeInfo();
                m_SubtreeHeaders.Add(subtree);

		        subtree.SetAabbFromQuantizeNode(m_quantizedContiguousNodes[0]);
		        subtree.m_rootNodeIndex = 0;
		        subtree.m_subtreeSize = m_quantizedContiguousNodes[0].IsLeafNode() ? 1 : m_quantizedContiguousNodes[0].GetEscapeIndex();
	        }

	        //PCK: update the copy of the size
	        m_subtreeHeaderCount = m_SubtreeHeaders.Count;

	        //PCK: clear m_quantizedLeafNodes and m_leafNodes, they are temporary
	        m_quantizedLeafNodes.Clear();
	        m_leafNodes.Clear();
        }

        public void Refit(StridingMeshInterface meshInterface, ref Vector3 aabbMin, ref Vector3 aabbMax)
        {
            if (m_useQuantization)
            {

                SetQuantizationValues(ref aabbMin, ref aabbMax);

                UpdateBvhNodes(meshInterface, 0, m_curNodeIndex, 0);

                ///now update all subtree headers

                for (int i = 0; i < m_SubtreeHeaders.Count; i++)
                {
                    BvhSubtreeInfo subtree = m_SubtreeHeaders[i];
                    subtree.SetAabbFromQuantizeNode(m_quantizedContiguousNodes[subtree.m_rootNodeIndex]);
                }
            }
            else
            {

            }
        }

	    public void	RefitPartial(StridingMeshInterface meshInterface,ref Vector3 aabbMin, ref Vector3 aabbMax)
        {
	        //incrementally initialize quantization values
            if (!m_useQuantization)
            {
                return;
            }
	        Debug.Assert(m_useQuantization);

	        Debug.Assert(aabbMin.X > m_bvhAabbMin.X);
	        Debug.Assert(aabbMin.Y > m_bvhAabbMin.Y);
	        Debug.Assert(aabbMin.Z > m_bvhAabbMin.Z);

	        Debug.Assert(aabbMax.X < m_bvhAabbMax.X);
	        Debug.Assert(aabbMax.Y < m_bvhAabbMax.Y);
	        Debug.Assert(aabbMax.Z < m_bvhAabbMax.Z);

	        ///we should update all quantization values, using updateBvhNodes(meshInterface);
	        ///but we only update chunks that overlap the given aabb
        	
	        UShortVector3 quantizedQueryAabbMin = new UShortVector3();
            UShortVector3 quantizedQueryAabbMax = new UShortVector3();

	        Quantize(ref quantizedQueryAabbMin,ref aabbMin,false);
	        Quantize(ref quantizedQueryAabbMax,ref aabbMax,true);

	        for (int i=0;i<m_SubtreeHeaders.Count;i++)
	        {
		        BvhSubtreeInfo subtree = m_SubtreeHeaders[i];

		        //PCK: unsigned instead of bool
		        bool overlap = AabbUtil2.TestQuantizedAabbAgainstQuantizedAabb(ref quantizedQueryAabbMin,ref quantizedQueryAabbMax,ref subtree.m_quantizedAabbMin,ref subtree.m_quantizedAabbMax);
		        if (overlap)
		        {
			        UpdateBvhNodes(meshInterface,subtree.m_rootNodeIndex,subtree.m_rootNodeIndex+subtree.m_subtreeSize,i);
			        subtree.SetAabbFromQuantizeNode(m_quantizedContiguousNodes[subtree.m_rootNodeIndex]);
		        }
	        }
        }

        public void UpdateBvhNodes(StridingMeshInterface meshInterface, int firstNode, int endNode, int index)
        {
            //(void)index;

	            Debug.Assert(m_useQuantization);

	        int curNodeSubPart=-1;

	        //get access info to trianglemesh data
		    Object vertexBaseObject = null;
		    int numverts = 0;
		    PHY_ScalarType type = PHY_ScalarType.PHY_INTEGER;
		    int stride = 0;
            Object indexBaseObject = null;
		    int indexstride = 0;
		    int numfaces = 0;
		    PHY_ScalarType indicestype = PHY_ScalarType.PHY_INTEGER;

		    Vector3[]	triangleVerts = new Vector3[3];
		    Vector3	aabbMin,aabbMax;
		    Vector3 meshScaling = meshInterface.GetScaling();
    		

		    for (int i=endNode-1;i>=firstNode;i--)
		    {
			    QuantizedBvhNode curNode = m_quantizedContiguousNodes[i];
			    if (curNode.IsLeafNode())
			    {
				    //recalc aabb from triangle data
				    int nodeSubPart = curNode.GetPartId();
				    int nodeTriangleIndex = curNode.GetTriangleIndex();
				    if (nodeSubPart != curNodeSubPart)
				    {
					    if (curNodeSubPart >= 0)
                        {
						    meshInterface.UnLockReadOnlyVertexBase(curNodeSubPart);
                        }
					    meshInterface.getLockedReadOnlyVertexIndexBase(out vertexBaseObject,out numverts,out type,out stride,out indexBaseObject,out indexstride,out numfaces,out indicestype,nodeSubPart);

					    curNodeSubPart = nodeSubPart;
					    Debug.Assert(indicestype==PHY_ScalarType.PHY_INTEGER||indicestype==PHY_ScalarType.PHY_SHORT);
				    }
				    //triangles.getLockedReadOnlyVertexIndexBase(vertexBase,numVerts,
                    int gfxBaseIndex = nodeTriangleIndex*indexstride;
                    //unsigned int* gfxbase = (unsigned int*)(indexbase+nodeTriangleIndex*indexstride);
                    IList<int> indexBase = (IList<int>)indexBaseObject;

                    if (vertexBaseObject is IList<Vector3>)
                    {
                        IList<Vector3> vertexBase = (IList<Vector3>)vertexBaseObject;
                        for (int j = 2; j >= 0; j--)
                        {
                            int graphicsIndex = indexBase[gfxBaseIndex+j];
                            if (type == PHY_ScalarType.PHY_FLOAT)
                            {

                                triangleVerts[j] = vertexBase[graphicsIndex] * meshScaling;
                            }
                            else
                            {
                                Debug.Assert(false, "Unsupported Type");
                            }
                        }
                    }
                    else if (vertexBaseObject is IList<float>)
                    {
                        IList<float> vertexBase = (IList<float>)vertexBaseObject;
                        for (int j = 2; j >= 0; j--)
                        {
                            //int graphicsindex = indicestype==PHY_ScalarType.PHY_SHORT?((unsigned short*)gfxbase)[j]:gfxbase[j];
                            int graphicsIndex = indexBase[gfxBaseIndex+j];
                            if (type == PHY_ScalarType.PHY_FLOAT)
                            {
                                int graphicsBaseIndex = (graphicsIndex * stride);
                                //IList<float> graphicsbase = (float*)(vertexbase+graphicsindex*stride);
                                triangleVerts[j] = new Vector3(
                                    vertexBase[graphicsBaseIndex] * meshScaling.X,
                                    vertexBase[graphicsBaseIndex + 1] * meshScaling.Y,
                                    vertexBase[graphicsBaseIndex + 2] * meshScaling.Z);
                            }
                            else
                            {
                                Debug.Assert(false, "Unsupported Type");
                            }
                        }


                    }
                    else
                    {
                        Debug.Assert(false, "Unsupported Type");
                    }
    				
				    aabbMin = MathUtil.MAX_VECTOR;
				    aabbMax = MathUtil.MIN_VECTOR; 
				    MathUtil.VectorMin(ref triangleVerts[0],ref aabbMin);
				    MathUtil.VectorMax(ref triangleVerts[0],ref aabbMax);
				    MathUtil.VectorMin(ref triangleVerts[1],ref aabbMin);
				    MathUtil.VectorMax(ref triangleVerts[1],ref aabbMax);
				    MathUtil.VectorMin(ref triangleVerts[2],ref aabbMin);
				    MathUtil.VectorMax(ref triangleVerts[2],ref aabbMax);

				    Quantize(ref curNode.m_quantizedAabbMin,ref aabbMin,false);
				    Quantize(ref curNode.m_quantizedAabbMax,ref aabbMax,true);
    				
			    } 
                else
			    {
				    //combine aabb from both children

				    QuantizedBvhNode leftChildNode = m_quantizedContiguousNodes[i+1];
    				
				    QuantizedBvhNode rightChildNode = leftChildNode.IsLeafNode() ? m_quantizedContiguousNodes[i+2] :
					    m_quantizedContiguousNodes[i+1+leftChildNode.GetEscapeIndex()];
    				

				    {
                        curNode.m_quantizedAabbMin = leftChildNode.m_quantizedAabbMin;
                        curNode.m_quantizedAabbMin.min(ref rightChildNode.m_quantizedAabbMin);

                        curNode.m_quantizedAabbMax = leftChildNode.m_quantizedAabbMax;
                        curNode.m_quantizedAabbMax.max(ref rightChildNode.m_quantizedAabbMax);
				    }
			    }

		    }

		    if (curNodeSubPart >= 0)
			    meshInterface.UnLockReadOnlyVertexBase(curNodeSubPart);




        }

    }
}
