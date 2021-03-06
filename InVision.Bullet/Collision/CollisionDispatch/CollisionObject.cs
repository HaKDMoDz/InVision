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
using InVision.Bullet.Collision.BroadphaseCollision;
using InVision.Bullet.Collision.CollisionShapes;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Collision.CollisionDispatch
{
	public class CollisionObject
    {

        public CollisionObject()
        {
            m_anisotropicFriction = new Vector3(1f,1f,1f);
            m_hasAnisotropicFriction = false;
            m_contactProcessingThreshold = MathUtil.BT_LARGE_FLOAT;
            m_broadphaseHandle = null;
            m_collisionShape = null;
            m_rootCollisionShape = null;
            m_collisionFlags = CollisionFlags.CF_STATIC_OBJECT;
            m_islandTag1 = -1;
            m_companionId = -1;
            m_activationState1 = ActivationState.ACTIVE_TAG;
            m_deactivationTime = 0f;
            m_friction = 0.5f;
            m_userObjectPointer = null;
            m_internalType = CollisionObjectTypes.CO_COLLISION_OBJECT;
            m_hitFraction = 1f;
            m_ccdSweptSphereRadius = 0f;
            m_ccdMotionThreshold = 0f;
            m_checkCollideWith = false;
            m_worldTransform = Matrix.Identity;
        }

        public virtual bool CheckCollideWithOverride(CollisionObject obj)
        {
            return true;
        }
        


        public bool MergesSimulationIslands() 
	    {
            CollisionFlags collisionMask = CollisionFlags.CF_STATIC_OBJECT | CollisionFlags.CF_KINEMATIC_OBJECT | CollisionFlags.CF_NO_CONTACT_RESPONSE;
		    ///static objects, kinematic and object without contact response don't merge islands
            return ((m_collisionFlags & collisionMask) == 0);
	    }



        public Vector3 GetAnisotropicFriction()
        {
            return m_anisotropicFriction;
        }



        public void SetAnisotropicFriction(ref Vector3 anisotropicFriction)
        {
            m_anisotropicFriction = anisotropicFriction;
		    m_hasAnisotropicFriction = (anisotropicFriction.X!=1f) || (anisotropicFriction.Y!=1f) || (anisotropicFriction.Z!=1f);
        }
    


        public bool	HasAnisotropicFriction() 
        {
            return m_hasAnisotropicFriction;
        }



	    public void	SetContactProcessingThreshold(float contactProcessingThreshold)
	    {
		    m_contactProcessingThreshold = contactProcessingThreshold;
	    }


	    
        public float GetContactProcessingThreshold()
	    {
		    return m_contactProcessingThreshold;
	    }



	    public bool	IsStaticObject() 
        {
		    return (m_collisionFlags & CollisionFlags.CF_STATIC_OBJECT) != 0;
	    }



	    public bool IsKinematicObject() 
	    {
		    return (m_collisionFlags & CollisionFlags.CF_KINEMATIC_OBJECT) != 0;
	    }



        public bool IsStaticOrKinematicObject()
	    {
		    return (m_collisionFlags & (CollisionFlags.CF_KINEMATIC_OBJECT | CollisionFlags.CF_STATIC_OBJECT)) != 0 ;
	    }



        public bool	HasContactResponse() 
        {
		    return (m_collisionFlags & CollisionFlags.CF_NO_CONTACT_RESPONSE)==0;
	    }
            


        public virtual void	SetCollisionShape(CollisionShape collisionShape)
	    {
		    m_collisionShape = collisionShape;
		    m_rootCollisionShape = collisionShape;
	    }



	    public CollisionShape GetCollisionShape()
	    {
		    return m_collisionShape;
	    }


        
        public CollisionShape GetRootCollisionShape()
	    {
		    return m_rootCollisionShape;
	    }


	    ///Avoid using this internal API call
	    ///internalSetTemporaryCollisionShape is used to temporary replace the actual collision shape by a child collision shape.
	    public void InternalSetTemporaryCollisionShape(CollisionShape collisionShape)
	    {
		    m_collisionShape = collisionShape;
	    }



	    public ActivationState GetActivationState() 
        { 
            return m_activationState1;
        }
        


	    public void SetActivationState(ActivationState newState)
        {
            if ((m_activationState1 != ActivationState.DISABLE_DEACTIVATION) && (m_activationState1 != ActivationState.DISABLE_SIMULATION))
            {
                m_activationState1 = newState;
            }
        }



        public void	SetDeactivationTime(float time)
	    {
		    m_deactivationTime = time;
	    }



	    public float GetDeactivationTime()
	    {
		    return m_deactivationTime;
	    }



	    public void ForceActivationState(ActivationState newState)
        {
            m_activationState1 = newState;
        }


        
        public void Activate()
        {
            Activate(false);
        }
	    public void	Activate(bool forceActivation)
        {
            CollisionFlags collMask = CollisionFlags.CF_STATIC_OBJECT | CollisionFlags.CF_KINEMATIC_OBJECT;
	        if (forceActivation || ((m_collisionFlags & collMask) == 0))
	        {
		        SetActivationState(ActivationState.ACTIVE_TAG);
                m_deactivationTime = 0f;
	        }
        }



	    public bool IsActive() 
	    {
		    return ((GetActivationState() != ActivationState.ISLAND_SLEEPING) && (GetActivationState() != ActivationState.DISABLE_SIMULATION));
	    }



	    public void SetRestitution(float rest)
	    {
		    m_restitution = rest;
	    }



	    public float GetRestitution()
	    {
		    return m_restitution;
	    }



	    public void SetFriction(float frict)
	    {
		    m_friction = frict;
	    }


	    
        public float GetFriction()
	    {
		    return m_friction;
	    }



	    ///reserved for Bullet internal usage
        public CollisionObjectTypes GetInternalType()
	    {
		    return m_internalType;
	    }



        public void SetInternalType(CollisionObjectTypes types)
        {
            m_internalType = types;
        }


        
        public Matrix GetWorldTransform()
	    {
		    return m_worldTransform;
	    }



	    public void SetWorldTransform(ref Matrix worldTrans)
	    {
		    m_worldTransform = worldTrans;
	    }



	    public BroadphaseProxy GetBroadphaseHandle()
	    {
		    return m_broadphaseHandle;
	    }



        public void	SetBroadphaseHandle(BroadphaseProxy handle)
	    {
		    m_broadphaseHandle = handle;
	    }



	    public Matrix GetInterpolationWorldTransform() 
	    {
		    return m_interpolationWorldTransform;
	    }



        public void	SetInterpolationWorldTransform(ref Matrix trans)
	    {
		    m_interpolationWorldTransform = trans;
	    }



	    public void SetInterpolationLinearVelocity(ref Vector3 linvel)
	    {
		    m_interpolationLinearVelocity = linvel;
	    }

	    public void	SetInterpolationAngularVelocity(ref Vector3 angvel)
	    {
		    m_interpolationAngularVelocity = angvel;
	    }



	    public Vector3	SetInterpolationLinearVelocity() 
	    {
		    return m_interpolationLinearVelocity;
	    }



	    public Vector3 GetInterpolationAngularVelocity()
	    {
		    return m_interpolationAngularVelocity;
	    }

        public Vector3 GetInterpolationLinearVelocity()
        {
            return m_interpolationLinearVelocity;
        }


	    public int GetIslandTag() 
	    {
		    return	m_islandTag1;
	    }


	    public void	SetIslandTag(int tag)
	    {
		    m_islandTag1 = tag;
	    }
        


        public int GetCompanionId() 
	    {
		    return	m_companionId;
	    }



	    public void	SetCompanionId(int id)
	    {
		    m_companionId = id;
	    }



	    public float GetHitFraction()
	    {
		    return m_hitFraction; 
	    }



	    public void	SetHitFraction(float hitFraction)
	    {
		    m_hitFraction = hitFraction;
	    }


	
	    public CollisionFlags GetCollisionFlags() 
	    {
		    return m_collisionFlags;
	    }



        public void SetCollisionFlags(CollisionFlags flags)
	    {
		    m_collisionFlags = flags;
	    }



	    ///Swept sphere radius (0.0 by default), see btConvexConvexAlgorithm::
	    public float GetCcdSweptSphereRadius()
        {
		    return m_ccdSweptSphereRadius;
	    }



	    ///Swept sphere radius (0.0 by default), see btConvexConvexAlgorithm::
	    public void	SetCcdSweptSphereRadius(float radius)
	    {
		    m_ccdSweptSphereRadius = radius;
	    }



	    public float GetCcdMotionThreshold()
	    {
		    return m_ccdMotionThreshold;
	    }



	    public float GetCcdSquareMotionThreshold() 
	    {
		    return m_ccdMotionThreshold*m_ccdMotionThreshold;
	    }



	    /// Don't do continuous collision detection if the motion (in one step) is less then m_ccdMotionThreshold
	    public void	SetCcdMotionThreshold(float ccdMotionThreshold)
	    {
		    m_ccdMotionThreshold = ccdMotionThreshold;
	    }



	    ///users can point to their objects, userPointer is not used by Bullet
	    public Object GetUserPointer() 
	    {
		    return m_userObjectPointer;
	    }
    	
	    ///users can point to their objects, userPointer is not used by Bullet
	    public void SetUserPointer(Object userPointer)
	    {
		    m_userObjectPointer = userPointer;
	    }



	    public bool CheckCollideWith(CollisionObject co)
	    {
		    if (m_checkCollideWith)
            {
			    return CheckCollideWithOverride(co);
            }
		    return true;
	    }



        public virtual void Cleanup()
        {
        }



        
        protected Matrix m_worldTransform= Matrix.Identity;
        protected Matrix m_interpolationWorldTransform= Matrix.Identity; 
        protected Vector3 m_interpolationAngularVelocity;
        protected Vector3 m_interpolationLinearVelocity;
        protected Vector3 m_anisotropicFriction;
        protected bool m_hasAnisotropicFriction;
        protected float m_contactProcessingThreshold;
        protected BroadphaseProxy m_broadphaseHandle;
        protected CollisionShape m_collisionShape;
        protected CollisionShape m_rootCollisionShape;
        protected CollisionFlags m_collisionFlags;
        protected int m_islandTag1;
        protected int m_companionId;
        protected ActivationState m_activationState1;
        protected float m_deactivationTime;
        protected float m_friction;
        protected float m_restitution;
        protected Object m_userObjectPointer;
        protected CollisionObjectTypes m_internalType;
        protected float m_hitFraction;
        protected float m_ccdSweptSphereRadius;
        protected float m_ccdMotionThreshold;
        protected bool m_checkCollideWith;
    }
}
