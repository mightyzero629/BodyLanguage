using System;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class ProximityHandler : MonoBehaviour
    {
        // private bool initialized;
        public Orifice orifice;
        public bool on;
        public FreeControllerV3 currentTipCtrl;
        private int numCollisions;
        private Penetrator candidate;
	    
        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger || !FillMeUp.singleton.enabled) return;
            if(numCollisions == 0) orifice.onProximityEnter.Trigger();
            numCollisions++;
            if (orifice.isPenetrated || !orifice.magnetic.val || (!(orifice is Throat) && orifice.other.isPenetrated)) return;
            if (FillMeUp.penetratorByCollider.TryGetValue(other, out candidate))
            {
                if(orifice.magnet.penetrator == candidate) return;
                orifice.magnet.penetrator = candidate;
                orifice.magnet.enabled = true;
                if (Person.stiffenEnabled.val && candidate.type > 0 && !(orifice is Throat))
                {
                    candidate.stimReceiver.Stiffen();
                }
                PoseMe.gaze.TouchFocus(other.gameObject.GetAtom(), other.attachedRigidbody);
                // $"{orifice} {candidate.atom} {candidate.tipCollider}".Print();
            }
            
            if(orifice.autoTogglePenisTip.val)
            {
                if (on || other.name != "AutoColliderGen3bHard") return;
                currentTipCtrl = other.GetAtom().freeControllers.FirstOrDefault(x => x.name == "penisTipControl");
                if (!currentTipCtrl) return;
                currentTipCtrl.currentPositionState = FreeControllerV3.PositionState.On;
                currentTipCtrl.currentRotationState = FreeControllerV3.RotationState.On;
                // currentTip.transform.position = new Vector3(0f, orifice.enterPointTF.InverseTransformVector(currentTip.transform.position -orifice.enterPointTF.position).y, 0f);
                on = true;
            }
		    
            
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.isTrigger || !FillMeUp.singleton.enabled) return;
            numCollisions--;
            if (numCollisions == 0)
            {
                Reset();
                PoseMe.gaze.TouchFocus(other.gameObject.GetAtom(), other.attachedRigidbody);
                // orifice.depthForce.enabled = false;
            }
        }

        public void Reset()
        {
            numCollisions = 0;
            orifice.onProximityExit.Trigger();
            if(orifice.magnet.penetrator != null)
            {
                if (orifice.magnet.penetrator.type > 0) orifice.magnet.penetrator.stimReceiver.StiffenReset();
                orifice.magnet.enabled = false;
                orifice.magnet.penetrator = null;
            }
            // orifice.thrustForce.enabled = orifice.maleForce.enabled = false;
        }

        private void OnDisable()
        {
            // orifice.magnet.penetrator.NullCheck();
            Reset();
        }
    }
}