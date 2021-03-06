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
#define DBVT_BP_PROFILE
#define DBVT_BP_MARGIN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using InVision.Bullet.LinearMath;
using InVision.GameMath;

namespace InVision.Bullet.Collision.BroadphaseCollision
{
//#define DBVT_BP_SORTPAIRS				1
//#define DBVT_BP_PREVENTFALSEUPDATE		0
//#define DBVT_BP_ACCURATESLEEPING		0
//#define DBVT_BP_ENABLE_BENCHMARK		0


	public class DbvtBroadphase : IBroadphaseInterface
    {
	    /* Methods		*/
        public DbvtBroadphase() : this(null) { }

        public DbvtBroadphase(IOverlappingPairCache paircache)
        {
            m_sets[0] = new Dbvt();
            m_sets[1] = new Dbvt();

            m_deferedcollide = false;
            m_needcleanup = true;
            m_releasepaircache = (paircache != null) ? false : true;
            m_prediction = 0;
            m_stageCurrent = 0;
            m_fixedleft = 0;
            m_fupdates = 1;
            m_dupdates = 0;
            m_cupdates = 10;
            m_newpairs = 1;
            m_updates_call = 0;
            m_updates_done = 0;
            m_updates_ratio = 0;
            m_paircache = paircache != null ? paircache : new HashedOverlappingPairCache();
            m_gid = 0;
            m_pid = 0;
            m_cid = 0;
#if DBVT_BP_PROFILE
            m_profiling = new ProfileBlock();
	        m_profiling.clear();
#endif        
        }

        public virtual void Cleanup()
        {
            if (m_releasepaircache)
            {
                //m_paircache.~btOverlappingPairCache();
                //btAlignedFree(m_paircache);
                m_paircache.Cleanup();
                m_paircache = null;
            }
        }

