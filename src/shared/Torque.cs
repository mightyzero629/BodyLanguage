using System;
using UnityEngine;

namespace CheesyFX
{
    public class Torque : Force
    {
        public Torque Init(string name, Rigidbody rb, Func<Vector3> getDirection)
        {
            base.Init(name, rb, getDirection);
            amplitude.mean.max = amplitude.delta.max = 200f;
            enabled = false;
            return this;
        }
        
        protected override void AddForce()
        {
            if(!SuperController.singleton.freezeAnimation && !atom.mainController.isGrabbing) rb.AddTorque(currentForce, ForceMode.Force);
        }
    }
}