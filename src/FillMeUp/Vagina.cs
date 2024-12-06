using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class Vagina : Orifice
	{
		public override void Init(string name)
		{
			type = 1;
			base.Init(name);
			guidance = FillMeUp.atom.forceReceivers.First(x => x.name == "pelvis").transform;
			DAZCharacterSelector characterSelector = FillMeUp.atom.GetStorableByID("geometry") as DAZCharacterSelector;
			GenerateDAZMorphsControlUI morphControl = characterSelector.morphsControlUI;
			
			// Transform labia = FillMeUp.atom.rigidbodies.First(x => x.name == "LabiaTrigger").transform;
			depthReference = FillMeUp.atom.rigidbodies.First(x => x.name == "abdomen");
			name = "Vagina";
			initialized = true;
		}

		public override void SetPenetrated(bool val)
		{
			base.SetPenetrated(val);
			if (!val)
			{
				zeroStretch = ZeroStretch().Start();
				enabled = false;
			}
		}

		public override void FixedUpdate()
		{
			try
			{
				base.FixedUpdate();
				if((object) audioSource != null && !audioSource.isPlaying && penetrationSoundsVolume.val > 0 && speed.val > .1f)
				{
					var clip = FillMeUp.squishLibrary.GetRandomClip();
					audioSource.clip = clip;
					audioSource.volume = 50f*speed.val * depth.val * penetrationSoundsVolume.val;
					audioSource.Play();
				}

				DriveForeski();
				lastDepth = depth.val;
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());
			}
			
		}
		
		public override void CreateSettingsUI()
		{
			base.CreateSettingsUI();
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
	}
}