	    public void	Collide(IDispatcher dispatcher)
        {
            Stopwatch totalStopwatch = new Stopwatch();
            totalStopwatch.Start();
            //SPC(m_profiling.m_total);
	        /* optimize				*/ 
	        m_sets[0].OptimizeIncremental(1+(m_sets[0].m_leaves*m_dupdates)/100);
	        if(m_fixedleft > 0)
	        {
		        int count=1+(m_sets[1].m_leaves*m_fupdates)/100;
		        m_sets[1].OptimizeIncremental(1+(m_sets[1].m_leaves*m_fupdates)/100);
		        m_fixedleft=System.Math.Max(0,m_fixedleft-count);
	        }
	        /* dynamic . fixed set	*/ 
	        m_stageCurrent=(m_stageCurrent+1)%STAGECOUNT;

            DbvtProxy current = m_stageRoots[m_stageCurrent];

	        if(current != null)
	        {
		        DbvtTreeCollider collider = new DbvtTreeCollider (this);
		        do	
                {
			        DbvtProxy next =current.links[1];
			        ListRemove(current,ref m_stageRoots[current.stage]);
			        ListAppend(current,ref m_stageRoots[STAGECOUNT]);
        #if DBVT_BP_ACCURATESLEEPING
			        m_paircache.removeOverlappingPairsContainingProxy(current,dispatcher);
			        collider.proxy=current;
			        btDbvt::collideTV(m_sets[0].m_root,current.aabb,collider);
			        btDbvt::collideTV(m_sets[1].m_root,current.aabb,collider);
        #endif
			        m_sets[0].Remove(current.leaf);
			        DbvtAabbMm	curAabb=DbvtAabbMm.FromMM(ref current.m_aabbMin,ref current.m_aabbMax);
			        current.leaf	=	m_sets[1].Insert(ref curAabb,current);
			        current.stage	=	STAGECOUNT;	
			        current			=	next;
		        } while(current != null);
		        m_fixedleft=m_sets[1].m_leaves;
		        m_needcleanup=true;
	        }
	        /* collide dynamics		*/ 
	        {
		        DbvtTreeCollider collider = new DbvtTreeCollider (this);
		        if(m_deferedcollide)
		        {
                    Stopwatch fdCollideStopwatch = new Stopwatch();
                    fdCollideStopwatch.Start();
                    //SPC(m_profiling.m_fdcollide);
			        Dbvt.CollideTTpersistentStack(m_sets[0].m_root,m_sets[1].m_root,collider);
                    fdCollideStopwatch.Stop();
                    m_profiling.m_fdcollide += (ulong)fdCollideStopwatch.ElapsedMilliseconds;
		        }
		        if(m_deferedcollide)
		        {
                    Stopwatch ddCollideStopwatch = new Stopwatch();
                    ddCollideStopwatch.Start();
                    //SPC(m_profiling.m_ddcollide);
			        Dbvt.CollideTTpersistentStack(m_sets[0].m_root,m_sets[0].m_root,collider);
                    ddCollideStopwatch.Stop();
                    m_profiling.m_ddcollide += (ulong)ddCollideStopwatch.ElapsedMilliseconds;
                }
	        }
	        /* clean up				*/ 
	        if(m_needcleanup)
	        {
                Stopwatch cleanupStopwatch = new Stopwatch();
                cleanupStopwatch.Start();
                //SPC(m_profiling.m_cleanup);
		        IList<BroadphasePair> pairs=m_paircache.GetOverlappingPairArray();
		        if(pairs.Count>0)
		        {

			        int	 ni= System.Math.Min(pairs.Count,System.Math.Max(m_newpairs,(pairs.Count*m_cupdates)/100));
			        for(int i=0;i<ni;++i)
			        {
				        BroadphasePair	p=pairs[(m_cid+i)%pairs.Count];
				        DbvtProxy		pa=(DbvtProxy)p.m_pProxy0;
				        DbvtProxy		pb=(DbvtProxy)p.m_pProxy1;
                        if (!DbvtAabbMm.Intersect(ref pa.leaf.volume, ref pb.leaf.volume))
				        {
        #if DBVT_BP_SORTPAIRS
					        if(pa.m_uniqueId>pb.m_uniqueId) 
						        btSwap(pa,pb);
        #endif
					        m_paircache.RemoveOverlappingPair(pa,pb,dispatcher);
					        --ni;--i;
				        }
			        }
			        if(pairs.Count>0)
                    {
                        m_cid=(m_cid+ni)%pairs.Count; 
                    }
                    else 
                    {
                        m_cid=0;
                    }
		        }
                cleanupStopwatch.Stop();
                m_profiling.m_cleanup += (ulong)cleanupStopwatch.ElapsedMilliseconds;
	        }
	        ++m_pid;
	        m_newpairs=1;
	        m_needcleanup=false;
	        if(m_updates_call>0)
	        { 
                m_updates_ratio=m_updates_done/(float)m_updates_call; 
            }
	        else
	        { 
                m_updates_ratio=0; 
            }
	        m_updates_done/=2;
	        m_updates_call/=2;
            
            totalStopwatch.Stop();
            m_profiling.m_total += (ulong)totalStopwatch.ElapsedMilliseconds;

        }

        
	    public void	Optimize()
        {
	        m_sets[0].OptimizeTopDown();
	        m_sets[1].OptimizeTopDown();
        }

        /* btBroadphaseInterface Implementation	*/
        public virtual BroadphaseProxy CreateProxy(Vector3 aabbMin, Vector3 aabbMax, BroadphaseNativeTypes shapeType, Object userPtr, CollisionFilterGroups collisionFilterGroup, CollisionFilterGroups collisionFilterMask, IDispatcher dispatcher, Object multiSapProxy)
        {
            return CreateProxy(ref aabbMin, ref aabbMax, shapeType, userPtr, collisionFilterGroup, collisionFilterMask, dispatcher, multiSapProxy);
        }

        public virtual BroadphaseProxy CreateProxy(ref Vector3 aabbMin, ref Vector3 aabbMax, BroadphaseNativeTypes shapeType, Object userPtr, CollisionFilterGroups collisionFilterGroup, CollisionFilterGroups collisionFilterMask, IDispatcher dispatcher, Object multiSapProxy)
        {
	        DbvtProxy proxy=new DbvtProxy(ref aabbMin,ref aabbMax,userPtr,collisionFilterGroup,collisionFilterMask);

	        DbvtAabbMm aabb = DbvtAabbMm.FromMM(ref aabbMin,ref aabbMax);

	        //bproxy.aabb			=	btDbvtAabbMm::FromMM(aabbMin,aabbMax);
	        proxy.stage		=	m_stageCurrent;
	        proxy.m_uniqueId	=	++m_gid;
	        proxy.leaf			=	m_sets[0].Insert(ref aabb,proxy);
	        ListAppend(proxy,ref m_stageRoots[m_stageCurrent]);
	        if(!m_deferedcollide)
	        {
		        DbvtTreeCollider collider = new DbvtTreeCollider(this);
		        collider.proxy=proxy;
		        Dbvt.CollideTV(m_sets[0].m_root,ref aabb,collider);
		        Dbvt.CollideTV(m_sets[1].m_root,ref aabb,collider);
	        }
	        return(proxy);

        }

