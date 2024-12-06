using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace CheesyFX
{
    public class ForceParam
    {
        public string name;
        public float current;
        public float target;
        // public float mean;
        // public float delta;
        // public float sharpness;
        // public float quickness;
        public JSONStorableFloat mean = new JSONStorableFloat("Mean", 0f, 0f, 1000f, false);
        public JSONStorableFloat delta = new JSONStorableFloat("Delta", 0f, 0f, 1000f, false);
        public JSONStorableFloat sharpness = new JSONStorableFloat("Distribution Sharpness", 2f, 1f, 3f);
        public JSONStorableFloat transitionQuicknessMean = new JSONStorableFloat("Transition Quickness Mean", 1f, 0f, 10f);
        public JSONStorableFloat transitionQuicknessDelta = new JSONStorableFloat("Transition Quickness Delta", .5f, 0f, 10f);
        public JSONStorableFloat randomizeTimeMean = new JSONStorableFloat("Randomize Time mean", 5f, 1f, 10f);
        public JSONStorableFloat randomizeTimeDelta = new JSONStorableFloat("Randomize Time Delta", 3f, 1f, 10f);

        public JSONStorableBool onesided = new JSONStorableBool("Onesided Distribution", false);
        public JSONStorableBool useNormalDistribution = new JSONStorableBool("Use Normal Distribution", true);

        public float transitionQuickness;
        public bool atTarget = true;

        private float timer;

        public ForceParam(string name, float mean, float delta)
        {
            this.name = name;
            this.mean.val = this.mean.defaultVal = mean;
            this.delta.val = this.delta.defaultVal = delta;

            this.mean.setCallbackFunction += val => GetNewTarget(0f);
        }

        public void Reset()
        {
            current = 0f;
        }
        
        public void Update()
        {
            timer -= Time.deltaTime;
            if (timer < 0f)
            {
                timer = NormalDistribution.GetValue(randomizeTimeMean.val, randomizeTimeDelta.val);
                GetNewTarget(delta.val);
            }
            LerpToTarget();
        }

        public void LerpToTarget()
        {
            if(!atTarget)
            {
                current = Mathf.Lerp(current, target, Time.deltaTime * transitionQuickness);
                if(Mathf.Abs(target - current) < .01f)
                {
                    current = target;
                    atTarget = true;
                    
                }
            }
        }
        
        public void GetNewTarget()
        {
            target = NormalDistribution.GetValue(mean.val, delta.val, sharpness.val, onesided.val, useNormalDistribution.val);
            transitionQuickness = NormalDistribution.GetValue(transitionQuicknessMean.val, transitionQuicknessDelta.val);
            atTarget = false;
            timer = randomizeTimeMean.val;
        }

        public UnityEvent onGetNewTarget = new UnityEvent();
        private void GetNewTarget(float delta)
        {
            target = NormalDistribution.GetValue(mean.val, delta, sharpness.val, onesided.val, useNormalDistribution.val);
            transitionQuickness = NormalDistribution.GetValue(transitionQuicknessMean.val, transitionQuicknessDelta.val);
            atTarget = false;
            onGetNewTarget.Invoke();
        }

        public void Store(JSONClass jsonClass, bool forceStore = false)
        {
            var jc = new JSONClass();
            bool doStore = forceStore || mean.Store(jc, forceStore);
            doStore = delta.Store(jc, forceStore) || doStore;
            doStore = sharpness.Store(jc, forceStore) || doStore;
            doStore = transitionQuicknessMean.Store(jc, forceStore) || doStore;
            doStore = transitionQuicknessDelta.Store(jc, forceStore) || doStore;
            doStore = randomizeTimeMean.Store(jc, forceStore) || doStore;
            doStore = randomizeTimeDelta.Store(jc, forceStore) || doStore;
            doStore = onesided.Store(jc, forceStore) || doStore;
            doStore = useNormalDistribution.Store(jc, forceStore) || doStore;
            if(doStore) jsonClass[name] = jc;
        }
        
        public void Load(JSONClass jsonClass, bool setMissingToDefault = false)
        {
            if (!jsonClass.HasKey(name))
            {
                SetToDefault();
                return;
            }
            var jc = jsonClass[name].AsObject;
            mean.Load(jc, setMissingToDefault);
            delta.Load(jc, setMissingToDefault);
            sharpness.Load(jc, setMissingToDefault);
            transitionQuicknessMean.Load(jc, setMissingToDefault);
            transitionQuicknessDelta.Load(jc, setMissingToDefault);
            randomizeTimeMean.Load(jc, setMissingToDefault);
            randomizeTimeDelta.Load(jc, setMissingToDefault);
            onesided.Load(jc, setMissingToDefault);
            useNormalDistribution.Load(jc, setMissingToDefault);
        }

        private void SetToDefault()
        {
            mean.SetValToDefault();
            delta.SetValToDefault();
            sharpness.SetValToDefault();
            transitionQuicknessMean.SetValToDefault();
            transitionQuicknessDelta.SetValToDefault();
            randomizeTimeMean.SetValToDefault();
            randomizeTimeDelta.SetValToDefault();
            onesided.SetValToDefault();
            useNormalDistribution.SetValToDefault();
        }
    }
}