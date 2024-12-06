using System;
using System.Collections.Generic;
using GPUTools.Physics.Scripts.Kernels;
using MacGruber;
using SimpleJSON;
using UnityEngine;

namespace CheesyFX
{
    public static class NippleManager
    {
	    
		
		public static JSONStorableBool enabledJ = new JSONStorableBool("Nipple Errection Enabled", true);
		public static JSONStorableFloat stimulationRegression = new JSONStorableFloat("Stimulation Regression", .2f, 0f, 1f);
		public static JSONStorableFloat stimulationGain = new JSONStorableFloat("Stimulation Gain", .1f, 0f, 1f);
		public static JSONStorableFloat erectionGain = new JSONStorableFloat("Erection Gain", .2f, 0f, .3f);
		public static JSONStorableFloat erectionRegression = new JSONStorableFloat("Erection Regression", .05f, 0f, 1f);
		public static JSONStorableFloat erectionScale = new JSONStorableFloat("Erection Scale", 1.5f, 0f, 3f);
		public static JSONStorableFloat bleed = new JSONStorableFloat("Other Side Bleed", .1f, 0f, 1f);
		public static JSONStorableFloat generalStimulationGain = new JSONStorableFloat("General Stim Gain (ReadMyLips)", 25f, 0f, 50f);
		
		public static NippleDriver[] nippleDrivers;

		public static JSONClass Store()
		{
			var jc = new JSONClass();
			enabledJ.Store(jc);
			stimulationRegression.Store(jc);
			stimulationGain.Store(jc);
			erectionGain.Store(jc);
			erectionRegression.Store(jc);
			erectionScale.Store(jc);
			bleed.Store(jc);
			generalStimulationGain.Store(jc);
			return jc;
		}
        
		public static void Load(JSONClass jc)
		{
			if(jc.HasKey("NippleManager"))
			{
				var nipples = jc["NippleManager"].AsObject;
				enabledJ.Load(nipples);
				stimulationRegression.Load(nipples);
				stimulationGain.Load(nipples);
				erectionGain.Load(nipples);
				erectionRegression.Load(nipples);
				erectionScale.Load(nipples);
				bleed.Load(nipples);
				generalStimulationGain.Load(nipples);
			}
		}
		
		public static void Init()
		{
			nippleDrivers = new[]
			{
				TouchMe.singleton.gameObject.AddComponent<NippleDriver>(),
				TouchMe.singleton.gameObject.AddComponent<NippleDriver>()
			};
			nippleDrivers[0].Init(0);
			nippleDrivers[1].Init(1);

			enabledJ.setCallbackFunction += val =>
			{
				foreach (var nipple in nippleDrivers)
				{
					if(!val) nipple.Reset();
					nipple.enabled = val;
				}
			};

			erectionScale.setCallbackFunction += val => nippleDrivers.ToList().ForEach(x => x.erectionScale = val);
			
			TouchMe.singleton.RegisterFloat(stimulationGain);
			TouchMe.singleton.RegisterFloat(stimulationRegression);
			TouchMe.singleton.RegisterFloat(erectionGain);
			TouchMe.singleton.RegisterFloat(erectionRegression);
			TouchMe.singleton.RegisterFloat(bleed);
			TouchMe.singleton.RegisterFloat(generalStimulationGain);
			TouchMe.singleton.RegisterBool(enabledJ);

			// test1.setCallbackFunction += val =>
			// {
			// 	nippleDrivers[0].nippleMorph.morphValue = val;
			// };
			// test2.setCallbackFunction += val =>
			// {
			// 	nippleDrivers[0].nippleSprings[3].val = val;
			// };
			// test3.setCallbackFunction += val =>
			// {
			// 	nippleDrivers[0].nippleDampers[3].val = val;
			// };
		}

		// public static void Stimulate(float stimulus, int side){
		// 	// nippleDrivers[side].Stimulate(stimulus);
		// 	ReadMyLips.Stimulate(10f*stimulus, doStim:true);
		// }

		public static List<object> CreateUI(){

			List<object> UIElements = new List<object>();
			enabledJ.CreateUI(UIElements);
			var button = TouchMe.singleton.CreateButton("Reset", true);
			button.button.onClick.AddListener(() => nippleDrivers.ToList().ForEach(x => x.Reset()));
			UIElements.Add(button);
			for (var i = 0; i < nippleDrivers.Length; i++)
			{
				var nipple = nippleDrivers[i];
				nipple.stimulation.CreateUI( UIElements, i==1);
				nipple.erection.CreateUI(UIElements, i==1);
			}

			// stimulationBuildUpOnStay.CreateUI(UIElements);
			// stimulationBuildUpOnEnter.CreateUI(UIElements, true);
			stimulationGain.CreateUI(UIElements);
			stimulationRegression.CreateUI(UIElements);
			erectionGain.CreateUI(UIElements, true);
			erectionRegression.CreateUI(UIElements, true);
			erectionScale.CreateUI(UIElements, true);
			generalStimulationGain.CreateUI(UIElements);
			bleed.CreateUI(UIElements, true);
			
			// test1.CreateUI(UIElements:UIElements);
			// test2.CreateUI(UIElements:UIElements);
			// test3.CreateUI(UIElements:UIElements);
			// UIManager.SetTabElements(UIElements);
			return UIElements;

			// button = Utils.SetupButton(script, "Reset stimulation", () => stimulation.val = 0f, true);
			// UIElements.Add(button);

			// button = Utils.SetupButton(script, "Reset erection", () => erection.val = 0f, true);
			// UIElements.Add(button);

			// popup = script.CreateFilterablePopup(addRegionChoice);
			// UIElements.Add(popup);
		}
    }
}