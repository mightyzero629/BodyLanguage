using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public class EmoteSprayer
    {
        public GameObject go;
        public ParticleSystem ps;
        public ParticleSystemRenderer psRenderer;
        public ParticleSystem.MainModule main;
        public ParticleSystem.EmissionModule emission;
        public ParticleSystem.ShapeModule shape;
        
        private static float burstTimer;
        private static IEnumerator burstTimeout;
        
        private Texture2D tex;
        private Texture2D baseTex;
        private Texture2D mask;
        
        private static string basePath;

        public JSONStorableBool enabled = new JSONStorableBool("Enabled", true);

        public JSONStorableStringChooser textureChoice = new JSONStorableStringChooser("Texture", EmoteManager.textureChoices, "Hearts/01", "Texture");
        public JSONStorableStringChooser parent = new JSONStorableStringChooser("Parent", new List<string> {"head", "LabiaTrigger"}, "head", "Parent");
        public JSONStorableColor color = new JSONStorableColor("Color", (new Color(1f, 0f, 0.35f)).ToHSV());
        private JSONStorableFloat size = new JSONStorableFloat("Size", .05f, 0f, .5f);
        private JSONStorableFloat speed = new JSONStorableFloat("Speed", .2f, 0f, 10f);
        public JSONStorableFloat lifetime = new JSONStorableFloat("Lifetime", 2.5f, 1f, 10f);
        private JSONStorableFloat gravity = new JSONStorableFloat("Gravity", -.01f, -1f, 1f);
        private JSONStorableFloat heightOffset = new JSONStorableFloat("Height Offset", .1f, -.1f, .3f);
        
        
        public virtual void Init()
        {
            basePath = FillMeUp.packageUid + "Custom/Scripts/CheesyFX/BodyLanguage/EmoteTextures/";
            go = new GameObject("EmoteSprayer");
            go.transform.SetParent(ReadMyLips.atom.forceReceivers.First(x => x.name == "head").transform, false);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            psRenderer = go.GetComponent<ParticleSystemRenderer>();
            psRenderer.material.shader = Shader.Find("Particles/Additive");
            main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSize = .05f;
            main.startSpeed = .2f;
            main.gravityModifier = -.01f;
            main.startLifetime = 2f;
            main.playOnAwake = false;
            shape = ps.shape;
            shape.rotation = new Vector3(-90, 0f, 0f);
            shape.angle = 60f;
            shape.radius = .1f;
            emission = ps.emission;
            main.duration = 5f;
            main.loop = false;
            var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            limitVelocityOverLifetime.drag = 10000f;
            SetRate(0f);
            // SetTexture(textureName);
            // Colorize(color);

            textureChoice.AddCallback(SetTexture);
            color.setJSONCallbackFunction += Colorize;
            size.AddCallback(SetSize);
            speed.AddCallback(SetSpeed);
            lifetime.AddCallback(SetLifetime);
            gravity.AddCallback(SetGravity);
            heightOffset.AddCallback(val => go.transform.localPosition = new Vector3(0f, val, 0f));
            parent.AddCallback(SetParent);
            
        }

        public void Trigger()
        {
            if (!enabled.val) return;
            ps.Play();
        }

        private void SetParent(string val)
        {
            go.transform.SetParent(ReadMyLips.atom.rigidbodies.First(x => x.name == val).transform, false);
            if (val == "head")
            {
                shape.rotation = new Vector3(-90, 0f, 0f);
                shape.angle = 60f;
                shape.radius = .1f;
                heightOffset.val = .1f;
            }
            else
            {
                shape.rotation = new Vector3(30, 0f, 0f);
                shape.angle = 30f;
                shape.radius = .05f;
                heightOffset.val = -.05f;
            }
        }

        public void SetTexture(string name)
        {
            tex = LoadTexture($"{basePath}{name}.png");
            baseTex  = LoadTexture($"{basePath}{name}.png");
            if (FileManagerSecure.FileExists($"{basePath}{name}m.png"))
            {
                mask = LoadTexture($"{basePath}{name}m.png");
            }
            else
            {
                mask = null;
            }
            psRenderer.material.mainTexture = tex;
            Colorize(color);
        }

        public static Texture2D LoadTexture(string path)
        {
            var tex = new Texture2D(2, 2);
            var data = FileManagerSecure.ReadAllBytes(path);
            tex.LoadImage(data);
            tex.Apply();
            return tex;
        }

        private void Colorize(JSONStorableColor color)
        {
            Colorize(color.val.ToRGB());
        }

        public void Colorize(Color color)
        {
            if (mask == null)
            {
                psRenderer.material.color = color;
                return;
            }
            psRenderer.material.color = Color.white;
            var pixels = baseTex.GetPixels();
            var maskPixels = mask.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] *= Color.Lerp(color, pixels[i], maskPixels[i].a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
        }
        
        private void SetGravity(float val)
        {
            main.gravityModifier = val;
        }
        
        private void SetSize(float val)
        {
            main.startSize = val;
        }
        
        public void SetRate(float val)
        {
            emission.rateOverTimeMultiplier = val;
        }
        
        private void SetSpeed(float val)
        {
            main.startSpeed = val;
            var limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            limitVelocityOverLifetime.drag = Mathf.Lerp(10000f, 3000f, val*.1f);
        }

        private void SetLifetime(float val)
        {
            main.startLifetime = val;
        }
        
        public virtual void CreateUI(List<object> UIElements)
        {
            enabled.CreateUI(UIElements, rightSide:false);
            color.CreateUI(UIElements, rightSide:false);
            // test.CreateUI(UIElements:UIElements, rightSide:true);
            textureChoice.CreateUI(UIElements, rightSide: true, chooserType: 1);
            parent.CreateUI(UIElements, rightSide:true, chooserType: 1);
            size.CreateUI(UIElements, rightSide:true);
            speed.CreateUI(UIElements, rightSide:true);
            gravity.CreateUI(UIElements, rightSide:true);
            lifetime.CreateUI(UIElements, rightSide:true);
            heightOffset.CreateUI(UIElements, rightSide:true);
        }

        public virtual JSONClass Store()
        {
            var jc = new JSONClass();
            enabled.Store(jc);
            color.Store(jc);
            textureChoice.Store(jc);
            parent.Store(jc);
            size.Store(jc);
            speed.Store(jc);
            gravity.Store(jc);
            lifetime.Store(jc);
            heightOffset.Store(jc);
            return jc;
        }
        
        public virtual void Load(JSONClass jc)
        {
            enabled.Load(jc);
            color.Load(jc);
            textureChoice.Load(jc);
            parent.Load(jc);
            size.Load(jc);
            speed.Load(jc);
            gravity.Load(jc);
            lifetime.Load(jc);
            heightOffset.Load(jc);
        }

        public void Destroy()
        {
            Object.Destroy(go);
        }
    }
}