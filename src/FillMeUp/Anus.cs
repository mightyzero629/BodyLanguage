using System;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Anus : Orifice
	{
		public JSONStorableBool twinkEnabled = new JSONStorableBool("Twinking Enabled", true);
		public JSONStorableFloat gapeRegressionSpeed = new JSONStorableFloat("Gape Regression", .2f, 0f, 3f, false, true);
		public JSONStorableFloat twinkInterval_mean = new JSONStorableFloat("Twink Interval Mean", 1.5f, .5f, 10f, true, true);
		public JSONStorableFloat twinkInterval_delta = new JSONStorableFloat("Twink Interval Delta", 1.2f, 0f, 1.35f, true, true);
		
		public JSONStorableFloat gapeMorph;
		public JSONStorableFloat inOutMorphWide;
		public JSONStorableFloat inOutMorphSmall;
		public JSONStorableFloat relaxationGainJSON = new JSONStorableFloat("Relaxation Gain", 10f, 0f, 100f, false);
		public JSONStorableFloat relaxationRegressionJSON = new JSONStorableFloat("Relaxation Regression", 20f, 0f, 100f, false);
		public JSONStorableFloat gapeScale = new JSONStorableFloat("Gape Scale", 1f, 0f, 1f, false);
		public JSONStorableFloat inOutScale = new JSONStorableFloat("In/Out Scale", 1f, 0f, 5f);

		private float stretchChangeRate;
		private float relaxationGain;
		private float relaxationRegression;
		float nextTwinkTime;
		private Transform _stretchRef;
		private float _baseStretch = .012f;
		private float _stretch;
		private float twinkMeanDeltaPercentage = .8f;
		private float currentStretch;
		private bool currentStretchSet;
		private float gapeTimer;
		private float totalTwinkTime;
		private float closeTime;
		private float a1;
		private float a2;

		public override void Init(string name){
			relaxation = new JSONStorableFloat("Relaxation", .1f, .1f, 5f, true, false);
			base.Init(name);
			guidance = FillMeUp.atom.forceReceivers.First(x => x.name == "hip").transform;
			
			DAZCharacterSelector characterSelector = FillMeUp.atom.GetStorableByID("geometry") as DAZCharacterSelector;
			GenerateDAZMorphsControlUI morphControl = characterSelector.morphsControlUI;
			// foreach(var s in morphControl.GetMorphUids()){
			// 	if(s.Contains("Belly Bulger")) s.Print();
			// }

			_stretchRef = FillMeUp.atom.rigidbodies.First(x => x.name == "_JointAr").transform;
			depthReference = FillMeUp.atom.rigidbodies.First(x => x.name == "abdomen");
			
			relaxationGain = relaxationGainJSON.defaultVal * .001f;
			relaxationRegression = relaxationRegressionJSON.defaultVal * .001f;

			twinkInterval_mean.setCallbackFunction += val =>
			{
				twinkInterval_delta.constrained = false;
				twinkInterval_delta.valNoCallback = twinkMeanDeltaPercentage * val;
				twinkInterval_delta.max = .9f * val;
				twinkInterval_delta.constrained = true;
			};

			twinkInterval_delta.setCallbackFunction += val => twinkMeanDeltaPercentage = val / twinkInterval_mean.val;

			enabledJ.setCallbackFunction += x => DoReset();
			relaxationGainJSON.setCallbackFunction += val => relaxationGain = val * .001f;
			relaxationRegressionJSON.setCallbackFunction += val => relaxationRegression = val * .001f;

			GetMorphs();

			nextTwinkTime = twinkInterval_mean.val;
			this.name = "Anus";
		}
		
		public void GetMorphs()
		{
			DAZCharacterSelector characterSelector = FillMeUp.atom.GetStorableByID("geometry") as DAZCharacterSelector;
			GenerateDAZMorphsControlUI morphControl = characterSelector.morphsControlUI;
			string gapeMorphPath = FillMeUp.packageUid+"Custom/Atom/Person/Morphs/female_genitalia/CheesyFX/BodyLanguage/BL_Gape.vmi";
			gapeMorph = morphControl.GetMorphByUid(gapeMorphPath).jsonFloat;
			gapeMorph.val = 0f;
			gapeMorph.constrained = false;
			
			inOutMorphWide = morphControl.GetMorphByDisplayName("610-Anus-In.Out-WideArea").jsonFloat;
			inOutMorphSmall = morphControl.GetMorphByDisplayName("609-Anus-In.Out-SmallArea").jsonFloat;
			inOutMorphWide.constrained = true;
			inOutMorphWide.max = 1f;
			inOutMorphWide.min = -1f;
		}



		private float lastStretch;
		public override void GetStretch()
		{
			_stretch = _stretchRef.localPosition.x - _baseStretch;
			stretchChangeRate = Math.Abs(_stretch - lastStretch) / Time.fixedDeltaTime;
			// stretchChangeRate.Print();
			lastStretch = _stretch;
		}

		public override float Stimulate(float bastStim = 0f, bool expressionChange = true)
		{
			float stimulus = base.Stimulate(5f*stretchChangeRate + bastStim, expressionChange);
			// stimulus *= 1f + stretchChangeRate;
			if(enabledJ.val){
				stretch.val += stimulus * relaxation.val * .001f;
				relaxation.val += stimulus * relaxationGain *.5f;
			}

			return stimulus;
		}

		public override void FixedUpdate(){
			if (isPenetrated)
			{
				base.FixedUpdate();
				stretch.max = Mathf.Lerp(stretch.max, _stretch * 17.5f, Time.fixedDeltaTime);
				gapeMorph.val = stretch.val;
				if (currentStretchSet)
				{
					currentStretchSet = false;
					gapeTimer = 0f;
				}

				var inOutBaseValue = Mathf.Lerp(0f, -.5f, depth.val / .18f);
				float inOutVal;
				if(speed.val > 0f) inOutVal = inOutBaseValue - 3f*speed.val;
				else inOutVal = inOutBaseValue -speed.val * 1f;
				inOutVal *= stretch.val * inOutScale.val;
				inOutMorphWide.val = inOutVal * .5f;
				inOutMorphSmall.val = inOutVal;
				
				
				if(!audioSource.isPlaying && penetrationSoundsVolume.val > 0f && speed.val > .1f)
				{
					var clip = FillMeUp.squishLibrary.GetRandomClip();
					audioSource.clip = clip;
					audioSource.volume = 50f*speed.val * depth.val * penetrationSoundsVolume.val;
					audioSource.Play();
				}
			}
			else
			{
				inOutMorphWide.val = 0f;
				inOutMorphSmall.val = 0f;
				if (!currentStretchSet)
				{
					currentStretch = stretch.val;
					nextTwinkTime = 0f;
					totalTwinkTime = 0f;
					currentStretchSet = true;
				}
				gapeTimer += Time.deltaTime;
				// relaxation.val -= .0005f;
				relaxation.val = Mathf.Lerp(relaxation.val, 0f, relaxationRegression * Time.fixedDeltaTime);
				if (gapeScale.val == 0f)
				{
					gapeMorph.val = 0f;
					return;
				}
				if(stretch.val > .001f)
				{
					stretch.val = gapeScale.val * currentStretch * (2f - Mathf.Pow(1f+gapeRegressionSpeed.val, gapeTimer / relaxation.val));
					if (twinkEnabled.val)
					{
						nextTwinkTime -= Time.fixedDeltaTime;
						if (nextTwinkTime <= 0f)
						{
							nextTwinkTime = NormalDistribution.GetValue(twinkInterval_mean.val, twinkInterval_delta.val,
								useNormalD: false);
							totalTwinkTime = nextTwinkTime * Random.Range(.4f, .6f);
							closeTime = totalTwinkTime * Random.Range(.4f, .6f);
							a1 = Mathf.Pow(2f, 1f / closeTime);
							a2 = Mathf.Pow(.001f, -1f / (totalTwinkTime - closeTime));
						}

						if (nextTwinkTime <= totalTwinkTime)
						{
							var t = totalTwinkTime - nextTwinkTime;
							if (t < closeTime) gapeMorph.val = stretch.val * (2f - Mathf.Pow(a1, t));
							else gapeMorph.val = stretch.val * (1f - Mathf.Pow(a2, closeTime - t));
						}
						else gapeMorph.val = stretch.val;
					}
					else gapeMorph.val = stretch.val;
				}
				else{
					gapeMorph.val = 0f;
					stretch.val = 0f;
					enabled = false;
				}
			}

			DriveForeski();
			lastDepth = depth.val;
		}

		public override void DoReset(){
			base.DoReset();
			// stretch.val = 0f;
			gapeMorph.val = 0f;
			inOutMorphWide.val = 0f;
			inOutMorphSmall.val = 0f;
			depth.val = 0f;
			depthMeter.transform.parent = null;
		}
		
		// private void  OnDisable()
		// {
		// 	DoReset();
		// }

		public void ResetTwink(){
			nextTwinkTime = -1f;
			// gape.val = stimulation.val;
		}

		// public override void CreateTriggersUI()
		// {
		// 	
		// }

		public override void CreateSettingsUI()
		{
			// magnetic.CreateUI(UIElements);
			// autoThrust.CreateUI(UIElements);
			// maleThrust.CreateUI(UIElements);
			// FillMeUp.singleton.SetupButton("Configure Magnet", true, magnet.CreateUINewPage, UIElements);
			// FillMeUp.singleton.SetupButton("Configure Thrust", true, () => CreateForceUINewPage(thrustForce), UIElements);
			// FillMeUp.singleton.SetupButton("Configure Male Thrust", true, () => CreateForceUINewPage(maleForce), UIElements);
			depthStimulationThreshold.CreateUI(FillMeUp.singleton, UIElements: UIElements);
			sensitivity.CreateUI(FillMeUp.singleton, UIElements: UIElements, rightSide:true);

			// relaxation.CreateUI(UIElements, true);
			gapeScale.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:false);
			inOutScale.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:true);
			gapeRegressionSpeed.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:false);
			relaxationGainJSON.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:true);
			gapeScale.CreateUI(FillMeUp.singleton, UIElements:UIElements);
			relaxationRegressionJSON.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:true);
			penetrationSoundsVolume.CreateUI(FillMeUp.singleton, UIElements: UIElements, rightSide:true);
			twinkInterval_mean.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:false);
			twinkInterval_delta.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:false);
			twinkEnabled.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:false);
			Person.stiffenEnabled.CreateUI(UIElements, true);
			Person.stiffenAmount.CreateUI(UIElements, true);
			FillMeUp.singleton.SetupButton("Penis Stiffen Reset", true,
				() => FillMeUp.persons.ForEach(x => x.StiffenReset()), UIElements);
		}
		
		public override void CreateBulgeUI()
		{
			bulgeScale.CreateUI(FillMeUp.singleton, UIElements:UIElements);
			bulgeSharpness.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:true);
			bulgeDepthScale.CreateUI(FillMeUp.singleton, UIElements:UIElements);
		}

		public override JSONClass Store(string subScenePrefix, bool storeTriggers = true)
		{
			JSONClass jc = base.Store(subScenePrefix, storeTriggers);
			gapeScale.Store(jc);
			inOutScale.Store(jc);
			gapeRegressionSpeed.Store(jc);
			relaxationGainJSON.Store(jc);
			relaxationRegressionJSON.Store(jc);
			twinkEnabled.Store(jc);
			twinkInterval_mean.Store(jc);
			twinkInterval_delta.Store(jc);
			return jc;
		}

		public override void Load(JSONClass jc, string subScenePrefix)
		{
			base.Load(jc, subScenePrefix);
			if(jc.HasKey(name))
			{
				JSONClass tc = jc[name].AsObject;
				gapeScale.Load(tc);
				inOutScale.Load(tc);
				gapeRegressionSpeed.Load(tc);
				relaxationGainJSON.Load(tc);
				relaxationRegressionJSON.Load(tc);
				twinkEnabled.Load(jc);
				twinkInterval_mean.Load(tc);
				twinkInterval_delta.Load(tc);
				// tc["In/Out Scale"].Print();
				
			}
		}
	}
}