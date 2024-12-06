using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class CumshotHandler
    {
        public bool cumming;
        public bool isInsideOrifice;
        private bool orificePsSet;
        public JSONStorableFloat load = new JSONStorableFloat("Load", 0f, 0f, 20f, true, false);

        private float baseLoad = 3f;

        private StimReceiver receiver;
        public ParticleSystem ps;
        // public ParticleSystem emitter;
        public ParticleSystem orificePs;

        private ParticleSystem.MainModule main;
        private ParticleSystem.EmissionModule emission;
        private ParticleSystem.EmissionModule orificeEmission;

        public IEnumerator cum;
        private WaitForEndOfFrame wait;

        private bool hasClothes;
        private Type receiverType;
        
        public JSONStorableBool particlesEnabled;
        public JSONStorableFloat cumShotPower;
        public JSONStorableFloat particleSpeed;
        public JSONStorableFloat particleAmount;

        public CumshotHandler(StimReceiver receiver)
        {
            this.receiver = receiver;
            ps = receiver.ps1;
            load.name = $"{receiver.penetrator.atom.name} Load";
            // emitter = ps;
            main = ps.main;
            emission = ps.emission;
            wait = new WaitForEndOfFrame();
            if (this.receiver is Person || this.receiver is AltFutaStim)
            {
                particlesEnabled = Person.particlesEnabled;
                cumShotPower = Person.cumShotPower;
                particleSpeed = Person.particleSpeed;
                particleAmount = Person.particleAmount;
            }
            else
            {
                particlesEnabled = Dildo.particlesEnabled;
                cumShotPower = Dildo.cumShotPower;
                particleSpeed = Dildo.particleSpeed;
                particleAmount = Dildo.particleAmount;
            }
        }

        private List<float> timings = new List<float>();
        private List<float> strengths = new List<float>();
        private int currentBurst;

        private void QueueBursts()
        {
            timings.Clear();
            strengths.Clear();
            var tempLoad = load.val;
            var cumulativeTiming = 0f;
            float strength;
            while (tempLoad > 1f)
            {
                timings.Add(cumulativeTiming);
                if (cumulativeTiming == 0f) strength = Random.Range(2f, 3.5f);
                else strength = Random.Range(1f, Mathf.Min(3.5f, load.val));
                strengths.Add(strength);
                // break;
                tempLoad -= strength;
                cumulativeTiming += Random.Range(.1f, .8f);
            }
            // timings.Count.Print();
        }

        private void ConsumeBurst()
        {
            if (currentBurst == timings.Count) return;
            var strength = strengths[currentBurst];
            currentBurst++;
            load.val -= strength;
            
            if(particlesEnabled.val)
            {
                emission.rateOverTimeMultiplier = strength * 750f * particleAmount.val;
                if(!isInsideOrifice) main.startSpeedMultiplier = .5f * strength * strength * particleSpeed.val * cumShotPower.val;
            }
            if(strength < 2f) return;
            if(receiver.penetrator.type == 1)
            {
                receiver.penetrator.tipCollider.attachedRigidbody.AddForce(strength*(receiver.penetrator.tip.up));
                receiver.penetrator.root.AddForce(100f*strength*(receiver.penetrator.root.transform.forward));
            }
            else
            {
                receiver.penetrator.tipCollider.attachedRigidbody.AddForce(5f*strength*(receiver.penetrator.tip.forward));
            }
        }

        private IEnumerator Cum()
        {
            currentBurst = 0;
            float currentTiming = timings[currentBurst];
            int clothBurst = 0;
            var timer = -.8f;
            // isInsideOrifice.Print();
            while(true)
            {
                if(!isInsideOrifice) main.startSpeedMultiplier = Mathf.Lerp(main.startSpeedMultiplier, 0f, 2f * Time.deltaTime);
                emission.rateOverTimeMultiplier = Mathf.Lerp(emission.rateOverTimeMultiplier, 0f, 15f * Time.deltaTime);
                if (orificePsSet) orificeEmission.rateOverTimeMultiplier = emission.rateOverTimeMultiplier;
                
                if (Person.cumClothingEnabled.val && (!receiver.isFucking || receiver.fuckable.type > 2) && clothBurst < currentBurst + 1 && timer >= currentTiming - .7f)
                {
                    receiver.LaunchClothing(strengths[currentBurst]);
                    clothBurst++;
                }

                if (timer >= currentTiming)
                {
                    ConsumeBurst();
                    if (currentBurst >= timings.Count) break;
                    currentTiming = timings[currentBurst];
                    // if(receiver.type == 1) receiver.maleShaker.Run(2f*Male.shakeStrength.val);
                }
                timer += Time.deltaTime;
                yield return wait;
            }

            if (particlesEnabled.val)
            {
                while (emission.rateOverTimeMultiplier > 10f)
                {
                    main.startSpeedMultiplier = Mathf.Lerp(main.startSpeedMultiplier, 0f, 2f * Time.deltaTime);
                    emission.rateOverTimeMultiplier = Mathf.Lerp(emission.rateOverTimeMultiplier, 0f, 15f * Time.deltaTime);
                    if (orificePsSet) orificeEmission.rateOverTimeMultiplier = emission.rateOverTimeMultiplier;
                    yield return wait;
                }                
            }
            cumming = false;
            ps.Stop();
            if (orificePsSet)
            {
                orificePs.Stop();
                if (!isInsideOrifice) orificePsSet = false;
            }
            if(receiver.type == 1) receiver.maleShaker.ShutDown();
        }

        public void Start()
        {
            try
            {
                if(cumming) return;
                QueueBursts();
                if(timings.Count == 0) return;
                cum = Cum().Start();
                cumming = true;
                if(isInsideOrifice)
                {
                    orificeEmission.rateOverTimeMultiplier = 0f;
                    orificePs.Play();
                }
                else
                {
                    emission.rateOverTimeMultiplier = 0f;
                    ps.Play();
                }
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        public void Stop()
        {
            cum.Stop();
            ps.Stop();
            cumming = false;
        }

        public void SetEmitter(ParticleSystem prtSys)
        {
            try
            {
                if (prtSys != null)
                {
                    isInsideOrifice = orificePsSet = true;
                    orificePs = prtSys;
                    orificeEmission = orificePs.emission;
                    ps.Stop();
                    if(cumming) orificePs.Play();
                }
                else
                {
                    isInsideOrifice = false;
                    if(cumming) ps.Play();
                }
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        public void SetClothes(List<CumClothing> clothes)
        {
            hasClothes = true;
        }
    }
}