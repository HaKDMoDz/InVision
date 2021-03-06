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


// HAVE TESTED THIS WITH REPLACEMENT VERSION WITH NO DIFFERENCE

using System;
using System.Diagnostics;
using InVision.Bullet.Collision.BroadphaseCollision;
using InVision.Bullet.Collision.CollisionShapes;
using InVision.Bullet.Debuging;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Collision.NarrowPhaseCollision
{
    public class GjkPairDetector : IDiscreteCollisionDetectorInterface
    {
        public GjkPairDetector(ConvexShape objectA, ConvexShape objectB, ISimplexSolverInterface simplexSolver, IConvexPenetrationDepthSolver penetrationDepthSolver)
        {
            m_minkowskiA = objectA;
            m_minkowskiB = objectB;
            m_shapeTypeA = objectA.ShapeType;
            m_shapeTypeB = objectB.ShapeType;
            m_marginA = objectA.Margin;
            m_marginB = objectB.Margin;

            m_cachedSeparatingAxis = new Vector3(0, 1, 0);


            m_simplexSolver = simplexSolver;
            m_penetrationDepthSolver = penetrationDepthSolver;
            m_ignoreMargin = false;
            m_lastUsedMethod = -1;
            m_catchDegeneracies = true;
        }

        public GjkPairDetector(ConvexShape objectA, ConvexShape objectB, BroadphaseNativeTypes shapeTypeA, BroadphaseNativeTypes shapeTypeB, float marginA, float marginB, ISimplexSolverInterface simplexSolver, IConvexPenetrationDepthSolver penetrationDepthSolver)
        {
            m_minkowskiA = objectA;
            m_minkowskiB = objectB;
            m_shapeTypeA = shapeTypeA;
            m_shapeTypeB = shapeTypeB;
            m_marginA = marginA;
            m_marginB = marginB;

            m_cachedSeparatingAxis = new Vector3(0, 1, 0);

            m_simplexSolver = simplexSolver;
            m_penetrationDepthSolver = penetrationDepthSolver;
            m_ignoreMargin = false;
            m_lastUsedMethod = -1;
            m_catchDegeneracies = true;

        }

        public virtual void GetClosestPoints(ClosestPointInput input, IDiscreteCollisionDetectorInterfaceResult output, IDebugDraw debugDraw)
        {
            GetClosestPoints(input, output, debugDraw, false);
        }
        public virtual void GetClosestPoints(ClosestPointInput input, IDiscreteCollisionDetectorInterfaceResult output, IDebugDraw debugDraw, bool swapResults)
        {
            GetClosestPointsNonVirtual(input, output, debugDraw);
        }

        public void GetClosestPointsNonVirtual(ClosestPointInput input, IDiscreteCollisionDetectorInterfaceResult output, IDebugDraw debugDraw)
        {
            m_cachedSeparatingDistance = 0f;

            float distance = 0f;
            Vector3 normalInB = Vector3.Zero;
            Vector3 pointOnA = Vector3.Zero, pointOnB = Vector3.Zero;
            Matrix localTransA = input.m_transformA;
            Matrix localTransB = input.m_transformB;
            Vector3 positionOffset = (localTransA.Translation + localTransB.Translation) * .5f;
            localTransA.Translation -= positionOffset;
            localTransB.Translation -= positionOffset;

            bool check2d = m_minkowskiA.IsConvex2D() && m_minkowskiB.IsConvex2D();

            float marginA = m_marginA;
            float marginB = m_marginB;

#if TEST_NON_VIRTUAL
            float marginAv = m_minkowskiA.getMarginNonVirtual();
            float marginBv = m_minkowskiB.getMarginNonVirtual();
            Debug.Assert(marginA == marginAv);
            Debug.Assert(marginB == marginBv);
#endif //TEST_NON_VIRTUAL

            gNumGjkChecks++;

#if DEBUG_SPU_COLLISION_DETECTION
           spu_printf("inside gjk\n");
#endif
            //for CCD we don't use margins
            if (m_ignoreMargin)
            {
                marginA = 0f;
                marginB = 0f;
#if DEBUG_SPU_COLLISION_DETECTION
                spu_printf("ignoring margin\n");
#endif
            }

            m_curIter = 0;
            int gGjkMaxIter = 1000;//this is to catch invalid input, perhaps check for #NaN?
            m_cachedSeparatingAxis = new Vector3(0, 1, 0);

            bool isValid = false;
            bool checkSimplex = false;
            bool checkPenetration = true;
            m_degenerateSimplex = 0;

            m_lastUsedMethod = -1;

            if (BulletGlobals.g_streamWriter != null && debugGJK)
            {
                MathUtil.PrintMatrix(BulletGlobals.g_streamWriter, "gjk::getClosestPointsNonVirtual transA", localTransA);
                MathUtil.PrintMatrix(BulletGlobals.g_streamWriter, "gjk::getClosestPointsNonVirtual transB", localTransB);

            }


            {
                float squaredDistance = MathUtil.BT_LARGE_FLOAT;
                float delta = 0f;

                float margin = marginA + marginB;

                m_simplexSolver.Reset();

                int count = 0;
                for (; ; )
                //while (true)
                {
                    count++;
                    if (gNumGjkChecks == 3 && count == 4)
                    {
                        int ibreak = 0;
                    }
                    Vector3 seperatingAxisInA = MathUtil.TransposeTransformNormal(-m_cachedSeparatingAxis, input.m_transformA);
                    Vector3 seperatingAxisInB = MathUtil.TransposeTransformNormal(m_cachedSeparatingAxis, input.m_transformB);
#if true

                    Vector3 pInA = m_minkowskiA.LocalGetSupportVertexWithoutMarginNonVirtual(ref seperatingAxisInA);
                    Vector3 qInB = m_minkowskiB.LocalGetSupportVertexWithoutMarginNonVirtual(ref seperatingAxisInB);

                    //			btVector3 pInA  = localGetSupportingVertexWithoutMargin(m_shapeTypeA, m_minkowskiA, seperatingAxisInA,input.m_convexVertexData[0]);//, &featureIndexA);
                    //			btVector3 qInB  = localGetSupportingVertexWithoutMargin(m_shapeTypeB, m_minkowskiB, seperatingAxisInB,input.m_convexVertexData[1]);//, &featureIndexB);

#else
                    Vector3 pInA = m_minkowskiA.localGetSupportingVertexWithoutMargin(ref seperatingAxisInA);
                    Vector3 qInB = m_minkowskiB.localGetSupportingVertexWithoutMargin(ref seperatingAxisInB);
#if TEST_NON_VIRTUAL
                    Vector3 pInAv = m_minkowskiA->localGetSupportingVertexWithoutMargin(seperatingAxisInA);
                    Vector3 qInBv = m_minkowskiB->localGetSupportingVertexWithoutMargin(seperatingAxisInB);
                    Debug.Assert((pInAv-pInA).Length() < 0.0001);
                    Debug.Assert((qInBv-qInB).Length() < 0.0001);
#endif //
#endif
                    Vector3 pWorld = Vector3.Transform(pInA, localTransA);
                    Vector3 qWorld = Vector3.Transform(qInB, localTransB);



                    if (check2d)
                    {
                        pWorld.Z = 0.0f;
                        qWorld.Z = 0.0f;
                    }

                    Vector3 w = pWorld - qWorld;
                    delta = Vector3.Dot(m_cachedSeparatingAxis, w);

                    if (BulletGlobals.g_streamWriter != null && debugGJK)
                    {
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "m_cachedSeparatingAxis", m_cachedSeparatingAxis);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "w", w);
                        BulletGlobals.g_streamWriter.WriteLine(String.Format("simplex num vertices [{0}]", m_simplexSolver.NumVertices()));
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "sepAxisA", seperatingAxisInA);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "sepAxisB", seperatingAxisInB);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "pInA", pInA);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "qInB", qInB);
                        MathUtil.PrintMatrix(BulletGlobals.g_streamWriter, "localTransA", localTransA);
                        MathUtil.PrintMatrix(BulletGlobals.g_streamWriter, "localTransB", localTransB);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "pWorld", pWorld);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "qWorld", qWorld);
                    }

                    if (m_simplexSolver.NumVertices() == 3)
                    {
                        int ibreak = 0;
                    }


                    // potential exit, they don't overlap
                    if ((delta > 0f) && (delta * delta > squaredDistance * input.m_maximumDistanceSquared))
                    {
                        m_degenerateSimplex = 10;
                        checkSimplex = true;
                        //checkPenetration = false;
                        break;
                    }

                    //exit 0: the new point is already in the simplex, or we didn't come any closer
                    if (m_simplexSolver.InSimplex(ref w))
                    {
                        m_degenerateSimplex = 1;
                        checkSimplex = true;
                        break;
                    }
                    // are we getting any closer ?
                    float f0 = squaredDistance - delta;
                    float f1 = squaredDistance * REL_ERROR2;



                    if (f0 <= f1)
                    {
                        if (f0 <= 0f)
                        {
                            m_degenerateSimplex = 2;
                        }
                        else
                        {
                            m_degenerateSimplex = 11;
                        }
                        checkSimplex = true;
                        break;
                    }
                    //add current vertex to simplex
                    m_simplexSolver.AddVertex(ref w, ref pWorld, ref qWorld);

                    //calculate the closest point to the origin (update vector v)
                    Vector3 newCachedSeparatingAxis = new Vector3();

                    if (!m_simplexSolver.Closest(ref newCachedSeparatingAxis))
                    {
                        m_degenerateSimplex = 3;
                        checkSimplex = true;
                        break;
                    }

                    if (newCachedSeparatingAxis.LengthSquared() < REL_ERROR2)
                    {
                        m_cachedSeparatingAxis = newCachedSeparatingAxis;
                        m_degenerateSimplex = 6;
                        checkSimplex = true;
                        break;
                    }

                    float previousSquaredDistance = squaredDistance;
                    squaredDistance = newCachedSeparatingAxis.LengthSquared();

                    if (BulletGlobals.g_streamWriter != null && debugGJK)
                    {
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "sepAxisA", seperatingAxisInA);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "sepAxisB", seperatingAxisInB);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "pInA", pInA);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "qInB", qInB);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "pWorld", pWorld);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "qWorld", qWorld);
                        MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "newSeperatingAxis", newCachedSeparatingAxis);
                        BulletGlobals.g_streamWriter.WriteLine(String.Format("f0[{0:0.00000000}] f1[{1:0.00000000}] checkSimplex[{2}] degen[{3}]", f0, f1, checkSimplex, m_degenerateSimplex));
                    }


