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

#define USE_BATCHED_SUPPORT

using System.Collections.Generic;
using InVision.Bullet.Collision.CollisionShapes;
using InVision.Bullet.Debuging;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Collision.NarrowPhaseCollision
{
    public class MinkowskiPenetrationDepthSolver : IConvexPenetrationDepthSolver
    {
        public bool CalcPenDepth(ISimplexSolverInterface simplexSolver, ConvexShape convexA, ConvexShape convexB, ref Matrix transA, ref Matrix transB,
                ref Vector3 v, ref Vector3 pa, ref Vector3 pb, IDebugDraw debugDraw)
        {
            bool check2d = convexA.IsConvex2D() && convexB.IsConvex2D();


            float minProj = float.MaxValue;
            Vector3 minNorm = Vector3.Zero;
            Vector3 minA = Vector3.Zero, minB = Vector3.Zero;
            Vector3 seperatingAxisInA, seperatingAxisInB;
            Vector3 pInA, qInB, pWorld, qWorld, w;

#if USE_BATCHED_SUPPORT


            IList<Vector4> supportVerticesABatch = new ObjectArray<Vector4>(NUM_UNITSPHERE_POINTS + ConvexShape.MAX_PREFERRED_PENETRATION_DIRECTIONS * 2);
            IList<Vector4> supportVerticesBBatch = new ObjectArray<Vector4>(NUM_UNITSPHERE_POINTS + ConvexShape.MAX_PREFERRED_PENETRATION_DIRECTIONS * 2);
            IList<Vector3> seperatingAxisInABatch = new ObjectArray<Vector3>(NUM_UNITSPHERE_POINTS + ConvexShape.MAX_PREFERRED_PENETRATION_DIRECTIONS * 2);
            IList<Vector3> seperatingAxisInBBatch = new ObjectArray<Vector3>(NUM_UNITSPHERE_POINTS + ConvexShape.MAX_PREFERRED_PENETRATION_DIRECTIONS * 2);


            int numSampleDirections = NUM_UNITSPHERE_POINTS;

            for (int i = 0; i < numSampleDirections; i++)
            {
                Vector3 norm = sPenetrationDirections[i];
				seperatingAxisInABatch[i] = MathUtil.TransposeTransformNormal(-norm, transA);
				seperatingAxisInBBatch[i] = MathUtil.TransposeTransformNormal(norm, transB);
            }

            {
                int numPDA = convexA.GetNumPreferredPenetrationDirections();
                if (numPDA > 0)
                {
                    for (int i = 0; i < numPDA; i++)
                    {
                        Vector3 norm = Vector3.Up;
                        convexA.GetPreferredPenetrationDirection(i, ref norm);
                        norm = Vector3.TransformNormal(norm, transA);
                        sPenetrationDirections[numSampleDirections] = norm;
                        seperatingAxisInABatch[numSampleDirections] = Vector3.TransformNormal(-norm, transA);
                        seperatingAxisInBBatch[numSampleDirections] = Vector3.Transform(norm, transB);
                        numSampleDirections++;
                    }
                }
            }

            {
                int numPDB = convexB.GetNumPreferredPenetrationDirections();
                if (numPDB > 0)
                {
                    for (int i = 0; i < numPDB; i++)
                    {
                        Vector3 norm = Vector3.Up;
                        convexB.GetPreferredPenetrationDirection(i, ref norm);
                        norm = Vector3.TransformNormal(norm, transB);
                        sPenetrationDirections[numSampleDirections] = norm;
                        seperatingAxisInABatch[numSampleDirections] = Vector3.TransformNormal(-norm, transA);
                        seperatingAxisInBBatch[numSampleDirections] = Vector3.TransformNormal(norm, transB);
                        numSampleDirections++;
                    }
                }
            }

            convexA.BatchedUnitVectorGetSupportingVertexWithoutMargin(seperatingAxisInABatch, supportVerticesABatch, numSampleDirections);
            convexB.BatchedUnitVectorGetSupportingVertexWithoutMargin(seperatingAxisInBBatch, supportVerticesBBatch, numSampleDirections);

            for (int i = 0; i < numSampleDirections; i++)
            {
                Vector3 norm = sPenetrationDirections[i];
                if (check2d)
                {
                    // shouldn't this be Y ?
                    norm.Z = 0;
                }
                seperatingAxisInA = seperatingAxisInABatch[i];
                seperatingAxisInB = seperatingAxisInBBatch[i];

                pInA = new Vector3(supportVerticesABatch[i].X, supportVerticesABatch[i].Y, supportVerticesABatch[i].Z);
                qInB = new Vector3(supportVerticesBBatch[i].X, supportVerticesBBatch[i].Y, supportVerticesBBatch[i].Z);

                pWorld = Vector3.Transform(pInA, transA);
                qWorld = Vector3.Transform(qInB, transB);
                if (check2d)
                {
                    // shouldn't this be Y ?
                    pWorld.Z = 0f;
                    qWorld.Z = 0f;
                }


                w = qWorld - pWorld;
                float delta = Vector3.Dot(norm, w);
                //find smallest delta
                if (delta < minProj)
                {
                    minProj = delta;
                    minNorm = norm;
                    minA = pWorld;
                    minB = qWorld;
                }
            }
#else
            int numSampleDirections = NUM_UNITSPHERE_POINTS;

	        {
		        int numPDA = convexA.getNumPreferredPenetrationDirections();
		        if (numPDA > 0)
		        {
			        for (int i=0;i<numPDA;i++)
			        {
				        Vector3 norm = Vector3.Zero;
				        convexA.getPreferredPenetrationDirection(i,ref norm);
				        norm  = Vector3.TransformNormal(norm,transA);
				        sPenetrationDirections[numSampleDirections] = norm;
				        numSampleDirections++;
			        }
		        }
	        }

	        {
		        int numPDB = convexB.getNumPreferredPenetrationDirections();
		        if (numPDB > 0)
		        {
			        for (int i=0;i<numPDB;i++)
			        {
                        Vector3 norm = Vector3.Zero;
				        convexB.getPreferredPenetrationDirection(i,ref norm);
				        norm  = Vector3.TransformNormal(norm,transB);
				        sPenetrationDirections[numSampleDirections] = norm;
				        numSampleDirections++;
			        }
		        }
	        }

	        for (int i=0;i<numSampleDirections;i++)
	        {
		        Vector3 norm = sPenetrationDirections[i];
		        if (check2d)
		        {
			        norm.Z = 0f;
		        }
                if (norm.LengthSquared() > 0.01f)
                {
                    seperatingAxisInA = Vector3.TransformNormal(-norm, transA);
                    seperatingAxisInB = Vector3.TransformNormal(norm, transB);
                    pInA = convexA.localGetSupportVertexWithoutMarginNonVirtual(ref seperatingAxisInA);
                    qInB = convexB.localGetSupportVertexWithoutMarginNonVirtual(ref seperatingAxisInB);
                    pWorld = Vector3.Transform(pInA, transA);
                    qWorld = Vector3.Transform(qInB, transB);
                    if (check2d)
                    {
                        pWorld.Z = 0.0f;
                        qWorld.Z = 0.0f;
                    }

                    w = qWorld - pWorld;
                    float delta = Vector3.Dot(norm, w);
                    //find smallest delta
                    if (delta < minProj)
                    {
                        minProj = delta;
                        minNorm = norm;
                        minA = pWorld;
                        minB = qWorld;
                    }
                }
	        }
#endif //USE_BATCHED_SUPPORT

            //add the margins

            minA += minNorm * convexA.GetMarginNonVirtual();
            minB -= minNorm * convexB.GetMarginNonVirtual();
            //no penetration
            if (minProj < 0f)
            {
                return false;
            }

            float extraSeparation = 0.5f;///scale dependent
            minProj += extraSeparation + (convexA.GetMarginNonVirtual() + convexB.GetMarginNonVirtual());

#if DEBUG_DRAW
	        if (debugDraw)
	        {
		        Vector3 color = new Vector3(0,1,0);
		        debugDraw.drawLine(minA,minB,color);
		        color = new Vector3(1,1,1);
		        Vector3 vec = minB-minA;
		        float prj2 = Vector3.Dot(minNorm,vec);
		        debugDraw.drawLine(minA,minA+(minNorm*minProj),color);

	        }
#endif //DEBUG_DRAW



            GjkPairDetector gjkdet = new GjkPairDetector(convexA, convexB, simplexSolver, null);

            float offsetDist = minProj;
            Vector3 offset = minNorm * offsetDist;

            ClosestPointInput input = new ClosestPointInput();

            Vector3 newOrg = transA.Translation + offset;

            Matrix displacedTrans = transA;
            displacedTrans.Translation = newOrg;

            input.m_transformA = displacedTrans;
            input.m_transformB = transB;
            input.m_maximumDistanceSquared = float.MaxValue;

            MinkowskiIntermediateResult res = new MinkowskiIntermediateResult();
            Vector3 temp = -minNorm;
            gjkdet.SetCachedSeperatingAxis(-minNorm);

            gjkdet.GetClosestPoints(input, res, debugDraw,false);

            float correctedMinNorm = minProj - res.m_depth;

            //the penetration depth is over-estimated, relax it
            float penetration_relaxation = 1f;
            minNorm *= penetration_relaxation;

            if (res.m_hasResult)
            {

                pa = res.m_pointInWorld - minNorm * correctedMinNorm;
                pb = res.m_pointInWorld;
                v = minNorm;

#if DEBUG_DRAW
		        if (debugDraw != null)
		        {
			        Vector3 color = new Vector3(1,0,0);
			        debugDraw.drawLine(pa,pb,color);
		        }
#endif//DEBUG_DRAW


            }
            return res.m_hasResult;
        }

        private readonly static int NUM_UNITSPHERE_POINTS = 42;
        private readonly static Vector3[] sPenetrationDirections = {
            new Vector3(0.000000f , -0.000000f,-1.000000f),
            new Vector3(0.723608f , -0.525725f,-0.447219f),
            new Vector3(-0.276388f , -0.850649f,-0.447219f),
            new Vector3(-0.894426f , -0.000000f,-0.447216f),
            new Vector3(-0.276388f , 0.850649f,-0.447220f),
            new Vector3(0.723608f , 0.525725f,-0.447219f),
            new Vector3(0.276388f , -0.850649f,0.447220f),
            new Vector3(-0.723608f , -0.525725f,0.447219f),
            new Vector3(-0.723608f , 0.525725f,0.447219f),
            new Vector3(0.276388f , 0.850649f,0.447219f),
            new Vector3(0.894426f , 0.000000f,0.447216f),
            new Vector3(-0.000000f , 0.000000f,1.000000f),
            new Vector3(0.425323f , -0.309011f,-0.850654f),
            new Vector3(-0.162456f , -0.499995f,-0.850654f),
            new Vector3(0.262869f , -0.809012f,-0.525738f),
            new Vector3(0.425323f , 0.309011f,-0.850654f),
            new Vector3(0.850648f , -0.000000f,-0.525736f),
            new Vector3(-0.525730f , -0.000000f,-0.850652f),
            new Vector3(-0.688190f , -0.499997f,-0.525736f),
            new Vector3(-0.162456f , 0.499995f,-0.850654f),
            new Vector3(-0.688190f , 0.499997f,-0.525736f),
            new Vector3(0.262869f , 0.809012f,-0.525738f),
            new Vector3(0.951058f , 0.309013f,0.000000f),
            new Vector3(0.951058f , -0.309013f,0.000000f),
            new Vector3(0.587786f , -0.809017f,0.000000f),
            new Vector3(0.000000f , -1.000000f,0.000000f),
            new Vector3(-0.587786f , -0.809017f,0.000000f),
            new Vector3(-0.951058f , -0.309013f,-0.000000f),
            new Vector3(-0.951058f , 0.309013f,-0.000000f),
            new Vector3(-0.587786f , 0.809017f,-0.000000f),
            new Vector3(-0.000000f , 1.000000f,-0.000000f),
            new Vector3(0.587786f , 0.809017f,-0.000000f),
            new Vector3(0.688190f , -0.499997f,0.525736f),
            new Vector3(-0.262869f , -0.809012f,0.525738f),
            new Vector3(-0.850648f , 0.000000f,0.525736f),
            new Vector3(-0.262869f , 0.809012f,0.525738f),
            new Vector3(0.688190f , 0.499997f,0.525736f),
            new Vector3(0.525730f , 0.000000f,0.850652f),
            new Vector3(0.162456f , -0.499995f,0.850654f),
            new Vector3(-0.425323f , -0.309011f,0.850654f),
            new Vector3(-0.425323f , 0.309011f,0.850654f),
            new Vector3(0.162456f , 0.499995f,0.850654f),
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero,
            Vector3.Zero};
    }
}
