using System;
using UnityEngine;

namespace CheesyFX
{
    public class RelativeForce : Force
    {
        public RelativeForce Init(string name, Rigidbody rb, Func<Vector3> getDirection)
        {
            base.Init(name, rb, getDirection);
            return this;
        }
        
        protected override void AddForce()
        {
            if(!Pose.isApplying || !SuperController.singleton.freezeAnimation && !atom.mainController.isGrabbing) rb.AddRelativeForce(currentForce * scale, ForceMode.Force);
        }
    }
}