        public virtual void DestroyProxy(BroadphaseProxy absproxy, IDispatcher dispatcher)
        {
            DbvtProxy proxy = (DbvtProxy)absproxy;
            if (proxy.stage == STAGECOUNT)
            {
                m_sets[1].Remove(proxy.leaf);
            }
            else
            {
                m_sets[0].Remove(proxy.leaf);
            }
            ListRemove(proxy,ref m_stageRoots[proxy.stage]);
            // FIXME - MAN Hacky cleanup protection
            if (m_paircache != null)
            {
                m_paircache.RemoveOverlappingPairsContainingProxy(proxy, dispatcher);
            }
            proxy = null;
            m_needcleanup = true;

        }
     
        
        
        public virtual void SetAabb(BroadphaseProxy absproxy, ref Vector3 aabbMin, ref Vector3 aabbMax, IDispatcher dispatcher)
        {
	        DbvtProxy proxy=(DbvtProxy)absproxy;
	        DbvtAabbMm	aabb = DbvtAabbMm.FromMM(ref aabbMin,ref aabbMax);
        #if DBVT_BP_PREVENTFALSEUPDATE
	        if(NotEqual(ref aabb,proxy,leaf.volume))
        #endif
	        {
		        bool docollide=false;
		        if(proxy.stage==STAGECOUNT)
		        {/* fixed . dynamic set	*/ 
			        m_sets[1].Remove(proxy.leaf);
			        proxy.leaf=m_sets[0].Insert(ref aabb,proxy);
			        docollide=true;
		        }
		        else
		        {/* dynamic set				*/ 
			        ++m_updates_call;
                    if (DbvtAabbMm.Intersect(ref proxy.leaf.volume, ref aabb))
			        {/* Moving				*/ 

				        Vector3	delta=aabbMin-proxy.m_aabbMin;
				        Vector3	velocity = (((proxy.m_aabbMax-proxy.m_aabbMin)/2f)*m_prediction);
				        if(delta.X<0) velocity.X = -velocity.X;
				        if(delta.Y<0) velocity.Y = -velocity.Y;
				        if(delta.Z<0) velocity.Z = -velocity.Z;
				        if	(
        #if DBVT_BP_MARGIN				
					        m_sets[0].Update(proxy.leaf,ref aabb,ref velocity,DBVT_BP_MARGIN)
        #else
					        m_sets[0].update(proxy.leaf,aabb,ref velocity)
        #endif
					        )
				        {
					        ++m_updates_done;
					        docollide=true;
				        }
			        }
			        else
			        {/* Teleporting			*/ 
				        m_sets[0].Update(proxy.leaf,ref aabb);
				        ++m_updates_done;
				        docollide=true;
			        }	
		        }
		        ListRemove(proxy,ref m_stageRoots[proxy.stage]);
		        proxy.m_aabbMin = aabbMin;
		        proxy.m_aabbMax = aabbMax;
		        proxy.stage	=	m_stageCurrent;
		        ListAppend(proxy,ref m_stageRoots[m_stageCurrent]);
		        if(docollide)
		        {
			        m_needcleanup=true;
			        if(!m_deferedcollide)
			        {
                        DbvtTreeCollider collider = new DbvtTreeCollider(this);
				        Dbvt.CollideTTpersistentStack(m_sets[1].m_root,proxy.leaf,collider);
                        Dbvt.CollideTTpersistentStack(m_sets[0].m_root, proxy.leaf, collider);
			        }
		        }	
	        }


        }

