using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using MeshVR;
using UnityEngine;
using SimpleJSON;
using MVR.FileManagementSecure;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;


namespace CheesyFX {

	public class ReadMyLips : MVRScript
	{
		public static string packageUid => FillMeUp.packageUid;
		public static ReadMyLips singleton;
		public static Atom atom;
		private UnityEventsListener uiListener;
		public static GenerateDAZMorphsControlUI morphControl;
		private static System.Random rng = new System.Random();

		private static JSONStorableBool voiceEnabledJ = new JSONStorableBool("Voice Enabled", true, val => voiceEnabled = val);
		private static JSONStorableBool expressionsEnabledJ = new JSONStorableBool("Expressions Enabled", true);
		private static JSONStorableBool breathingEnabledJ = new JSONStorableBool("Breathing Enabled", true, val => breathingDriver.enabled = val);
		private static JSONStorableBool showScreenOverlay = new JSONStorableBool("Show Screen Overlay On Orgasm", false);
		
		private static List<LerpableMorph> painMorphs = new List<LerpableMorph>();
		private static List<LerpableMorph> joyMorphs = new List<LerpableMorph>();
		private static List<LerpableMorph> lastMorphs = new List<LerpableMorph>();
		private static JSONStorableBool autoJaw = new JSONStorableBool("Auto Jaw", true);
		private static JSONStorableBool displeaseOnly = new JSONStorableBool("Displeased Only", false);
		public static JSONStorableFloat painThresholdAtOne = new JSONStorableFloat("Displease Threshold At High Stim", .1f, 0f, .5f, true);
		public static JSONStorableFloat painThresholdAtZero = new JSONStorableFloat("Displease Threshold At Low Stim", 0f, 0f, .5f, true);
		public static JSONStorableFloat joyExpressionScale = new JSONStorableFloat("Joy Expression Scale", 20f, 1f, 40f, false);
		public static JSONStorableFloat painExpressionScale = new JSONStorableFloat("Pain Expression Scale", 10f, 1f, 40f, false);
		public static JSONStorableFloat idleThreshold = new JSONStorableFloat("Idle Threshold", .15f, 0f, 1f);
		public static JSONStorableFloat randomBurstChanceJ = new JSONStorableFloat("Random Burst Chance", .15f, 0f, 1f);
		public static JSONStorableFloat randomBurstStrenght = new JSONStorableFloat("Random Burst Strength", 7f, 0f, 10f);
		private static JSONStorableFloat maxLipLiftJ = new JSONStorableFloat("Max Upper Lip Lift", 4.5f, 0f, 10f);
		private static JSONStorableFloat maxLowerLidLiftJ = new JSONStorableFloat("Max Lower Lid Lift", 1.2f, 0f, 5f);
		private static JSONStorableFloat maxMorphCount = new JSONStorableFloat("Max Morph Count", 20f, 5f, 50f);
		private static JSONStorableFloat morphUpdateThreshold = new JSONStorableFloat("Morph Update Threshold", .001f, .001f, .005f);
		
		public static JSONStorableFloat jawLimit = new JSONStorableFloat("Jaw Limit", -11f, -35f, 0f);
		public static JSONStorableFloat headAudioVolume = new JSONStorableFloat("Head Audio Volume", 1f, 0f, 1f);
		public static JSONStorableFloat headAudioPitch = new JSONStorableFloat("Head Audio Pitch", 1f, -3f, 3f);
		
		private static JSONStorableFloat sexMoanChanceJ = new JSONStorableFloat("Sex Moan Chance", .1f, 0f, 1f, true);
		public static JSONStorableFloat slapMoanChance = new JSONStorableFloat("Slap Moan Chance", .6f, 0f, 1f, true);
		public static JSONStorableFloat sexSlapMoanChance = new JSONStorableFloat("Sex Slap Moan Chance", .4f, 0f, 1f, true);
		public static JSONStorableFloat burstMoanChance = new JSONStorableFloat("Burst Moan Chance", .5f, 0f, 1f, true);
		private static JSONStorableFloat mouthStuffedDepthThreshold = new JSONStorableFloat("Mouth Stuffed Depth Threshold", .003f, 0f, .05f);
		
		public static JSONStorableFloat orgasmCount = new JSONStorableFloat("Orgasms", 0f, 0f, 10f, false, false);
		public static JSONStorableFloat multiOrgasmCount = new JSONStorableFloat("Multi Orgasms", 0f, 0f, 10f, false, false);
		public static JSONStorableFloat stimulation = new JSONStorableFloat("Stimulation", 0f, 0f, 1f, true, false);
		public static JSONStorableFloat stimulationGain = new JSONStorableFloat("Stimulation Gain", .1f, 0f, 1f, true);
		public static JSONStorableFloat stimulationRegressionJ = new JSONStorableFloat("Stimulation Regression", .15f, 0f, 1f, true);
		public static JSONStorableFloat orgasmThreshold = new JSONStorableFloat("Orgasm Threshold", 1f, 0f, 1f);
		public static JSONStorableFloat orgasmTime = new JSONStorableFloat("Orgasm Time", 10f, 0f, 30f, false);
		public static JSONStorableFloat orgasmFadeTime = new JSONStorableFloat("Orgasm Fade Time", 10f, 0f, 30f, false);
		public static JSONStorableFloat orgasmRecoverTime = new JSONStorableFloat("Orgasm Recover Time", 120f, 0f, 900f, false);
		public static JSONStorableAction orgasmNow = new JSONStorableAction("Orgasm Now", ForceOrgasm);
		public static JSONStorableAction resetOrgasms = new JSONStorableAction("Reset Orgasms", ResetOrgasms);
		public static JSONStorableAction orgasmMalesNow = new JSONStorableAction("Orgasm Males Now", () => ForceMaleOrgasm(1));
		public static JSONStorableAction orgasmDildosNow = new JSONStorableAction("Orgasm Dildos Now", () => ForceMaleOrgasm(2));
		public static JSONStorableAction resetCumClothes = new JSONStorableAction("Reset Cum Clothes", ResetCumClothes);

		private static JSONStorableFloat dynamicStimGainAdjustment =
			new JSONStorableFloat("Dynamic Stim Gain Adjustment", .03f, 0f, .05f, false);

		public static JSONStorableString dynamicGainInfo = new JSONStorableString("DynamicGainInfo", "");
		
		private static bool targetsReset;
		private float burstTimer;
		public static bool isOrgasmPleasure;
		
		private static float maxLipLift;
		private static float lipLiftSum;
		private static float maxLowerLidLift;
		private static int morphsActive;

		public static float dynamicStimGain = stimulationGain.val;
		private static float lastStimulation;
		private static float stimRate;
		private static float stimDelta;
		private FloatTriggerManager stimTriggers;
		private FloatTriggerManager orgasmTriggers;
		private FloatTriggerManager multiOrgasmTriggers;
		private static float resetTargetsTimer = 1f;
		private static float stimBurstTimer;
		public static float randomBurstChance;
		public static float sexMoanChance;
		public static float stimulationRegression;
		// private static float painThreshold;
		private static float orgasmTimer = 30f;
		private static float orgRecoverTimer = orgasmRecoverTime.defaultVal;
		private static bool isOrgasming;
		private static bool isIdling = true;
		
		private static float minJoyIncrement = .05f;
		private static float minPainIncrement = .05f;
		
		private bool vammoanPackageInstalled;
		public static string vammoanAudioPath;
		public static List<ClipLibrary> moanLibrary;
		public static AudioSource headAudioSource;
		public static JSONStorableStringChooser vamMoanVoice;
		public static JSONStorableStringChooser voice = new JSONStorableStringChooser("Voice",
			new List<string>{"Abby", "Claire", "Ella", "Emma", "Grace", "Isabella", "Lana", "Lia", "Lillian", "Lise", "Seth", "Skye", "Yumi"},
			"Isabella", "VAMMoan Voice");

		public static AudioSourceControl headAudioControl;
		public static DAZMeshEyelidControl eyelidBehavior;
		private static AdjustJoints jawControl;
		// private static JSONStorable autoJawMouthMorphs;
		public static float blinkTimeout;
		private List<object> UIElements = new List<object>();
		private UIDynamicTabBar tabbar;
		public static int lastTabId;
		private UIDynamicButton stimTriggerButton;
		private UIDynamicButton orgasmTriggerButton;
		private UIDynamicButton multiOrgasmTriggerButton;
		
		public static EventTrigger onOrgasmStartTrigger;
		public static EventTrigger onOrgasmEndTrigger;
		public static EventTrigger onOrgasmRecoverTrigger;

		private static JSONStorableAction resetMorphs = new JSONStorableAction("Reset Morphs", ResetExpressions);

		private static bool voiceEnabled = true;
		private static bool expressionsEnabled = true;

		public PresetSystem presetSystem;
		private List<JSONStorableParam> parameters = new List<JSONStorableParam>();

		
		private static BreathingDriver breathingDriver;

		public static Ahegao ahegao;

		// private static ScreenInfo screenInfo;

		private static KeyCode maleCumKey = KeyCode.Y;
		private static KeyCode dildoCumKey = KeyCode.Y;
		
		public static AltFutaStim altFutaStim;

