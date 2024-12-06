using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class LimbIdle
    {
        public PoseIdle poseIdle;
        public Rigidbody target;
        private Vector3 force;
        private Vector3 torque;

        private Vector3 forceTarget;
        private Vector3 torqueTarget;
        
        private float quickness;

        private JSONStorableFloat forceScale = new JSONStorableFloat("Force Scale", 50f, 0f, 200f);
        private JSONStorableFloat torqueScale = new JSONStorableFloat("Torque Scale", 20f, 0f, 200f);
        public MyJSONStorableVector3 directionalForce = new MyJSONStorableVector3("Force", 100f*Vector3.one, -200f*Vector3.one, 200f*Vector3.one);
        public MyJSONStorableVector3 directionalTorque = new MyJSONStorableVector3("Torque", 10f*Vector3.one, -20f*Vector3.one, 20f*Vector3.one);
        // private float maxQuickness = .75f;

        public JSONStorableBool forceEnabled = new JSONStorableBool("Force Enabled", true);
        public JSONStorableBool torqueEnabled = new JSONStorableBool("Torque Enabled", true);
        public JSONStorableBool[] forceOnesided;
        public JSONStorableBool[] torqueOnesided;

        private float timer;
        public float[] timers = new float[3];

        public bool isEdited;

        public LimbIdle(PoseIdle poseIdle, Rigidbody rb)
        {
            this.poseIdle = poseIdle;
            target = rb;
            directionalForce.setCallbackFunction += val => timer = 0f;
            directionalTorque.setCallbackFunction += val => timer = 0f;
            forceEnabled.setCallbackFunction += val =>
            {
                if (!val)
                {
                    forceTarget = Vector3.zero;
                    SetInfo();
                }
                else timer = 0f;
                
            };
            torqueEnabled.setCallbackFunction += val => {
                if (!val)
                {
                    torqueTarget = Vector3.zero;
                    SetInfo();
                }
                else timer = 0f;

            };
            forceOnesided = new []
            {
                new JSONStorableBool("Force Onsided X", false),
                new JSONStorableBool("Force Onsided Y", false),
                new JSONStorableBool("Force Onsided Z", false),
            };
            torqueOnesided = new []
            {
                new JSONStorableBool("Torque Onsided X", false),
                new JSONStorableBool("Torque Onsided Y", false),
                new JSONStorableBool("Torque Onsided Z", false),
            };
            if (rb.name == "head")
            {
                directionalTorque.valAndDefault = 30f * Vector3.one;
            }
            else if (rb.name.Contains("Foot"))
            {
                directionalTorque.valAndDefault = new Vector3(20f, 10f, 10f);
            }
            else if (rb.name.Contains("ForeArm"))
            {
                forceEnabled.SetWithDefault(false);
                torqueEnabled.SetWithDefault(false);
            }
            else if (rb.name.Contains("Shldr"))
            {
                // forceEnabled.SetWithDefault(false);
                torqueEnabled.SetWithDefault(false);
            }
            // if (rb.name != "rHand")
            // {
            //     forceEnabled.val = torqueEnabled.val = false;
            // }
            // rb.transform.Draw();
            // if (rb.name.Contains("Shldr"))
            // {
            //     directionalTorque = new Vector3(.1f, .5f, .5f);
            // }
            // else if (rb.name.Contains("head"))
            // {
            //     this.torqueScale.val *= 3f;
            // }
            // else if (rb.name.Contains("Shin"))
            // {
            //     useForce.val = false;
            //     directionalTorque = new Vector3(10f, 2f, 1f);
            // }
            
            // else if (rb.name.Contains("Thigh"))
            // {
            //     directionalForce = new Vector3(3f, 3f, 5f);
            //     directionalTorque = new Vector3(3f, 3f, 3f);
            // }
            // else if (rb.name.Contains("hip"))
            // {
            //     directionalTorque = new Vector3(3f, 1f, 3f);
            // }
            // maxForceComponent *= forceScale;
            // maxTorqueComponent *= torqueScale;
        }
        
        public void Update()
        {
            if(!forceEnabled.val && !torqueEnabled.val) return;
            for(int i=0; i<3; i++)
            {
                timers[i] -= Time.fixedDeltaTime;
                if (timers[i] < 0f)
                {
                    timers[i] = Random.Range(2f, 20f);
                    quickness = Random.Range(.1f, poseIdle.maxQuickness.val);
                    if (forceEnabled.val)
                    {
                        forceTarget[i] = poseIdle.scale.val * NormalDistribution.GetValue(0f, directionalForce.val[i],
                            onesided: forceOnesided[i].val, sharpness: 2);
                    }

                    if (torqueEnabled.val)
                    {
                        torqueTarget[i] = poseIdle.scale.val * NormalDistribution.GetValue(0f, directionalTorque.val[i],
                            onesided: torqueOnesided[i].val, sharpness: 2);
                    }

                    SetInfo();
                }
            }
            if (forceEnabled.val)
            {
                force = Vector3.Lerp(force, forceTarget, quickness * Time.fixedDeltaTime);
                target.AddForce(target.transform.TransformDirection(force));
            }

            if (torqueEnabled.val)
            {
                torque = Vector3.Lerp(torque, torqueTarget, quickness * Time.fixedDeltaTime);
                target.AddTorque(target.transform.TransformDirection(torque));
            }
        }

        public void Reset()
        {
            force = torque = Vector3.zero;
            RefreshTargets();
        }

        public void RefreshTargets()
        {
            for (int i = 0; i < timers.Length; i++)
            {
                timers[i] = 0f;
            }
        }

        private void SetInfo()
        {
            if (isEdited) IdleUIProvider.info.val = GetInfo();
        }

        public string GetInfo()
        {
            return $"Force: ({forceTarget.x:0.00}, {forceTarget.y:0.00}, {forceTarget.z:0.00})\n" +
                   $"Torque: ({torqueTarget.x:0.00}, {torqueTarget.y:0.00}, {torqueTarget.z:0.00})";
        }

        public JSONClass Store()
        {
            var jc = new JSONClass();
            forceEnabled.Store(jc);
            torqueEnabled.Store(jc);
            forceScale.Store(jc);
            torqueScale.Store(jc);
            directionalForce.Store(jc, true);
            directionalTorque.Store(jc, true);
            for (int i = 0; i < 3; i++)
            {
                forceOnesided[i].Store(jc);
                torqueOnesided[i].Store(jc);
            }
            return jc;
        }
        
        public void Load(JSONClass jc)
        {
            if(jc.HasKey(target.name))
            {
                jc = jc[target.name].AsObject;
                forceEnabled.Load(jc);
                torqueEnabled.Load(jc);
                forceScale.Load(jc);
                torqueScale.Load(jc);
                directionalForce.Load(jc);
                directionalTorque.Load(jc);
                for (int i = 0; i < 3; i++)
                {
                    forceOnesided[i].Load(jc);
                    torqueOnesided[i].Load(jc);
                }
            }
        }

        private void AutoSettings()
        {
            
        }
    }
}