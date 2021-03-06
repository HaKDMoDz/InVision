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

using InVision.Bullet.Dynamics.Dynamics;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Dynamics.ConstraintSolver
{
    public class Point2PointConstraint : TypedConstraint
    {
	    public JacobianEntry[] m_jac = new JacobianEntry[3]; //3 orthogonal linear constraints
	    public Vector3 m_pivotInA;
	    public Vector3 m_pivotInB;
        public Point2PointFlags m_flags = 0;
        public float m_erp;
        public float m_cfm;

	    public ConstraintSetting m_setting = new ConstraintSetting();

        public Point2PointConstraint(RigidBody rbA, RigidBody rbB, ref Vector3 pivotInA, ref Vector3 pivotInB)
            : base(TypedConstraintType.POINT2POINT_CONSTRAINT_TYPE,rbA,rbB)
        {
            m_pivotInA = pivotInA;
            m_pivotInB = pivotInB;
        }
	    public Point2PointConstraint(RigidBody rbA,ref Vector3 pivotInA) : base(TypedConstraintType.POINT2POINT_CONSTRAINT_TYPE,rbA)
        {
            m_pivotInA = pivotInA;
            m_pivotInB = Vector3.Transform(pivotInA, rbA.GetCenterOfMassTransform());
        }

        public override void GetInfo1(ConstraintInfo1 info)
        {
            GetInfo1NonVirtual(info);
        }
        public void GetInfo1NonVirtual(ConstraintInfo1 info)
        {
            info.m_numConstraintRows = 3;
            info.nub = 3;
        }

        public override void GetInfo2(ConstraintInfo2 info)
        {
            GetInfo2NonVirtual(info, m_rbA.GetCenterOfMassTransform(), m_rbB.GetCenterOfMassTransform());
        }

        public void GetInfo2NonVirtual(ConstraintInfo2 info,Matrix body0_trans,Matrix body1_trans)
        {
            // anchor points in global coordinates with respect to body PORs.

            // set jacobian
            info.m_solverConstraints[0].m_contactNormal.X = 1;
            info.m_solverConstraints[1].m_contactNormal.Y = 1;
            info.m_solverConstraints[2].m_contactNormal.Z = 1;

            Vector3 a1 = Vector3.TransformNormal(GetPivotInA(),body0_trans);
            {
                Vector3 angular0 = info.m_solverConstraints[0].m_relpos1CrossNormal;
                Vector3 angular1 = info.m_solverConstraints[1].m_relpos1CrossNormal;
                Vector3 angular2 = info.m_solverConstraints[2].m_relpos1CrossNormal;
                Vector3 a1neg = -a1;

                MathUtil.GetSkewSymmetricMatrix(ref a1neg, ref angular0, ref angular1, ref angular2);
            }

            /*info->m_J2linearAxis[0] = -1;
            info->m_J2linearAxis[s+1] = -1;
            info->m_J2linearAxis[2*s+2] = -1;
            */

            Vector3 a2 = Vector3.TransformNormal(GetPivotInB(),body1_trans);

            {
                Vector3 a2n = -a2;
                Vector3 angular0 = info.m_solverConstraints[0].m_relpos2CrossNormal;
                Vector3 angular1 = info.m_solverConstraints[1].m_relpos2CrossNormal;
                Vector3 angular2 = info.m_solverConstraints[2].m_relpos2CrossNormal;
                MathUtil.GetSkewSymmetricMatrix(ref a2, ref angular0, ref angular1, ref angular2);
            }

            // set right hand side
            float currERP = ((m_flags & Point2PointFlags.BT_P2P_FLAGS_ERP) != 0) ? m_erp : info.erp;
            float k = info.fps * currERP;
            int j;
            Vector3 body0Origin = body0_trans.Translation;
            Vector3 body1Origin = body1_trans.Translation;

            for (j = 0; j < 3; j++)
            {
                info.m_solverConstraints[j].m_rhs = k * (MathUtil.VectorComponent(ref a2,j) + MathUtil.VectorComponent(ref body1Origin,j) - MathUtil.VectorComponent(ref a1,j) - MathUtil.VectorComponent(ref body0Origin,j));
                //printf("info->m_constraintError[%d]=%f\n",j,info->m_constraintError[j]);
            }

            if ((m_flags & Point2PointFlags.BT_P2P_FLAGS_CFM) != 0)
            {
                for (j = 0; j < 3; j++)
                {
                    info.m_solverConstraints[j].m_cfm = m_cfm;
                }
            }


            float impulseClamp = m_setting.m_impulseClamp;//
            for (j = 0; j < 3; j++)
            {
                if (m_setting.m_impulseClamp > 0)
                {
                    info.m_solverConstraints[j].m_lowerLimit = -impulseClamp;
                    info.m_solverConstraints[j].m_upperLimit = impulseClamp;
                }
            }
        }


        public void UpdateRHS(float timeStep)
        {
        }

	    public void SetPivotA(ref Vector3 pivotA)
	    {
		    m_pivotInA = pivotA;
	    }

	    public void SetPivotB(ref Vector3 pivotB)
	    {
		    m_pivotInB = pivotB;
	    }

	    public Vector3 GetPivotInA()
	    {
		    return m_pivotInA;
	    }

	    public Vector3 GetPivotInB() 
	    {
		    return m_pivotInB;
	    }



    }
}