        public virtual void	AabbTest(ref Vector3 aabbMin,ref Vector3 aabbMax,IBroadphaseAabbCallback aabbCallback)
        {
	        BroadphaseAabbTester callback = new BroadphaseAabbTester(aabbCallback);
            DbvtAabbMm	bounds=DbvtAabbMm.FromMM(ref aabbMin,ref aabbMax);
	        Dbvt.CollideTV(m_sets[0].m_root,ref bounds,callback);
	        Dbvt.CollideTV(m_sets[1].m_root,ref bounds,callback);
        }


        public virtual void RayTest(ref Vector3 rayFrom, ref Vector3 rayTo, BroadphaseRayCallback rayCallback)
        {
            Vector3 min = MathUtil.MIN_VECTOR;
            Vector3 max = MathUtil.MAX_VECTOR;
            RayTest(ref rayFrom, ref rayTo, rayCallback, ref min, ref max);
        }

        public virtual void RayTest(ref Vector3 rayFrom, ref Vector3 rayTo, BroadphaseRayCallback rayCallback, ref Vector3 aabbMin, ref Vector3 aabbMax)
        {
                BroadphaseRayTester callback = new BroadphaseRayTester(rayCallback);

				m_sets[0].RayTestInternal(m_sets[0].m_root,
                    ref rayFrom,
                    ref rayTo,
                    ref rayCallback.m_rayDirectionInverse,
                    rayCallback.m_signs,
                    rayCallback.m_lambda_max,
                    ref aabbMin,
                    ref aabbMax,
                    callback);

				m_sets[1].RayTestInternal(m_sets[1].m_root,
                    ref rayFrom,
                    ref rayTo,
                    ref rayCallback.m_rayDirectionInverse,
                    rayCallback.m_signs,
                    rayCallback.m_lambda_max,
                    ref aabbMin,
                    ref aabbMax,
                    callback);
        }

        public virtual void GetAabb(BroadphaseProxy absproxy, ref Vector3 aabbMin, ref Vector3 aabbMax)
        {
            DbvtProxy proxy = (DbvtProxy)absproxy;
            aabbMin = proxy.GetMinAABB();
            aabbMax = proxy.GetMaxAABB();
        }


        ///this setAabbForceUpdate is similar to setAabb but always forces the aabb update. 
        ///it is not part of the btBroadphaseInterface but specific to btDbvtBroadphase.
        ///it bypasses certain optimizations that prevent aabb updates (when the aabb shrinks), see
        ///http://code.google.com/p/bullet/issues/detail?id=223
        public void setAabbForceUpdate(BroadphaseProxy absproxy, ref Vector3 aabbMin, ref Vector3 aabbMax, IDispatcher dispatcher)
        {
	        DbvtProxy proxy=(DbvtProxy)absproxy;
            DbvtAabbMm	bounds=DbvtAabbMm.FromMM(ref aabbMin,ref aabbMax);
	        bool	docollide=false;
	        if(proxy.stage==STAGECOUNT)
	        {/* fixed . dynamic set	*/ 
		        m_sets[1].Remove(proxy.leaf);
		        proxy.leaf=m_sets[0].Insert(ref bounds,proxy);
		        docollide=true;
	        }
	        else
	        {/* dynamic set				*/ 
		        ++m_updates_call;
		        /* Teleporting			*/ 
		        m_sets[0].Update(proxy.leaf,ref bounds);
		        ++m_updates_done;
		        docollide=true;
	        }
	        ListRemove(proxy,ref m_stageRoots[proxy.stage]);
	        proxy.m_aabbMin = aabbMin;
	        proxy.m_aabbMax = aabbMax;
	        proxy.stage	= m_stageCurrent;
	        ListAppend(proxy,ref m_stageRoots[m_stageCurrent]);
	        if(docollide)
	        {
		        m_needcleanup=true;
		        if(!m_deferedcollide)
		        {
                    DbvtTreeCollider collider = new DbvtTreeCollider(this);
                    Dbvt.CollideTTpersistentStack(m_sets[1].m_root, proxy.leaf, collider);
                    Dbvt.CollideTTpersistentStack(m_sets[0].m_root, proxy.leaf, collider);
		        }
	        }	
        }