		private Image stimbarImg;
		public static JSONStorableColor femaleStimColor = new JSONStorableColor("Female Max Stim Color", new Color(1f, 0.54f, 0.8f).ToHSV(), val => Gaze.SetDebugColor());
		public static JSONStorableColor maleStimColor = new JSONStorableColor("Male Max Stim Color", new Color(0.47f, 1f, 1f).ToHSV(), val => Gaze.SetDebugColor());

		private static void SetMaleCumKey(string val)
		{
			maleCumKey = (KeyCode)Enum.Parse(typeof(KeyCode), val);
		}
		
		private static void SetDildoCumKey(string val)
		{
			dildoCumKey = (KeyCode)Enum.Parse(typeof(KeyCode), val);
		}

		private JSONClass Store()
		{
			JSONClass jc = new JSONClass();
			parameters.ForEach(x => x.Store(jc));
			Ahegao.Store(jc);
			return jc;
		}

		private void Load(JSONClass jc)
		{
			parameters.ForEach(x => x.Load(jc));
			Ahegao.Load(jc);
		}

		private void OnEnable()
		{
			voiceEnabled = voiceEnabledJ.val;
			expressionsEnabled = expressionsEnabledJ.val;
		}

		public override void Init()
		{
			if (FillMeUp.abort) return;
			UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements);
			Utils.OnInitUI(CreateUIElement);
			// LoadAsset();
			singleton = this;
			atom = containingAtom;
			// screenInfo = gameObject.AddComponent<ScreenInfo>().Init();
			headAudioControl = (AudioSourceControl)containingAtom.GetStorableByID("HeadAudioSource");
			eyelidBehavior = (DAZMeshEyelidControl) containingAtom.GetStorableByID("EyelidControl");
			jawControl = (AdjustJoints)containingAtom.GetStorableByID("JawControl");
			jawControl.SetBoolParamValue("driveXRotationFromAudioSource", true);
			((SetDAZMorphControl)containingAtom.GetStorableByID("AutoExpressions")).enabledJSON.val = false;
			// autoJawMouthMorphs = containingAtom.GetStorableByID("AutoJawMouthMorph");
			DAZCharacterSelector characterSelector = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
			morphControl = characterSelector.morphsControlUI;
			
			GetMorphs();

			enabledJSON.setCallbackFunction += val => Reset();
			
			headAudioSource = containingAtom.GetStorableByID("HeadAudioSource").GetComponent<AudioSource>();
			moanLibrary = new List<ClipLibrary>
			{
				new ClipLibrary("moans0"),
				new ClipLibrary("moans1"),
				new ClipLibrary("moans2"),
				new ClipLibrary("moans3"),
				new ClipLibrary("moans4"),
				new ClipLibrary("moans5"),
			};
			var moansettings = new MoanSettings();
			
			voice.setCallbackFunction += val =>
			{
				AudioImporter.DeferredReadMoanBundle().Start();
			};
			var vammoan = this.FindPluginPeer("VAMMoan");
			if (vammoan)
			{
				vamMoanVoice = vammoan.GetStringChooserJSONParam("voice");
				voice.val = vamMoanVoice.val;
				vamMoanVoice.setCallbackFunction += val => voice.val = val;
				voice.setCallbackFunction += val =>
				{
					vamMoanVoice.val = val;
				};
			}
			// else RetrySyncWithVamMoan().Start();

			string vamMoanPackageUid = CheesyUtils.GetLatestPackageUid("hazmhox.vammoan");
			if (vamMoanPackageUid != null)
			{
				vammoanPackageInstalled = true;
				vammoanAudioPath = vamMoanPackageUid + ":/Custom/Scripts/VAMMoan/audio/";
				AudioImporter.GetClipsFromAssetBundle(moanLibrary, vammoanAudioPath+"voices.voicebundle");
				AudioImporter.GetClipsFromAssetBundle(new List<ClipLibrary>{FillMeUp.squishLibrary, FillMeUp.bjLibrary},
					vammoanAudioPath+"voices-shared.voicebundle");
				// AudioImporter.LoadMoanBundle();
			}
			else
			{
				vammoanPackageInstalled = false;
				slapMoanChance.val = 0f;
				sexSlapMoanChance.val = 0f;
			}
			slapMoanChance.setCallbackFunction += val =>
			{
				if (!vammoanPackageInstalled)
				{
					SuperController.LogError("SlapMe: Download VAMMoan to enable moan reactions.");
					slapMoanChance.valNoCallback = 0f;
				}
			};
			sexSlapMoanChance.setCallbackFunction += val =>
			{
				if (!vammoanPackageInstalled)
				{
					SuperController.LogError("SlapMe: Download VAMMoan to enable moan reactions.");
					sexSlapMoanChance.valNoCallback = 0f;
				}
			};
			randomBurstChanceJ.AddCallback(val => randomBurstChance = .01f * val);
			// idleBurstChanceJ.AddCallback(val => idleBurstChance = );
			sexMoanChanceJ.AddCallback(val => sexMoanChance = .1f * val);
			stimulationRegressionJ.AddCallback(val => stimulationRegression = val);
			orgasmRecoverTime.AddCallback(val => orgRecoverTimer = val);
			jawLimit.AddCallback((val => jawControl.SetFloatParamValue("driveXRotationFromAudioSourceMaxAngle", val)));
			headAudioVolume.AddCallback(val => headAudioControl.SetFloatParamValue("volume", val));
			headAudioPitch.AddCallback(val => headAudioControl.SetFloatParamValue("pitch", val));
			expressionsEnabledJ.AddCallback(val =>
			{
				expressionsEnabled = val;
				if(!val) ResetExpressions();
			});
			joyExpressionScale.AddCallback(val =>
			{
				if (val <= 20f) minJoyIncrement = .05f;
				else minJoyIncrement = .0025f * val;
			});
			painExpressionScale.AddCallback(val =>
			{
				if (val <= 20f) minPainIncrement = .05f;
				else minPainIncrement = .0025f * val;
			});
			stimulation.setCallbackFunction += val =>
			{
				if (isIdling)
				{
					if (val > idleThreshold.val) SetIdleMode(false);
				}
				else if (val < idleThreshold.val) SetIdleMode(true);
			};
			testStim.setCallbackFunction += val =>
			{
				// if(val > .8f) EmoteManager.stimulationEmotes.PlayBurst(1f);
			};
			maxLipLiftJ.AddCallback(val => maxLipLift = .001f * val);
			maxLowerLidLiftJ.AddCallback(val => maxLowerLidLift = .001f * val);
			morphUpdateThreshold.AddCallback(val => LerpableMorph.updateThreshold = val);
			stimulationGain.AddCallback(val => dynamicStimGain = val);
			
			SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRename;
			
			lastMorphs.Add(joyMorphs[0]);

			onOrgasmStartTrigger = new EventTrigger(this, "On Orgasm Start");
			onOrgasmEndTrigger = new EventTrigger(this, "On Orgasm End");
			onOrgasmRecoverTrigger = new EventTrigger(this, "On Orgasm Recover");
			stimTriggers = this.AddFloatTriggerManager(stimulation);
			orgasmTriggers = this.AddFloatTriggerManager(orgasmCount);
			multiOrgasmTriggers = this.AddFloatTriggerManager(multiOrgasmCount);

			parameters.Add(randomBurstChanceJ);
			parameters.Add(randomBurstStrenght);
			parameters.Add(joyExpressionScale);
			parameters.Add(painThresholdAtZero);
			parameters.Add(painThresholdAtOne);
			parameters.Add(painExpressionScale);
			parameters.Add(jawLimit);
			parameters.Add(headAudioVolume);
			parameters.Add(headAudioPitch);
			parameters.Add(idleThreshold);
			parameters.Add(stimulationGain);
			parameters.Add(stimulationRegressionJ);
			parameters.Add(orgasmThreshold);
			parameters.Add(orgasmTime);
			parameters.Add(orgasmFadeTime);
			parameters.Add(orgasmRecoverTime);
			parameters.Add(sexMoanChanceJ);
			parameters.Add(slapMoanChance);
			parameters.Add(sexSlapMoanChance);
			parameters.Add(burstMoanChance);
			parameters.Add(voice);
			parameters.Add(expressionsEnabledJ);
			parameters.Add(voiceEnabledJ);
			parameters.Add(breathingEnabledJ);
			parameters.Add(maxLipLiftJ);
			parameters.Add(displeaseOnly);
			parameters.Add(dynamicStimGainAdjustment);
			parameters.Add(showScreenOverlay);
			parameters.Add(maxMorphCount);
			parameters.Add(morphUpdateThreshold);
			parameters.Add(MoanSettings.silenceThreshold);
			parameters.Add(mouthStuffedDepthThreshold);
			parameters.Add(BJHelper.enabledJ);
			parameters.Add(Person.cumKeyChooser);
			parameters.Add(Person.cleanKeyChooser);
			parameters.Add(Person.particlesEnabled);
			parameters.Add(Person.cumClothingEnabled);
			parameters.Add(Person.foreskiEnabled);
			parameters.Add(Person.forceFullLoadJ);
			parameters.Add(Person.cumInteracting);
			parameters.Add(Person.particleAmount);
			parameters.Add(Person.particleOpacity);
			parameters.Add(Person.particleSpeed);
			parameters.Add(Person.cumShotPower);
			parameters.Add(Person.loadGainJ);
			parameters.Add(Person.maxLoad);
			parameters.Add(Person.clothingBreakThreshold);
			parameters.Add(Person.clothingFadeTime);
			parameters.Add(Person.stimGainJ);
			parameters.Add(Person.stimRegressionJ);
			parameters.Add(Person.stiffenEnabled);
			parameters.Add(Person.stiffenAmount);
			// parameters.Add(Male.particleBrightness);
			parameters.Add(Dildo.cumKeyChooser);
			parameters.Add(Dildo.particlesEnabled);
			parameters.Add(Dildo.forceFullLoadJ);
			parameters.Add(Dildo.cumInteracting);
			parameters.Add(Dildo.particleAmount);
			parameters.Add(Dildo.particleOpacity);
			parameters.Add(Dildo.particleSpeed);
			parameters.Add(Dildo.cumShotPower);
			parameters.Add(Dildo.loadGainJ);
			parameters.Add(Dildo.maxLoad);
			parameters.Add(Dildo.stimGainJ);
			parameters.Add(Dildo.stimRegressionJ);
			
