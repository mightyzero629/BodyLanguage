using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace CheesyFX
{
    public class Penetrator
    {
        public new string name => atom.name;
        public int type;
        public Transform tip;
        public Rigidbody mid;
        public Rigidbody root;
        public Transform rootTransform;
        public Collider tipCollider;
        public List<Collider> colliders = new List<Collider>();
        public List<Rigidbody> rigidbodies;
        public Dictionary<Collider, Func<Vector3>> forwardByCollider = new Dictionary<Collider, Func<Vector3>>();
        public Atom atom;
        public float width;
        public float length = 1f;
        private bool hasScaleChangeReceiver;
        private ScaleChangeReceiverJSONStorable scaleChangeReceiver;
        public Func<Vector3> forward;
        public StimReceiver stimReceiver;
        public bool isPenetrating;
        public Fuckable penetrating; 

        public Penetrator(Collider collider)
        {
            tipCollider = collider;
            colliders.Add(tipCollider);
            atom = collider.GetAtom();
            tip = new GameObject("PenetratorTip").transform;
            tip.SetParent(this.tipCollider.transform, false);
            tip.localEulerAngles = new Vector3(0f, 90f, 0f);
            if (atom.scaleChangeReceiverJSONStorables.Length > 0)
            {
                scaleChangeReceiver = atom.scaleChangeReceiverJSONStorables[0];
                hasScaleChangeReceiver = true;
            }

            var tipRB = tipCollider.attachedRigidbody;
            if (tipRB == null)
            {
                if(atom.type == "Person") tipRB = atom.rigidbodies.FirstOrDefault(x => x.name == "Gen3");
                else tipRB = atom.rigidbodies.FirstOrDefault(x => x.name == "b3");
            }
            rootTransform = tipRB.transform;
            if (rootTransform.parent != null && rootTransform.parent.GetComponent<Rigidbody>() != null)
            {
                forward = () => (rootTransform.position - rootTransform.parent.position).normalized;
                rootTransform = rootTransform.parent;
                if (rootTransform.parent != null && rootTransform.parent.GetComponent<Rigidbody>() != null)
                {
                    rootTransform = rootTransform.parent;
                }
            }
            else
            {
                forward = () => rootTransform.up;
            }
            // rootTransform.Draw();
            FillMeUp.penetratorByTip[tipCollider] = this;
            var on = atom.GetBoolJSONParam("on");
            on.setCallbackFunction -= FillMeUp.OnAtomToggled;
            on.setCallbackFunction += FillMeUp.OnAtomToggled;
        }

        public float GetScale()
        {
            if (hasScaleChangeReceiver) return scaleChangeReceiver.scale;
            return 1f;
        }
        
        public virtual void SetTipAndWith(){}

        // public virtual void Update()
        // {
        // }
    }
}