        public virtual void CalculateOverlappingPairs(IDispatcher dispatcher)
        {
                Collide(dispatcher);
    #if DBVT_BP_PROFILE
	    if(0==(m_pid%DbvtBroadphase.DBVT_BP_PROFILING_RATE))
	    {	
		    System.Console.WriteLine("fixed({0}) dynamics({1}) pairs({2})\r\n",m_sets[1].m_leaves,m_sets[0].m_leaves,m_paircache.GetNumOverlappingPairs());
		    uint total= (uint)m_profiling.m_total;
		    if(total<=0) total=1;
		    System.Console.WriteLine("ddcollide: {0} ({1})\r\n",(50+m_profiling.m_ddcollide*100)/total,m_profiling.m_ddcollide/DBVT_BP_PROFILING_RATE);
		    System.Console.WriteLine("fdcollide: {0} ({1})\r\n",(50+m_profiling.m_fdcollide*100)/total,m_profiling.m_fdcollide/DBVT_BP_PROFILING_RATE);
		    System.Console.WriteLine("cleanup:   {0} ({1})\r\n",(50+m_profiling.m_cleanup*100)/total,m_profiling.m_cleanup/DBVT_BP_PROFILING_RATE);
		    System.Console.WriteLine("total:     {0}\r\n",total/DBVT_BP_PROFILING_RATE);
		    ulong	sum=m_profiling.m_ddcollide+
			    m_profiling.m_fdcollide+
			    m_profiling.m_cleanup;
		    System.Console.WriteLine("leaked: %u%% (%uus)\r\n",100-((50+sum*100)/total),(total-sum)/DBVT_BP_PROFILING_RATE);
            System.Console.WriteLine("job counts: %u%%\r\n", (m_profiling.m_jobcount * 100) / (ulong)((m_sets[0].m_leaves + m_sets[1].m_leaves) * DbvtBroadphase.DBVT_BP_PROFILING_RATE));
		    m_profiling.clear();
	    }
    #endif

            PerformDeferredRemoval(dispatcher);
        }

	    public virtual IOverlappingPairCache GetOverlappingPairCache()
        {
            return m_paircache;
        }

	    public virtual void	GetBroadphaseAabb(ref Vector3 aabbMin,ref Vector3 aabbMax)
        {
	        DbvtAabbMm bounds = new DbvtAabbMm();

	        if(!m_sets[0].Empty())
            {
		        if(!m_sets[1].Empty())
                {
                    DbvtAabbMm.Merge(ref m_sets[0].m_root.volume,ref m_sets[1].m_root.volume,ref bounds);
                }
		        else
                {
			        bounds=m_sets[0].m_root.volume;
                }
            }
	        else if(!m_sets[1].Empty())
            {
                bounds=m_sets[1].m_root.volume;
            }
	        else
            {
                Vector3 temp = Vector3.Zero;
		        bounds=DbvtAabbMm.FromCR(ref temp,0);
            }
	        aabbMin=bounds.Mins();
	        aabbMax=bounds.Maxs();

        }

	    public void	SetVelocityPrediction(float prediction)
	    {
		    m_prediction = prediction;
	    }
	    
        public float GetVelocityPrediction()
	    {
		    return m_prediction;
	    }

        public virtual void PrintStats()
        {
        }
	    
