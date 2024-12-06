using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;

namespace CheesyFX
{
    public static class SlapHandler
    {
	    private static Transform labia;
        
        public static JSONStorableBool feetSlapsEnabled = new JSONStorableBool("Feet Slaps Enabled", false);
        public static JSONStorableBool pussySlapsEnabled = new JSONStorableBool("Pussy Slaps Enabled", false);
        public static JSONStorableBool selfSlapsEnabled = new JSONStorableBool("Self Slaps Enabled (Hands)", true);
        public static JSONStorableBool slapVocalsEnabled = new JSONStorableBool("Slap Vocal Reaction Enabled", false);
        // public static JSONStorableBool slapMoanEnabled = new JSONStorableBool("Slap Moans Enabled", true);
        // public static JSONStorableBool sexSlapMoanEnabled = new JSONStorableBool("Sex Slap Moans Enabled", true);
        public static JSONStorableBool isSlappingOthers = new JSONStorableBool("Slap Others Enabled", true);
        public static JSONStorableFloat slapVolumeJ = new JSONStorableFloat("Slap Volume", 1f, 0f, 10f, false);
        public static JSONStorableFloat sexSlapVolumeJ = new JSONStorableFloat("Sex Slap Volume", 1f, 0f, 5f, false);
        public static JSONStorableFloat sexSlapThresholdJ = new JSONStorableFloat("Sex Slap Threshold", .5f, .5f, 5f, false);
        public static JSONStorableFloat sexSlapImpactOffsetJ = new JSONStorableFloat("Sex Slap Impact Offset", .1f, 0f, 1f, true);
        public static JSONStorableFloat slapThreshold = new JSONStorableFloat("Slap Threshold", 1f, 1f, 10f, true);
        public static JSONStorableFloat slapStimScale = new JSONStorableFloat("Slap Stimulation Scale", 1f, 0f, 2f, false);
        public static JSONStorableFloat sexSlapStimScale = new JSONStorableFloat("Sex Slap Stimulation Scale", 1f, 0f, 2f, false);
        
        public static float slapVolume;
        public static float sexSlapVolume;
        public static float sexSlapThreshold;
        private static float sexSlapImpactOffset;

        private static float sexSlapIntensity;
        private static AudioSource slapAS;
        private static AudioSource sexSlapAS;
        private static IEnumerator sexSlap;
        private static IEnumerator pussySlap;
        
        public static ClipLibrary slapLibrary;
        public static ClipLibrary sexSlapLibrary;
        public static ClipLibrary buttSlapLibrary;

        private static GameObject debug;
        public static JSONStorableFloat debugSlider1 = new JSONStorableFloat("debug0", .08f, 0f, 1f, false);
        public static JSONStorableFloat debugSlider2 = new JSONStorableFloat("debug0", .1f, 0f, 1f, false);

        public static JSONClass Store()
        {
	        var jc = new JSONClass();
	        feetSlapsEnabled.Store(jc);
	        pussySlapsEnabled.Store(jc);
	        selfSlapsEnabled.Store(jc);
	        slapVocalsEnabled.Store(jc);
	        isSlappingOthers.Store(jc);
	        slapVolumeJ.Store(jc);
	        sexSlapVolumeJ.Store(jc);
	        sexSlapThresholdJ.Store(jc);
	        sexSlapImpactOffsetJ.Store(jc);
	        slapThreshold.Store(jc);
	        slapStimScale.Store(jc);
	        sexSlapStimScale.Store(jc);
	        return jc;
        }
        
        public static void Load(JSONClass jc)
        {
	        if (jc.HasKey("SlapHandler"))
	        {
		        var jc1 = jc["SlapHandler"].AsObject;
		        feetSlapsEnabled.Load(jc1);
		        pussySlapsEnabled.Load(jc);
		        selfSlapsEnabled.Load(jc1);
		        slapVocalsEnabled.Load(jc1);
		        isSlappingOthers.Load(jc1);
		        slapVolumeJ.Load(jc1);
		        sexSlapVolumeJ.Load(jc1);
		        sexSlapThresholdJ.Load(jc1);
		        sexSlapImpactOffsetJ.Load(jc1);
		        slapThreshold.Load(jc1);
		        slapStimScale.Load(jc1);
		        sexSlapStimScale.Load(jc1);
	        }
        }
        
