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

using System.Diagnostics;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Collision.CollisionShapes
{
    public class TriangleMesh : TriangleIndexVertexArray
    {
        public TriangleMesh() : this(true,true)
        {
        }

		public TriangleMesh(bool use32bitIndices,bool use4componentVertices)
        {
            m_use32bitIndices = use32bitIndices;
            m_use4componentVertices = use4componentVertices;

	        IndexedMesh meshIndex = new IndexedMesh();
	        m_indexedMeshes.Add(meshIndex);

	        if (m_use32bitIndices)
	        {
		        m_indexedMeshes[0].m_numTriangles = m_32bitIndices.Count/3;
		        m_indexedMeshes[0].m_indexType = PHY_ScalarType.PHY_INTEGER;
		        m_indexedMeshes[0].m_triangleIndexStride = 3;
	        } 
            else
	        {
		        m_indexedMeshes[0].m_numTriangles = m_16bitIndices.Count/3;
		        m_indexedMeshes[0].m_triangleIndexBase = null;
                m_indexedMeshes[0].m_indexType = PHY_ScalarType.PHY_SHORT;
		        m_indexedMeshes[0].m_triangleIndexStride = 3;
	        }

	        if (m_use4componentVertices)
	        {
		        m_indexedMeshes[0].m_numVertices = m_4componentVertices.Count;
		        m_indexedMeshes[0].m_vertexStride = 1;
	        } 
            else
	        {
		        m_indexedMeshes[0].m_numVertices = m_3componentVertices.Count/3;
		        m_indexedMeshes[0].m_vertexStride = 3;
	        }

        }
        ///findOrAddVertex is an internal method, use addTriangle instead
		public int FindOrAddVertex(ref Vector3 vertex, bool removeDuplicateVertices)
        {
	        //return index of new/existing vertex
	        ///@todo: could use acceleration structure for this
	        if (m_use4componentVertices)
	        {
		        if (removeDuplicateVertices)
                {
			        for (int i=0;i< m_4componentVertices.Count;i++)
			        {
				        if ((m_4componentVertices[i]-vertex).LengthSquared() <= m_weldingThreshold)
				        {
					        return i;
				        }
			        }
		        }
		        m_indexedMeshes[0].m_numVertices++;
		        m_4componentVertices.Add(vertex);
		        m_indexedMeshes[0].m_vertexBase.Add(vertex);

		        return m_4componentVertices.Count-1;
	        } 
            else
	        {
		        if (removeDuplicateVertices)
		        {
			        for (int i=0;i< m_3componentVertices.Count;i+=3)
			        {
				        Vector3 vtx = new Vector3(m_3componentVertices[i],m_3componentVertices[i+1],m_3componentVertices[i+2]);
				        if ((vtx-vertex).LengthSquared() <= m_weldingThreshold)
				        {
					        return i/3;
				        }
			        }
	        }
		        m_3componentVertices.Add(vertex.X);
		        m_3componentVertices.Add(vertex.Y);
		        m_3componentVertices.Add(vertex.Z);
		        m_indexedMeshes[0].m_numVertices++;
                m_indexedMeshes[0].m_vertexBase.Add(vertex);
		        return (m_3componentVertices.Count/3)-1;
	        }

        }

        ///addIndex is an internal method, use addTriangle instead
		public void	AddIndex(int index)
        {
	        if (m_use32bitIndices)
	        {
		        m_32bitIndices.Add(index);
                m_indexedMeshes[0].m_triangleIndexBase = m_32bitIndices;
	        } else
	        {
		        m_16bitIndices.Add((uint)index);
                // not really supported yet.
                Debug.Assert(false);
                //m_indexedMeshes[0].m_triangleIndexBase = (unsigned char*) &m_16bitIndices[0];
	        }
        }

		public bool	GetUse32bitIndices()
		{
			return m_use32bitIndices;
		}

		public bool	GetUse4componentVertices()
		{
			return m_use4componentVertices;
		}

		///By default addTriangle won't search for duplicate vertices, because the search is very slow for large triangle meshes.
		///In general it is better to directly use btTriangleIndexVertexArray instead.

        public void AddTriangle(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2)
        {
            AddTriangle(ref vertex0, ref vertex1, ref vertex2);
        }

        public void	AddTriangle(ref Vector3 vertex0,ref Vector3 vertex1,ref Vector3 vertex2)
        {
            AddTriangle(ref vertex0,ref vertex1, ref vertex2,false);
        }
        public void	AddTriangle(ref Vector3 vertex0,ref Vector3 vertex1,ref Vector3 vertex2, bool removeDuplicateVertices)
        {
	        m_indexedMeshes[0].m_numTriangles++;
	        AddIndex(FindOrAddVertex(ref vertex0,removeDuplicateVertices));
	        AddIndex(FindOrAddVertex(ref vertex1,removeDuplicateVertices));
	        AddIndex(FindOrAddVertex(ref vertex2,removeDuplicateVertices));
        }
		
		public int GetNumTriangles()
        {
	        if (m_use32bitIndices)
	        {
		        return m_32bitIndices.Count / 3;
	        }
	        return m_16bitIndices.Count / 3;
        }

		public override void PreallocateVertices(int numverts)
        {
            //(void) numverts;
        }
        public override void PreallocateIndices(int numindices)
        {
            //(void) numindices;
        }

        private ObjectArray<Vector3>  m_4componentVertices = new ObjectArray<Vector3>();
        private ObjectArray<float> m_3componentVertices = new ObjectArray<float>();
        private ObjectArray<int> m_32bitIndices = new ObjectArray<int>();
        private ObjectArray<uint> m_16bitIndices = new ObjectArray<uint>();
	    private bool m_use32bitIndices;
        private bool m_use4componentVertices;

        public float m_weldingThreshold;

    }
}
