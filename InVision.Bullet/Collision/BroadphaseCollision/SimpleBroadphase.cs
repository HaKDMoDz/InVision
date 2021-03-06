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
#define CLEAN_INVALID_PAIRS

using System.Collections.Generic;
using System;
using System.Diagnostics;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Collision.BroadphaseCollision
{
    public class SimpleBroadphase : IBroadphaseInterface
    {

	    public SimpleBroadphase(int maxProxies,IOverlappingPairCache overlappingPairCache)
        {
            if (overlappingPairCache == null)
            {
                overlappingPairCache = new HashedOverlappingPairCache();
                m_ownsPairCache = true;
            }
            m_pHandles = new SimpleBroadphaseProxy[maxProxies];
            m_pairCache = overlappingPairCache;
            m_maxHandles = maxProxies;
	        m_numHandles = 0;
	        m_firstFreeHandle = 0;
	        m_LastHandleIndex = -1;
	
		    for (int i = m_firstFreeHandle; i < maxProxies; i++)
		    {
                m_pHandles[i] = new SimpleBroadphaseProxy(i);
			    m_pHandles[i].SetNextFree(i + 1);
                m_pHandles[i].m_uniqueId = ++m_proxyCounter;;//any UID will do, we just avoid too trivial values (0,1) for debugging purposes
		    }
		    m_pHandles[maxProxies - 1].SetNextFree(0);
	
        }

        public virtual void Cleanup()
        {
            for (int i = 0; i < m_pHandles.Length;++i )
            {
                if (m_pHandles[i] != null)
                {
                    m_pHandles[i].Cleanup();
                }
                m_pHandles[i] = null;
            }
            
	        if (m_ownsPairCache)
	        {
		        m_pairCache.Cleanup();
                m_pairCache = null;
		        m_ownsPairCache = false;
	        }
        }


        public static bool AabbOverlap(SimpleBroadphaseProxy proxy0, SimpleBroadphaseProxy proxy1)
        {
            Vector3 p0Min = proxy0.GetMinAABB();
            Vector3 p0Max = proxy0.GetMaxAABB();
            Vector3 p1Min = proxy1.GetMinAABB();
            Vector3 p1Max = proxy1.GetMaxAABB();

            return p0Min.X <= p1Max.X && p1Min.X <= p0Max.X &&
               p0Min.Y <= p1Max.Y && p1Min.Y <= p0Max.Y &&
               p0Min.Z <= p1Max.Z && p1Min.Z <= p0Max.Z;
        }


        private int AllocHandle()
        {
            Debug.Assert(m_numHandles < m_maxHandles);
            int freeHandle = m_firstFreeHandle;
            m_firstFreeHandle = m_pHandles[freeHandle].GetNextFree();
            m_numHandles++;
            if (freeHandle > m_LastHandleIndex)
            {
                m_LastHandleIndex = freeHandle;
            }
            return freeHandle;
        }

        private void FreeHandle(SimpleBroadphaseProxy proxy)
	    {
		    int handle = proxy.GetPosition();
		    Debug.Assert(handle >= 0 && handle < m_maxHandles);
		    if(handle == m_LastHandleIndex)
		    {
			    m_LastHandleIndex--;
		    }
		    proxy.SetNextFree(m_firstFreeHandle);
		    m_firstFreeHandle = handle;

		    proxy.m_clientObject = null;

		    m_numHandles--;
	    }


        public virtual BroadphaseProxy CreateProxy(Vector3 aabbMin, Vector3 aabbMax, BroadphaseNativeTypes shapeType, Object userPtr, CollisionFilterGroups collisionFilterGroup, CollisionFilterGroups collisionFilterMask, IDispatcher dispatcher, Object multiSapProxy)
        {
            return CreateProxy(ref aabbMin, ref aabbMax, shapeType, userPtr, collisionFilterGroup, collisionFilterMask, dispatcher, multiSapProxy);
        }

        public virtual BroadphaseProxy CreateProxy(ref Vector3 aabbMin, ref Vector3 aabbMax, BroadphaseNativeTypes shapeType, Object userPtr, CollisionFilterGroups collisionFilterGroup, CollisionFilterGroups collisionFilterMask, IDispatcher dispatcher, Object multiSapProxy)
        {
            if (m_numHandles >= m_maxHandles)
            {
                Debug.Assert(false);
                return null; //should never happen, but don't let the game crash ;-)
            }

            int position = AllocHandle();
            m_pHandles[position] = new SimpleBroadphaseProxy(position,ref aabbMin,ref aabbMax,shapeType,userPtr,collisionFilterGroup,collisionFilterMask,multiSapProxy);;
            m_pHandles[position].m_uniqueId = ++m_proxyCounter;
            return m_pHandles[position];
        }

	    public virtual void	CalculateOverlappingPairs(IDispatcher dispatcher)
        {
	        //first check for new overlapping pairs
	        if (m_numHandles > 0)
	        {
                int new_largest_index = -1;
		        for (int i=0; i <= m_LastHandleIndex; i++)
		        {
			        SimpleBroadphaseProxy proxy0 = m_pHandles[i];
			        if(proxy0.GetClientObject() == null)
			        {
				        continue;
			        }
                    new_largest_index = i;
                    for (int j=i+1; j <= m_LastHandleIndex; j++)
			        {
				        SimpleBroadphaseProxy proxy1 = m_pHandles[j];
                        //btAssert(proxy0 != proxy1);
				        if(proxy1.GetClientObject() == null)
				        {
					        continue;
				        }

				        if (AabbOverlap(proxy0,proxy1))
				        {
					        if (m_pairCache.FindPair(proxy0,proxy1) == null)
					        {
						        m_pairCache.AddOverlappingPair(proxy0,proxy1);
					        }
				        } 
                        else
				        {
					        if (!m_pairCache.HasDeferredRemoval())
					        {
						        if ( m_pairCache.FindPair(proxy0,proxy1) != null)
						        {
							        m_pairCache.RemoveOverlappingPair(proxy0,proxy1,dispatcher);
						        }
					        }
				        }
			        }
		        }

		        m_LastHandleIndex = new_largest_index;

		        if (m_ownsPairCache && m_pairCache.HasDeferredRemoval())
		        {
			        IList<BroadphasePair> overlappingPairArray = m_pairCache.GetOverlappingPairArray();

			        //perform a sort, to find duplicates and to sort 'invalid' pairs to the end
					((List<BroadphasePair>)overlappingPairArray).Sort();

					//overlappingPairArray.Capacity = (overlappingPairArray.Count - m_invalidPair);
			        m_invalidPair = 0;

			        BroadphasePair previousPair = new BroadphasePair();

			        for (int i=0;i<overlappingPairArray.Count;i++)
			        {

				        BroadphasePair pair = overlappingPairArray[i];

				        bool isDuplicate = (pair == previousPair);

				        previousPair = pair;

				        bool needsRemoval = false;

				        if (!isDuplicate)
				        {
					        bool hasOverlap = TestAabbOverlap(pair.m_pProxy0,pair.m_pProxy1);

					        if (hasOverlap)
					        {
						        needsRemoval = false;//callback->processOverlap(pair);
					        } 
                            else
					        {
						        needsRemoval = true;
					        }
				        } 
                        else
				        {
					        //remove duplicate
					        needsRemoval = true;
					        //should have no algorithm
                            //btAssert(!pair.m_algorithm);
				        }

				        if (needsRemoval)
				        {
					        m_pairCache.CleanOverlappingPair(pair,dispatcher);

					        //		m_overlappingPairArray.swap(i,m_overlappingPairArray.size()-1);
					        //		m_overlappingPairArray.pop_back();
					        pair.m_pProxy0 = null;
					        pair.m_pProxy1 = null;
					        m_invalidPair++;
                            BulletGlobals.gOverlappingPairs--;
				        } 

			        }

			        ///if you don't like to skip the invalid pairs in the array, execute following code:
        #if CLEAN_INVALID_PAIRS

			        //perform a sort, to sort 'invalid' pairs to the end
                    //overlappingPairArray.quickSort(new BroadphasePairSortPredicate());
					((List<BroadphasePair>)overlappingPairArray).Sort();
					//overlappingPairArray.Capacity = overlappingPairArray.Count - m_invalidPair;
			        m_invalidPair = 0;
        #endif//CLEAN_INVALID_PAIRS

		        }
	        }
        }

	    public virtual void	DestroyProxy(BroadphaseProxy proxy,IDispatcher dispatcher)
        {
		    SimpleBroadphaseProxy proxy0 = (SimpleBroadphaseProxy)(proxy);
            //freeHandle(proxy0);
		    m_pairCache.RemoveOverlappingPairsContainingProxy(proxy,dispatcher);
        }
	    
        public virtual void	SetAabb(BroadphaseProxy proxy,ref Vector3 aabbMin,ref Vector3 aabbMax, IDispatcher dispatcher)
        {
	        SimpleBroadphaseProxy sbp = GetSimpleProxyFromProxy(proxy);
	        sbp.SetMinAABB(ref aabbMin);
	        sbp.SetMaxAABB(ref aabbMax);
        }

	    public virtual void	GetAabb(BroadphaseProxy proxy,ref Vector3 aabbMin, ref Vector3 aabbMax)
        {
	        SimpleBroadphaseProxy sbp = GetSimpleProxyFromProxy(proxy);
	        aabbMin = sbp.GetMinAABB();
	        aabbMax = sbp.GetMaxAABB();
        }

        public virtual void RayTest(ref Vector3 rayFrom, ref Vector3 rayTo, BroadphaseRayCallback rayCallback)
        {
            Vector3 min = MathUtil.MIN_VECTOR;
            Vector3 max = MathUtil.MAX_VECTOR;
            RayTest(ref rayFrom, ref rayTo, rayCallback, ref min, ref max);
        }


	    public virtual void RayTest(ref Vector3 rayFrom,ref Vector3 rayTo, BroadphaseRayCallback rayCallback, ref Vector3 aabbMin,ref Vector3 aabbMax)
        {
            for (int i = 0; i <= m_LastHandleIndex; i++)
            {
                SimpleBroadphaseProxy proxy = m_pHandles[i];
                if (proxy.m_clientObject == null)
                {
                    continue;
                }
                rayCallback.Process(proxy);
            }
        }

    	
        public virtual void AabbTest(ref Vector3 aabbMin, ref Vector3 aabbMax, IBroadphaseAabbCallback callback)
        {
	        for (int i=0; i <= m_LastHandleIndex; i++)
	        {
		        SimpleBroadphaseProxy proxy = m_pHandles[i];
		        if(proxy.m_clientObject == null)
		        {
			        continue;
		        }
		        if (AabbUtil2.TestAabbAgainstAabb2(ref aabbMin,ref aabbMax,ref proxy.m_aabbMin,ref proxy.m_aabbMax))
		        {
			        callback.Process(proxy);
		        }
	        }
        }

	

	    public IOverlappingPairCache GetOverlappingPairCache()
	    {
		    return m_pairCache;
	    }

	    public bool	TestAabbOverlap(BroadphaseProxy proxy0,BroadphaseProxy proxy1)
        {
            return AabbOverlap((SimpleBroadphaseProxy)proxy0, (SimpleBroadphaseProxy)proxy1);
        }


	    ///getAabb returns the axis aligned bounding box in the 'global' coordinate frame
	    ///will add some transform later
	    public virtual void GetBroadphaseAabb(ref Vector3 aabbMin,ref Vector3 aabbMax)
	    {
		    aabbMin = MathUtil.MIN_VECTOR;
		    aabbMax = MathUtil.MAX_VECTOR;
	    }

	    public virtual void	PrintStats()
	    {
    //		printf("btSimpleBroadphase.h\n");
    //		printf("numHandles = %d, maxHandles = %d\n",m_numHandles,m_maxHandles);
	    }


        protected SimpleBroadphaseProxy GetSimpleProxyFromProxy(BroadphaseProxy proxy)
	    {
		    SimpleBroadphaseProxy proxy0 = (SimpleBroadphaseProxy)(proxy);
		    return proxy0;
	    }

        ///reset broadphase internal structures, to ensure determinism/reproducability
        public virtual void ResetPool(IDispatcher dispatcher)
        {
            // not yet
        }

        protected void Validate()
        {
            for (int i = 0; i < m_numHandles; i++)
            {
                for (int j = i + 1; j < m_numHandles; j++)
                {
                    Debug.Assert(m_pHandles[i] != m_pHandles[j]);
                }
            }
        }

        private int m_proxyCounter = 2;

        private IOverlappingPairCache m_pairCache;
        private bool m_ownsPairCache;

        int m_invalidPair;


	    protected int m_numHandles;						// number of active handles
        protected int m_maxHandles;						// max number of handles
        protected int m_LastHandleIndex;

        protected SimpleBroadphaseProxy[] m_pHandles;						// handles pool

        protected Object m_pHandlesRawPtr;
        protected int m_firstFreeHandle;		// free handles list
     
    }

    //-------------------------------------------------------------------------------------------------

	//-------------------------------------------------------------------------------------------------

	//-------------------------------------------------------------------------------------------------

	//-------------------------------------------------------------------------------------------------

    //then remove non-overlapping ones
	//-------------------------------------------------------------------------------------------------

}
