using System;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class RBDynamics : MonoBehaviour
    {
        private int regionId;
        private int sideId;
        public Rigidbody rigidbody;
        private Transform transform; 
        private RBDynamics parent;
        private RBDynamics child;
        private Rigidbody tensionRB;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        private Vector3 _lastVelocity;
        private Vector3 _lastAngularVel;

        private Vector3 _acceleration;
        private bool _accelerationSet;

        private Vector3 _localAcceleration;
        private bool _localAccelerationSet;
        
        private Vector3 _angularAcceleration;
        private bool _angularAccelerationSet;
        
        private Vector3 _localAngularAcceleration;
        private bool _localAngularAccelerationSet;

        private float _tension;
        private bool _tensionSet;
        
        private float _tensionAlt;
        private bool _tensionAltSet;

        private float _activity;
        private bool _activitySet;

        public int uses;

        private Func<float> getTension;
        private Func<float> getTensionAlt;

        public void Init(Rigidbody rb, int regionId = -1, int sideId = -1)
        {
            this.regionId = regionId;
            this.sideId = sideId;
            rigidbody = rb;
            transform = rb.transform;
            getTension = () => transform.localRotation.x;
            getTensionAlt = () => transform.localRotation.z;
        }

        public void FixedUpdate()
        {
            _lastVelocity = velocity;
            _lastAngularVel = angularVelocity;
            velocity = rigidbody.velocity;
            angularVelocity = rigidbody.angularVelocity;
            _accelerationSet = false;
            _localAccelerationSet = false;
            _angularAccelerationSet = false;
            _localAngularAccelerationSet = false;
            _activitySet = false;
            _tensionSet = false;
            _tensionAltSet = false;
        }

        public Vector3 localAcceleration
        {
            get
            {
                if (_localAccelerationSet) return _localAcceleration;
                _localAcceleration = transform.InverseTransformDirection(acceleration);
                _localAccelerationSet = true;
                return _localAcceleration;
            }
        }
        
        public Vector3 acceleration
        {
            get
            {
                if (_accelerationSet) return _acceleration;
                for (int i = 0; i < 3; i++)
                {
                    if (Mathf.Sign(velocity[i]) + Mathf.Sign(_lastVelocity[i]) == 0f)
                    {
                        _acceleration = new Vector3();
                        break;
                    }
                    else
                    {
                        _acceleration[i] = (velocity[i] - _lastVelocity[i]) / Time.fixedDeltaTime;
                    }
                }
                _accelerationSet = true;
                return _acceleration;
            }
        }

        public Vector3 angularAcceleration
        {
            get
            {
                if (_angularAccelerationSet) return _angularAcceleration;
                for (int i = 0; i < 3; i++)
                {
                    if (Mathf.Sign(angularVelocity[i]) + Mathf.Sign(_lastAngularVel[i]) == 0f)
                    {
                        _angularAcceleration = new Vector3();
                        break;
                    }
                    else _angularAcceleration[i] = (angularVelocity[i] - _lastAngularVel[i]) / Time.fixedDeltaTime;
                }
                _angularAccelerationSet = true;
                return _angularAcceleration;
            }
        }

        public Vector3 localAngularAcceleration
        {
            get
            {
                if (_localAngularAccelerationSet) return _localAngularAcceleration;
                _localAngularAcceleration = transform.InverseTransformDirection(angularAcceleration);
                _localAngularAccelerationSet = true;
                return _localAngularAcceleration;
            }
        }

        public float tension
        {
            get
            {
                if (_tensionSet) return _tension;
                _tension = getTension();
                _tensionSet = true;
                return _tension;
            }
        }
        public float tensionAlt
        {
            get
            {
                if (_tensionAltSet) return _tensionAlt;
                // _tension = 1f-1.5f*Mathf.Abs(rigidbody.transform.localRotation.x);
                _tensionAlt = getTensionAlt();
                _tensionAltSet = true;
                return _tensionAlt;
            }
        }
        
        public float activity
        {
            get
            {
                if (_activitySet) return _activity;
                float act = rigidbody.mass * Math.Abs(Vector3.Dot(acceleration, velocity));
                for (int i = 0; i < 3; i++)
                {
                    act += Mathf.Abs(angularAcceleration[i] * rigidbody.inertiaTensor[i] * angularVelocity[i]);
                }
                _activity = act;
                _activitySet = true;
                return act;
            }
        }

        private void GetRelatives()
        {
            parent = transform.parent.GetComponent<Rigidbody>().AddRbDynamic();
            for (int i = 0; i < transform.childCount; i++)
            {
                var rb = transform.GetChild(i).GetComponent<Rigidbody>();
                if (rb != null)
                {
                    child = rb.AddRbDynamic();
                    break;
                }
            }
            // (rigidbody.name+":"+parent.name).Print();
        }

        public void SetTensionFunction()
        {
            GetRelatives();
            getTension = () => 1f - Mathf.Clamp(1.2f * Mathf.Abs(transform.localRotation.x), 0f, 1f);
            if (rigidbody.name.Contains("Shin"))
            {
                getTension = () => 1f - Mathf.Clamp(1.2f * Mathf.Abs(transform.localRotation.x), 0f, 1f);
            }
            else if (rigidbody.name.Contains("abdomen"))
            {
                getTension = () => Mathf.Clamp(.3f + parent.tension - transform.localRotation.x, 0f, 1f);
            }
            else if (rigidbody.name.Contains("Glute"))
            {
                var side = sideId == 1? "r" : "l";
                tensionRB = rigidbody.GetAtom().forceReceivers.First(x => x.name == side + "Thigh")
                    .GetComponent<Rigidbody>();
                getTension = () => Mathf.Clamp(Mathf.Abs(.3f + 1.6f * transform.localRotation.x), 0f, 1f);
            }
            else if(rigidbody.name.Contains("Thigh")) getTension = () => Mathf.Clamp(Mathf.Abs(.3f + 1.6f * transform.localRotation.x), 0f, 1f);
            // else if(rigidbody.name.Contains("Thigh")) getTension = () =>
            // {
            //     float thisTension = Mathf.Clamp(1.2f * Mathf.Abs(rigidbody.transform.localRotation.x), 0f, 1f);
            //     float childTension = child.tension;
            //     return (thisTension + childTension) * .5f;
            // };
        }
    }

    public static class RBDynamicExtensions
    {
        public static RBDynamics AddRbDynamic(this Rigidbody rb, int sideId = -1)
        {
            var rbDynamic = rb.gameObject.GetComponent<RBDynamics>();
            if (rbDynamic == null)
            {
                rbDynamic = rb.gameObject.AddComponent<RBDynamics>();
                // var sideId = -1;
                // var name = rb.name.ToLower();
                // if (name.StartsWith("l")) sideId = 0;
                // else if (name.StartsWith("r")) sideId = 1;
                rbDynamic.Init(rb, sideId: sideId);
            }
            rbDynamic.uses ++;
            return rbDynamic;
        }
        
        public static RBDynamics AddRbDynamic(this GameObject go, int sideId = -1)
        {
            var rbDynamic = go.GetComponent<RBDynamics>();
            if (rbDynamic == null)
            {
                rbDynamic = go.AddComponent<RBDynamics>();
                // var sideId = -1;
                // var name = rb.name.ToLower();
                // if (name.StartsWith("l")) sideId = 0;
                // else if (name.StartsWith("r")) sideId = 1;
                rbDynamic.Init(go.GetComponent<Rigidbody>(), sideId: sideId);
            }
            rbDynamic.uses ++;
            return rbDynamic;
        }
    }
}