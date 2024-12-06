using System;
using System.Collections;
using UnityEngine;

namespace CheesyFX
{
    public class OrificeTriggerHandler : MonoBehaviour
    {
        public bool isActive;
        public Orifice orifice;
        public Collider lastColliding;
        
        
        public int numCollisions
        {
            get { return _numCollisions; }
            set
            {
                if (value > 0) _numCollisions = value;
                else
                {
                    _numCollisions = 0;
                    orifice.isPenetrated = false;
                    lastColliding = null;
                }
            }
        }
        private int _numCollisions;
		
        public void OnTriggerEnter(Collider collider)
        {
            if (collider.isTrigger || collider.attachedRigidbody.GetRegionName() == "Pelvis")
            {
                return;
            }
            lastColliding = collider;
            numCollisions++;
            if (!enabled)
            {
                if (orifice.isPenetrated)
                {
                    orifice.isPenetrated = false;
                }
                return;
            }
            if (!orifice.isPenetrated)
            {
                orifice.RegisterCollision(collider);
            }
        }
		
        public void OnTriggerExit(Collider collider)
        {
            // if (collider is MeshCollider)
            // {
            //     var renderer = collider.gameObject.GetComponent<SkinnedMeshRenderer>();
            //     renderer.enabled = false;
            // }
            if (collider.isTrigger) return;
            numCollisions--;
        }

        private void OnDisable()
        {
            orifice.isPenetrated = false;
        }

        public void Reset()
        {
            numCollisions = 0;
        }
    }
}