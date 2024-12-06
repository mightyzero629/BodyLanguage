using UnityEngine;

namespace CheesyFX
{
    public class ChestShaker : Shaker
    {
        private float torqueFactor;
        private float baseTorqueFactor = 400f;
        private float randomTorqueFactor;
        private Vector3 targetTorque;
        private Vector3 currentTorque;
        
        public new ChestShaker Init(Rigidbody rb)
        {
            RB = rb;
            // baseForceFactor *= 2f;
            // baseForceFactor = 0f;
            
            forceFactor = baseForceFactor;
            enabled = false;
            ready = true;
            return this;
        }
        
        public override void Randomize()
        {
            forceQuickness = Random.Range(1f, 2f);
            randomForceFactor = Random.Range(forceFactor*.5f, forceFactor*1.5f);
            
            period = Random.Range(.2f, .3f);
            periodRatio = Random.Range(.2f, .7f);
            rotation = Random.Range(0f, 45f);
            
            randomTorqueFactor = Random.Range(torqueFactor*.5f, torqueFactor*1.5f);
        }
        
        protected override void SetTargets()
        {
            timer -= Time.fixedDeltaTime;
            forceTimer -= Time.fixedDeltaTime;
            if (Random.Range(0f, 1f) < .2f)
            {
                targetForce = Vector3.zero;
                targetTorque = Vector3.zero;
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

        public override void FixedUpdate()
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
                targetTorque *= .25f * shutDownFactor;
                if (shutDownFactor < 0f)
                {
                    enabled = false;
                    shutDownFactor = 4f;
                    shutDown = false;
                }
            }
            currentForce = Vector3.Lerp(currentForce, targetForce, Time.fixedDeltaTime * forceQuickness);
            RB.AddForce(RB.transform.InverseTransformDirection(currentForce), ForceMode.Force);
            currentTorque = Vector3.Lerp(currentTorque, targetTorque, Time.fixedDeltaTime * forceQuickness);
            RB.AddForce(RB.transform.InverseTransformDirection(currentTorque), ForceMode.Force);
        }

        public override void SetForce(float percent) {
            targetForce = percent * randomForceFactor * (Quaternion.AngleAxis(rotation, RB.transform.right) * RB.transform.forward);
            targetTorque = percent * randomTorqueFactor * (Quaternion.AngleAxis(rotation, RB.transform.up) * RB.transform.forward);
        }
        
        public override void Run(float strength)
        {
            forceFactor = baseForceFactor * strength;
            torqueFactor = baseTorqueFactor * strength;
            if(forceFactor == 0f && torqueFactor == 0f) return;
            shutDown = false;
            shutDownFactor = 2f;
            enabled = true;
        }
    }
}