using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using MeshVR;
using UnityEngine;
using UnityEngine.EventSystems;
using MacGruber;

namespace CheesyFX
{
	public class TouchCollisionTrigger : MonoBehaviour
	{
		public TouchZone touchZone;
		float onStayTimer = 0f;
		public HashSet<Rigidbody> stuffColliding = new HashSet<Rigidbody>();
		
		public Collision lastCollision;
		

		public void Init(TouchZone bodyRegion, Rigidbody rb){
			touchZone = bodyRegion;
			// isColliding = new JSONStorableBool("isStuffed", false, isCollidingCB);
		}

		void RegisterCollision(Collision collision){
			if(TouchMe.excludedAtoms.Contains(collision.rigidbody.GetAtom())) return;
			stuffColliding.Add(collision.rigidbody);
			touchZone.touchCollisionListener.RegisterCollision(this);
		}

		public void OnCollisionEnter(Collision collision){
			if(TouchMe.singleton.enabled)
			{
				Rigidbody other = collision.rigidbody;
				if(other.GetAtom() == TouchMe.singleton.containingAtom)
				{
					if (!SlapHandler.selfSlapsEnabled.val) return;
					var otherRegion = other.GetRegion();
					if(otherRegion != null)
					{
						// if(!(otherRegion.Contains("Palm") || otherRegion.Contains("Mid") || otherRegion.Contains("Ring"))) return;
						bool touchedByHand = otherRegion.parents.Any(x => x.name.Contains("Hand"));
						if (!touchedByHand || touchZone.parents.Exists(x => x.name == "Hands")) return;
					}
				}
				// $"{touchZone.name} {collision.rigidbody.name}".Print();
				lastCollision = collision;
				RegisterCollision(collision);
			}
		}

		public void OnCollisionExit(Collision collision){
			stuffColliding.Remove(collision.rigidbody);
			touchZone.touchCollisionListener.DeregisterCollision(collision);
		}
		
		public void OnTriggerEnter(Collider collider){
			if (WatchMe.singleton.enabled && collider == WatchMe.viewTrigger)
			{
				touchZone.watchListener.RegisterLookAt();
			}
		}
		
		public void OnTriggerExit(Collider collider){
			if (collider == WatchMe.viewTrigger)
			{
				touchZone.watchListener.DeregisterLookAt();
			}
		}

		// public void OnParticleCollision(GameObject other)
		// {
		// 	var cumRegion = touchZone.cumRegion;
		// 	cumRegion?.Increase(.02f);
		// }
	}
	
}