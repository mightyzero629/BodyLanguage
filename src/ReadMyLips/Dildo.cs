using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class Dildo : StimReceiver
    {
        public static JSONStorableBool particlesEnabled = new JSONStorableBool("Particles Enabled (Dildo)", true);
        public static JSONStorableFloat cumShotPower = new JSONStorableFloat("Cumshot Power (Dildo)", 1f, 0f, 5f, false);
        public static JSONStorableFloat maxLoad = new JSONStorableFloat("Max Load (Dildo)", 20f, 5f, 50f);
        public static JSONStorableFloat particleSpeed = new JSONStorableFloat("Particle Speed (Dildo)", 1f, 0f, 10f);
        public static JSONStorableFloat particleAmount = new JSONStorableFloat("Particle Amount (Dildo)", 1f, 0f, 10f);
        public static JSONStorableFloat particleOpacity = new JSONStorableFloat("Particle Opacity (Dildo)", 1f, SetParticleOpacity, 0f, 1f);
        public static JSONStorableBool cumInteracting = new JSONStorableBool("Forced: Only Interacting (Dildo)", false);
        public static JSONStorableBool forceFullLoadJ = new JSONStorableBool("Forced: Full Load (Dildo)", true);
        
        public static JSONStorableFloat stimGainJ = new JSONStorableFloat("Stim Gain (Dildo)", 1f, 0f, 10f, false);
        public static JSONStorableFloat stimRegressionJ = new JSONStorableFloat("Stim Regression (Dildo)", 1f, 0f, 10f, false);
        public static JSONStorableFloat loadGainJ = new JSONStorableFloat("Load Gain (Dildo)", 1f, 0f, 10f, false);
        
        public static JSONStorableStringChooser cumKeyChooser = new JSONStorableStringChooser("cumKeyChooserDildo",
            new List<string> { "Q", "Y", "I", "O", "P", "K", "L" }, "Q", "Orgasm Dildos", SetCumKey);
        
        public override float stimGain => stimGainJ.val;
        public override float stimRegression => stimRegressionJ.val;
        public override float loadGain => loadGainJ.val;
        public override bool forceFullLoad => forceFullLoadJ.val;
        public override bool infoUIOpen => ReadMyLips.dildoInfoUIOpen;
        public static KeyCode cumKey = KeyCode.Q;

        public JSONStorableFloat stiffness;
        
        private static float[] baseRadii =
            { .019f, .01f, .01f, .01f, .01f, .018f, .018f, .015f, .018f, .015f, .015f, .0075f, .018f };
        public float tipHeight = 0.036f;
        private bool collidersModified;

        public new Dildo Init(CapsulePenetrator penetrator)
        {
            base.Init(penetrator);
            stiffness = ((AdjustJointSpringsControl)penetrator.atom.GetStorableByID("springControl")).springStrengthJSON;
            stiffness.val = 1f;
            penisGazeTarget = new Gaze.GazeDildo(this);
            Gaze.RegisterTarget(penisGazeTarget);
            ResetColliders();
            // GetColliderDefaults();
            // foreach (var VARIABLE in base.penetrator.rigidbodies)
            // {
            //     VARIABLE.transform.Draw();
            // }
            // DestroyImmediate(atom.transform.Find("meshContainer")?.gameObject);
            // var meshContainer = new GameObject("meshContainer");
            // var mesh = atom.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            
            // meshContainer.transform.parent = atom.transform;
            // meshContainer.layer = 18;
            // var renderer = meshContainer.AddComponent<MeshRenderer>();
            // var meshFilter = meshContainer.AddComponent<MeshFilter>();
            // meshFilter.sharedMesh = mesh;
            // atom.GetComponentsInChildren<ParticleSystem>().Length.Print();
            // atom.GetComponentsInChildren<Renderer>().ToList().ForEach(x => x.material.shader.Print());
            // var originalRenderer = atom.GetComponentsInChildren<Renderer>()[4];
            // originalRenderer.sharedMaterials.Length.Print();
            // var oldMaterial = originalRenderer.sharedMaterials[0];
            // //Shader.Find("Custom/Subsurface/TransparentGlossNMNoCullSeparateAlpha")
            // var material = new Material(oldMaterial);
            // // {
            // //     shader = Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff")
            // // };
            // var mats = new Material[originalRenderer.sharedMaterials.Length];
			         //
            // // for (int i = 0; i < mats.Length; i++)
            // // {
            // //     // mats[i] = new Material(Shader.Find("Custom/Discard"));
            // //     mats[i] = new Material(originalRenderer.sharedMaterials[i]);
            // // }
            // mats[0] = material;
            //
            // var matOptions = meshContainer.AddComponent<MaterialOptionsWrapper>();
            // matOptions.materialContainer = meshContainer.transform;
            // matOptions.textureGroup1 = new MaterialOptionTextureGroup();
            // matOptions.materialForDefaults = material;
		          //
            // matOptions.paramMaterialSlots = new int[]{0};
            // matOptions.textureGroup1.materialSlots = matOptions.paramMaterialSlots;
            //
            // renderer.material = material;
            // renderer.sharedMaterials = mats;
            // renderer.materials = mats;
            // renderer.enabled = true;
            //
            // matOptions.SetParams();
            // XRay.overlayCamera.enabled = true;
            // matOptions.SetMaterialColor(Color.cyan);
            // matOptions.SetAlpha(.1f);
            // // atom.GetComponentInChildren<MaterialOptions>().customTexture5UrlText.text.Print();
            // // matOptions.SetBumpTexture(new JSONStorableUrl("", atom.GetComponentInChildren<MaterialOptions>().customTexture5UrlText.text));
            // // matOptions.SetAlphaTexture(new JSONStorableUrl("", "Custom/Scripts/CheesyFX/BodyLanguage/XRayTextures/half.png"));
            // // material.SetColor("_Color", Color.cyan);
            // // xrayClient = XRay.RegisterClient(this);
            return this;
        }
        
        public class MaterialOptionsWrapper : MaterialOptions
        {
            public void SetParams(){
                this.SetAllParameters();
            }

            public float renderQueue
            {
                get { return renderQueueJSON.val; }
                set { SetMaterialRenderQueue(Mathf.FloorToInt(value)); }
            }
            
            public void SetAlpha(float val)
            {
                // renderers[0].materials[0].SetFloat("_AlphaAdjust", val);
                base.SetMaterialParam("_AlphaAdjust", val);
            }

            public void SetMaterialColor(Color c) => base.SetMaterialColor("_Color", c);

            public void SetAlphaTexture(JSONStorableString jurl)
            {
                if(jurl == null) return;
                SyncCustomTexture4Url(jurl);
            }
            
            public void SetBumpTexture(JSONStorableString jurl)
            {
                if(jurl == null) return;
                SyncCustomTexture5Url(jurl);
            }
        }

        private void Bend(Rigidbody rb, Orifice orifice, float bendFactor, bool dialIn = false, float depth = .02f)
        {
            var dot = Vector3.Dot(orifice.entrance.transform.up, rb.transform.position - orifice.entrance.transform.position);
            if(dot > depth)
            {
                if (!SuperController.singleton.freezeAnimation)
                {
                    // if(ctrl.name == "penisTipControl") dot.Print();
                    var scale = dialIn ? Mathf.Lerp(0f, bendFactor, (dot - .02f) * 5f) : bendFactor;
                    var cross = scale * Vector3.Cross(rb.transform.up, orifice.guidance.up);
                    rb.AddTorque(cross, ForceMode.Acceleration);
                }
            }
        }

        private static void SetCumKey(string val)
        {
            cumKey = (KeyCode)Enum.Parse(typeof(KeyCode), val);
        }
        
        private static void SetParticleOpacity(float val)
        {
            foreach (var person in FillMeUp.dildos)
            {
                var main = person.ps1.main;
                main.startColor = new Color(1f, 0.97f, 0.91f, val);
            }
        }
        
        public void FixedUpdate()
        {
            // if (!(penetrator?.fuckable is Throat)) return;
            if(penetrator == null || !(fuckable is Orifice)) return;
            var orifice = fuckable as Orifice;
            if (orifice is Throat)
            {
                SyncCollidersBJ();
                // ShrinkColliders(.9f);
                Bend(penetrator.rigidbodies[1], orifice, -5000f, true);
                Bend(penetrator.rigidbodies[2], orifice, -10000f, false, -.02f);
            }
            else
            {
                Bend(penetrator.rigidbodies[1], orifice, 5000f, true);
                Bend(penetrator.rigidbodies[2], orifice, 5000f);
            }
        }
        
        public override void SyncColliderBJ(int id)
        {
            CapsuleCollider cap = penetrator.colliders[id] as CapsuleCollider;
            float defaultRadius = baseRadii[id]*.95f;
            if (Vector3.Dot(FillMeUp.throat.entrance.transform.up, cap.transform.position - FillMeUp.throat.entrance.transform.position) > .4f*cap.height)
            {
                var targetRadius = fuckable.penetratorScaling.val * defaultRadius;
                if (cap.radius > 1.01f * targetRadius)
                {
                    // penetrator.rigidbodies[Math.Min(id, 2)].mass = .02f;
                    cap.radius = Mathf.Lerp(cap.radius, targetRadius, 3f * Time.deltaTime);
                    if(id == 10) cap.height = Mathf.Lerp(cap.height, tipHeight * (1f+fuckable.penetratorScaling.val)*.5f, 3f * Time.deltaTime);
                }
            }
            else if(cap.radius != defaultRadius)
            {
                cap.radius = defaultRadius;
                if (id == 10) cap.height = tipHeight;
                // penetrator.rigidbodies[Math.Min(id, 2)].mass = .2f;
            }
        }
        
        public override void SyncCollidersBJ()
        {
            if(!collidersModified) collidersModified = true;
            for (int i = 0; i < penetrator.colliders.Count; i++)
            {
                SyncColliderBJ(i);
            }
        }

        public override void ShrinkColliders(float percent)
        {
            collidersModified = true;
            for (int i = 0; i < penetrator.colliders.Count; i++)
            {
                CapsuleCollider cap = penetrator.colliders[i] as CapsuleCollider;
                cap.radius = baseRadii[i] * percent;
            }
            // $"{((CapsuleCollider)colliders[0]).radius} {autoColliders[0].colliderRadius}".Print();
        }
        
        public override void ResetColliders()
        {
            for (int i = 0; i < penetrator.colliders.Count; i++)
            {
                CapsuleCollider cap = penetrator.colliders[i] as CapsuleCollider;
                cap.radius = baseRadii[i];
                if (i == 3) cap.height = tipHeight;
            }

            for (int i = 0; i < 3; i++)
            {
                penetrator.rigidbodies[i].mass = .2f;
            }

            collidersModified = false;
        }
        
        // private void GetColliderDefaults()
        // {
        //     if(collidersModified) return;
        //     baseRadii.Clear();
        //     foreach (var aut in penetrator.colliders)
        //     {
        //         baseRadii.Add(((CapsuleCollider)aut).radius);
        //         if(atom != FillMeUp.atom) baseRadii.Last().Print();
        //     }
        //
        //     tipHeight = penetrator.capsule.height;
        // }
    }
}