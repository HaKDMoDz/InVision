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
    public class ConeShape : ConvexInternalShape
    {

	    public ConeShape (float radius,float height)
        {
            m_radius = radius;
            m_height = height;
            m_shapeType = BroadphaseNativeTypes.CONE_SHAPE_PROXYTYPE;
            SetConeUpIndex(1);
            m_sinAngle = (float)(m_radius / System.Math.Sqrt(m_radius * m_radius + m_height * m_height));

        }

        public override Vector3 LocalGetSupportingVertex(ref Vector3 vec)
        {
            return ConeLocalSupport(ref vec);
        }

        public override Vector3 LocalGetSupportingVertexWithoutMargin(ref Vector3 vec)
        {
            return ConeLocalSupport(ref vec);
        }
        public override void BatchedUnitVectorGetSupportingVertexWithoutMargin(IList<Vector3> vectors, IList<Vector4> supportVerticesOut, int numVectors)
        {
	        for (int i=0;i<numVectors;i++)
	        {
		        Vector3 vec = vectors[i];
		        supportVerticesOut[i] = new Vector4(ConeLocalSupport(ref vec),0);
	        }
        }

	    public float GetRadius()  { return m_radius;}
	    public float GetHeight()  { return m_height;}


	    public override Vector3 CalculateLocalInertia(float mass) 
	    {
            Matrix identity = Matrix.Identity;
		    Vector3 aabbMin = new Vector3();
            Vector3 aabbMax = new Vector3();
		    GetAabb(ref identity,ref aabbMin,ref aabbMax);

		    Vector3 halfExtents = (aabbMax-aabbMin)*0.5f;

		    float margin = Margin;

            float lx=2f*(halfExtents.X+margin);
            float ly=2f*(halfExtents.Y+margin);
            float lz=2f*(halfExtents.Z+margin);
            float x2 = lx*lx;
            float y2 = ly*ly;
            float z2 = lz*lz;
            float scaledmass = mass * 0.08333333f;

            return scaledmass * (new Vector3(y2+z2,x2+z2,x2+y2));
	    }


    	public override string Name
    	{
    		get { return "Cone"; }
    	}

    	///choose upAxis index
        public void SetConeUpIndex(int upIndex)
        {
            switch (upIndex)
            {
                case 0:
                    {
                        m_coneIndices[0] = 1;
                        m_coneIndices[1] = 0;
                        m_coneIndices[2] = 2;
                        break;
                    }
                case 1:
                    {
                        m_coneIndices[0] = 0;
                        m_coneIndices[1] = 1;
                        m_coneIndices[2] = 2;
                        break;
                    }
                case 2:
                    {
                        m_coneIndices[0] = 0;
                        m_coneIndices[1] = 2;
                        m_coneIndices[2] = 1;
                        break;
                    }
                default:
                    Debug.Assert(false);
                    break;
            };

        }
    		
	    public int GetConeUpIndex() 
	    {
		    return m_coneIndices[1];
	    }

	    protected float m_sinAngle;
	    protected float m_radius;
	    protected float m_height;
	    protected int[] m_coneIndices = new int[3];

        protected Vector3 ConeLocalSupport(ref Vector3 v)
        {
	        float halfHeight = m_height * 0.5f;

            if (MathUtil.VectorComponent(ref v,m_coneIndices[1]) > v.Length() * m_sinAngle)
            {
                Vector3 tmp = Vector3.Zero;
                MathUtil.VectorComponent(ref tmp,m_coneIndices[0],0f);
                MathUtil.VectorComponent(ref tmp,m_coneIndices[1],halfHeight);
                MathUtil.VectorComponent(ref tmp,m_coneIndices[2],0f);
                return tmp;
            }
            else 
            {
                float v0 = MathUtil.VectorComponent(ref v,m_coneIndices[0]);
                float v2 = MathUtil.VectorComponent(ref v,m_coneIndices[2]);
                float s = (float)System.Math.Sqrt(v0 * v0 + v2 * v2);
                if (s > MathUtil.SIMD_EPSILON) 
                {
                    Vector3 tmp = Vector3.Zero;
                    float d = m_radius / s;
                    MathUtil.VectorComponent(ref tmp,m_coneIndices[0],v0*d);
                    MathUtil.VectorComponent(ref tmp,m_coneIndices[1],-halfHeight);
                    MathUtil.VectorComponent(ref tmp,m_coneIndices[2],v2*d);
                    return tmp;
                }
                else  
                {
                    Vector3 tmp = Vector3.Zero;
                    MathUtil.VectorComponent(ref tmp,m_coneIndices[0],0f);
                    MathUtil.VectorComponent(ref tmp,m_coneIndices[1],-halfHeight);
                    MathUtil.VectorComponent(ref tmp,m_coneIndices[2],0f);
                    return tmp;
                }
            }

        }
    }
}