        public static void Init()
        {
	        slapLibrary = new ClipLibrary("slaps");
            sexSlapLibrary = new ClipLibrary("sexslaps");
            buttSlapLibrary = new ClipLibrary("buttslaps");
            
            var slapsPath = TouchMe.packageUid + "Custom/Scripts/CheesyFX/BodyLanguage/audiobundles/slaps.audiobundle";
            AudioImporter.GetClipsFromAssetBundle(new List<ClipLibrary>{slapLibrary, sexSlapLibrary, buttSlapLibrary},
	            slapsPath);
            
            var labTrigger = TouchMe.singleton.containingAtom.rigidbodies.First(x => x.name == "LabiaTrigger").transform;
            labia = new GameObject("Labia").transform;
            labia.parent = labTrigger;
            labia.localRotation = Quaternion.identity;
            labia.localPosition = new Vector3(0f, 0f, .03f);
            
            slapVolumeJ.AddCallback(val => slapVolume = val * .03f);
            sexSlapVolumeJ.AddCallback(val => sexSlapVolume = val * .6f);
            sexSlapThresholdJ.AddCallback(val => sexSlapThreshold = val);
            sexSlapImpactOffsetJ.AddCallback(val => sexSlapImpactOffset = val);
            
            TouchMe.singleton.RegisterBool(feetSlapsEnabled);
            TouchMe.singleton.RegisterBool(pussySlapsEnabled);
            TouchMe.singleton.RegisterBool(selfSlapsEnabled);
            TouchMe.singleton.RegisterFloat(slapVolumeJ);
            TouchMe.singleton.RegisterFloat(sexSlapVolumeJ);
            TouchMe.singleton.RegisterFloat(slapThreshold);
            TouchMe.singleton.RegisterFloat(sexSlapThresholdJ);
            TouchMe.singleton.RegisterFloat(sexSlapImpactOffsetJ);
            TouchMe.singleton.RegisterFloat(slapStimScale);
            TouchMe.singleton.RegisterFloat(sexSlapStimScale);

            // debug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // debug.transform.parent = labia;
            // debug.transform.localPosition = Vector3.zero;
            // Destroy(debug.GetComponent<Collider>());
            // debugSlider1.AddCallback(val => debug.transform.localScale = Vector3.one * debugSlider1.val /.5f);
            // debugSlider2.AddCallback(val => debug.transform.localPosition = new Vector3(0f, 0f, val));
        }
        
        
        public static List<object> CreateUI()
        {
	        List<object> UIElements = new List<object>();
	        slapVolumeJ.CreateUI(UIElements);
	        sexSlapVolumeJ.CreateUI(UIElements, true);
	        slapThreshold.CreateUI(UIElements);
	        sexSlapThresholdJ.CreateUI(UIElements, true);
	        slapStimScale.CreateUI(UIElements);
	        sexSlapStimScale.CreateUI(UIElements, true);
	        sexSlapImpactOffsetJ.CreateUI(UIElements, true);
	        
	        selfSlapsEnabled.CreateUI(UIElements);
	        pussySlapsEnabled.CreateUI(UIElements);
	        feetSlapsEnabled.CreateUI(UIElements);
	        
	        // UIManager.SetupInfoOneLine(null, "Expressions", false, false, this);
	        // var textfield = CreateTextField(new JSONStorableString("", "Expressions"));
	        // textfield.height = 50f;
	        // textfield.UItext.alignment = TextAnchor.MiddleCenter;
	        
	        // debugSlider1.CreateUI();
	        // debugSlider2.CreateUI();
	        return UIElements;
        }
        
