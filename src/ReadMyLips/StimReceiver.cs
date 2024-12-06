using System;
using System.Linq;
using System.Xml.Schema;
using MacGruber;
using UnityEngine;
using UnityEngine.UI;

namespace CheesyFX
{
    public abstract class StimReceiver : MonoBehaviour
    {
        private bool initialized;
        public Atom atom => penetrator.atom;
        public string uid => penetrator.atom.uid;
        public int type => penetrator.type;
        public CapsulePenetrator penetrator;
        public JSONStorableFloat stimulation = new JSONStorableFloat("Stimulation", 0f, 0f, 1f, true, false);
        public abstract float stimGain { get;}
        public abstract float stimRegression { get;}
        public abstract float loadGain { get;}
        public abstract bool forceFullLoad { get; }
        private float _dynamicStimGain;
        private Color stimColor;
        public XRayClient xrayClient;
        public Gaze.GazeTarget penisGazeTarget;

        public virtual float dynamicStimGain
        {
            get
            {
                return _dynamicStimGain;
            }
            set
            {
                _dynamicStimGain = value;
            }
        }

        protected float dynamicStimRegression;

        public Fuckable fuckable
        {
            get { return penetrator.fuckable; }
            set { penetrator.fuckable = value; }
        }

        public bool isFucking
        {
            get { return penetrator.isFucking; }
            set { penetrator.isFucking = value; }
        }
        private GameObject fluidGO1;
        private GameObject fluidGO2;
        public ParticleSystem ps1;
        public ParticleSystem ps2;
        
        public DAZMorph foreski;

        public CumshotHandler cumshotHandler;
        
        protected float orgasmTimer;
        protected bool orgasming;

        protected float timer;

        private Rigidbody lBreast;
        private Rigidbody rBreast;

        public EventTrigger onOrgasmTrigger;
        public JSONStorableAction orgasm;

        private float depthNormalizer = 1f;
        
        public MaleShaker maleShaker;
        
        public virtual bool infoUIOpen { get; }
        public Image stimbarImg;

        public StimReceiver Init(CapsulePenetrator penetrator)
        {
            this.penetrator = penetrator;
            this.penetrator.stimReceiver = this;
            stimulation.name = this.penetrator.atom.name + " Stimulation";
            enabled = false;

            fluidGO1 = CreateFluidGO(ref ps1, "BL_FluidGameObject01");
            // ps1 = fluidGO1.AddComponent<ParticleSystem>();
            fluidGO2 = CreateFluidGO(ref ps2,"BL_FluidGameObject02");
            // fluidGO2.transform.SetParent(fluidGO1.transform);
            // ps2 = fluidGO2.AddComponent<ParticleSystem>();
            // if (ReadMyLips.fluidMaterial != null) SetFluidMaterial(ReadMyLips.fluidMaterial);
            InitPS(ps1);
            InitPS(ps2);
            var main = ps2.main;
            main.startSize = .001f;
            // main.gravityModifier = 1f;
            main.startSpeed = 0f;
            // main.startColor = Color.red;

            cumshotHandler = new CumshotHandler(this);

            var emission1 = ps1.emission;
            // emission1.rateOverTimeMultiplier = 200f;
            emission1 = ps2.emission;
            
            var subEmitters = ps1.subEmitters;
            subEmitters.AddSubEmitter(ps2, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);
            subEmitters.enabled = true;
            var iv = ps2.inheritVelocity;
            iv.mode = ParticleSystemInheritVelocityMode.Initial;
            iv.curveMultiplier = .75f;
            iv.enabled = true;

            lBreast = FillMeUp.atom.rigidbodies.FirstOrDefault(x => x.name == "lPectoral");
            rBreast = FillMeUp.atom.rigidbodies.FirstOrDefault(x => x.name == "rPectoral");

            onOrgasmTrigger = new EventTrigger(ReadMyLips.singleton, $"{uid} On Orgasm");
            orgasm = new JSONStorableAction($"{uid} Orgasm", () => ForceOrgasm());
            ReadMyLips.singleton.RegisterAction(orgasm);

            dynamicStimGain = stimGain;
            dynamicStimRegression = stimRegression;
            stimColor = penetrator.atom == FillMeUp.atom ? ReadMyLips.femaleStimColor.val.ToRGB() : ReadMyLips.maleStimColor.val.ToRGB();
            initialized = true;
            return this;
        }

        protected virtual GameObject CreateFluidGO(ref ParticleSystem ps, string name)
        {
            var go = new GameObject
            {
                name = name,
                transform =
                {
                    parent = penetrator.tipCollider.transform,
                    localPosition = new Vector3(0f, .04f, 0f),
                    localEulerAngles = new Vector3(-90f, 0f, 0f)
                }
            };
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            
            return go;
        }

        public void SetFluidMaterial(Material material)
        {
            var renderer = ps1.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Additive"));
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.material.color = Color.white;
            
            renderer = ps2.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
        }

