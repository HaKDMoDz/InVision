using System;
using System.Collections.Generic;
using System.Diagnostics;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Collision.BroadphaseCollision
{
	public class AxisSweep3Internal : IBroadphaseInterface
	{
		protected int m_bpHandleMask;
		protected ushort m_handleSentinel;
		protected Vector3 m_worldAabbMin;						// overall system bounds
		protected Vector3 m_worldAabbMax;						// overall system bounds
		protected Vector3 m_quantize;						// scaling factor for quantization

		protected ushort m_numHandles;						// number of active handles
		protected ushort m_maxHandles;						// max number of handles
		protected Handle[] m_pHandles;						// handles pool
		protected ushort m_firstFreeHandle;		// free handles list

		protected Edge[,] m_pEdges = null; // edge arrays for the 3 axes (each array has m_maxHandles * 2 + 2 sentinel entries)
		//protected Object[] m_pEdgesRawPtr = new Object[3];

		protected IOverlappingPairCache m_pairCache;

		///btOverlappingPairCallback is an additional optional user callback for adding/removing overlapping pairs, similar interface to btOverlappingPairCache.
		protected IOverlappingPairCallback m_userPairCallback;
	
		protected bool	m_ownsPairCache;

		protected int	m_invalidPair;

		///additional dynamic aabb structure, used to accelerate ray cast queries.
		///can be disabled using a optional argument in the constructor
		protected DbvtBroadphase m_raycastAccelerator;
		protected IOverlappingPairCache	m_nullPairCache;

		// allocation/deallocation
		protected ushort AllocHandle()
		{
			Debug.Assert(m_firstFreeHandle!=0);

			ushort handle = m_firstFreeHandle;
			m_firstFreeHandle = GetHandle(handle).GetNextFree();
			m_numHandles++;
			return handle;

		}
		protected void FreeHandle(ushort handle)
		{
			Debug.Assert(handle > 0 && handle < m_maxHandles);

			GetHandle(handle).SetNextFree(m_firstFreeHandle);
			m_firstFreeHandle = handle;
			m_numHandles--;
		}


		protected bool TestOverlap2D(Handle pHandleA, Handle pHandleB, int axis0, int axis1)
		{
			//optimization 1: check the array index (memory address), instead of the m_pos

			if (pHandleA.m_maxEdges[axis0] < pHandleB.m_minEdges[axis0] ||
			    pHandleB.m_maxEdges[axis0] < pHandleA.m_minEdges[axis0] ||
			    pHandleA.m_maxEdges[axis1] < pHandleB.m_minEdges[axis1] ||
			    pHandleB.m_maxEdges[axis1] < pHandleA.m_minEdges[axis1])
			{
				return false;
			}
			return true;


		}

#if DEBUG_BROADPHASE
	    protected void debugPrintAxis(int axis)
        {
            debugPrintAxis(axis,true);
        }
	    protected void debugPrintAxis(int axis,bool checkCardinality)
        {
	        int numEdges = m_pHandles[0].m_maxEdges[axis];
            if (numEdges == 5)
            {
                int ibreak = 0;
            }

            System.Console.Out.WriteLine("SAP Axis {0}, numEdges={1}", axis, numEdges);
            if (numEdges == 3)
            {
                int ibreak = 0;
            }
	        for (int i=0;i<numEdges+1;i++)
	        {
		        Edge pEdge = m_pEdges[axis,i];
		        Handle pHandlePrev = getHandle(pEdge.m_handle);
		        int handleIndex = pEdge.IsMax()? pHandlePrev.m_maxEdges[axis] : pHandlePrev.m_minEdges[axis];
		        char beginOrEnd;
		        beginOrEnd=pEdge.IsMax()?'E':'B';
                System.Console.Out.WriteLine("[{0},h={1},p={2:x},i={3}]", beginOrEnd, pEdge.m_handle, pEdge.m_pos, handleIndex);
                if (pEdge.m_pos == 0x7fd4)
                {
                    int ibreak = 0;
                }
	        }

	        if (checkCardinality)
            {
                Debug.Assert(numEdges == m_numHandles*2+1);
            }

        }
#endif //DEBUG_BROADPHASE

		//Overlap* AddOverlap(int handleA, int handleB);
		//void RemoveOverlap(int handleA, int handleB);

		protected void SortMinDown(int axis, ushort edge, IDispatcher dispatcher, bool updateOverlaps)
		{
			int edgeIndex = edge;
			int prevIndex = edgeIndex - 1;
			Edge pEdge = m_pEdges[axis,edgeIndex];
			Edge pPrev = m_pEdges[axis,prevIndex];
			Handle pHandleEdge = GetHandle(pEdge.m_handle);

			while (pEdge.m_pos < pPrev.m_pos)
			{
				Handle pHandlePrev = GetHandle(pPrev.m_handle);

				if (pPrev.IsMax())
				{
					// if previous edge is a maximum check the bounds and add an overlap if necessary
					int axis1 = (1 << axis) & 3;
					int axis2 = (1 << axis1) & 3;
					if (updateOverlaps && TestOverlap2D(pHandleEdge, pHandlePrev, axis1, axis2))
					{
						m_pairCache.AddOverlappingPair(pHandleEdge, pHandlePrev);
						if (m_userPairCallback != null)
						{
							m_userPairCallback.AddOverlappingPair(pHandleEdge, pHandlePrev);
						}

						//AddOverlap(pEdge.m_handle, pPrev.m_handle);

					}

					// update edge reference in other handle
					pHandlePrev.m_maxEdges[axis]++;
				}
				else
				{
					pHandlePrev.m_minEdges[axis]++;
				}

				pHandleEdge.m_minEdges[axis]--;

				SanityCheckHandle(pHandleEdge, axis);

				Edge.Swap(pEdge, pPrev);

				// decrement
				edgeIndex--;
				prevIndex--;
				pEdge = m_pEdges[axis,edgeIndex];
				pPrev = m_pEdges[axis,prevIndex];
			}

#if DEBUG_BROADPHASE
	        debugPrintAxis(axis);
#endif //DEBUG_BROADPHASE



		}
		protected void SortMinUp(int axis, ushort edge, IDispatcher dispatcher, bool updateOverlaps)
		{
			int edgeIndex = edge;
			int nextIndex = edgeIndex + 1;
			Edge pEdge = m_pEdges[axis,edgeIndex];
			Edge pNext = m_pEdges[axis,nextIndex];

			Handle pHandleEdge = GetHandle(pEdge.m_handle);

			while (pNext.m_handle != 0 && (pEdge.m_pos >= pNext.m_pos))
			{
				Handle pHandleNext = GetHandle(pNext.m_handle);

				if (pNext.IsMax())
				{
					Handle handle0 = GetHandle(pEdge.m_handle);
					Handle handle1 = GetHandle(pNext.m_handle);
					int axis1 = (1 << axis) & 3;
					int axis2 = (1 << axis1) & 3;

					// if next edge is maximum remove any overlap between the two handles
					if (updateOverlaps
#if USE_OVERLAP_TEST_ON_REMOVES
					    && TestOverlap2D(handle0,handle1,axis1,axis2)
#endif //USE_OVERLAP_TEST_ON_REMOVES
						)
					{


						m_pairCache.RemoveOverlappingPair(handle0, handle1, dispatcher);
						if (m_userPairCallback != null)
						{
							m_userPairCallback.RemoveOverlappingPair(handle0, handle1, dispatcher);
						}
					}

					// update edge reference in other handle
					pHandleNext.m_maxEdges[axis]--;
				}
				else
				{
					pHandleNext.m_minEdges[axis]--;
				}

				pHandleEdge.m_minEdges[axis]++;

				SanityCheckHandle(pHandleEdge,axis);

				Edge.Swap(pEdge, pNext);
                
				// increment
				edgeIndex++;
				nextIndex++;
				pEdge = m_pEdges[axis,edgeIndex];
				pNext = m_pEdges[axis,nextIndex];
			}

		}

		private void SanityCheckHandle(Handle handle,int axis)
		{
			if (handle.m_minEdges[axis] == ushort.MaxValue)
			{
				int ibreak = 0;
			}

		}

		protected void SortMaxDown(int axis, ushort edge, IDispatcher dispatcher, bool updateOverlaps)
		{
			int edgeIndex = edge;
			int prevIndex = edgeIndex - 1;
			Edge pEdge = m_pEdges[axis,edgeIndex];
			Edge pPrev = m_pEdges[axis,prevIndex];
			Handle pHandleEdge = GetHandle(pEdge.m_handle);

			while (pEdge.m_pos < pPrev.m_pos)
			{
				Handle pHandlePrev = GetHandle(pPrev.m_handle);

				if (!pPrev.IsMax())
				{
					// if previous edge was a minimum remove any overlap between the two handles
					Handle handle0 = GetHandle(pEdge.m_handle);
					Handle handle1 = GetHandle(pPrev.m_handle);
					int axis1 = (1  << axis) & 3;
					int axis2 = (1  << axis1) & 3;

					if (updateOverlaps  
#if USE_OVERLAP_TEST_ON_REMOVES
					    && TestOverlap2D(handle0,handle1,axis1,axis2)
#endif //USE_OVERLAP_TEST_ON_REMOVES
						)
					{
						//this is done during the overlappingpairarray iteration/narrowphase collision

        				
						m_pairCache.RemoveOverlappingPair(handle0,handle1,dispatcher);
						if (m_userPairCallback != null)
						{
							m_userPairCallback.RemoveOverlappingPair(handle0,handle1,dispatcher);
						}
					}

					// update edge reference in other handle
					pHandlePrev.m_minEdges[axis]++;;
				}
				else
					pHandlePrev.m_maxEdges[axis]++;

				pHandleEdge.m_maxEdges[axis]--;

				SanityCheckHandle(pHandleEdge, axis);

				Edge.Swap(pEdge, pPrev);

				// decrement
				edgeIndex--;
				prevIndex--;
                
				pEdge = m_pEdges[axis,edgeIndex];
				pPrev = m_pEdges[axis,prevIndex];
			}

        	
#if DEBUG_BROADPHASE
	        debugPrintAxis(axis);
#endif //DEBUG_BROADPHASE


		}
		protected void SortMaxUp(int axis, ushort edge, IDispatcher dispatcher, bool updateOverlaps)
		{
			int edgeIndex = edge;
			int nextIndex = edgeIndex + 1;
			Edge pEdge = m_pEdges[axis,edgeIndex];
			Edge pNext = m_pEdges[axis,nextIndex];
			Handle pHandleEdge = GetHandle(pEdge.m_handle);

			while (pNext.m_handle != 0 && (pEdge.m_pos >= pNext.m_pos))
			{
				Handle pHandleNext = GetHandle(pNext.m_handle);

				int axis1 = (1 << axis) & 3;
				int axis2 = (1 << axis1) & 3;

				if (!pNext.IsMax())
				{
					// if next edge is a minimum check the bounds and add an overlap if necessary
					if (updateOverlaps && TestOverlap2D(pHandleEdge, pHandleNext, axis1, axis2))
					{
						Handle handle0 = GetHandle(pEdge.m_handle);
						Handle handle1 = GetHandle(pNext.m_handle);
						m_pairCache.AddOverlappingPair(handle0, handle1);
						if (m_userPairCallback != null)
						{
							m_userPairCallback.AddOverlappingPair(handle0, handle1);
						}
					}

					// update edge reference in other handle
					pHandleNext.m_minEdges[axis]--;
				}
				else
				{
					pHandleNext.m_maxEdges[axis]--;
				}

				pHandleEdge.m_maxEdges[axis]++;

				SanityCheckHandle(pHandleEdge, axis);

				Edge.Swap(pEdge, pNext);

				// increment
				edgeIndex++;
				nextIndex++;
				pEdge = m_pEdges[axis,edgeIndex];
				pNext = m_pEdges[axis,nextIndex];
			}

		}


		//public AxisSweep3Internal(ref Vector3 worldAabbMin,ref Vector3 worldAabbMax, int handleMask, int handleSentinel, int maxHandles = 16384, OverlappingPairCache* pairCache=0,bool disableRaycastAccelerator = false);
		public AxisSweep3Internal(ref Vector3 worldAabbMin, ref Vector3 worldAabbMax, int handleMask, ushort handleSentinel, ushort userMaxHandles, IOverlappingPairCache pairCache, bool disableRaycastAccelerator)
		{
			m_bpHandleMask = (handleMask);
			m_handleSentinel = (handleSentinel);
			m_pairCache = (pairCache);
			m_userPairCallback = null;
			m_ownsPairCache = (false);
			m_invalidPair = 0;
			m_raycastAccelerator = null;
			ushort maxHandles = (ushort)(userMaxHandles+1);//need to add one sentinel handle

			if (m_pairCache == null)
			{
				m_pairCache = new HashedOverlappingPairCache();
				m_ownsPairCache = true;
			}

			if (!disableRaycastAccelerator)
			{
				m_nullPairCache = new NullPairCache();
				m_raycastAccelerator = new DbvtBroadphase(m_nullPairCache);//m_pairCache);
				m_raycastAccelerator.m_deferedcollide = true;//don't add/remove pairs
			}

			//btAssert(bounds.HasVolume());

			// init bounds
			m_worldAabbMin = worldAabbMin;
			m_worldAabbMax = worldAabbMax;

			Vector3 aabbSize = m_worldAabbMax - m_worldAabbMin;

			int maxInt = m_handleSentinel;

			m_quantize = new Vector3((float)maxInt,(float)maxInt,(float)maxInt) / aabbSize;

			// allocate handles buffer, using btAlignedAlloc, and put all handles on free list
			m_pHandles = new Handle[maxHandles];
			for (int i = 0; i < m_pHandles.Length; ++i)
			{
				m_pHandles[i] = new Handle();
			}

			m_maxHandles = maxHandles;
			m_numHandles = 0;

			// handle 0 is reserved as the null index, and is also used as the sentinel
			m_firstFreeHandle = 1;
			{
				for (ushort i = m_firstFreeHandle; i < maxHandles; i++)
				{
					ushort nextFree = (ushort)(i + (ushort)1);
					m_pHandles[i].SetNextFree(nextFree);
				}
				m_pHandles[maxHandles - 1].SetNextFree(0);
			}

			{
				m_pEdges = new Edge[3, (maxHandles * 2)];
				// allocate edge buffers
				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < maxHandles * 2; ++j)
					{
						m_pEdges[i,j] = new Edge();
					}
				}
			}
			//removed overlap management

			// make boundary sentinels
    	
			m_pHandles[0].SetClientObject(null);

			for (int axis = 0; axis < 3; axis++)
			{
				m_pHandles[0].m_minEdges[axis] = 0;
				m_pHandles[0].m_maxEdges[axis] = 1;

				m_pEdges[axis,0].m_pos = 0;
				m_pEdges[axis,0].m_handle = 0;
				m_pEdges[axis,1].m_pos = m_handleSentinel;
				m_pEdges[axis,1].m_handle = 0;

		    
#if DEBUG_BROADPHASE
		    debugPrintAxis(axis);
#endif //DEBUG_BROADPHASE

			}
		}

		public virtual void Cleanup()
		{
			if (m_raycastAccelerator != null)
			{
				m_nullPairCache.Cleanup();
				m_nullPairCache = null;
				m_raycastAccelerator.Cleanup();
				m_raycastAccelerator = null;
			}

			for (int i = 0; i < m_pHandles.Length; ++i)
			{
				m_pHandles[i].Cleanup();
			}
			m_pHandles = null;
			if (m_ownsPairCache)
			{
				m_pairCache = null;
			}
		}

		public ushort GetNumHandles() 
		{
			return m_numHandles;
		}

		public virtual void CalculateOverlappingPairs(IDispatcher dispatcher)
		{
			if (m_pairCache.HasDeferredRemoval())
			{
    	
				IList<BroadphasePair> overlappingPairArray = m_pairCache.GetOverlappingPairArray();

				//perform a sort, to find duplicates and to sort 'invalid' pairs to the end
				((List<BroadphasePair>)overlappingPairArray).Sort();
				//overlappingPairArray.resize(overlappingPairArray.size() - m_invalidPair);
				m_invalidPair = 0;

				BroadphasePair previousPair = new BroadphasePair();
    		
				for (int i=0;i<overlappingPairArray.Count;i++)
				{
    		
					BroadphasePair pair = overlappingPairArray[i];

					bool isDuplicate = pair.Equals(previousPair);

					// MAN - not sure if this should be a deep copy or not... 
					previousPair = pair;

					bool needsRemoval = false;

					if (!isDuplicate)
					{
						///important to use an AABB test that is consistent with the broadphase
						bool hasOverlap = TestAabbOverlap(pair.m_pProxy0,pair.m_pProxy1);

						if (hasOverlap)
						{
							needsRemoval = false;//callback.processOverlap(pair);
						} else
						{
							needsRemoval = true;
						}
					} else
					{
						//remove duplicate
						needsRemoval = true;
						//should have no algorithm
						Debug.Assert(pair.m_algorithm == null);
					}   
    			
					if (needsRemoval)
					{
						m_pairCache.CleanOverlappingPair(pair,dispatcher);

						//		m_overlappingPairArray.swap(i,m_overlappingPairArray.size()-1);
						//		m_overlappingPairArray.pop_back();
						pair.m_pProxy0 = null;
						pair.m_pProxy1 = null;
						m_invalidPair++;
						OverlappingPairCacheGlobals.gOverlappingPairs--;
					} 
    	    		
				}

				///if you don't like to skip the invalid pairs in the array, execute following code:
#if CLEAN_INVALID_PAIRS

				//perform a sort, to sort 'invalid' pairs to the end
				((List<BroadphasePair>)overlappingPairArray).Sort();

				//overlappingPairArray.Capacity = (overlappingPairArray.Count - m_invalidPair);
				m_invalidPair = 0;
#endif//CLEAN_INVALID_PAIRS
    		
				//printf("overlappingPairArray.size()=%d\n",overlappingPairArray.size());
			}


		}

		public ushort AddHandle(ref Vector3 aabbMin, ref Vector3 aabbMax, Object pOwner, CollisionFilterGroups collisionFilterGroup, CollisionFilterGroups collisionFilterMask, IDispatcher dispatcher, Object multiSapProxy)
		{
			// quantize the bounds
			ushort[] min = new ushort[3], max = new ushort[3];
			Quantize(min, ref aabbMin, 0);
			Quantize(max, ref aabbMax, 1);

			// allocate a handle
			ushort handle = AllocHandle();
    	

			Handle pHandle = GetHandle(handle);
    	
			pHandle.m_uniqueId = handle;
			//pHandle.m_pOverlaps = 0;
			pHandle.m_clientObject = pOwner;
			pHandle.m_collisionFilterGroup = collisionFilterGroup;
			pHandle.m_collisionFilterMask = collisionFilterMask;
			pHandle.m_multiSapParentProxy = multiSapProxy;

			// compute current limit of edge arrays
			ushort limit = (ushort)(m_numHandles * 2);

			// insert new edges just inside the max boundary edge
			for (int axis = 0; axis < 3; axis++)
			{

				m_pHandles[0].m_maxEdges[axis] += 2;

				m_pEdges[axis,limit + 1].Copy(m_pEdges[axis,limit - 1]);

				m_pEdges[axis,limit - 1].m_pos = min[axis];
				m_pEdges[axis,limit - 1].m_handle = handle;

				m_pEdges[axis,limit].m_pos = max[axis];
				m_pEdges[axis,limit].m_handle = handle;


				Edge limitEdge = m_pEdges[axis, limit];
				Edge limitEdgeMinus = m_pEdges[axis, limit-1];
				Edge limitEdgePlus = m_pEdges[axis, limit+1];

				pHandle.m_minEdges[axis] = (ushort)(limit - 1);
				pHandle.m_maxEdges[axis] = limit;
			}

			// now sort the new edges to their correct position
			SortMinDown(0, pHandle.m_minEdges[0], dispatcher,false);
			SortMaxDown(0, pHandle.m_maxEdges[0], dispatcher,false);
			SortMinDown(1, pHandle.m_minEdges[1], dispatcher,false);
			SortMaxDown(1, pHandle.m_maxEdges[1], dispatcher,false);
			SortMinDown(2, pHandle.m_minEdges[2], dispatcher,true);
			SortMaxDown(2, pHandle.m_maxEdges[2], dispatcher,true);


			return handle;



		}
		public void RemoveHandle(ushort handle, IDispatcher dispatcher)
		{
			Handle pHandle = GetHandle(handle);

			//explicitly remove the pairs containing the proxy
			//we could do it also in the sortMinUp (passing true)
			///@todo: compare performance
			if (!m_pairCache.HasDeferredRemoval())
			{
				m_pairCache.RemoveOverlappingPairsContainingProxy(pHandle,dispatcher);
			}

			// compute current limit of edge arrays
			int limit = m_numHandles * 2;
    	
			for (int axis = 0;axis<3;axis++)
			{
				m_pHandles[0].m_maxEdges[axis] -= 2;
			}

			// remove the edges by sorting them up to the end of the list
			for (int axis = 0; axis < 3; axis++)
			{
				//Edge[] pEdges = m_pEdges[axis];
				ushort max = pHandle.m_maxEdges[axis];
				m_pEdges[axis,max].m_pos = m_handleSentinel;

				SortMaxUp(axis, max, dispatcher, false);

				ushort i = pHandle.m_minEdges[axis];
				m_pEdges[axis,i].m_pos = m_handleSentinel;

				SortMinUp(axis, i, dispatcher, false);

				m_pEdges[axis,limit - 1].m_handle = 0;
				m_pEdges[axis,limit - 1].m_pos = m_handleSentinel;

			}


			// free the handle
			FreeHandle(handle);
		}

		public void UpdateHandle(ushort handle, ref Vector3 aabbMin, ref Vector3 aabbMax, IDispatcher dispatcher)
		{
			Handle pHandle = GetHandle(handle);

			// quantize the new bounds
			ushort[] min = new ushort[3], max = new ushort[3];
			Quantize(min, ref aabbMin, 0);
			Quantize(max, ref aabbMax, 1);

			// update changed edges
			for (int axis = 0; axis < 3; axis++)
			{
				ushort emin = pHandle.m_minEdges[axis];
				ushort emax = pHandle.m_maxEdges[axis];

				int dmin = min[axis] - m_pEdges[axis,emin].m_pos;
				int dmax = max[axis] - m_pEdges[axis,emax].m_pos;

				m_pEdges[axis,emin].m_pos = min[axis];
				m_pEdges[axis,emax].m_pos = max[axis];

				// expand (only adds overlaps)
				if (dmin < 0)
					SortMinDown(axis, emin,dispatcher,true);

				if (dmax > 0)
					SortMaxUp(axis, emax,dispatcher,true);

				// shrink (only removes overlaps)
				if (dmin > 0)
					SortMinUp(axis, emin,dispatcher,true);

				if (dmax < 0)
					SortMaxDown(axis, emax,dispatcher,true);

#if DEBUG_BROADPHASE
	    debugPrintAxis(axis);
#endif //DEBUG_BROADPHASE
			}
		}
		public Handle GetHandle(ushort index) 
		{
			return m_pHandles[index];
		}

		public virtual void ResetPool(IDispatcher dispatcher)
		{
			if (m_numHandles == 0)
			{
				m_firstFreeHandle = 1;
				{
					for (ushort i = m_firstFreeHandle; i < m_maxHandles; i++)
					{
						m_pHandles[i].SetNextFree((ushort)(i + 1));
					}
					m_pHandles[m_maxHandles - 1].SetNextFree(0);
				}
			}
		}

		public void ProcessAllOverlappingPairs(IOverlapCallback callback)
		{
			int ibreak = 0;
		}

		//Broadphase Interface
		public virtual BroadphaseProxy CreateProxy(Vector3 aabbMin, Vector3 aabbMax, BroadphaseNativeTypes shapeType, Object userPtr, CollisionFilterGroups collisionFilterGroup, CollisionFilterGroups collisionFilterMask, IDispatcher dispatcher, Object multiSapProxy)
		{
			return CreateProxy(ref aabbMin, ref aabbMax, shapeType, userPtr, collisionFilterGroup, collisionFilterMask, dispatcher, multiSapProxy);
		}

		public virtual BroadphaseProxy CreateProxy(ref Vector3 aabbMin, ref Vector3 aabbMax, BroadphaseNativeTypes shapeType, Object userPtr, CollisionFilterGroups collisionFilterGroup, CollisionFilterGroups collisionFilterMask, IDispatcher dispatcher, Object multiSapProxy)
		{
			ushort handleId = AddHandle(ref aabbMin,ref aabbMax, userPtr,collisionFilterGroup,collisionFilterMask,dispatcher,multiSapProxy);
		
			Handle handle = GetHandle(handleId);
		
			if (m_raycastAccelerator != null)
			{
				BroadphaseProxy rayProxy = m_raycastAccelerator.CreateProxy(ref aabbMin,ref aabbMax,shapeType,userPtr,collisionFilterGroup,collisionFilterMask,dispatcher,0);
				handle.m_dbvtProxy = rayProxy;
			}
			return handle;

		}
		public virtual void DestroyProxy(BroadphaseProxy proxy,IDispatcher dispatcher)
		{
			Handle handle = (Handle)proxy;
			if (m_raycastAccelerator != null)
			{
				m_raycastAccelerator.DestroyProxy(handle.m_dbvtProxy,dispatcher);
			}
			RemoveHandle((ushort)handle.GetUid(), dispatcher);

		}
		public virtual void SetAabb(BroadphaseProxy proxy,ref Vector3 aabbMin,ref Vector3 aabbMax,IDispatcher dispatcher)
		{
			Handle handle = (Handle)proxy;
			handle.SetMinAABB(ref aabbMin);
			handle.SetMaxAABB(ref aabbMax);
			UpdateHandle((ushort)handle.GetUid(), ref aabbMin, ref aabbMax,dispatcher);
			if (m_raycastAccelerator != null)
			{
				m_raycastAccelerator.SetAabb(handle.m_dbvtProxy,ref aabbMin,ref aabbMax,dispatcher);
			}
		}
		public virtual void GetAabb(BroadphaseProxy proxy,ref Vector3 aabbMin, ref Vector3 aabbMax )
		{
			Handle pHandle = (Handle)proxy;
			aabbMin = pHandle.GetMinAABB();
			aabbMax = pHandle.GetMaxAABB();
		}
	
		public virtual void	AabbTest(ref Vector3 aabbMin, ref Vector3 aabbMax, IBroadphaseAabbCallback callback)
		{
			if (m_raycastAccelerator != null)
			{
				m_raycastAccelerator.AabbTest(ref aabbMin,ref aabbMax,callback);
			} 
			else
			{
				//choose axis?
				int axis = 0;
				//for each proxy
				for (int i=1;i<m_numHandles*2+1;i++)
				{
					if (m_pEdges[axis,i].IsMax())
					{
						Handle handle = GetHandle(m_pEdges[axis,i].m_handle);
						if (AabbUtil2.TestAabbAgainstAabb2(ref aabbMin,ref aabbMax,ref handle.m_aabbMin,ref handle.m_aabbMax))
						{
							callback.Process(handle);
						}
					}
				}
			}

		}


		//virtual void	rayTest(const Vector3& rayFrom,const Vector3& rayTo, btBroadphaseRayCallback& rayCallback, const Vector3& aabbMin=Vector3(0,0,0), const Vector3& aabbMax = Vector3(0,0,0));
		public virtual void RayTest(ref Vector3 rayFrom, ref Vector3 rayTo, BroadphaseRayCallback rayCallback)
		{
			Vector3 min = MathUtil.MIN_VECTOR;
			Vector3 max = MathUtil.MAX_VECTOR;
			RayTest(ref rayFrom, ref rayTo, rayCallback, ref min, ref max);
		}

		public virtual void	RayTest(ref Vector3 rayFrom,ref Vector3 rayTo, BroadphaseRayCallback rayCallback, ref Vector3 aabbMin, ref Vector3 aabbMax)
		{
			if (m_raycastAccelerator != null)
			{
				m_raycastAccelerator.RayTest(ref rayFrom,ref rayTo,rayCallback,ref aabbMin,ref aabbMax);
			} 
			else
			{
				//choose axis?
				ushort axis = 0;
				//for each proxy
				for (int i=1;i<m_numHandles*2+1;i++)
				{
					if (m_pEdges[axis,i].IsMax())
					{
						rayCallback.Process(GetHandle(m_pEdges[axis,i].m_handle));
					}
				}
			}
		}

		public void Quantize(ushort[] output, ref Vector3 point, int isMax)
		{
#if OLD_CLAMPING_METHOD
	///problem with this clamping method is that the floating point during quantization might still go outside the range [(0|isMax) .. (m_handleSentinel&m_bpHandleMask]|isMax]
	///see http://code.google.com/p/bullet/issues/detail?id=87
	    Vector3 clampedPoint = point;
	    clampedPoint.setMax(m_worldAabbMin);
	    clampedPoint.setMin(m_worldAabbMax);
	    Vector3 v = (clampedPoint - m_worldAabbMin) * m_quantize;
	    output[0] = (int)(((int)v.X & m_bpHandleMask) | isMax);
	    output[1] = (int)(((int)v.Y & m_bpHandleMask) | isMax);
	    output[2] = (int)(((int)v.Z & m_bpHandleMask) | isMax);
#else
			Vector3 v = (point - m_worldAabbMin) * m_quantize;
			output[0]=(v.X<=0)?(ushort)isMax:(v.X>=m_handleSentinel)?(ushort)((m_handleSentinel&m_bpHandleMask)|isMax):(ushort)(((ushort)v.X&m_bpHandleMask)|isMax);
			output[1]=(v.Y<=0)?(ushort)isMax:(v.Y>=m_handleSentinel)?(ushort)((m_handleSentinel&m_bpHandleMask)|isMax):(ushort)(((ushort)v.Y&m_bpHandleMask)|isMax);
			output[2]=(v.Z<=0)?(ushort)isMax:(v.Z>=m_handleSentinel)?(ushort)((m_handleSentinel&m_bpHandleMask)|isMax):(ushort)(((ushort)v.Z&m_bpHandleMask)|isMax);
#endif //OLD_CLAMPING_METHOD

		}
		///unQuantize should be conservative: aabbMin/aabbMax should be larger then 'getAabb' result
		public void UnQuantize(BroadphaseProxy proxy,ref Vector3 aabbMin, ref Vector3 aabbMax )
		{
			Handle pHandle = (Handle)proxy;

			ushort[] vecInMin = new ushort[3];
			ushort[] vecInMax = new ushort[3];

			vecInMin[0] = (ushort)m_pEdges[0,pHandle.m_minEdges[0]].m_pos ;
			vecInMax[0] = (ushort)(m_pEdges[0,pHandle.m_maxEdges[0]].m_pos + 1);
			vecInMin[1] = (ushort)m_pEdges[1,pHandle.m_minEdges[1]].m_pos;
			vecInMax[1] = (ushort)(m_pEdges[1,pHandle.m_maxEdges[1]].m_pos + 1);
			vecInMin[2] = (ushort)m_pEdges[2,pHandle.m_minEdges[2]].m_pos;
			vecInMax[2] = (ushort)(m_pEdges[2,pHandle.m_maxEdges[2]].m_pos + 1);

			aabbMin.X = (float)(vecInMin[0]) / (m_quantize.X);
			aabbMin.Y = (float)(vecInMin[1]) / (m_quantize.Y);
			aabbMin.Z = (float)(vecInMin[2]) / (m_quantize.Z);
			aabbMin += m_worldAabbMin;

			aabbMax.X = (float)(vecInMax[0]) / (m_quantize.X);
			aabbMax.Y = (float)(vecInMax[1]) / (m_quantize.Y);
			aabbMax.Z = (float)(vecInMax[2]) / (m_quantize.Z);
			aabbMax += m_worldAabbMax;

		}

		public bool TestAabbOverlap(BroadphaseProxy proxy0, BroadphaseProxy proxy1)
		{
			Handle pHandleA = (Handle)(proxy0);
			Handle pHandleB = (Handle)(proxy1);

			//optimization 1: check the array index (memory address), instead of the m_pos

			for (int axis = 0; axis < 3; axis++)
			{
				if (pHandleA.m_maxEdges[axis] < pHandleB.m_minEdges[axis] ||
				    pHandleB.m_maxEdges[axis] < pHandleA.m_minEdges[axis])
				{
					return false;
				}
			}
			return true;


		}

		public IOverlappingPairCache	GetOverlappingPairCache()
		{
			return m_pairCache;
		}

		public void SetOverlappingPairUserCallback(IOverlappingPairCallback pairCallback)
		{
			m_userPairCallback = pairCallback;
		}

		public IOverlappingPairCallback	GetOverlappingPairUserCallback()
		{
			return m_userPairCallback;
		}

		///getAabb returns the axis aligned bounding box in the 'global' coordinate frame
		///will add some transform later
		public virtual void GetBroadphaseAabb(ref Vector3 aabbMin,ref Vector3 aabbMax)
		{
			aabbMin = m_worldAabbMin;
			aabbMax = m_worldAabbMax;
		}

		public virtual void	PrintStats()
		{
/*		printf("btAxisSweep3.h\n");
		printf("numHandles = %d, maxHandles = %d\n",m_numHandles,m_maxHandles);
		printf("aabbMin=%f,%f,%f,aabbMax=%f,%f,%f\n",m_worldAabbMin.getX(),m_worldAabbMin.getY(),m_worldAabbMin.getZ(),
			m_worldAabbMax.getX(),m_worldAabbMax.getY(),m_worldAabbMax.getZ());
			*/

		}

	}
}