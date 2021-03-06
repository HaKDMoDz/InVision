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
    public class ConvexTriangleMeshShape : PolyhedralConvexAabbCachingShape
    {

	    public ConvexTriangleMeshShape(StridingMeshInterface meshInterface, bool calcAabb)
        {
	        m_shapeType = BroadphaseNativeTypes.CONVEX_TRIANGLEMESH_SHAPE_PROXYTYPE;
	        if ( calcAabb )
            {
		        RecalcLocalAabb();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }

	    public StridingMeshInterface GetMeshInterface()
	    {
		    return m_stridingMesh;
	    }

	    public override Vector3 LocalGetSupportingVertex(ref Vector3 vec)
        {
	        Vector3 supVertex = LocalGetSupportingVertexWithoutMargin(ref vec);

	        if (Margin != 0f)
	        {
		        Vector3 vecnorm = vec;
		        if (vecnorm.LengthSquared() < (MathUtil.SIMD_EPSILON*MathUtil.SIMD_EPSILON))
		        {
			        vecnorm = new Vector3(-1f,-1f,-1f);
		        } 
		        vecnorm.Normalize();
		        supVertex+= Margin * vecnorm;
	        }
	        return supVertex;

        }

        public override Vector3 LocalGetSupportingVertexWithoutMargin(ref Vector3 vec0)
        {
	        Vector3 supVec = Vector3.Zero;

	        Vector3 vec = vec0;
	        float lenSqr = vec.LengthSquared();
	        if (lenSqr < 0.0001f)
	        {
		        vec = Vector3.Right;
	        } 
            else
	        {
                float rlen = (1.0f) / (float)System.Math.Sqrt(lenSqr);
                vec *= rlen;
                //vec.Normalize();
            }

	        LocalSupportVertexCallback supportCallback = new LocalSupportVertexCallback(ref vec);
	        Vector3 aabbMax = new Vector3(float.MaxValue,float.MaxValue,float.MaxValue);
            Vector3 aabbMin = -aabbMax;
	        m_stridingMesh.InternalProcessAllTriangles(supportCallback,ref aabbMin,ref aabbMax);
	        supVec = supportCallback.GetSupportVertexLocal();

	        return supVec;

        }
        public override void BatchedUnitVectorGetSupportingVertexWithoutMargin(IList<Vector3> vectors, IList<Vector4> supportVerticesOut, int numVectors)
        {
	        for (int j=0;j<numVectors;j++)
	        {
		        Vector3 vec = vectors[j];
		        LocalSupportVertexCallback	supportCallback = new LocalSupportVertexCallback(ref vec);
                Vector3 aabbMax = MathUtil.MAX_VECTOR;
                Vector3 aabbMin = MathUtil.MIN_VECTOR;

		        m_stridingMesh.InternalProcessAllTriangles(supportCallback,ref aabbMin,ref aabbMax);
		        supportVerticesOut[j] = new Vector4(supportCallback.GetSupportVertexLocal(),0);
	        }
        }
	
	    //debugging

    	public override string Name
    	{
    		get { return "ConvexTrimesh"; }
    	}

    	public override int GetNumVertices()
        {
            return 0;
        }

        public override int GetNumEdges()
        {
            return 0;
        }
        public override void GetEdge(int i, ref Vector3 pa, ref Vector3 pb)
        {
            Debug.Assert(false);
        }
        public override void GetVertex(int i, ref Vector3 vtx)
        {
            Debug.Assert(false);
        }
        public override int GetNumPlanes()
        {
            return 0;
        }

        public override void GetPlane(ref Vector3 planeNormal, ref Vector3 planeSupport, int i)
        {
            Debug.Assert(false);
        }

        public override bool IsInside(ref Vector3 pt, float tolerance)
        {
            Debug.Assert(false);
            return false;
        }

        public override void SetLocalScaling(ref Vector3 scaling)
        {
            m_stridingMesh.SetScaling(ref scaling);
            RecalcLocalAabb();
        }

        public override Vector3 GetLocalScaling()
        {
            return m_stridingMesh.GetScaling();
        }

	    ///computes the exact moment of inertia and the transform from the coordinate system defined by the principal axes of the moment of inertia
	    ///and the center of mass to the current coordinate system. A mass of 1 is assumed, for other masses just multiply the computed "inertia"
	    ///by the mass. The resulting transform "principal" has to be applied inversely to the mesh in order for the local coordinate system of the
	    ///shape to be centered at the center of mass and to coincide with the principal axes. This also necessitates a correction of the world transform
	    ///of the collision object by the principal transform. This method also computes the volume of the convex mesh.
        public void CalculatePrincipalAxisTransform(ref Matrix principal, ref Vector3 inertia, float volume)
        {
            CenterCallback centerCallback = new CenterCallback();
            Vector3 aabbMax = MathUtil.MAX_VECTOR;
            Vector3 aabbMin = MathUtil.MIN_VECTOR;
            m_stridingMesh.InternalProcessAllTriangles(centerCallback, ref aabbMin, ref aabbMax);
            Vector3 center = centerCallback.GetCenter();
            principal.Translation = center;
            volume = centerCallback.GetVolume();

            InertiaCallback inertiaCallback = new InertiaCallback(ref center);
            m_stridingMesh.InternalProcessAllTriangles(inertiaCallback, ref aabbMax, ref aabbMax);

            Matrix i = inertiaCallback.GetInertia();
            MathUtil.Diagonalize(ref i, ref principal, 0.00001f, 20);
            //i.diagonalize(principal.getBasis(), 0.00001f, 20);
            inertia = new Vector3(i.M11,i.M22,i.M33);
            inertia /= volume;
        }

	    private StridingMeshInterface m_stridingMesh;

    }
}