        public static void Benchmark(IBroadphaseInterface broadphaseInterface)
        {
	        IList<BroadphaseBenchmarkObject>	objects = new List<BroadphaseBenchmarkObject>();
	        Stopwatch wallclock = new Stopwatch();
	        /* Begin			*/ 
	        for(int iexp=0;iexp<s_experiments.Length;++iexp)
	        {
		        BroadphaseBenchmarkExperiment experiment=DbvtBroadphase.s_experiments[iexp];
		        int	object_count=experiment.object_count;
		        int	update_count=(object_count*experiment.update_count)/100;
		        int	spawn_count=(object_count*experiment.spawn_count)/100;
		        float speed=experiment.speed;	
		        float amplitude=experiment.amplitude;
		        System.Console.WriteLine("Experiment #{0} '{1}':\r\n",iexp,experiment.name);
		        System.Console.WriteLine("\tObjects: {0}\r\n",object_count);
		        System.Console.WriteLine("\tUpdate: {0}\r\n",update_count);
		        System.Console.WriteLine("\tSpawn: {0}\r\n",spawn_count);
		        System.Console.WriteLine("\tSpeed: {0}\r\n",speed);
		        System.Console.WriteLine("\tAmplitude: {0}\r\n",amplitude);
                //srand(180673);
		        /* Create objects	*/ 
		        wallclock.Reset();
				//objects.Capacity = object_count;
		        for(int i=0;i<object_count;++i)
		        {
			        BroadphaseBenchmarkObject po = new BroadphaseBenchmarkObject();
			        po.center.X=BroadphaseBenchmark.UnitRand()*50;
			        po.center.Y=BroadphaseBenchmark.UnitRand()*50;
			        po.center.Z=BroadphaseBenchmark.UnitRand()*50;
			        po.extents.X=BroadphaseBenchmark.UnitRand()*2+2;
			        po.extents.Y=BroadphaseBenchmark.UnitRand()*2+2;
			        po.extents.Z=BroadphaseBenchmark.UnitRand()*2+2;
			        po.time=BroadphaseBenchmark.UnitRand()*2000;
                    po.proxy = broadphaseInterface.CreateProxy(po.center - po.extents, po.center + po.extents, BroadphaseNativeTypes.BOX_SHAPE_PROXYTYPE, po, CollisionFilterGroups.DefaultFilter, CollisionFilterGroups.DefaultFilter, null, null);
			        objects.Add(po);
                    
		        }
		        BroadphaseBenchmark.OutputTime("\tInitialization",wallclock,1);
		        /* First update		*/ 
		        wallclock.Reset();
		        for(int i=0;i<objects.Count;++i)
		        {
                    objects[i].update(speed, amplitude, broadphaseInterface);
		        }
		        BroadphaseBenchmark.OutputTime("\tFirst update",wallclock,1);
		        /* Updates			*/ 
		        wallclock.Reset();
		        for(int i=0;i<experiment.iterations;++i)
		        {
			        for(int j=0;j<update_count;++j)
			        {
                        objects[j].update(speed, amplitude, broadphaseInterface);
			        }
                    broadphaseInterface.CalculateOverlappingPairs(null);
		        }
		        BroadphaseBenchmark.OutputTime("\tUpdate",wallclock,(uint)experiment.iterations);
		        /* Clean up			*/ 
		        wallclock.Reset();
		        for(int i=0;i<objects.Count;++i)
		        {
                    broadphaseInterface.DestroyProxy(objects[i].proxy, null);
			        objects[i] = null;
		        }
		        objects.Clear();
		        BroadphaseBenchmark.OutputTime("\tRelease",wallclock,1);
	        }
        }