        public static void InitPS(ParticleSystem ps)
        {
            ps.Stop();
            var shape = ps.shape;
            shape.radius = .00005f;
            shape.angle = .01f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
            var main = ps.main;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.simulationSpeed = .5f;
            main.startLifetime = 1f;
            main.startSize = .0025f;
            main.startSpeed = 10f;
            // main.startColor = new Color(1f, 1f, 1f, .75f);
            var emission = ps.emission;
            emission.rateOverTime = 10f;
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.collidesWith = collision.collidesWith &= ~(1 << 8);
            collision.bounce = 0f;
            collision.dampen = .95f;
            main.playOnAwake = false;
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Additive"));
            renderer.material.mainTexture = EmoteSprayer.LoadTexture($"{FillMeUp.packageUid}Custom/Scripts/CheesyFX/BodyLanguage/FluidTextures/alpha.png");
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            var col = new Color(1f, 0.97f, 0.91f, 1f);
            // col.a = .3f;
            main.startColor = col;
        }

        public virtual void ResetColliders(){}
        public virtual void ShrinkColliders(float percent){}
        
        public virtual void SyncCollidersBJ(){}
        
        public virtual void SyncColliderBJ(int id){}
        
        public virtual void Stiffen(){}
        
        public virtual void StiffenHalf(){}
        
        public virtual void StiffenReset(){}


        public virtual float Stimulate()
        {
            // var delta = (1f + .03f * cumshotHandler.load.val)*.2f * Mathf.Abs(fuckable.speed.val) * fuckable.depth.val * depthNormalizer * Time.deltaTime;
            var delta = dynamicStimGain *.2f * Mathf.Abs(fuckable.speed.val) * fuckable.depth.val * depthNormalizer * Time.deltaTime;
            if (fuckable.type < 3) delta /= penetrator.length;
            if (fuckable.type == 4)
            {
                var d = .005f * (lBreast.velocity.magnitude + rBreast.velocity.magnitude);
                delta += d;
                // d.Print();
            }
            return delta;
        }

        private float lastStim;
        public virtual void Update()
        {
            if (timer > 0f) timer -= Time.deltaTime;
            if(isFucking) stimulation.val += Stimulate();
            if (!orgasming && timer <= 0f && stimulation.val < .01f && !cumshotHandler.cumming)
            {
                if(isFucking) return;
                stimulation.val = 0f;
                enabled = false;
                // fluidHandler.Stop();
            }
            // stimulation.val.Print();
            if (!orgasming && stimulation.val >= 1f)
            {
                Orgasm();
            }
            // dynamicStimGain.PrintEvery(2f);
            var stimDelta = stimulation.val - lastStim;
            dynamicStimGain += .03f * Mathf.Abs(stimDelta);
            if (stimDelta > 0f) cumshotHandler.load.val += loadGain*stimDelta*(1f+stimulation.val);
            
            stimulation.val = Mathf.Lerp(stimulation.val, 0f, .8f*dynamicStimRegression*Time.fixedDeltaTime);

            // if (orgasming || fluidHandler.pumping)
            // {
            //     fluidHandler.Update();
            // }
            
            if (orgasming)
            {
                orgasmTimer -= Time.deltaTime;
                if (orgasmTimer < 0f)
                {
                    OrgasmEnd();
                }
            }
            lastStim = stimulation.val;
            onOrgasmTrigger.Update();
            if(ReadMyLips.singleton.UITransform.gameObject.activeSelf && infoUIOpen) stimbarImg.color = Color.Lerp(Color.white, stimColor, (dynamicStimGain - stimGain));
            // dynamicStimGain.PrintEvery(.5f);
            // stimbarImg.color = Color.cyan;
        }

        public virtual void Orgasm()
        {
            if (cumshotHandler.load.val < 1f) return;
            orgasming = true;
            dynamicStimRegression = 3f*stimRegression;
            orgasmTimer = 10f;
            cumshotHandler.Start();
            onOrgasmTrigger.Trigger();
            ReadMyLips.Stimulate(100f, true, true);
            if(penisGazeTarget.enabled.val) Gaze.FocusAll(penisGazeTarget);
        }

        public virtual void OrgasmEnd()
        {
            dynamicStimRegression = stimRegression;
            dynamicStimGain = stimGain;
            orgasming = false;
        }

        public void ForceOrgasm(bool fullLoad = false)
        {
            if(fullLoad || forceFullLoad) cumshotHandler.load.val = cumshotHandler.load.max;
            Orgasm();
        }
        
        public virtual void LaunchClothing(float strength){}
        
        public virtual void UnequipClothes() {}

        public void SetFucking(Fuckable fuckable)
        {
            this.fuckable = fuckable;
            if (fuckable == null)
            {
                isFucking = false;
                stimulation.name = penetrator.atom.name;
                cumshotHandler.SetEmitter(null);
                
                return;
            }

            var orifice = fuckable as Orifice;
            cumshotHandler.SetEmitter(!ReferenceEquals(orifice, null) ? orifice.ps : null);
            timer = 2f;
            stimulation.name = $"{penetrator.atom.name}: {fuckable.name}";
            // if (stimulation.slider != null) stimulation.slider.name = $"{penetrator.atom.name}: {fuckable.name}";
            isFucking = true;
            enabled = true;
            depthNormalizer = this.fuckable.type > 2 ? 1f : 1f / penetrator.length;
        }

        private void OnDisable()
        {
            if (!initialized) return;
            stimulation.name = penetrator.atom.name + " Stimulation";
            cumshotHandler.Stop();
        }

        public virtual void OnDestroy()
        {
            try
            {
                // "StimReceiver OnDestroy".Print();
                Destroy(fluidGO1);
                Destroy(fluidGO2);
                cumshotHandler.Stop();
                onOrgasmTrigger.Remove();
                XRay.DeregisterClient(xrayClient);
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
                throw;
            }
        }
    }
}