#if false
                    ///warning: this termination condition leads to some problems in 2d test case see Bullet/Demos/Box2dDemo
                    if (squaredDistance>previousSquaredDistance)
                    {
                        m_degenerateSimplex = 7;
                        squaredDistance = previousSquaredDistance;
                        checkSimplex = false;
                        break;
                    }
#endif //
                    m_cachedSeparatingAxis = newCachedSeparatingAxis;

                    //redundant m_simplexSolver->compute_points(pointOnA, pointOnB);

                    //are we getting any closer ?
                    if (previousSquaredDistance - squaredDistance <= MathUtil.SIMD_EPSILON * previousSquaredDistance)
                    {
                        m_simplexSolver.BackupClosest(ref m_cachedSeparatingAxis);
                        checkSimplex = true;
                        m_degenerateSimplex = 12;
                        break;
                    }

                    //degeneracy, this is typically due to invalid/uninitialized worldtransforms for a btCollisionObject   
                    if (m_curIter++ > gGjkMaxIter)
                    {
                        //#if defined(DEBUG) || defined (_DEBUG) || defined (DEBUG_SPU_COLLISION_DETECTION)

                        //        printf("btGjkPairDetector maxIter exceeded:%i\n",m_curIter);   
                        //        printf("sepAxis=(%f,%f,%f), squaredDistance = %f, shapeTypeA=%i,shapeTypeB=%i\n",   
                        //        m_cachedSeparatingAxis.getX(),   
                        //        m_cachedSeparatingAxis.getY(),   
                        //        m_cachedSeparatingAxis.getZ(),   
                        //        squaredDistance,   
                        //        m_minkowskiA->getShapeType(),   
                        //        m_minkowskiB->getShapeType());   

                        //#endif   
                        break;

                    }

                    bool check = (!m_simplexSolver.FullSimplex());
                    //bool check = (!m_simplexSolver->fullSimplex() && squaredDistance > SIMD_EPSILON * m_simplexSolver->maxVertex());

                    if (!check)
                    {
                        //do we need this backup_closest here ?
                        m_simplexSolver.BackupClosest(ref m_cachedSeparatingAxis);
                        m_degenerateSimplex = 13;

                        break;
                    }
                }

                if (checkSimplex)
                {
                    m_simplexSolver.ComputePoints(ref pointOnA, ref pointOnB);
                    normalInB = pointOnA - pointOnB;
                    float lenSqr = m_cachedSeparatingAxis.LengthSquared();
                    //valid normal
                    if (lenSqr < 0.0001f)
                    {
                        m_degenerateSimplex = 5;
                    }
                    if (lenSqr > MathUtil.SIMD_EPSILON * MathUtil.SIMD_EPSILON)
                    {
                        //float rlen = 1 / normalInB.Length();
                        float rlen = 1.0f / (float)System.Math.Sqrt((float)lenSqr );
                        //normalInB.Normalize();
                        normalInB *= rlen;
                        float s = (float)System.Math.Sqrt((float)squaredDistance);

                        Debug.Assert(s > 0f);
                        pointOnA -= m_cachedSeparatingAxis * (marginA / s);
                        pointOnB += m_cachedSeparatingAxis * (marginB / s);
                        distance = ((1f / rlen) - margin);
                        isValid = true;

                        m_lastUsedMethod = 1;
                    }
                    else
                    {
                        m_lastUsedMethod = 2;
                    }
                }

                bool catchDegeneratePenetrationCase =
                    (m_catchDegeneracies && m_penetrationDepthSolver != null && m_degenerateSimplex > 0 && ((distance + margin) < 0.01));

                //if (checkPenetration && !isValid)
                if (checkPenetration && (!isValid || catchDegeneratePenetrationCase))
                {
                    //penetration case

                    //if there is no way to handle penetrations, bail ref
                    if (m_penetrationDepthSolver != null)
                    {
                        // Penetration depth case.
                        Vector3 tmpPointOnA = Vector3.Zero, tmpPointOnB = Vector3.Zero;

                        gNumDeepPenetrationChecks++;
                        m_cachedSeparatingAxis = Vector3.Zero;
                        bool isValid2 = m_penetrationDepthSolver.CalcPenDepth(
                            m_simplexSolver,
                            m_minkowskiA, m_minkowskiB,
                            ref localTransA, ref localTransB,
                            ref m_cachedSeparatingAxis, ref tmpPointOnA, ref tmpPointOnB,
                            debugDraw
                            );


                        if (BulletGlobals.g_streamWriter != null && debugGJK)
                        {
                            BulletGlobals.g_streamWriter.WriteLine("calcPenDepthResult");
                            MathUtil.PrintMatrix(BulletGlobals.g_streamWriter, "localTransA", localTransA);
                            MathUtil.PrintMatrix(BulletGlobals.g_streamWriter, "localTransB", localTransB);
                            MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "sepAxis", m_cachedSeparatingAxis);
                            MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "tmpA", tmpPointOnA);
                            MathUtil.PrintVector3(BulletGlobals.g_streamWriter, "tmpB", tmpPointOnB);
                        }


                        if (isValid2)
                        {
                            Vector3 tmpNormalInB = tmpPointOnB - tmpPointOnA;
                            float lenSqr = tmpNormalInB.LengthSquared();
                            if (lenSqr <= (MathUtil.SIMD_EPSILON * MathUtil.SIMD_EPSILON))
                            {
                                tmpNormalInB = m_cachedSeparatingAxis;
                                lenSqr = m_cachedSeparatingAxis.LengthSquared();
                            }

                            if (lenSqr > (MathUtil.SIMD_EPSILON * MathUtil.SIMD_EPSILON))
                            {
                                tmpNormalInB.Normalize();
                                float distance2 = -(tmpPointOnA - tmpPointOnB).Length();
                                //only replace valid penetrations when the result is deeper (check)
                                if (!isValid || (distance2 < distance))
                                {
                                    distance = distance2;
                                    pointOnA = tmpPointOnA;
                                    pointOnB = tmpPointOnB;
                                    normalInB = tmpNormalInB;
                                    isValid = true;
                                    m_lastUsedMethod = 3;
                                }
                                else
                                {
                                    m_lastUsedMethod = 8;
                                }
                            }
                            else
                            {
                                //isValid = false;
                                m_lastUsedMethod = 9;
                            }
                        }
                        else
                        {
                            ///this is another degenerate case, where the initial GJK calculation reports a degenerate case
                            ///EPA reports no penetration, and the second GJK (using the supporting vector without margin)
                            ///reports a valid positive distance. Use the results of the second GJK instead of failing.
                            ///thanks to Jacob.Langford for the reproduction case
                            ///http://code.google.com/p/bullet/issues/detail?id=250


                            if (m_cachedSeparatingAxis.LengthSquared() > 0f)
                            {
                                float distance2 = (tmpPointOnA - tmpPointOnB).Length() - margin;
                                //only replace valid distances when the distance is less
                                if (!isValid || (distance2 < distance))
                                {
                                    distance = distance2;
                                    pointOnA = tmpPointOnA;
                                    pointOnB = tmpPointOnB;
                                    pointOnA -= m_cachedSeparatingAxis * marginA;
                                    pointOnB += m_cachedSeparatingAxis * marginB;
                                    normalInB = m_cachedSeparatingAxis;
                                    normalInB.Normalize();
                                    isValid = true;
                                    m_lastUsedMethod = 6;
                                }
                                else
                                {
                                    m_lastUsedMethod = 5;
                                }
                            }
                        }
                    }
                }
            }

            if (isValid && ((distance < 0) || (distance * distance < input.m_maximumDistanceSquared)))
            {
                m_cachedSeparatingAxis = normalInB;
                m_cachedSeparatingDistance = distance;
                Vector3 temp = pointOnB + positionOffset;
                output.AddContactPoint(
                    ref normalInB,
                    ref temp,
                    distance);
            }
        }

        public void SetMinkowskiA(ConvexShape minkA)
        {
            m_minkowskiA = minkA;
        }

        public void SetMinkowskiB(ConvexShape minkB)
        {
            m_minkowskiB = minkB;
        }

        public void SetCachedSeperatingAxis(Vector3 seperatingAxis)
        {
            SetCachedSeperatingAxis(ref seperatingAxis);
        }

        public void SetCachedSeperatingAxis(ref Vector3 seperatingAxis)
        {
            m_cachedSeparatingAxis = seperatingAxis;
        }

        public Vector3 GetCachedSeparatingAxis()
        {
            return m_cachedSeparatingAxis;
        }

        public float GetCachedSeparatingDistance()
        {
            return m_cachedSeparatingDistance;
        }

        public void SetPenetrationDepthSolver(IConvexPenetrationDepthSolver penetrationDepthSolver)
        {
            m_penetrationDepthSolver = penetrationDepthSolver;
        }

        ///don't use setIgnoreMargin, it's for Bullet's internal use
        public void SetIgnoreMargin(bool ignoreMargin)
        {
            m_ignoreMargin = ignoreMargin;
        }

        public int m_lastUsedMethod;
        public int m_curIter;
        public int m_degenerateSimplex;
        public bool m_catchDegeneracies;



        private Vector3 m_cachedSeparatingAxis;
        private IConvexPenetrationDepthSolver m_penetrationDepthSolver;
        private ISimplexSolverInterface m_simplexSolver;
        private ConvexShape m_minkowskiA;
        private ConvexShape m_minkowskiB;
        private BroadphaseNativeTypes m_shapeTypeA;
        private BroadphaseNativeTypes m_shapeTypeB;
        private float m_marginA;
        private float m_marginB;


        private bool m_ignoreMargin;
        private float m_cachedSeparatingDistance;

        //must be above the machine epsilon
        private static readonly float REL_ERROR2 = 1.0e-6f;

        //temp globals, to improve GJK/EPA/penetration calculations
        private static int gNumDeepPenetrationChecks = 0;
        private static int gNumGjkChecks = 0;
        public static bool debugGJK = false;
    }
}