        public void PerformDeferredRemoval(IDispatcher dispatcher)
        {
            if (m_paircache.HasDeferredRemoval())
            {

                IList<BroadphasePair> overlappingPairArray = m_paircache.GetOverlappingPairArray();

                //perform a sort, to find duplicates and to sort 'invalid' pairs to the end
                ((List<BroadphasePair>)overlappingPairArray).Sort();


                int invalidPair = 0;
                int i;

                BroadphasePair previousPair = new BroadphasePair();

                for (i = 0; i < overlappingPairArray.Count; i++)
                {

                    BroadphasePair pair = overlappingPairArray[i];

                    bool isDuplicate = (pair == previousPair);

                    previousPair = pair;

                    bool needsRemoval = false;

                    if (!isDuplicate)
                    {
                        //important to perform AABB check that is consistent with the broadphase
                        DbvtProxy pa = (DbvtProxy)pair.m_pProxy0;
                        DbvtProxy pb = (DbvtProxy)pair.m_pProxy1;
                        bool hasOverlap = DbvtAabbMm.Intersect(ref pa.leaf.volume, ref pb.leaf.volume);

                        if (hasOverlap)
                        {
                            needsRemoval = false;
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
                        Debug.Assert(pair.m_algorithm != null);
                    }

                    if (needsRemoval)
                    {
                        m_paircache.CleanOverlappingPair(pair, dispatcher);

                        pair.m_pProxy0 = null;
                        pair.m_pProxy1 = null;
                        invalidPair++;
                    }
                }

                //perform a sort, to sort 'invalid' pairs to the end
				((List<BroadphasePair>)overlappingPairArray).Sort();
                //overlappingPairArray.resize(overlappingPairArray.size() - invalidPair);
				//overlappingPairArray.Capacity = overlappingPairArray.Count - invalidPair;
            }

        }

	    ///reset broadphase internal structures, to ensure determinism/reproducability
	    public virtual void ResetPool(IDispatcher dispatcher)
        {
        	
	        int totalObjects = m_sets[0].m_leaves + m_sets[1].m_leaves;
	        if (totalObjects == 0)
	        {
		        //reset internal dynamic tree data structures
		        m_sets[0].Clear();
		        m_sets[1].Clear();
        		
		        m_deferedcollide	=	false;
		        m_needcleanup		=	true;
		        //m_prediction		=	1/2f;
		        m_stageCurrent		=	0;
		        m_fixedleft			=	0;
		        m_fupdates			=	1;
		        m_dupdates			=	0;
		        m_cupdates			=	10;
		        m_newpairs			=	1;
		        m_updates_call		=	0;
		        m_updates_done		=	0;
		        m_updates_ratio		=	0;
        		
		        m_gid				=	0;
		        m_pid				=	0;
		        m_cid				=	0;
		        for(int i=0;i<=STAGECOUNT;++i)
		        {
			        m_stageRoots[i]=null;
		        }
	        }
        }

        //
        // Helpers
        //

        //

        public static void ListAppend(DbvtProxy item, ref DbvtProxy list)
        {
	        item.links[0]=null;
	        item.links[1]=list;
	        if(list != null)
            {   
                list.links[0]=item;
            }
	        list=item;
        }

        //
        public static void ListRemove(DbvtProxy item,ref DbvtProxy list)
        {
	        if(item.links[0] != null)
            {
                item.links[0].links[1]=item.links[1]; 
            }
            else 
            {
                list=item.links[1];
            }
	        if(item.links[1] != null) 
            {
                item.links[1].links[0]=item.links[0];
            }
        }

        //
        
        public static int ListCount(DbvtProxy root)
        {
	        int	n=0;
	        while(root != null) 
            { 
                ++n;
                root=root.links[1]; 
            }
	        return n;
        }


        /* Config		*/
        /* Fields		*/
        public Dbvt[] m_sets = new Dbvt[2];					// Dbvt sets
        public DbvtProxy[] m_stageRoots = new DbvtProxy[STAGECOUNT + 1];	// Stages list
        public IOverlappingPairCache m_paircache;				// Pair cache
        public float m_prediction;				// Velocity prediction
        public int m_stageCurrent;				// Current stage
        public int m_fupdates;					// % of fixed updates per frame
        public int m_dupdates;					// % of dynamic updates per frame
        public int m_cupdates;					// % of cleanup updates per frame
        public int m_newpairs;					// Number of pairs created
        public int m_fixedleft;				// Fixed optimization left
        public uint m_updates_call;				// Number of updates call
        public uint m_updates_done;				// Number of updates done
        public float m_updates_ratio;			// m_updates_done/m_updates_call
        public int m_pid;						// Parse id
        public int m_cid;						// Cleanup index
        public int m_gid;						// Gen id
        public bool m_releasepaircache;			// Release pair cache on delete
        public bool m_deferedcollide;			// Defere dynamic/static collision to collide call
        public bool m_needcleanup;				// Need to run cleanup?
#if DBVT_BP_PROFILE
        ProfileBlock m_profiling;
#endif

    
    
        public const int DYNAMIC_SET =	0;	/* Dynamic set index	*/ 
        public const int FIXED_SET = 1;	/* Fixed set index		*/ 
        public const int STAGECOUNT = 2;	/* Number of stages		*/
        public const float DBVT_BP_MARGIN = 0.05f;
        public const int DBVT_BP_PROFILING_RATE = 256;

        public static BroadphaseBenchmarkExperiment[] s_experiments =
	        {
		        new BroadphaseBenchmarkExperiment("1024o.10%",1024,10,0,8192,0.005f,100f),
		        /*{"4096o.10%",4096,10,0,8192,(btScalar)0.005,(btScalar)100},
		        {"8192o.10%",8192,10,0,8192,(btScalar)0.005,(btScalar)100},*/
	        };

    
    }
}
