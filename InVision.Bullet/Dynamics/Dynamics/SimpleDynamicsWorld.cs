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

using InVision.Bullet.Collision.BroadphaseCollision;
using InVision.Bullet.Collision.CollisionDispatch;
using InVision.Bullet.Collision.NarrowPhaseCollision;
using InVision.Bullet.Dynamics.ConstraintSolver;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Dynamics.Dynamics
{
    public abstract class SimpleDynamicsWorld : DynamicsWorld
    {
	    ///this btSimpleDynamicsWorld constructor creates dispatcher, broadphase pairCache and constraintSolver
        public SimpleDynamicsWorld(IDispatcher dispatcher, IBroadphaseInterface pairCache, IConstraintSolver constraintSolver, ICollisionConfiguration collisionConfiguration) :base(dispatcher,pairCache,collisionConfiguration)
        {
            m_constraintSolver = constraintSolver;
            m_ownsConstraintSolver = false;
            Gravity = new Vector3(0, 0, -10f);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (m_ownsConstraintSolver)
            {
                m_constraintSolver.Cleanup();
                m_constraintSolver = null;
            }
        }
    		
	    ///maxSubSteps/fixedTimeStep for interpolation is currently ignored for btSimpleDynamicsWorld, use btDiscreteDynamicsWorld instead
	    public override int	StepSimulation(float timeStep,int maxSubSteps, float fixedTimeStep)
        {
            ///apply gravity, predict motion
            PredictUnconstraintMotion(timeStep);

            DispatcherInfo dispatchInfo = GetDispatchInfo();
            dispatchInfo.SetTimeStep(timeStep);
            dispatchInfo.SetStepCount(0);
            dispatchInfo.SetDebugDraw(GetDebugDrawer());

            ///perform collision detection
            PerformDiscreteCollisionDetection();

            ///solve contact constraints
            int numManifolds = m_dispatcher1.GetNumManifolds();
            if (numManifolds != 0)
            {
                ObjectArray<PersistentManifold> manifoldPtr = ((CollisionDispatcher)m_dispatcher1).GetInternalManifoldPointer();

                ContactSolverInfo infoGlobal = new ContactSolverInfo();
                infoGlobal.m_timeStep = timeStep;
                m_constraintSolver.PrepareSolve(0, numManifolds);
                m_constraintSolver.SolveGroup(null, 0, manifoldPtr, numManifolds, null, 0, infoGlobal, m_debugDrawer, m_dispatcher1);
                m_constraintSolver.AllSolved(infoGlobal, m_debugDrawer);
            }

            ///integrate transforms
            IntegrateTransforms(timeStep);

            UpdateAabbs();

            SynchronizeMotionStates();

            ClearForces();

            return 1;

        }

		public override Vector3 Gravity
		{
			get
			{
				return m_gravity;
			}
			set
			{
				m_gravity = value;

				foreach (CollisionObject colObj in m_collisionObjects)
				{
					RigidBody body = RigidBody.Upcast(colObj);
					
					if (body != null)
						body.Gravity = value;
				}
			}
		}

        public override void AddRigidBody(RigidBody body)
        {
            body.Gravity = m_gravity;

            if (body.GetCollisionShape() != null)
            {
                AddCollisionObject(body);
            }

        }

	    public override void RemoveRigidBody(RigidBody body)
        {
            base.RemoveCollisionObject(body);
        }

        ///removeCollisionObject will first check if it is a rigid body, if so call removeRigidBody otherwise call btCollisionWorld::removeCollisionObject
        public override void RemoveCollisionObject(CollisionObject collisionObject)
        {
	        RigidBody body = RigidBody.Upcast(collisionObject);
	        if (body != null)
            {
		        RemoveRigidBody(body);
            }
	        else
            {
		        base.RemoveCollisionObject(collisionObject);
            }
        }

	    public override void UpdateAabbs()
        {
            Matrix predictedTrans = Matrix.Identity;
	        foreach (CollisionObject colObj in m_collisionObjects)
            {
		        RigidBody body = RigidBody.Upcast(colObj);
		        if (body != null)
		        {
			        if (body.IsActive() && (!body.IsStaticObject()))
			        {
                        Vector3 minAabb = Vector3.Zero, maxAabb = Vector3.Zero;
				        colObj.GetCollisionShape().GetAabb(colObj.GetWorldTransform(), ref minAabb,ref maxAabb);
				        IBroadphaseInterface bp = GetBroadphase();
				        bp.SetAabb(body.GetBroadphaseHandle(),ref minAabb,ref maxAabb, m_dispatcher1);
			        }
		        }
	        }
        }


	    public override void SynchronizeMotionStates()
        {
	        ///@todo: iterate over awake simulation islands!
	        foreach (CollisionObject colObj in m_collisionObjects)
	        {
		        RigidBody body = RigidBody.Upcast(colObj);
		        if (body != null && body.GetMotionState() != null)
		        {
			        if (body.GetActivationState() != ActivationState.ISLAND_SLEEPING)
			        {
				        body.GetMotionState().SetWorldTransform(body.GetWorldTransform());
			        }
		        }
	        }

        }

	    public override void SetConstraintSolver(IConstraintSolver solver)
        {
            m_ownsConstraintSolver = false;
            m_constraintSolver = solver;

        }

	    public override IConstraintSolver GetConstraintSolver()
        {
            return m_constraintSolver;
        }

	    public override DynamicsWorldType GetWorldType()
	    {
		    return DynamicsWorldType.BT_SIMPLE_DYNAMICS_WORLD;
	    }

	    public override void ClearForces()
        {
	        ///@todo: iterate over awake simulation islands!
	        for ( int i=0;i<m_collisionObjects.Count;i++)
	        {
		        CollisionObject colObj = m_collisionObjects[i];
        		
		        RigidBody body = RigidBody.Upcast(colObj);
		        if (body != null)
		        {
			        body.ClearForces();
		        }
	        }

        }


        protected void PredictUnconstraintMotion(float timeStep)
        {
            foreach (CollisionObject colObj in m_collisionObjects)
            {
                RigidBody body = RigidBody.Upcast(colObj);
		        if (body != null)
		        {
			        if (!body.IsStaticObject())
			        {
				        if (body.IsActive())
				        {
					        body.ApplyGravity();
					        body.IntegrateVelocities( timeStep);
					        body.ApplyDamping(timeStep);
                            Matrix temp = body.GetInterpolationWorldTransform();
					        body.PredictIntegratedTransform(timeStep,ref temp);
                            body.SetInterpolationWorldTransform(ref temp);
				        }
			        }
		        }
	        }
        }

        protected void IntegrateTransforms(float timeStep)
        {
            Matrix predictedTrans = Matrix.Identity;
            foreach (CollisionObject colObj in m_collisionObjects)
            {
                RigidBody body = RigidBody.Upcast(colObj);
		        if (body != null)
		        {
			        if (body.IsActive() && (!body.IsStaticObject()))
			        {
				        body.PredictIntegratedTransform(timeStep, ref predictedTrans);
				        body.ProceedToTransform(ref predictedTrans);
			        }
		        }
	        }
        }

        protected IConstraintSolver m_constraintSolver;
        protected bool m_ownsConstraintSolver;
        protected Vector3 m_gravity;

    }
}
