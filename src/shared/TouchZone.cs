using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class TouchZone : BodyRegion
    {
        private System.Random rdn = new System.Random();
        public TouchCollisionListener touchCollisionListener;
        public WatchListener watchListener;
        public bool isTouched => touchCollisionListener.isOnStay;
        
        // public CumRegion cumRegion;
        // public List<Atom> atomsToCollideWith = new List<Atom>();
        // public List<Action> onWatchActions = new List<Action>();
        // public UnityEvent onEnterEvent => touchCollisionListener.onEnterEvent;
        // public UnityEvent onStayEvent => touchCollisionListener.onStayEvent;
        // public UnityEvent onExitEvent => touchCollisionListener.onExitEvent;
        
        public JSONStorableBool slapEnabled;
        public GameObject slapAudioObject;
        public AudioSource slapAudioSource;
        public float slapIntensity;
        public float slapDirection;
        public IEnumerator slap;
        public SlapTrigger slapTrigger;
        public WatchTrigger watchTrigger;
        public IEnumerator resetGazeTimeout;
        private static WaitForEndOfFrame waitForUpdate = new WaitForEndOfFrame();

        public TouchTrigger touchTrigger
        {
            get {return touchCollisionListener.touchTrigger;}
            set { touchCollisionListener.touchTrigger = value; }
        }

        public TouchZone(string region) : base(region){
            touchCollisionListener = new TouchCollisionListener(this);
            watchListener = new WatchListener(this);
            ToggleSlapAudioObject(true);
        }

        public BodyRegionTrigger GetTrigger(int type)
        {
            switch (type)
            {
                case 0: return touchTrigger;
                case 1: return slapTrigger;
                case 2: return watchTrigger;
            }
            return null;
        }
        
        public void DestroyTrigger(int type)
        {
            switch (type)
            {
                case 0:
                {
                    Object.Destroy(touchTrigger);
                    touchTrigger = null;
                    break;
                }
                case 1:
                {
                    Object.Destroy(slapTrigger);
                    slapTrigger = null;
                    break;
                }
                case 2:
                {
                    Object.Destroy(watchTrigger);
                    watchTrigger = null;
                    break;
                }
            }
        }
        
        public void ToggleSlapAudioObject(bool enabled){
            if(enabled){
                slapAudioObject = new GameObject();
                slapAudioSource = slapAudioObject.AddComponent<AudioSource>();
                slapAudioSource.spatialBlend = 1f;
                slapAudioSource.spatialize = true;
            }
            else Object.Destroy(slapAudioObject);
        }

        public void Slap(float intensity, float direction, TouchZone touchZone, bool doVocals, bool doMoan)
        {
            if(intensity <= slapIntensity) return;
            slapIntensity = intensity;
            slapDirection = direction;
            if (slap == null) slap = DeferrredSlap(touchZone, doVocals, doMoan).Start();
        }
        
        private IEnumerator DeferrredSlap(TouchZone touchZone, bool doVacals, bool doMoan)
        {
            // if (!slapVocalsEnabled) yield break;
            AudioClip clip = SlapHandler.slapLibrary.GetRandomClip();

            yield return new WaitForSeconds(.05f);
            float delta = slapIntensity - SlapHandler.slapThreshold.val;
            float volumeFactor = delta * delta * SlapHandler.slapVolume;
            slapAudioSource.pitch = Mathf.Lerp(.9f, 1.5f* slapPitchFactor.val, slapIntensity*.166f * (1f-slapDirection * slapDirection));
            // volumeFactor.Print();
            slapAudioSource.PlayOneShot(clip, volumeFactor);
            if (ReadMyLips.blinkTimeout <= 0f && Random.Range(0f, slapIntensity) > 3f)
            {
                ReadMyLips.eyelidBehavior.Blink();
                ReadMyLips.blinkTimeout = 1f;
            }
            if(ReadMyLips.singleton.enabled) ReadMyLips.Stimulate(SlapHandler.slapStimScale.val * slapIntensity, doStim:true);
            if(doMoan && slapIntensity > SlapHandler.slapThreshold.val * 1.5f) ReadMyLips.PlaySlapMoan();
            // var prompt = "{{user}} slaps {{char}}'s "+ touchZone.name +" with a " + collision.rigidbody.GetAtom().type+
            //     " with an intensity of " + slapIntensity.ToString();
            // prompt.Print();
            // touchZone.name.Print();
            if(slapTrigger != null) slapTrigger.Trigger(slapIntensity);
            slapIntensity = 0f;
            slap = null;
        }

        public IEnumerator ResetGazeTimeout()
        {
            while (gazeTimeout > 0f)
            {
                gazeTimeout -= Time.deltaTime;
                yield return waitForUpdate;
            }
        }

        public void Destroy()
        {
            touchCollisionListener.Destroy();
            Object.Destroy(slapAudioObject);
            slap.Stop();
            resetGazeTimeout.Stop();
            slap = null;
        }

        // public void GetCumRegion()
        // {
        //     foreach (var prnt in parents)
        //     {
        //         if (bodyManager.cumRegions.TryGetValue(prnt.name, out cumRegion))
        //         {
        //             // prnt.name.Print();
        //             return;
        //         }
        //     }
        // }
    }
}