			parameters.Add(femaleStimColor);
			parameters.Add(maleStimColor);
			// parameters.Add(Dildo.particleBrightness);
			parameters.AddRange(MoanSettings.thresholds.Select(x => x as JSONStorableParam));
			
			parameters.ForEach(x => x.Register(this));
			RegisterFloat(stimulation);
			orgasmNow.RegisterWithKeybingings(keyBindings);
			orgasmMalesNow.RegisterWithKeybingings(keyBindings);
			orgasmDildosNow.RegisterWithKeybingings(keyBindings);
			resetOrgasms.RegisterWithKeybingings(keyBindings);
			resetMorphs.RegisterWithKeybingings(keyBindings);
			resetCumClothes.RegisterWithKeybingings(keyBindings);

			EmoteManager.Init();
			ahegao = gameObject.AddComponent<Ahegao>().Init();

			presetSystem = new PresetSystem(this)
			{
				saveDir = "Saves/PluginData/CheesyFX/BodyLanguage/ReadMyLips/",
				Store = Store,
				Load = Load
			};
			breathingDriver = gameObject.AddComponent<BreathingDriver>().Init();
			stimulation.setCallbackFunction += breathingDriver.SetParameters;

			presetSystem.Init();
			CreateUI();
			SelectTab(1);
			SelectTab(0);
			Reset();
			stimbarImg = stimulation.slider.transform.Find("Fill Area/Fill").GetComponent<Image>();
			// PrintMorphFile();
			
			// var mbs = containingAtom.GetComponentsInChildren<DAZMorphBank>();
			// foreach (var mb in mbs)
			// {
			// 	mb.name.Print();
			// 	mb.morphs.ForEach(x =>
			// 	{
			// 		if (x.morphValue > 0f)
			// 			$"{x.displayName} {x.morphValue}".Print();
			// 	});
			// }
			FillMeUp.singleton.AddPenetrator(containingAtom);
			foreach (var atom in SuperController.singleton.GetAtoms().Where(x => x != containingAtom))
			{
				FillMeUp.singleton.AddPenetrator(atom);
			}
			SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
		}
		
		public static List<object> keyBindings = new List<object>();
		public void OnBindingsListRequested(List<object> bindings)
		{
			bindings.Add(new Dictionary<string, string>
			{
				{ "CheesyFX", "PoseMe" }
			});
            
			bindings.AddRange(keyBindings);
		}