        public static void Slap(TouchZone touchZone, Collision collision, float intensity, ContactPoint contactPoint)
        {
	        try
	        {
				if (!feetSlapsEnabled.val && touchZone.parents.Any(x => x.name == "Feet")) return;
		        // ContactPoint contactPoint = collision.contacts[0];
		        // Vector3 velocity = collision.relativeVelocity;
		        // float intensity = Math.Abs(Vector3.Dot(velocity, contactPoint.normal));
		        // if (intensity < sexSlapThreshold) return;
				AudioSource audioSource = touchZone.slapAudioSource;
				audioSource.transform.position = contactPoint.point;
				Atom collidingAtom = collision.rigidbody.GetAtom();
				bool selfSlap = collidingAtom == TouchMe.singleton.containingAtom;
				bool slapsOtherPerson = false;
				// bool slappedByPerson = false;
				bool slappedByPersonOrToys = collidingAtom.type == "Person" || collidingAtom.type == "Dildo" || collidingAtom.type == "Paddle" || collidingAtom.type == "Capsule";
				string collidingRegion = collision.rigidbody.GetRegionName();
				if(collidingRegion != null)
				{
					bool contactWithPerson = collidingAtom.type == "Person";
					slapsOtherPerson = contactWithPerson && touchZone.parents.Any(x => x.name.Contains("Hand"));
					slappedByPersonOrToys = slappedByPersonOrToys && !slapsOtherPerson;
					// slapsOtherPerson.Print();
					// bool slappedByHand = collidingRegion.Contains("Hand") || collidingRegion.Contains("Thumb") || collidingRegion.Contains("Index") || collidingRegion.Contains("Mid") || collidingRegion.Contains("Ring") || collidingRegion.Contains("Pinky");

					// slappedByPerson = contactWithPerson && (slappedByHand || collidingRegion.Contains("Arm"));
					// slappedByPersonOrToys = slappedByPersonOrToys || slappedByPerson;
				}
				bool collidingAtomIsPenetrating = FillMeUp.penetratingAtoms.Values.ToList().Contains(collidingAtom);
				// slapsOtherPerson.Print();
				if (!slapsOtherPerson && collidingAtomIsPenetrating &&
				    (collidingRegion == "Penis" || collidingRegion == "DildoPenis")) return;
				// $"{touchZone.name} {collidingRegion} {collidingAtomIsPenetrating} {collidingAtom.name}".Print();
				if (
					collidingAtomIsPenetrating
					&& collidingRegion != null
					&& (collidingRegion == "Pelvis" || collidingRegion == "Abdomen" ||
					    collidingRegion.Contains("Thigh") || collidingRegion == "Testes" ||
					    collidingRegion == "DildoSack"))
				{
					
					if ((touchZone.name.Contains("Thigh") || touchZone.name.Contains("Glutes") ||
					     touchZone.name == "Labia" || touchZone.name == "Anus" || touchZone.name == "Vagina"))
					{
						if (intensity < sexSlapIntensity) return;
						sexSlapIntensity = intensity;
						sexSlapAS = audioSource;
						if (sexSlap == null) sexSlap = SexSlap().Start();

					}
				}
				else if(pussySlapsEnabled.val && Vector3.Distance(labia.position, contactPoint.point) < .08f)
				{
					if(intensity < sexSlapIntensity || (collidingRegion != null && (collidingRegion == "Pelvis" || collidingRegion == "Penis"))) return;
					sexSlapIntensity = intensity;
					sexSlapAS = audioSource;
					if (pussySlap == null) pussySlap = PussySlap().Start();
					// collidingRegion.Print();
					// collidingAtomIsPenetrating.Print();
				}
				else{
					if(intensity < slapThreshold.val) return;
					float direction = intensity / collision.relativeVelocity.magnitude;
					// (collision.rigidbody.name+"->"+touchZone.name).Print();
					List<BodyRegion> touchedParents = touchZone.parents;
					if (touchedParents.Any(x => x.name.Contains("Hand")) ||
					    touchedParents.Any(x => x.name.Contains("ForeArm")))
					{
						if (!slapsOtherPerson || !isSlappingOthers.val) return;
					}
					bool slappedByOtherPerson = !selfSlap && slappedByPersonOrToys;
					// touchZone.name.Print();
					touchZone.Slap(intensity, direction, touchZone,slapVocalsEnabled.val && slappedByOtherPerson, slappedByOtherPerson);
				}
	        }
	        catch (Exception e)
	        {
		        e.Print();
	        }
	        
		}
        
        private static IEnumerator PussySlap(){
	        AudioClip clip = buttSlapLibrary.GetRandomClip();
	        yield return new WaitForSeconds(.05f);
	        float volumeFactor = sexSlapIntensity * sexSlapIntensity * sexSlapVolumeJ.val;//*1.3f;
	        sexSlapAS.pitch = Mathf.Lerp(1.2f, 1.5f, sexSlapIntensity);
	        // volumeFactor.Print();
	        // sexSlapIntensity.Print();
	        sexSlapAS.PlayOneShot(clip, volumeFactor);
	        var vagAS = FillMeUp.vagina.audioSource;
	        if (vagAS == null)
	        {
		        sexSlapIntensity = 0f;
		        pussySlap = null;
		        yield break;
	        }
	        vagAS.volume = Mathf.Lerp(.75f, 1f,sexSlapIntensity);
	        
	        vagAS.PlayOneShot(FillMeUp.squishLibrary.GetRandomClip());
	        
	        // if(globalSlapMoanReactionEnabled.val) AudioManager.moanClips.PlayRandom(allowRepeat:true);
	        if(ReadMyLips.singleton.enabled) ReadMyLips.Stimulate(sexSlapStimScale.val * sexSlapIntensity * 8f);
	        ReadMyLips.PlaySlapMoan(true);
	        sexSlapIntensity = 0f;
	        pussySlap = null;
        }
        
        private static IEnumerator SexSlap(){
	        AudioClip clip = buttSlapLibrary.GetRandomClip();
	        yield return new WaitForSeconds(.05f);
	        float delta = sexSlapIntensity + sexSlapImpactOffset - sexSlapThreshold;
	        float volumeFactor = delta * delta * sexSlapVolume;
	        sexSlapAS.pitch = Mathf.Lerp(.6f, 1.2f, sexSlapIntensity);
	        sexSlapAS.PlayOneShot(clip, volumeFactor);
	        if(ReadMyLips.singleton.enabled) ReadMyLips.Stimulate(sexSlapStimScale.val * sexSlapIntensity * 6f);
	        ReadMyLips.PlaySlapMoan(true);
	        sexSlapIntensity = 0f;
	        sexSlap = null;
        }

        public static void Destroy()
        {
            sexSlap.Stop();
            sexSlap = null;
            pussySlap.Stop();
            pussySlap = null;
            Object.Destroy(labia.gameObject);
        }
    }
}