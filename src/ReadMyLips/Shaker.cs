using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Shaker : MonoBehaviour
    {
        protected Rigidbody RB;
        protected float timer;
        protected float forceTimer;
        protected float flip;
        protected bool applyForceOnReturn = true;

        protected Vector3 currentForce;
        protected Vector3 targetForce;
        public float forceFactor;
        protected float randomForceFactor;
        protected float rotation;

        protected float periodRatio = .5f;
        protected float period = .15f;
        public float baseForceFactor = 400f;
        protected float forceDuration = 1f;
        protected float forceQuickness = 1f;
        private float torqueQuickness = 1f;

        protected float rngTimer;
        protected bool shutDown;
        protected float shutDownFactor;

        protected bool ready;

        public Shaker Init(Rigidbody rb)
        {
            RB = rb;
            if(RB.name.StartsWith("l")) baseForceFactor = -baseForceFactor;

            forceFactor = baseForceFactor;
            enabled = false;
            ready = true;
            return this;
        }

        public virtual void Randomize()
        {
            forceQuickness = Random.Range(1f, 2f);
            randomForceFactor = Random.Range(forceFactor*.8f, forceFactor*1.5f);
            
            period = Random.Range(.15f, .25f);
            periodRatio = Random.Range(.3f, .8f);
            rotation = Random.Range(0f, 90f);
        }

        protected virtual void SetTargets()
        {
            timer -= Time.fixedDeltaTime;
            forceTimer -= Time.fixedDeltaTime;
            if (Random.Range(0f, 1f) < .1f)
            {
                targetForce = Vector3.zero;
                return;
            }
            if (timer < 0.0f) {
                if ((flip > 0f && periodRatio != 1f) || periodRatio == 0f) {
                    if (applyForceOnReturn) {
                        flip = -1f;
                    } else {
                        flip = 0f;
                    }
                    timer = period * (1f - periodRatio);
                    forceTimer = forceDuration * period;
                } else {
                    flip = 1f;
                    timer = period * periodRatio;
                    forceTimer = forceDuration * period;
                }
                SetForce(flip);
            } else if (forceTimer < 0.0f) {
                SetForce(0f);
            }
        }

        public virtual void FixedUpdate() {
            try
            {
                if (SuperController.singleton.freezeAnimation) return;
                rngTimer -= Time.fixedDeltaTime;
                if (rngTimer < 0f)
                {
                    Randomize();
                    rngTimer = .5f;
                }
                SetTargets();
                if (shutDown)
                {
                    shutDownFactor -= Time.fixedDeltaTime;
                    targetForce *= .25f * shutDownFactor;
                    if (shutDownFactor < 0f)
                    {
                        enabled = false;
                        shutDownFactor = 4f;
                        shutDown = false;
                    }
                }
                currentForce = Vector3.Lerp(currentForce, targetForce, Time.fixedDeltaTime * forceQuickness);
                RB.AddForce(RB.transform.InverseTransformDirection(currentForce), ForceMode.Force);
            }
            catch (Exception e) {
                SuperController.LogError("Exception caught: " + e);
            }
        }
        
        public virtual void SetForce(float percent) {
            targetForce = percent * randomForceFactor * (Quaternion.AngleAxis(rotation, RB.transform.up) * RB.transform.right);
        }

        public virtual void Run(float strength)
        {
            forceFactor = baseForceFactor * strength;
            if(forceFactor == 0f) return;
            shutDown = false;
            shutDownFactor = 2f;
            enabled = true;
        }
        
        public void ShutDown()
        {
            if(!enabled) return;
            shutDown = true;
        }
    }
}