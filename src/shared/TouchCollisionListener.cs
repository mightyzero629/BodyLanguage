using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace CheesyFX
{
    public class TouchCollisionListener
	{
		public TouchZone touchZone;
		public Collision lastCollision;
		public JSONStorableBool touchEnabled = new JSONStorableBool("Touch Enabled", true);
		
		bool hasTriggers;
		public List<TouchCollisionTrigger> collisionTriggers = new List<TouchCollisionTrigger>();
		public HashSet<Rigidbody> stuffColliding = new HashSet<Rigidbody>();
		public bool isOnStay;
		float onStayTimeout;
		IEnumerator onStayRoutine;
		public JSONStorableFloat onStayTolerance = new JSONStorableFloat("OnStay Tolerance", .1f, .1f, 2f, false, true);
		public JSONStorableFloat reactionChance = new JSONStorableFloat("Reaction Chance", 1f, 0f, 1f, true, true);

		public JSONStorableStringChooser addEventChoice;
		public JSONStorableFloat SlapIntensityFactor = new JSONStorableFloat("SlapIntensityFactor", 1f, 0f,5f);
		public UnityEvent onEnterEvent = new UnityEvent();
		public UnityEvent onStayEvent = new UnityEvent();
		public UnityEvent onExitEvent = new UnityEvent();
		public TouchTrigger touchTrigger;
		public IEnumerator touchTimerReset;
		

		public bool slapVocalsEnabled = true;

		public TouchCollisionListener(TouchZone touchZone){
			this.touchZone = touchZone;
			InitTriggers();
		}

		void AddCollisionTrigger(TouchZone bodyRegion, Rigidbody rb){
			if(rb != null){
				TouchCollisionTrigger collisionTrigger = rb.gameObject.GetComponent<TouchCollisionTrigger>();
				if(collisionTrigger == null){
					collisionTrigger = rb.gameObject.AddComponent<TouchCollisionTrigger>();
					collisionTrigger.Init(bodyRegion, rb);
				}
				collisionTriggers.Add(collisionTrigger);
			}
		}

		public void InitTriggers(){
			foreach(Rigidbody rb in FillMeUp.atom.GetRigidbodiesByRegion(touchZone.name)){
				AddCollisionTrigger(touchZone, rb);
			}
		}


		public void Destroy(){
			collisionTriggers.ForEach(x => Object.Destroy(x));
			collisionTriggers.Clear();
			onStayRoutine.Stop();
			hasTriggers = false;
		}

		private Atom gazeAtom;
		public void RegisterCollision(TouchCollisionTrigger collisionTrigger){
			if(touchEnabled.val){
				if(!stuffColliding.IsSupersetOf(collisionTrigger.stuffColliding)){
					lastCollision = collisionTrigger.lastCollision;
					TouchMe.OnEnter(touchZone, lastCollision);
					// if(isOnStay) events.Keys.ToList().ForEach(x => x.Trigger(touchZone, lastCollision, 0));
					stuffColliding.UnionWith(collisionTrigger.stuffColliding);
				}
				onStayTimeout = onStayTolerance.val;
				if(!isOnStay)
				{
					isOnStay = true;
					// events.Keys.ToList().ForEach(x => x.Trigger(touchZone, lastCollision, 0));
					stuffColliding.UnionWith(collisionTrigger.stuffColliding);
					
					if (touchZone.gazeTimeout <= 0f 
					    && PoseMe.gaze.enabledJ.val && Gaze.gazeSettings.touchReactionsEnabled.val )
					    // && !FillMeUp.penetratingAtoms.Values.Contains(collidingAtom) 
					    // && (collidingAtom.IsToyOrDildo() || collidingAtom.type.Contains("Capsule") || ))
					{
						var collidingAtom = lastCollision.collider.GetAtom();
						// collidingAtom.type.Print();
						if(PoseMe.gaze.TouchFocus(collidingAtom, lastCollision.rigidbody))
						{
							// touchZone.name.Print();
							touchZone.resetGazeTimeout.Stop();
							touchZone.gazeTimeout = 2f;
							touchZone.resetGazeTimeout = touchZone.ResetGazeTimeout().Start();
						}
					}
					onStayRoutine = OnStayRoutine();
					onStayRoutine.Start();
					onEnterEvent.Invoke();
				}
			}
		}

		public void DeregisterCollision(Collision collision){
			stuffColliding.Remove(collision.rigidbody);
			if(stuffColliding.Count == 0) onStayTimeout = onStayTolerance.val;
		}
		
		public void DeregisterCollision(Collider collider){
			stuffColliding.Remove(collider.attachedRigidbody);
			if(stuffColliding.Count == 0) onStayTimeout = onStayTolerance.val;
		}

		public IEnumerator OnStayRoutine(){
			touchTimerReset.Stop();
			touchTimerReset = null;
			while(stuffColliding.Count > 0 || onStayTimeout > 0f)
			{
				if (stuffColliding.Count == 0) onStayTimeout -= Time.fixedDeltaTime;
				if(touchTrigger != null && touchTrigger.enabled) touchZone.timeTouched += Time.fixedDeltaTime;
				if(touchZone.gazeTimeout < 2f) touchZone.gazeTimeout = 2f;
				onStayEvent.Invoke();
				yield return new WaitForFixedUpdate();
			}
			isOnStay = false;
			onExitEvent.Invoke();
			if (touchTrigger != null && !touchTrigger.instantReset.val)
			{
				if(touchTrigger.decayRate.val > 0f) touchTimerReset = TouchTimerReset().Start();
			}
			else touchZone.timeTouched = 0f;
		}

		public IEnumerator TouchTimerReset()
		{
			while (touchZone.timeTouched > 0f)
			{
				touchZone.timeTouched -= touchTrigger.decayRate.val * Time.fixedDeltaTime;
				yield return new WaitForFixedUpdate();
			}
			touchZone.timeTouched = 0f;
			touchTimerReset = null;
		}

		public bool ShouldReact(){
			float rand = UnityEngine.Random.Range(0f, 1f);
			return rand < reactionChance.val;
		}

		// public void RegisterLookAt(){
		// 	BodyRegion region = BodyManager.detailedViewScan.val ? touchZone : touchZone.topParent;
		// 	if (region.numLookAtColliders == 0)
		// 	{
		// 		BodyManager.regionsLookedAt.Add(region);
		// 		// region.parents.ForEach(x => BodyManager.regionsLookedAt.Add(x));
		// 	}
		// 	region.numLookAtColliders += 1;
		// }
		//
		// public void DeregisterLookAt()
		// {
		// 	BodyRegion region = BodyManager.detailedViewScan.val ? touchZone : touchZone.topParent;
		// 	if (region.numLookAtColliders > 0)
		// 	{
		// 		region.numLookAtColliders -= 1;
		// 		if (region.numLookAtColliders == 0)
		// 		{
		// 			BodyManager.regionsLookedAt.Remove(region);
		// 			// region.parents.ForEach(x => BodyManager.regionsLookedAt.Remove(x));
		// 		}
		// 	}
		// }

		public void CreateUI(){
			// UIManager.ClearItems();
			// UIManager.CreateItemsRemove(onEnterEvents.ToArray(), RemoveEvent, tabLevel);
			// UIManager.CreateItemsRemove(onStayEvents.ToArray(), RemoveEvent, tabLevel);
			// UIManager.CreateItemsRemove(onExitEvents.ToArray(), RemoveEvent, tabLevel);
			// UIManager.CreateItemsToggle(bodyRegion.bodyRegionManager.collisionEvents.ToArray(), ToggleEvent, 1);
		}
	}
}