using System;
using UnityEngine;

namespace CheesyFX
{
    public class RelativeTorque : Torque
    {
        public RelativeTorque Init(string name, Rigidbody rb, Func<Vector3> getDirection)
        {
            base.Init(name, rb, getDirection);
            return this;
        }
        
        protected override void AddForce()
        {
            if(!Pose.isApplying || !SuperController.singleton.freezeAnimation && !atom.mainController.isGrabbing) rb.AddRelativeTorque(currentForce * scale, ForceMode.Force);
        }
    }
}