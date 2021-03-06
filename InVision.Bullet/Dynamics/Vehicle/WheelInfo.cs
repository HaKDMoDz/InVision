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
using InVision.Bullet.Dynamics.Dynamics;
using InVision.GameMath;

namespace InVision.Bullet.Dynamics.Vehicle
{
	/// btWheelInfo contains information per wheel about friction and suspension.
    public class WheelInfo
    {
	    public WheelRaycastInfo m_raycastInfo;

        public Matrix m_worldTransform;

        public Vector3 m_chassisConnectionPointCS; //const

        public Vector3 m_wheelDirectionCS;//const
        
        public Vector3 m_wheelAxleCS; // const or modified by steering


        public float m_suspensionRestLength1;//const
        public float m_maxSuspensionTravelCm;
        public float GetSuspensionRestLength()
        {
            return m_suspensionRestLength1;
        }

        public float m_wheelsRadius;//const
        public float m_suspensionStiffness;//const
        public float m_wheelsDampingCompression;//const
        public float m_wheelsDampingRelaxation;//const
        public float m_frictionSlip;
        public float m_steering;
        public float m_rotation;
        public float m_deltaRotation;
        public float m_rollInfluence;
        public float m_maxSuspensionForce;

        public float m_engineForce;

        public float m_brake;

        public bool m_bIsFrontWheel;

        public Object m_clientInfo;//can be used to store pointer to sync transforms...

	    public WheelInfo(ref WheelInfoConstructionInfo ci)
	    {
		    m_suspensionRestLength1 = ci.m_suspensionRestLength;
		    m_maxSuspensionTravelCm = ci.m_maxSuspensionTravelCm;

		    m_wheelsRadius = ci.m_wheelRadius;
		    m_suspensionStiffness = ci.m_suspensionStiffness;
		    m_wheelsDampingCompression = ci.m_wheelsDampingCompression;
		    m_wheelsDampingRelaxation = ci.m_wheelsDampingRelaxation;
		    m_chassisConnectionPointCS = ci.m_chassisConnectionCS;
		    m_wheelDirectionCS = ci.m_wheelDirectionCS;
		    m_wheelAxleCS = ci.m_wheelAxleCS;
		    m_frictionSlip = ci.m_frictionSlip;
		    m_steering = 0f;
		    m_engineForce = 0f;
		    m_rotation = 0f;
		    m_deltaRotation = 0f;
		    m_brake = 0f;
		    m_rollInfluence = .1f;
		    m_bIsFrontWheel = ci.m_bIsFrontWheel;
            m_maxSuspensionForce = ci.m_maxSuspensionForce;

	    }

        public void UpdateWheel(RigidBody chassis, ref WheelRaycastInfo raycastInfo)
        {
	        if (m_raycastInfo.m_isInContact)
	        {
		        float project= Vector3.Dot(m_raycastInfo.m_contactNormalWS,m_raycastInfo.m_wheelDirectionWS );
		        Vector3	chassis_velocity_at_contactPoint;
		        Vector3 relpos = m_raycastInfo.m_contactPointWS - chassis.GetCenterOfMassPosition();
		        chassis_velocity_at_contactPoint = chassis.GetVelocityInLocalPoint( ref relpos );
		        float projVel = Vector3.Dot(m_raycastInfo.m_contactNormalWS,chassis_velocity_at_contactPoint );
		        if ( project >= -0.1f)
		        {
			        m_suspensionRelativeVelocity = 0f;
			        m_clippedInvContactDotSuspension = 1.0f / 0.1f;
		        }
		        else
		        {
			        float inv = -1f / project;
			        m_suspensionRelativeVelocity = projVel * inv;
			        m_clippedInvContactDotSuspension = inv;
		        }
	        }
	        else	// Not in contact : position wheel in a nice (rest length) position
	        {
		        m_raycastInfo.m_suspensionLength = this.GetSuspensionRestLength();
		        m_suspensionRelativeVelocity = 0f;
		        m_raycastInfo.m_contactNormalWS = -m_raycastInfo.m_wheelDirectionWS;
		        m_clippedInvContactDotSuspension = 1f;
	        }
        }

        public float m_clippedInvContactDotSuspension;
        public float m_suspensionRelativeVelocity;
	    //calculated by suspension
        public float m_wheelsSuspensionForce;
        public float m_skidInfo;
    }
}