		public override void InitUI()
		{
			base.InitUI();
			if(UITransform == null || uiListener != null || ReferenceEquals(FillMeUp.singleton, null) || FillMeUp.abort) return;
			uiListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
			uiListener.onEnabled.AddListener(() => UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements));
			uiListener.onEnabled.AddListener(() => Utils.OnInitUI(CreateUIElement));
			enabledJSON.toggle.interactable = false;
		}

		private void OnDisable()
		{
			voiceEnabled = false;
			expressionsEnabled = false;
			Gagger.RestoreBlink();
		}

		private void OnDestroy()
		{
			Reset();
			onOrgasmStartTrigger.Remove();
			onOrgasmEndTrigger.Remove();
			onOrgasmRecoverTrigger.Remove();
			
			Destroy(uiListener);
			parameters.ForEach(x => x.Deregister(this));
			DeregisterAction(orgasmNow);
			DeregisterAction(resetOrgasms);
			DeregisterAction((resetMorphs));
			SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
		}

		private void GetMorphs()
		{
			ResetExpressions();
			joyMorphs.Clear();
			painMorphs.Clear();
			lastMorphs.Clear();
			string joyMorphPath = packageUid + "Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Expressions/Joy/";
			string painMorphPath = packageUid + "Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/Expressions/Displease";
		
			foreach (var path in FileManagerSecure.GetFiles(joyMorphPath + "Ascorad"))
			{
				if(path.EndsWith(".vmb")) continue;
				var pathNormalized = FileManagerSecure.NormalizePath(path);
				joyMorphs.Add(GetMorph(pathNormalized));
			}
			foreach (var path in FileManagerSecure.GetFiles(joyMorphPath + "Jackaroo"))
			{
				if(!path.EndsWith(".vmi")) continue;
				var pathNormalized = FileManagerSecure.NormalizePath(path);
				joyMorphs.Add(GetMorph(pathNormalized));
			}
			foreach (var path in FileManagerSecure.GetFiles(joyMorphPath + "AnalogueBob"))
			{
				if(!path.EndsWith(".vmi")) continue;
				var pathNormalized = FileManagerSecure.NormalizePath(path);
				joyMorphs.Add(GetMorph(pathNormalized));
			}
			foreach (var path in FileManagerSecure.GetFiles(joyMorphPath + "WeebU"))
			{
				if(!path.EndsWith(".vmi")) continue;
				var pathNormalized = FileManagerSecure.NormalizePath(path);
				joyMorphs.Add(GetMorph(pathNormalized));
			}
			foreach (var path in FileManagerSecure.GetFiles(joyMorphPath + "AshAuryn"))
			{
				if(path.EndsWith(".vmb")) continue;
				var pathNormalized = FileManagerSecure.NormalizePath(path);
				joyMorphs.Add(GetMorph(pathNormalized));
			}
			foreach (var path in FileManagerSecure.GetFiles(joyMorphPath + "AshAuryn/AsymMouth"))
			{
				if(path.EndsWith(".vmb")) continue;
				var pathNormalized = FileManagerSecure.NormalizePath(path);
				joyMorphs.Add(GetMorph(pathNormalized));
			}
			foreach (var path in FileManagerSecure.GetFiles(painMorphPath))
			{
				if(path.EndsWith(".vmb")) continue;
				var pathNormalized = FileManagerSecure.NormalizePath(path);
				painMorphs.Add(GetMorph(pathNormalized));
			}
			
			
			joyMorphs.Add(GetMorph("Desire"));
			joyMorphs.Add(GetMorph("Excitement"));
			joyMorphs.Add(GetMorph("Flirting"));
			joyMorphs.Add(GetMorph("Flirting Feminine Left"));
			joyMorphs.Add(GetMorph("Flirting Feminine Right"));
			joyMorphs.Add(GetMorph("Flirting Masculine Left"));
			joyMorphs.Add(GetMorph("Flirting Masculine Right"));
			joyMorphs.Add(GetMorph("Frown"));
			joyMorphs.Add(GetMorph("Mouth Frown"));
			joyMorphs.Add(GetMorph("Sad"));
			joyMorphs.Add(GetMorph("Scream"));
			joyMorphs.Add(GetMorph("Shock"));
			joyMorphs.Add(GetMorph("Smile Full Face"));
			joyMorphs.Add(GetMorph("Snarl Left"));
			joyMorphs.Add(GetMorph("Snarl Right"));
			joyMorphs.Add(GetMorph("Surprise"));
			
			joyMorphs.Add(GetMorph("Brow Down"));
			joyMorphs.Add(GetMorph("Brow Down Left"));
			joyMorphs.Add(GetMorph("Brow Down Right"));
			joyMorphs.Add(GetMorph("Brow Inner Down"));
			joyMorphs.Add(GetMorph("Brow Inner Down Left"));
			joyMorphs.Add(GetMorph("Brow Inner Down Right"));
			joyMorphs.Add(GetMorph("Brow Inner Up Left"));
			joyMorphs.Add(GetMorph("Brow Inner Up Right"));
			joyMorphs.Add(GetMorph("Brow Outer Down Left"));
			joyMorphs.Add(GetMorph("Brow Outer Down Right"));
			joyMorphs.Add(GetMorph("Brow Outer Up"));
			joyMorphs.Add(GetMorph("Brow Outer Up Left"));
			joyMorphs.Add(GetMorph("Brow Outer Up Right"));
			joyMorphs.Add(GetMorph("Brow Up"));
			joyMorphs.Add(GetMorph("Brow Up Left"));
			joyMorphs.Add(GetMorph("Brow Up Right"));
			joyMorphs.Add(GetMorph("Brow Squeeze"));
			
			joyMorphs.Add(GetMorph("Eyes Closed"));
			joyMorphs.Add(GetMorph("Eyes Squint"));
			joyMorphs.Add(GetMorph("Eyes Squint Left"));
			joyMorphs.Add(GetMorph("Eyes Squint Right"));
			
			// joyMorphs.Add(GetMorph("Mouth Open"));
			// joyMorphs.Add(GetMorph("Mouth Open Wide"));
			// joyMorphs.Add(GetMorph("Mouth Narrow"));
			// joyMorphs.Add(GetMorph("Mouth Narrow Left"));
			// joyMorphs.Add(GetMorph("Mouth Narrow Right"));
			joyMorphs.Add(GetMorph("Mouth Side-Side Left"));
			joyMorphs.Add(GetMorph("Mouth Side-Side Right"));
			joyMorphs.Add(GetMorph("Mouth Smile"));
			joyMorphs.Add(GetMorph("Mouth Smile Open"));
			joyMorphs.Add(GetMorph("Mouth Smile Simple"));
			joyMorphs.Add(GetMorph("Mouth Smile Simple Left"));
			joyMorphs.Add(GetMorph("Mouth Smile Simple Right"));
			
			joyMorphs.Add(GetMorph("Lip Bottom Down"));
			joyMorphs.Add(GetMorph("Lip Bottom Out"));
			joyMorphs.Add(GetMorph("Lip Bottom In"));
			joyMorphs.Add(GetMorph("Lip Bottom In Left"));
			joyMorphs.Add(GetMorph("Lip Bottom Out Right"));
			joyMorphs.Add(GetMorph("Lip Bottom Up"));
			joyMorphs.Add(GetMorph("Lip Bottom Up Left"));
			joyMorphs.Add(GetMorph("Lip Bottom Up Right"));
			// joyMorphs.Add(GetMorph("Lip Top Up"));
			joyMorphs.Add(GetMorph("Lip Top Up Left"));
			joyMorphs.Add(GetMorph("Lip Top Up Right"));
			joyMorphs.Add(GetMorph("Lips Close"));
			joyMorphs.Add(GetMorph("Lips Part"));
			joyMorphs.Add(GetMorph("Lips Pucker"));
			joyMorphs.Add(GetMorph("Lips Pucker Wide"));
			
			painMorphs.Add(GetMorph("Nose Wrinkle"));
			painMorphs.Add(GetMorph("Snarl Left"));
			painMorphs.Add(GetMorph("Snarl Right"));
			
			LoadExpressionSets();
			// joyMorphs = joyMorphs.Where(x => !x.dazMorph.isDriven && !x.dazMorph.hasFormulas).ToList();
		}

		public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
		{
			JSONClass jc = base.GetJSON(includePhysical, includeAppearance, forceStore);
			if (includePhysical || forceStore)
			{
				needsStore = true;
				jc[onOrgasmStartTrigger.Name] = onOrgasmStartTrigger.GetJSON(base.subScenePrefix);
				jc[onOrgasmEndTrigger.Name] = onOrgasmEndTrigger.GetJSON(base.subScenePrefix);
				jc[onOrgasmRecoverTrigger.Name] = onOrgasmRecoverTrigger.GetJSON(base.subScenePrefix);
				jc["StimTriggers"] = stimTriggers.Store();
				jc["OrgasmTriggers"] = orgasmTriggers.Store();
				jc["MultiOrgasmTriggers"] = multiOrgasmTriggers.Store();
				foreach (var receiver in FillMeUp.stimReceivers.Values.ToList())
				{
					jc[receiver.onOrgasmTrigger.Name] = receiver.onOrgasmTrigger.GetJSON(base.subScenePrefix);
				}
			}
			Ahegao.Store(jc);
			return jc;
		}

		public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
		{
			base.LateRestoreFromJSON(jc, restorePhysical, restoreAppearance, setMissingToDefault);
			Load(jc);
			if (!physicalLocked && restorePhysical && !IsCustomPhysicalParamLocked("trigger"))
			{
				needsStore = true;
				
				onOrgasmStartTrigger.RestoreFromJSON(jc, base.subScenePrefix, base.mergeRestore, setMissingToDefault);
				onOrgasmEndTrigger.RestoreFromJSON(jc, base.subScenePrefix, base.mergeRestore, setMissingToDefault);
				onOrgasmRecoverTrigger.RestoreFromJSON(jc, base.subScenePrefix, base.mergeRestore, setMissingToDefault);
				
				stimTriggers.Load(jc["StimTriggers"].AsObject);
				orgasmTriggers.Load(jc["OrgasmTriggers"].AsObject);
				multiOrgasmTriggers.Load(jc["MultiOrgasmTriggers"].AsObject);
				stimTriggerButton.label = $"Stimulation Triggers ({stimTriggers.triggers.Count})";
				orgasmTriggerButton.label = $"Orgasm Count Triggers ({orgasmTriggers.triggers.Count})";
				multiOrgasmTriggerButton.label = $"Multi Orgasm Triggers ({multiOrgasmTriggers.triggers.Count})";
				foreach (var receiver in FillMeUp.stimReceivers.Values.ToList())
				{
					receiver.onOrgasmTrigger.RestoreFromJSON(jc, base.subScenePrefix, base.mergeRestore, setMissingToDefault);
				}
				
				foreach (var setting in MoanSettings.thresholds)
				{
					setting.RestoreFromJSON(jc);
				}
			}
		}

		public void OnAtomRename(string oldUid, string newUid)
		{
			// stimTriggers.ForEach(x => x.OnAtomRename());
			// orgasmTriggers.ForEach(x => x.OnAtomRename());
			onOrgasmStartTrigger.SyncAtomNames();
			onOrgasmEndTrigger.SyncAtomNames();
			onOrgasmRecoverTrigger.SyncAtomNames();
			foreach (var receiver in FillMeUp.stimReceivers.Values.ToList())
			{
				receiver.onOrgasmTrigger.SyncAtomNames();
			}
		}

		private JSONStorableString orgasmInfo = new JSONStorableString("OrgasmInfo", "");

		public void CreateUI()
		{
			presetSystem.CreateUI();
			// UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements);
			UIManager.RemoveUIElements(UIElements);
			stimulation.CreateUI(this);
			// orgasmCount.CreateUI(this, true);
			var textfield = CreateTextField(orgasmInfo, true);
			textfield.height = 120f;
			textfield.UItext.fontSize = 34;
			string[] items;
			// if (!FillMeUp.isFuta)
			// 	items = new[] { "Stimulation", "Orgasm", "Ahegao", "Expressions", "Voice", "Pre Blowjob", "Male Stimulation" };
			// else items = new[] { "Stimulation", "Orgasm", "Ahegao", "Expressions", "Voice", "Pre Blowjob", "Male Stimulation", "AltFuta" };
			items = new[] { "Stimulation", "Orgasm", "Ahegao", "Expressions", "Voice", "Pre Blowjob", "Males/AltFuta", "Dildos" };
			tabbar = UIManager.CreateTabBar(items, SelectTab, script:this, columns:4);
			tabbar.SelectTab(lastTabId);
		}
		
		public void ClearUI()
		{
			UIManager.RemoveUIElements(leftUIElements.Select(x => (object) x.GetComponent<UIDynamic>()).ToList());
			UIManager.RemoveUIElements(rightUIElements.Select(x => (object) x.GetComponent<UIDynamic>()).ToList());
		}

		public static bool mainWindowOpen = true;
		private void SelectTab(int id)
		{
			lastTabId = id;
			UIManager.RemoveUIElements(UIElements);
			UIManager.RemoveUIElements(maleSubUIElements);
			maleInfoUIOpen = dildoInfoUIOpen = false;
			switch (id)
			{
				case 0:
				{
					CreateStimUI();
					break;
				}
				case 1:
				{
					CreateOrgasmUI();
					break;
				}
				case 2:
				{
					Ahegao.CreateUI(UIElements);
					break;
				}
				case 3:
				{
					CreateExpressionsUI();
					break;
				}
				case 4:
				{
					CreateVoiceUI();
					break;
				}
				case 5:
				{
					BJHelper.CreateUI(UIElements);
					break;
				}
				case 6:
				{
					CreateMaleUI(1);
					break;
				}
				case 7:
				{
					CreateMaleUI(2);
					break;
				}
			}
		}

		public static JSONStorableFloat testStim = new JSONStorableFloat("Test Stim", 0f, 0f, 1f);

		private void CreateStimUI()
		{
			stimulationGain.CreateUI(script: this, rightSide:false, UIElements:UIElements);
			stimulationRegressionJ.CreateUI(script: this, rightSide:true, UIElements:UIElements);
			dynamicStimGainAdjustment.CreateUI(script: this, rightSide:false, UIElements:UIElements);
			stimTriggerButton = CreateButton($"Stimulation Triggers ({stimTriggers.triggers.Count})", true);
			stimTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
			stimTriggerButton.button.onClick.AddListener(() =>
			{
				{
					ClearUI();
					stimTriggers.OpenPanel(this, CreateUI);
				}
			});
			UIElements.Add(stimTriggerButton);
			breathingEnabledJ.CreateUI(script:this, rightSide:true, UIElements:UIElements);
			var textfield = CreateTextField(dynamicGainInfo);
			textfield.height = 110f;
			UIElements.Add(textfield);
		}

		private void CreateOrgasmUI()
		{
			showScreenOverlay.CreateUI(UIElements, false);
			orgasmThreshold.CreateUI(UIElements);
			orgasmTime.CreateUI(UIElements);
			orgasmFadeTime.CreateUI(UIElements);
			orgasmRecoverTime.CreateUI(UIElements);
			
			
			orgasmTriggerButton = CreateButton($"Orgasm Count Triggers ({orgasmTriggers.triggers.Count})", true);
			orgasmTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
			orgasmTriggerButton.button.onClick.AddListener(() =>
			{
				{
					ClearUI();
					orgasmTriggers.OpenPanel(this, CreateUI);
				}
			});
			UIElements.Add(orgasmTriggerButton);
			multiOrgasmTriggerButton = CreateButton($"Multi Orgasm Triggers ({multiOrgasmTriggers.triggers.Count})", true);
			multiOrgasmTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
			multiOrgasmTriggerButton.button.onClick.AddListener(() =>
			{
				{
					ClearUI();
					multiOrgasmTriggers.OpenPanel(this, CreateUI);
				}
			});
			UIElements.Add(multiOrgasmTriggerButton);
			var button = CreateButton("On Orgasm Start", true);
			button.buttonColor = new Color(0.45f, 1f, 0.45f);
			button.button.onClick.AddListener(onOrgasmStartTrigger.OpenPanel);
			UIElements.Add(button);
			button = CreateButton("On Orgasm End", true);
			button.buttonColor = new Color(0.45f, 1f, 0.45f);
			button.button.onClick.AddListener(onOrgasmEndTrigger.OpenPanel);
			UIElements.Add(button);
			button = CreateButton("On Orgasm Recover", true);
			button.buttonColor = new Color(0.45f, 1f, 0.45f);
			button.button.onClick.AddListener(onOrgasmRecoverTrigger.OpenPanel);
			UIElements.Add(button);
			button = CreateButton("Orgasm Now", true);
			button.button.onClick.AddListener(ForceOrgasm);
			UIElements.Add(button);
			button = CreateButton("Reset Orgasms", true);
			button.button.onClick.AddListener(resetOrgasms.actionCallback.Invoke);
			UIElements.Add(button);
			// successiveOrgasmCount.CreateUI(UIElements: UIElements, rightSide: true);
		}

		private void CreateExpressionsUI()
		{
			joyExpressionScale.CreateUI(this, UIElements:UIElements);
			painExpressionScale.CreateUI(this, UIElements:UIElements, rightSide:true);
			// maxMorphVal.CreateUI(this, UIElements:UIElements);
			painThresholdAtZero.CreateUI(UIElements, true);
			painThresholdAtOne.CreateUI(UIElements, true);
			randomBurstChanceJ.CreateUI(UIElements);
			randomBurstStrenght.CreateUI(UIElements);
			idleThreshold.CreateUI(UIElements, false);
			maxLipLiftJ.CreateUI(UIElements, true);
			maxLowerLidLiftJ.CreateUI(UIElements, true);
			expressionsEnabledJ.CreateUI(UIElements);
			displeaseOnly.CreateUI(UIElements, rightSide:false);
			maxMorphCount.CreateUI(UIElements, rightSide:true);
			maxMorphCount.slider.wholeNumbers = true;
			UIDynamicSlider slider = (UIDynamicSlider)morphUpdateThreshold.CreateUI(UIElements, false);
			slider.valueFormat = "0.000";
			this.SetupButton("Reset Morphs", false, ResetExpressions, UIElements);
		}
		
		private void CreateVoiceUI()
		{
			voiceEnabledJ.CreateUI(this, UIElements: UIElements);
			voice.CreateUI(script: this, rightSide:true, chooserType:1, UIElements:UIElements);
			sexMoanChanceJ.CreateUI(script: this, UIElements:UIElements, rightSide:true);
			burstMoanChance.CreateUI(script: this, UIElements:UIElements, rightSide:true);
			slapMoanChance.CreateUI(script: this, UIElements:UIElements, rightSide:true);
			sexSlapMoanChance.CreateUI(script: this, rightSide:true, UIElements:UIElements);
			MoanSettings.silenceThreshold.CreateUI(UIElements);
			foreach (var setting in MoanSettings.thresholds)
			{
				setting.CreateUI(script: this, UIElements: UIElements);
			}
			orgasmThreshold.CreateUI(script: this, UIElements: UIElements);
			headAudioVolume.CreateUI(script: this, UIElements:UIElements, rightSide:true);
			headAudioPitch.CreateUI(script: this, UIElements:UIElements, rightSide:true);
			jawLimit.CreateUI(this, UIElements:UIElements, rightSide:false);
			var slider = (UIDynamicSlider)mouthStuffedDepthThreshold.CreateUI(UIElements, true);
			slider.valueFormat = "0.000";
		}

		private UIDynamicTabBar maleTabbar;
		public static int lastMaleTabId;
		private List<object> maleUIElements = new List<object>();
		private List<object> maleSubUIElements = new List<object>();
		private void CreateMaleUI(int type)
		{
			maleTabbar = UIManager.CreateTabBar(new []{"Info & Triggers", "Settings"}, val => UISelectMaleTab(val, type), script:this, columns:4);
			// var names = FillMeUp.stimReceivers.Values.Select(x => x.name).ToArray();
			// maleTabbar = UIManager.CreateTabBar(names, UISelectStimReceiver, script:this, columns:6);
			UIElements.Add(maleTabbar);
			maleTabbar.SelectTab(lastMaleTabId);
		}

		public void UISelectMaleTab(int id, int type)
		{
			UIManager.RemoveUIElements(maleSubUIElements);
			lastMaleTabId = id;
			maleInfoUIOpen = dildoInfoUIOpen = false;
			switch (id)
			{
				case 0:
				{
					CreateMaleInfoUI(type);
					break;
				}
				case 1:
				{
					CreateMaleSettingsUI(type);
					break;
				}
			}
		}

		public static bool maleInfoUIOpen;
		public static bool dildoInfoUIOpen;
		private void CreateMaleInfoUI(int type)
		{
			if (type == 1) maleInfoUIOpen = true;
			else dildoInfoUIOpen = true;
			foreach (var receiver in FillMeUp.stimReceivers.Values.Where(x => x.penetrator.type == type))
			{
				if(type == 1)
				{
					var male = (Person)receiver;
					if (male.dcs.gender == DAZCharacterSelector.Gender.Female && !male.characterListener.isFuta) continue;
				}
				receiver.stimulation.CreateUI(maleSubUIElements);
				receiver.cumshotHandler.load.CreateUI(maleSubUIElements, true);
				if (type == 1)
				{
					var twinButton = Utils.SetupTwinButton(this, $"<b>{receiver.uid}</b> Orgasm", () => receiver.ForceOrgasm(),
						"Reset Cum Clothes", receiver.UnequipClothes, false);
					maleSubUIElements.Add(twinButton);
				}
				else this.SetupButton($"<b>{receiver.uid}</b> Orgasm", false, () => receiver.ForceOrgasm(), maleSubUIElements);
				var button = CreateButton(receiver.onOrgasmTrigger.Name, true);
				button.buttonColor = new Color(0.45f, 1f, 0.45f);
				button.button.onClick.AddListener(receiver.onOrgasmTrigger.OpenPanel);
				maleSubUIElements.Add(button);
				receiver.stimbarImg = receiver.stimulation.slider.transform.Find("Fill Area/Fill").GetComponent<Image>();
				// if (type == 1)
				// {
				// 	this.SetupButton("Reset Cum Clothes", true, () => ((Male)receiver).UnequipClothes(), maleSubUIElements);
				// }
				// else
				// {
				// 	spacer = CreateSpacer(true);
				// 	spacer.ForceHeight(50f);
				// 	maleSubUIElements.Add(spacer);
				// }

				// spacer = CreateSpacer(true);
				// spacer.ForceHeight(50f);
				// maleSubUIElements.Add(spacer);
			}
		}

		private void CreateMaleSettingsUI(int type)
		{
			if (type == 1 )
			{
				Person.particlesEnabled.CreateUI(maleSubUIElements);
				Person.cumClothingEnabled.CreateUI(maleSubUIElements);
				Person.foreskiEnabled.CreateUI(maleSubUIElements);
				Person.cumKeyChooser.CreateUI(maleSubUIElements);
				Person.cleanKeyChooser.CreateUI(maleSubUIElements);
				Person.cumInteracting.CreateUI(maleSubUIElements);
				Person.forceFullLoadJ.CreateUI(maleSubUIElements);
				Person.stimGainJ.CreateUI(maleSubUIElements);
				Person.stimRegressionJ.CreateUI(maleSubUIElements);
				Person.cumShotPower.CreateUI(maleSubUIElements, true);
				Person.particleAmount.CreateUI(maleSubUIElements, true);
				Person.particleOpacity.CreateUI(maleSubUIElements, true);
				Person.loadGainJ.CreateUI(maleSubUIElements, true);
				Person.maxLoad.CreateUI(maleSubUIElements, true);
				
				Person.clothingBreakThreshold.CreateUI(maleSubUIElements, true);
				Person.clothingFadeTime.CreateUI(maleSubUIElements, true);
			}
			else
			{
				Dildo.particlesEnabled.CreateUI(maleSubUIElements);
				Dildo.cumKeyChooser.CreateUI(maleSubUIElements);
				Dildo.cumInteracting.CreateUI(maleSubUIElements);
				Dildo.forceFullLoadJ.CreateUI(maleSubUIElements);
				Dildo.stimGainJ.CreateUI(maleSubUIElements);
				Dildo.stimRegressionJ.CreateUI(maleSubUIElements);
				Dildo.cumShotPower.CreateUI(maleSubUIElements, true);
				Dildo.particleAmount.CreateUI(maleSubUIElements, true);
				Dildo.particleOpacity.CreateUI(maleSubUIElements, true);
				Dildo.loadGainJ.CreateUI(maleSubUIElements, true);
				Dildo.maxLoad.CreateUI(maleSubUIElements, true);
			}
			
		}

		private void UISelectStimReceiver(int id)
		{
			UIManager.RemoveUIElements(maleUIElements);
			var type = id + 1;
			foreach (var receiver in FillMeUp.stimReceivers.Values.ToList())
			{
				if (receiver.penetrator.type == type)
				{
					receiver.stimulation.CreateUI(maleUIElements);
					Person.particlesEnabled.CreateUI(maleUIElements);
					Dildo.particlesEnabled.CreateUI(maleUIElements);
				}
			}
		}

		public static void Stimulate(float stimulus, bool doChange = false, bool doStim = false, bool doPlease = false)
		{
			if (!singleton.enabled || SuperController.singleton.freezeAnimation || FillMeUp.isSavingScene) return;
			float stim = stimulus * .01f;
			if (doChange)
			{
				// if (stimulation.val <= .01f)
				// {
				// 	resetTargetsTimer = stimBurstTimer = Random.Range(.5f, 4f);
				// }
				// resetTargetsTimer = 1f;
				resetTargetsTimer = stimBurstTimer = Random.Range(.5f, 4f);
			}
			// resetTargetsTimer = 1f;
			DistributeStimulus(stim*(1f-.5f*stimulation.val*stimulation.val), doChange, doPlease);
			if(FillMeUp.isPenetrated || doStim)
			{
				stimulation.val += stim * dynamicStimGain;
				if (!isOrgasmPleasure && stimulation.val >= orgasmThreshold.val)
				{
					DoOrgasm();
				}
			}
		}

		private static float lLowerLidLiftSum;
		private static float rLowerLidLiftSum;
		private static List<LerpableMorph> morphs = new List<LerpableMorph>();

		public static void DistributeStimulus(float stim, bool doChange, bool doPlease)
		{
			// stim.Print();
			if (!expressionsEnabled) return;
			float available = stim;
			float factor;
			float minIncrement;
			morphs.Clear();
			if(displeaseOnly.val || (!doPlease && stimRate > 0f && stimRate >= Mathf.Lerp(painThresholdAtZero.val, painThresholdAtOne.val, stimulation.val))){
				morphs.AddRange(painMorphs);
				factor = painExpressionScale.val;
				minIncrement = minPainIncrement;
			}
			else{
				morphs.AddRange(joyMorphs);
				// factor = Mathf.Lerp(joyExpressionScale.val*2f, joyExpressionScale.val , stimulation.val);
				factor = joyExpressionScale.val * 2f;
				// factor = joyExpressionScale.val * 1.5f;
				minIncrement = minJoyIncrement;
			}

			GetLipLiftSum();
			GetLLowerLidLiftSum();
			GetRLowerLidLiftSum();
			available *= factor;
			if (!doChange && Random.Range(0f, 1f) > .1f)
			{
				foreach(var morph in lastMorphs)
				{
					if (available <= minIncrement) break;
					float maxIncrement = Math.Min(available, morph.max - morph.target);
					if(morph.lipLift > 0f) maxIncrement = Mathf.Min(maxIncrement, (maxLipLift-lipLiftSum)/morph.lipLift);
					if(morph.lLowerLidLift > 0f) maxIncrement = Mathf.Min(maxIncrement, (maxLowerLidLift-lLowerLidLiftSum)/morph.lLowerLidLift);
					if(morph.rLowerLidLift > 0f) maxIncrement = Mathf.Min(maxIncrement, (maxLowerLidLift-rLowerLidLiftSum)/morph.rLowerLidLift);
					if(maxIncrement < minIncrement) continue;
					float increment = Random.Range(minIncrement, maxIncrement);
					morph.target += increment;
					available -= increment;
					lipLiftSum += morph.target * morph.lipLift;
					lLowerLidLiftSum += morph.target * morph.lLowerLidLift;
					rLowerLidLiftSum += morph.target * morph.rLowerLidLift;
					morphs.Remove(morph);
				}
			}
			else
			{
				lastMorphs.Clear();
				for (var i = 0; i <= maxMorphCount.val; i++)
				{
					if (available <= minIncrement) break;
					var morph = morphs[rng.Next(morphs.Count)];
					float maxIncrement = Math.Min(available, morph.max - morph.target);
					if(morph.lipLift > 0f) maxIncrement = Mathf.Min(maxIncrement, (maxLipLift-lipLiftSum)/morph.lipLift);
					if(morph.lLowerLidLift > 0f) maxIncrement = Mathf.Min(maxIncrement, (maxLowerLidLift-lLowerLidLiftSum)/morph.lLowerLidLift);
					if(morph.rLowerLidLift > 0f) maxIncrement = Mathf.Min(maxIncrement, (maxLowerLidLift-rLowerLidLiftSum)/morph.rLowerLidLift);
					if(maxIncrement < minIncrement) continue;
					float increment;
					if (i == maxMorphCount.val) increment = maxIncrement;
					else increment = Random.Range(minIncrement, maxIncrement);
					morph.target += increment;
					available -= increment;
					lastMorphs.Add(morph);
					lipLiftSum += morph.target * morph.lipLift;
					lLowerLidLiftSum += morph.target * morph.lLowerLidLift;
					rLowerLidLiftSum += morph.target * morph.rLowerLidLift;
					morphs.Remove(morph);
				}
			}
			if(targetsReset) targetsReset = false;
			// if(lLowerLidLiftSum > .0025f) lLowerLidLiftSum.Print();
			// lipLiftSum = 0f;
		}

		private static void GetLipLiftSum()
		{
			lipLiftSum = 0f;
			for (int i = 0; i < joyMorphs.Count; i++)
			{
				if(joyMorphs[i].active) lipLiftSum += joyMorphs[i].lipLift * joyMorphs[i].target;
			}
			for (int i = 0; i < painMorphs.Count; i++)
			{
				if(painMorphs[i].active) lipLiftSum += painMorphs[i].lipLift * painMorphs[i].target;
			}
			// lipLiftSum.Print();
		}
		
		private static void GetLLowerLidLiftSum()
		{
			lLowerLidLiftSum = 0f;
			for (int i = 0; i < joyMorphs.Count; i++)
			{
				if(joyMorphs[i].active) lLowerLidLiftSum += joyMorphs[i].lLowerLidLift * joyMorphs[i].target;
			}
			for (int i = 0; i < painMorphs.Count; i++)
			{
				if(painMorphs[i].active) lLowerLidLiftSum += painMorphs[i].lLowerLidLift * painMorphs[i].target;
			}
			// lLowerLidLiftSum.Print();
		}
		
		private static void GetRLowerLidLiftSum()
		{
			rLowerLidLiftSum = 0f;
			for (int i = 0; i < joyMorphs.Count; i++)
			{
				if(joyMorphs[i].active) rLowerLidLiftSum += joyMorphs[i].rLowerLidLift * joyMorphs[i].target;
			}
			for (int i = 0; i < painMorphs.Count; i++)
			{
				if(painMorphs[i].active) rLowerLidLiftSum += painMorphs[i].rLowerLidLift * painMorphs[i].target;
			}
			// rLowerLidLiftSum.Print();
		}

		private JSONStorableFloat morphsUpdateRate = new JSONStorableFloat("Expression Update Rate", 30f, 10f, 60f);

		public void Update()
		{
			// return;
			// var dcr = containingAtom.gameObject.GetComponentInChildren<DAZCharacterRun>();
			// dcr.ResetMorphs();
			// $"m: {maleInfoUIOpen} d: {dildoInfoUIOpen}".Print();
			if (Input.GetKeyUp(Person.cumKey))
			{
				ForceMaleOrgasm(1);
			}
			if (Input.GetKeyUp(Dildo.cumKey))
			{
				ForceMaleOrgasm(2);
			}
			if (Input.GetKeyUp(Person.cleanKey))
			{
				ResetCumClothes();
			}
			if (SuperController.singleton.freezeAnimation) return;
			// lEye.transform.localEulerAngles = new Vector3(-25f, 0f, 0f);
			// GetLipLiftSum();
			onOrgasmStartTrigger.Update();
			onOrgasmEndTrigger.Update();
			onOrgasmRecoverTrigger.Update();
			// stimDelta = Mathf.Max((stimulation.val - lastStimulation), 0f);
			stimDelta = stimulation.val - lastStimulation;
			if (stimDelta > 0f)
			{
				altFutaStim.cumshotHandler.load.val += 3f*altFutaStim.loadGain*stimDelta*(1f+stimulation.val);
				dynamicStimGain += stimDelta * dynamicStimGainAdjustment.val;
				stimRate = stimDelta / Time.deltaTime;
			}
			else
			{
				stimRate = 0f;
			}
			lastStimulation = stimulation.val;
			burstTimer -= Time.deltaTime;
			if (burstTimer < 0f)
			{
				burstTimer = .2f;

				if (randomBurstChanceJ.val >= .999f || (randomBurstChance > 0f && randomBurstStrenght.val > 0f &&
				    Random.Range(0f, 1f) <= randomBurstChanceJ.val))
				{
					Stimulate(randomBurstStrenght.val, true, doPlease:true);
					PlayBurstMoan();
				}
			}
			
			if (isOrgasming)
			{
				EmoteManager.stimulationEmotes.emission.enabled = false;
				orgasmTimer -= Time.deltaTime;
				if (orgasmTimer < orgasmFadeTime.val)
				{
					if(isOrgasmPleasure)
					{
						isOrgasmPleasure = false;
						stimulationRegression = stimulationRegressionJ.val + .1f*orgasmCount.val;
						Ahegao.ShutDown();
					}

					if (multiOrgasmCount.val > 0f && orgasmTimer < orgasmFadeTime.val * .5f)
					{
						if(EmoteManager.enabled.val) EmoteManager.orgasmFadeEmotes.Trigger(multiOrgasmCount.val);
						multiOrgasmCount.val = 0f;
					}
					if (orgasmTimer < 0f)
					{
						onOrgasmEndTrigger.Trigger();
						if (orgasmCount.val > 0)
							stimulationRegression =
								stimulationRegressionJ.val * Mathf.Log(2f * orgasmCount.val + 1f);
						else stimulationRegression = stimulationRegressionJ.val;
						isOrgasming = false;
						dynamicStimGain = stimulationGain.val;
						EmoteManager.stimulationEmotes.emission.enabled = EmoteManager.enabled.val;
					}
				}
			}
			else if(orgasmCount.val > 0)
			{
				orgRecoverTimer -= Time.deltaTime;
				if (orgRecoverTimer < 0f)
				{
					onOrgasmRecoverTrigger.Trigger();
					orgasmCount.val -= 1;
					if (orgasmCount.val > 0)
						stimulationRegression = stimulationRegressionJ.val * Mathf.Log(2f * orgasmCount.val + 1f);
					else stimulationRegression = stimulationRegressionJ.val;
					orgRecoverTimer = orgasmRecoverTime.val;
				}
			}
			
			if(blinkTimeout > 0f) blinkTimeout -= Time.deltaTime;
			if(stimulation.val > .01f){
				stimulation.val = Mathf.Lerp(stimulation.val, 0f, stimulationRegression*Time.deltaTime);
			}
			
			// if(lastMorphs[0].target >0f) lastMorphs[0].target.Print();
			// else if(!isReset) ResetTargets();
			if(resetTargetsTimer > 0f) resetTargetsTimer -= Time.deltaTime;
			if(stimBurstTimer > 0f) stimBurstTimer -= Time.deltaTime;

			morphsActive = 0;
			foreach(var morph in painMorphs)
			{
				if (morph.active)
				{
					morph.LerpToTarget();
					morphsActive++;
				}
			}
			foreach(var morph in joyMorphs){
				// if(morph.target > 0f) $"{morph.name} {morph.target} {morph.val}".Print();
				if (morph.active)
				{
					morph.LerpToTarget();
					morphsActive++;
				}
			}
			if(!targetsReset && (resetTargetsTimer <= 0f || stimBurstTimer <= 0f))
			{
				foreach(var morph in painMorphs){
					if(morph.target > 0f) morph.target = 0f;
				}
				foreach(var morph in joyMorphs){
					if(morph.target > 0f) morph.target = 0f;
				}
				targetsReset = true;
				resetTargetsTimer = 1f;
			}

			if (UITransform.gameObject.activeSelf && mainWindowOpen)
			{
				if(lastTabId == 0) dynamicGainInfo.val = $"Dynamic Stim Gain:\t{dynamicStimGain:0.0000}\nDynamic Stim Regression:\t{stimulationRegression:0.000}";
				orgasmInfo.val = $"Total Orgasms: \t{orgasmCount.val}\nMulti Orgasms \t{multiOrgasmCount.val}";
				stimbarImg.color = Color.Lerp(Color.white, Color.magenta, 3f*(dynamicStimGain - stimulationGain.val));
			}
			EmoteManager.stimulationEmotes.Update(stimulation.val);
		}

		

		private static void DoOrgasm()
		{
			// lookMode = eyeBehavior.currentLookMode;
			// eyeBehavior.currentLookMode = EyesControl.LookMode.None;
			isOrgasming = true;
			isOrgasmPleasure = true;
			orgasmCount.val += 1f;
			Stimulate(3f, true);
			onOrgasmStartTrigger.Trigger();
			// float eyeRoll = Random.Range(-22, -10f);
			ahegao.Run();
			orgasmTimer = orgasmTime.val + orgasmFadeTime.val;
			PlayRandomMoan(5);
			multiOrgasmCount.val += 1;
			if(FillMeUp.isFuta) altFutaStim.Orgasm();
			foreach (var fuckable in FillMeUp.fuckables)
			{
				if(fuckable.penetrator != null && fuckable.penetrator.stimReceiver) fuckable.penetrator.stimReceiver.ForceOrgasm(true);
			}
			// if(showScreenOverlay.val) SyncScreenInfo();
			// orgasmMorphs.ForEach(x => x.target = eyeRoll);
			// lEye.transform.localEulerAngles = new Vector3(eyeRoll, 12f, 0f);
			// rEye.transform.localEulerAngles = new Vector3(eyeRoll, -12f, 0f);
		}

		public static void ForceOrgasm()
		{
			orgasmTimer = orgasmTime.val;
			stimulation.val = 1f;
			stimulationRegression = stimulationRegressionJ.val;
			if(FillMeUp.isFuta && Person.forceFullLoadJ.val) altFutaStim.cumshotHandler.load.val = Person.maxLoad.val;
			DoOrgasm();
		}

		private static void ForceMaleOrgasm(int type)
		{
			bool cumInteracting = type == 1 ? Person.cumInteracting.val : Dildo.cumInteracting.val;
			foreach (var item in FillMeUp.stimReceivers)
			{
				var receiver = item.Value;
				if(receiver.type != type || (cumInteracting && !receiver.isFucking)) continue;
				receiver.ForceOrgasm();
				// if(receiver.penetrator.atom == atom) ForceOrgasm();
			}
		}

		public static void ResetCumClothes()
		{
			foreach (var item in FillMeUp.stimReceivers)
			{
				var receiver = item.Value;
				if(receiver.type != 1) continue;
				((Person)receiver).UnequipClothes();
			}
		} 
		
		private static void ResetOrgasms()
		{
			orgasmCount.val = 0f;
			// orgRecoverTimer = orgasmRecoverTime.val;
			stimulationRegression = stimulationRegressionJ.val;
			// orgasmTimer = orgasmTime.val;
		}

		// private static void SyncScreenInfo()
		// {
		// 	if(screenInfo.dynText != null) screenInfo.dynText.text = $"Total Orgasms: \t{orgasmCount.val}\nMulti Orgasms \t{multiOrgasmCount.val}";
		// 	screenInfo.Run();
		// }

		private static void ResetExpressions()
		{
			foreach(var morph in painMorphs)
			{
				morph.target = 0f;
				morph.morphVal = 0f;
			}
			foreach (var morph in joyMorphs)
			{
				morph.target = 0f;
				morph.morphVal = 0f;
				// $"{morph.dazMorph.morphValue} {morph.dazMorph.jsonFloat.val} {morph.dazMorph.appliedValue}".Print();
			}
			var dcr = atom.gameObject.GetComponentInChildren<DAZCharacterRun>();
			// dcr.ResetMorphs();
		}

		public static void Reset()
		{
			ResetExpressions();
			breathingDriver.Reset();
			if (isOrgasming) ahegao.OnDisable();
			stimulation.val = 0f;
			Gagger.RestoreBlink();
		}

		public static LerpableMorph GetMorph(string uid)
		{
			DAZMorph morph = morphControl.GetMorphByUid(uid);
			if (morph == null) uid.Print();
			// morph.displayName.Print();
			// morph.morphValue.Print();
			return new LerpableMorph(morph);
		}

		public static void PlaySlapMoan(bool isSexSlap = false)
		{
			if(!voiceEnabled || FillMeUp.throat.depth.val > mouthStuffedDepthThreshold.val || headAudioSource.isPlaying) return;
			if (isSexSlap)
			{
				if (sexSlapMoanChance.val > 0f)
				{
					if(Random.Range(0f, 1f) < sexSlapMoanChance.val) PlayRandomMoan(GetMoanLevel());
				}
			}
			else if(slapMoanChance.val > 0f && Random.Range(0f, 1f) <= slapMoanChance.val) PlayRandomMoan(GetMoanLevel());
		}

		private static void PlayRandomMoan(int minLvl)
		{
			if(!voiceEnabled || FillMeUp.throat.depth.val > mouthStuffedDepthThreshold.val) return;
			moanLibrary[rng.Next(minLvl, 5)].Play(headAudioSource);
		}
		
		public static void PlayBurstMoan()
		{
			if(!voiceEnabled || FillMeUp.throat.depth.val > mouthStuffedDepthThreshold.val || headAudioSource.isPlaying || burstMoanChance.val == 0f || stimulation.val < MoanSettings.silenceThreshold.val) return;
			if (Random.Range(0f, 1f) <= burstMoanChance.val)
			{
				moanLibrary[GetMoanLevel()].Play(headAudioSource);
			}
		}
		
		public static bool PlayMoan()
		{
			if(!voiceEnabled || FillMeUp.throat.depth.val > mouthStuffedDepthThreshold.val || headAudioSource.isPlaying || sexMoanChance == 0f  || stimulation.val < MoanSettings.silenceThreshold.val) return false;
			if (Random.Range(0f, 1f) <= sexMoanChance * stimulation.val)
			{
				moanLibrary[GetMoanLevel()].Play(headAudioSource);
				return true;
			}
			return false;
		}

		private static int GetMoanLevel()
		{
			if (isOrgasmPleasure) return 5;
			if(stimulation.val < MoanSettings.thresholds[0].val) return 0;
			for (var i = MoanSettings.thresholds.Count - 1; i > 0; i--)
			{
				if (stimulation.val <= MoanSettings.thresholds[i].val) continue;
				return i+1;
			}
			return 1;
		}
		
		private IEnumerator RetrySyncWithVamMoan()
		{
			yield return new WaitForEndOfFrame();
			for (int i = 0; i < 5; i++)
			{
				yield return new WaitForSeconds(2f);
				var vammoan = this.FindPluginPeer("VAMMoan");
				if (vammoan)
				{
					vamMoanVoice = vammoan.GetStringChooserJSONParam("voice");
					voice.val = vamMoanVoice.val;
					vamMoanVoice.setCallbackFunction += val => voice.val = val;
					yield break;
				}
			}
		}

		// public void AddTrigger()
		// {
		// 	var trigger = gameObject.AddComponent<CustomFloatTrigger>();
		// 	trigger.Init(this, stimulation, $"{stimulation.name}_{stimTriggers.Count}");
		// 	stimTriggers.Add(trigger);
		// 	trigger = gameObject.AddComponent<CustomFloatTrigger>();
		// 	trigger.Init(this, orgasmCount, $"{orgasmCount.name}_{orgasmTriggers.Count}", false, 1f);
		// 	orgasmTriggers.Add(trigger);
		// }

		private void PrintMorphFile()
		{
			FileManagerSecure.CreateDirectory("Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/joy");
			FileManagerSecure.CreateDirectory("Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/displease");
			JSONClass existing = null;
			JSONClass jc = new JSONClass();
			if(FileManagerSecure.FileExists("Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/joy/general.json"))
			{
				jc = LoadJSON("Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/joy/general.json").AsObject;
				// FileManagerSecure.DeleteFile("Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/joy/general.json");
			}
			jc["set"] = "general";
			foreach (var morph in joyMorphs)
			{
				morph.dazMorph.LoadDeltas();
				var vertex = morph.dazMorph.deltas.FirstOrDefault(x => x.vertex == 2138);
				jc[morph.uid]["lipLift"].AsFloat = vertex == null ? 0f : vertex.delta.y;
				vertex = morph.dazMorph.deltas.FirstOrDefault(x => x.vertex == 16899);
				jc[morph.uid]["rLowerLidLift"].AsFloat = vertex == null ? 0f : vertex.delta.y;
				vertex = morph.dazMorph.deltas.FirstOrDefault(x => x.vertex == 6179);
				jc[morph.uid]["lLowerLidLift"].AsFloat = vertex == null ? 0f : vertex.delta.y;
				// if(vertex != null) $"{morph.dazMorph.displayName} {vertex.vertex} {vertex.delta.y}".Print();
				// if(jc.HasKey(morph.uid)) continue;
				jc[morph.uid]["weight"].AsFloat = 1f;
				jc[morph.uid]["max"].AsFloat = morph.uid.Contains("Snarl")? .3f : .1f;
				jc[morph.uid]["exclude"] = new JSONArray();
			}
			SaveJSON(jc, "Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/joy/general.json");
			
			if(FileManagerSecure.FileExists("Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/displease/general.json"))
			{
				jc = LoadJSON("Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/displease/general.json").AsObject;
				// FileManagerSecure.DeleteFile("Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/displease/general.json");
			}
			else jc = new JSONClass();
			jc["set"] = "general";
			foreach (var morph in painMorphs)
			{
				// if(jc.HasKey(morph.uid)) continue;
				morph.dazMorph.LoadDeltas();
				var vertex = morph.dazMorph.deltas.FirstOrDefault(x => x.vertex == 2138);
				jc[morph.uid]["lipLift"].AsFloat = vertex == null ? 0f : vertex.delta.y;
				vertex = morph.dazMorph.deltas.FirstOrDefault(x => x.vertex == 16899);
				jc[morph.uid]["rLowerLidLift"].AsFloat = vertex == null ? 0f : vertex.delta.y;
				vertex = morph.dazMorph.deltas.FirstOrDefault(x => x.vertex == 6179);
				jc[morph.uid]["lLowerLidLift"].AsFloat = vertex == null ? 0f : vertex.delta.y;
				jc[morph.uid]["weight"].AsFloat = 1f;
				jc[morph.uid]["max"].AsFloat = morph.uid.Contains("Snarl")? .3f : .1f;
				jc[morph.uid]["exclude"] = new JSONArray();
			}
			SaveJSON(jc, "Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/displease/general.json");
		}

		private Dictionary<string, JSONClass> expressionSets = new Dictionary<string, JSONClass>();
		private void LoadExpressionSets()
		{
			Func<string, string> GetBaseUid = val => val;
			if (packageUid != "") GetBaseUid = val => val.Replace(packageUid, "");
			var joySets = packageUid + "Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/joy";
			var painSets = packageUid + "Custom/Scripts/CheesyFX/BodyLanguage/ExpressionSets/displease";
			JSONClass jc;
			JSONClass morphJc;
			foreach (var path in FileManagerSecure.GetFiles(joySets))
			{
				var normalizedPath = FileManagerSecure.NormalizePath(path);
				jc = LoadJSON(normalizedPath).AsObject;
				expressionSets[jc["set"]] = jc;
			}
			jc = expressionSets["general"];
			foreach (var morph in joyMorphs)
			{
				// if (morph.dazMorph.hasFormulas)
				// {
				// 	morph.uid.Print();
				// 	morph.dazMorph.formulas.ToList().ForEach(x => x.target.Print());
				// }
				if(!jc.HasKey(GetBaseUid(morph.uid))) continue;
				morphJc = jc[GetBaseUid(morph.uid)].AsObject;
				morph.lipLift = morphJc["lipLift"].AsFloat;
				morph.lLowerLidLift = morphJc["lLowerLidLift"].AsFloat;
				morph.rLowerLidLift = morphJc["rLowerLidLift"].AsFloat;
				morph.max = morphJc["max"].AsFloat;
				morph.weight = morphJc["weight"].AsFloat;
			}
			foreach (var path in FileManagerSecure.GetFiles(painSets))
			{
				var normalizedPath = FileManagerSecure.NormalizePath(path);
				jc = LoadJSON(normalizedPath).AsObject;
				expressionSets[jc["set"]] = jc;
			}
			jc = expressionSets["general"];
			foreach (var morph in painMorphs)
			{
				if(!jc.HasKey(GetBaseUid(morph.uid))) continue;
				morphJc = jc[GetBaseUid(morph.uid)].AsObject;
				morph.lipLift = morphJc["lipLift"].AsFloat;
				morph.lLowerLidLift = morphJc["lLowerLidLift"].AsFloat;
				morph.rLowerLidLift = morphJc["rLowerLidLift"].AsFloat;
				morph.max = morphJc["max"].AsFloat;
				morph.weight = morphJc["weight"].AsFloat;
			}
		}

		public static void SetIdleMode(bool val)
		{
			// var idling = stimulation.val < .05f || !(FillMeUp.orifices.Any(x => x.isPenetrated) || BodyRegionMapping.touchZones["Labia"].touchCollisionListener.isOnStay);
			if (isIdling == val) return;
			isIdling = val;
			if (val)
			{
				LerpableMorph.quicknessIn = 2f;
				LerpableMorph.quicknessOut = .5f;
			}
			else
			{
				LerpableMorph.quicknessIn = 3f;
				LerpableMorph.quicknessOut = .5f;
			}
		}
	}

	public class MoanSettings
	{
		public static JSONStorableFloat silenceThreshold = new JSONStorableFloat("Silence Threshold", 0f, 0f, 1f);
		public static List<JSONStorableFloat> thresholds = new List<JSONStorableFloat>();

		public MoanSettings()
		{
			for (int i = 0; i < 4; i++)
			{
				var setting = new JSONStorableFloat($"Level {i+1} Threshold", .1f + .2f * i, 0f, 1f);
				thresholds.Add(setting);
				// setting.setCallbackFunction += val => SyncRanges();
			}
		}

		// private void SyncRanges()
		// {
		// 	for (var i = 0; i < thresholds.Count-1; i++)
		// 	{
		// 		thresholds[i + 1].min = thresholds[i].val;
		// 		thresholds[i].max = thresholds[i+1].val;
		// 	}
		// }
	}
}