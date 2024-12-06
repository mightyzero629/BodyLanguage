using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using MeshVR;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace CheesyFX
{
    public class FillMeUp : MVRScript
    {
	    public static FillMeUp singleton;
	    public static string packageUid;
	    private static bool isVar;
	    public static GenerateDAZMorphsControlUI morphControl;
	    private UnityEventsListener uiListener;
	    public static Atom atom;
	    // public static Transform neck;
		public static Collider abdomen;
		public static Collider lGlute1_2;
		public static Collider lGlute1_7;
		public static Collider rGlute1_2;
		public static Collider rGlute1_7;
		public static Collider[] physGlute;
		public static Anus anus;
		public static Throat throat;
		public static Vagina vagina;
		public static List<Orifice> orifices;
		public static List<Hand> hands;
		public static List<Orifice> crotch;
		public static Cleavage cleavage;
		public static Fuckable[] fuckables;
		public static Dictionary<Collider, Penetrator> penetratorByCollider = new Dictionary<Collider, Penetrator>();
		public static Dictionary<Collider, Penetrator> penetratorByTip = new Dictionary<Collider, Penetrator>();
		public static Dictionary<Orifice, Atom> penetratingAtoms;
		// public static Dictionary<CapsulePenetrator, MaleShaker> maleShakers = new Dictionary<CapsulePenetrator, MaleShaker>();
		public UIDynamicTabBar regionTabbar;
		public UIDynamicTabBar settingsTabbar;
		public static List<DAZMorph> bellyBulgeMorphs = new List<DAZMorph>();
		public static List<DAZMorph> throatBulgeMorphs = new List<DAZMorph>();
		public static readonly Shader debugShader = Shader.Find("Battlehub/RTGizmos/Handles");
		
		private static JSONStorable VAMMoan;
		public static JSONStorableFloat VAMMoanIntensity;
		public static JSONStorableStringChooser VAMMoanVoice;
		public static JSONStorableAction resetDetection = new JSONStorableAction("Reset", OnPoseSnap);
		public static JSONStorableAction toggleForces = new JSONStorableAction("Toggle Forces", ForceToggle.Toggle);
		public static JSONStorableAction resetPenetrators = new JSONStorableAction("Reset Registered Penetrators", ResetPenetrators);
		
		public static JSONStorableStringChooser forceChooser = new JSONStorableStringChooser("Force", new List<string>(), "", "Force", OnForceChoiceChanged);
		// public static JSONStorableBool allowDeepPenetration = new JSONStorableBool("Allow Deep Penetration", true);
		// public static JSONStorableBool visualizeDepth = new JSONStorableBool("Visualize Depth", false);
		// private static JSONStorableBool debugMode = new JSONStorableBool("debugMode", false);
		
		// List<object> UIElements = new List<object>();
		private static Fuckable currentFuckable;
		private JSONStorableUrl loadURL;
		public JSONClass factoryDefaults;
		// private const string saveDir = "Saves/PluginData/CheesyFX/FillMeUp/";
		public static ClipLibrary squishLibrary;
		public static ClipLibrary bjLibrary;
		public static bool isPenetrated;
		private static bool _debugMode;
		private bool triggersCreated;
		public static IEnumerator waitForPose;

		private DAZCharacterSelector dcs;
		public static DAZMeshEyelidControl eyelidBehavior;
		
		private PresetSystem presetSystem;

		public static SyncedForceGroup orificeForceGroup;
		public static SyncedForceGroup handForceGroup;

		public static bool isFuta;
		public static CharacterListener characterListener;

		private Transform inbuiltClothes;
		public static bool isSavingScene;

		public void OnEnable()
		{
			resourcesUid = GetLatestPackageUid("CheesyFX.BodyLanguage_Resources");
			if (resourcesUid == null)
			{
				SuperController.LogError("BodyLanguage Error: Package 'CheesyFX.BodyLanguage_Resources.latest.var' not found.\nPlease download the package and put it inside the AddonPackages folder.\n" +
				                         "It is recommended to use 'CheesyFX.PluginSuite' as a session plugin to auto download and keep the resources package up to date.");
				abort = true;
			}
		}

		public static bool debugMode
		{
			set
			{
				if (_debugMode == value) return;
				ToggleDepthMeters(value);
				orifices.ForEach(x =>
				{
					x.enterTriggerGO.GetComponent<Renderer>().enabled = value;
					if(x.proximityTrigger != null && x.showProximity.val) x.proximityTrigger.gameObject.GetComponent<Renderer>().enabled = value;
				});
				foreach (var hand in hands)
				{
					hand.triggerGO.GetComponent<Renderer>().enabled = value;
				}
				cleavage.triggerGO.GetComponent<Renderer>().enabled = value;
				_debugMode = value;
			}
			get
			{
				return _debugMode;
			}
		}

		public static string resourcesUid;
		public static bool abort;

		public static List<string> otherUidsWithBL = new List<string>();
		public static JSONStorableFloat instanceId = new JSONStorableFloat("processId", 0f, 0f, 10f);
		public static JSONStorableVector3 levels = new JSONStorableVector3("levels", -Vector3.one, -Vector3.one, 10f*Vector3.one);
		public static JSONStorableVector3 lastToEnter = new JSONStorableVector3("lastToEnter", Vector3.zero, Vector3.zero, 10f*Vector3.one);
		private static IEnumerator connectToMaster;
		public static bool processesSorted;
		
		private IEnumerator ConnectToMaster()
		{
			while (true)
			{
				foreach (var other in otherUidsWithBL.Select(x => SuperController.singleton.GetAtomByUid(x)))
				{
					var plugins = other.transform.Find("reParentObject/object/PluginManager/Plugins");
					if (plugins == null) yield return null;
					var scripts = plugins.GetComponentsInChildren<MVRScript>();
					var fmu = scripts.FirstOrDefault(x => x.name.EndsWith("CheesyFX.FillMeUp"));
					if (!fmu) yield return null;
					var id = fmu.GetFloatJSONParam("processId");
					if (id == null) yield return null;
					if (id.val == 0)
					{
						yield return new WaitUntil(() => (levels = fmu.GetVector3JSONParam("levels")) != null);
						yield return new WaitUntil(() => (lastToEnter = fmu.GetVector3JSONParam("lastToEnter")) != null);
						// $"{other.name} is master".Print();
						// levels.val[0].Print();
						// $"{containingAtom.name}:   {processId.val}".Print();
						processesSorted = true;
						yield break;
					}
				}

				yield return null;
			}
		}

		public static IEnumerator WaitForThreadRace(IEnumerator criticalAction)
		{
			int p = Mathf.RoundToInt(instanceId.val);
			for (int l = 0; l < otherUidsWithBL.Count + 1; l++)
			{
				levels.val = levels.val.SetComponent(p, l);
				lastToEnter.val = lastToEnter.val.SetComponent(l, p);
				// $"{PoseMe.atom.name} {l} {FillMeUp.lastToEnter.val[l]} {FillMeUp.levels.val.MaxExcluding(p)} {FillMeUp.levels.val}".Print();
				while (Mathf.RoundToInt(lastToEnter.val[l]) == p && levels.val.MaxExcluding(p) >= l)
				{
					// $"{l} {FillMeUp.lastToEnter.val[l]} {FillMeUp.levels.val.MaxExcluding(p)}".Print();
					// $"{PoseMe.atom.name} > {stimReceiver.atom.name} waiting".Print();
					// $"{PoseMe.atom.name} {l} {FillMeUp.singleton.lastToEnter.val[l]} {FillMeUp.singleton.levels.val.MaxExcluding(p)} {FillMeUp.singleton.levels.val}".Print();
					yield return null;
				}
			}
			yield return criticalAction;
			levels.val = levels.val.SetComponent(p, -1);
		}
		
		public static IEnumerator WaitForThreadRace(Action criticalAction)
		{
			int p = Mathf.RoundToInt(instanceId.val);
			for (int l = 0; l < otherUidsWithBL.Count + 1; l++)
			{
				levels.val = levels.val.SetComponent(p, l);
				lastToEnter.val = lastToEnter.val.SetComponent(l, p);
				// $"{PoseMe.atom.name} {l} {FillMeUp.lastToEnter.val[l]} {FillMeUp.levels.val.MaxExcluding(p)} {FillMeUp.levels.val}".Print();
				while (Mathf.RoundToInt(lastToEnter.val[l]) == p && levels.val.MaxExcluding(p) >= l)
				{
					// $"{l} {FillMeUp.lastToEnter.val[l]} {FillMeUp.levels.val.MaxExcluding(p)}".Print();
					// $"{PoseMe.atom.name} > {stimReceiver.atom.name} waiting".Print();
					// $"{PoseMe.atom.name} {l} {FillMeUp.singleton.lastToEnter.val[l]} {FillMeUp.singleton.levels.val.MaxExcluding(p)} {FillMeUp.singleton.levels.val}".Print();
					yield return null;
				}
			}
			yield return criticalAction;
			levels.val = levels.val.SetComponent(p, -1);
		}
		
		public override void Init()
		{
			// transform.Print();
			// transform.PrintParents();
			// var scripts = transform.parent.GetComponentsInChildren<MVRScript>();
			// foreach (var script in scripts)
			// {
			// 	script.name.Print();
			// }
			RegisterFloat(instanceId);
			RegisterVector3(levels);
			RegisterVector3(lastToEnter);
			instanceId.isRestorable = instanceId.isStorable = false;
			levels.isRestorable = levels.isStorable = false;
			lastToEnter.isRestorable = lastToEnter.isStorable = false;
			// processId.setCallbackFunction += val => $"{containingAtom.uid} processId changed to {val}".Print();
			// lastToEnter.setCallbackFunction += val => $"lastToEnter changed to {val}".Print();
			// lastToEnter.val = new Vector3(2f, 5f, 6f);
			int k = 0;
			var sceneName = SuperController.singleton.LoadedSceneName;
			if(!string.IsNullOrEmpty(sceneName))
			{
				foreach (var atomJSON in SuperController.singleton.LoadJSON(sceneName)["atoms"].Childs)
				{
					if (atomJSON["type"].Value != "Person") continue;
					var hasBL = atomJSON["storables"].Childs.Any(x => x["id"].Value.EndsWith("CheesyFX.FillMeUp"));
					if (!hasBL) continue;
					if (atomJSON["id"].Value == containingAtom.uid)
					{
						instanceId.val = k;
						break;
					}

					otherUidsWithBL.Add(atomJSON["id"].Value);
					k++;
				}
			}
			else
			{
				foreach (var atom in SuperController.singleton.GetAtoms())
				{
					if(atom == containingAtom || atom.type != "Person") continue;
					var plugins = atom.transform.Find("reParentObject/object/PluginManager/Plugins");
					if(!plugins) continue;
					var scripts = plugins.GetComponentsInChildren<MVRScript>();
					var fmu = scripts.FirstOrDefault(x => x.name.EndsWith("CheesyFX.FillMeUp"));
					if(!fmu) continue;
					instanceId.val++;
					otherUidsWithBL.Add(atom.uid);
				}
			}

			if (instanceId.val > 0) connectToMaster = ConnectToMaster().Start();
			else
			{
				// $"{containingAtom.name}:   {processId.val}".Print();
				processesSorted = true;
			}

			// ResetMorphs();
			if (containingAtom.GetGender() == DAZCharacterSelector.Gender.Male)
			{
				RemoveSelf();
				SuperController.LogError("Please put BodyLanguage on a female atom.");
				abort = true;
				return;
			}

			if (abort)
			{
				RemoveSelf();
				return;
			}

			Gaze.StaticInit();
			singleton = this;
			isVar = Utils.GetPackagePath(this) != string.Empty;
			packageUid = isVar? GetLatestPackageUid("CheesyFX.BodyLanguage_Resources")+":/" : string.Empty;			
			XRay.Init();
			eyelidBehavior = (DAZMeshEyelidControl)containingAtom.GetStorableByID("EyelidControl");
			UIManager.script = this;
			atom = containingAtom;
			BodyRegionMapping.Init(atom);
			dcs = atom.GetStorableByID("geometry") as DAZCharacterSelector;
			morphControl = dcs.morphsControlUI;

			anus = gameObject.AddComponent<Anus>();
			vagina = gameObject.AddComponent<Vagina>();
			throat = gameObject.AddComponent<Throat>();
			orifices = new List<Orifice> { anus, vagina, throat };
			crotch = new List<Orifice> { vagina, anus };
			anus.Init("Anus");
			vagina.Init("Vagina");
			throat.Init("Throat");
			anus.other = vagina;
			vagina.other = anus;

			hands = new List<Hand>
				{ gameObject.AddComponent<Hand>().Init("l"), gameObject.AddComponent<Hand>().Init("r") };
			hands[0].other = hands[1];
			hands[1].other = hands[0];
			cleavage = gameObject.AddComponent<Cleavage>().Init();

			fuckables = new Fuckable[] { anus, vagina, throat, hands[0], hands[1], cleavage };

			orificeForceGroup = new SyncedForceGroup(new List<Force>
			{
				anus.thrustForce, anus.maleForce,
				vagina.thrustForce, vagina.maleForce,
				throat.thrustForce, throat.maleForce,
				cleavage.thrustForce, cleavage.maleForce
			}, new List<int> { 3, 2, 3, 2, 1, 0, 4, 3 });
			orificeForceGroup.modeChooser.val = orificeForceGroup.modeChooser.defaultVal = "Randomized";
			handForceGroup = new SyncedForceGroup(new List<Force>
			{
			hands[0].thrustForce, hands[0].maleForce,
			hands[1].thrustForce, hands[1].maleForce
			}, new List<int>{2,1,2,1});
			
			penetratingAtoms = new Dictionary<Orifice, Atom>
			{
				{throat, null},
				{vagina, null},
				{anus, null}
			};
			// lHand = gameObject.AddComponent<HandJob>();
			
			RegisterAction(resetDetection);
			RegisterAction(toggleForces);
			RegisterAction(resetPenetrators);

			GetBulgeMorphs();
			// string morphDir = packageUid + "Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/";
			// for(int i=1; i<9; i++){
			// 	bellyBulgeMorphs.Add(morphControl.GetMorphByUid(morphDir+"BellyBulge/BL_BellyBulge"+i+".vmi"));
			// }
			// for (int i = 0; i < 3; i++)
			// {
			// 	throatBulgeMorphs.Add(morphControl.GetMorphByUid(morphDir+"ThroatBulge/BL_ThroatBulge"+i+".vmi"));
			// }
			
			// CreateTriggersHandlers().Start();
			CreateOrificeTriggers();
			abdomen = atom.rigidbodies.First(x => x.name == "abdomen").GetComponentsInChildren<Collider>().First(x => x.name == "HardCollider");
			// abdomen.NullCheck();
			// abdomen.name.Print();
			lGlute1_2 = atom.rigidbodies.FirstOrDefault(x => x.name == "AutoColliderFemaleAutoCollidersLGlute1 (2)Joint").GetComponent<Collider>();
			lGlute1_7 = atom.rigidbodies.FirstOrDefault(x => x.name == "AutoColliderFemaleAutoCollidersLGlute1 (7)Joint").GetComponent<Collider>();
			rGlute1_2 = atom.rigidbodies.FirstOrDefault(x => x.name == "AutoColliderFemaleAutoCollidersRGlute1 (2)Joint").GetComponent<Collider>();
			rGlute1_7 = atom.rigidbodies.FirstOrDefault(x => x.name == "AutoColliderFemaleAutoCollidersRGlute1 (7)Joint").GetComponent<Collider>();
			var colliders = atom.GetComponentsInChildren<Collider>(true);
			physGlute = new[]
			{
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointleft glute1"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointleft glute2"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointleft glute3"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointleft glute4"),
				// colliders.First(x => x.name == "PhysicsMeshJointleft glute5"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointleft glute43"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointleft glute51"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointleft glute64"),
				
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute0"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute1"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute2"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute3"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute4"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute5"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute44"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute51"),
				colliders.FirstOrDefault(x => x.name == "PhysicsMeshJointright glute52"),
			};
			// neck.Draw();
			// physGlute.Length.Print();
			// physGlute[1].NullCheck();
			SimpleTriggerHandler.LoadAssets();
			
			squishLibrary = new ClipLibrary("squishes");
			bjLibrary = new ClipLibrary("blowjobs");
			
			// abdomen.detectCollisions = false;
			
			SuperController.singleton.onAtomRemovedHandlers += OnAtomRemove;
			SuperController.singleton.onAtomAddedHandlers += AddPenetrator;
			SuperController.singleton.onAtomUIDRenameHandlers += OnAtomRename;
			SuperController.singleton.onBeforeSceneSaveHandlers += OnBeforeSceneSaved;
			SuperController.singleton.onSceneSavedHandlers += OnSceneSaved;

			var pmc = containingAtom.presetManagerControls.First(x => x.name == "PosePresets");
			var presetManager = pmc.GetComponent<PresetManager>();
			presetManager.postLoadEvent.AddListener(OnPoseSnap);
			presetManager.postLoadEvent.AddListener(ResetHand);
			
			factoryDefaults = Store(false);
			UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements);
			Utils.OnInitUI(CreateUIElement);
			presetSystem = new PresetSystem(this)
			{
				saveDir = "Saves/PluginData/CheesyFX/BodyLanguage/FillMeUp/",
				Store = () => Store(),
				Load = Load
			};
			presetSystem.Init();
			
			foreach (var fuckable in fuckables)
			{
				fuckable.ApplyLatestMatchingForcePresets();
			}

			foreach (var force in ForceToggle.forces)
			{
				RegisterBool(force.enabledJ);
			}

			var gen1Object = containingAtom.GetStorableByID("PhysicsModel").transform.GetAllChildren()
				.FirstOrDefault(x => x.name == "Gen1").gameObject;
			isFuta = gen1Object.activeSelf;
			// futaListener = gen1Object.AddComponent<UnityEventsListener>();
			// futaListener.onEnabled.AddListener(() => SetFuta(true));
			// futaListener.onDisabled.AddListener(() => SetFuta(false));
			//
			// characterListener = new CharacterListener(atom);
			// characterListener.OnChangedToFuta.AddListener(() => SetFuta(true));
			// characterListener.OnChangedToFemale.AddListener(() => SetFuta(false));
			// characterListener.OnChangedToMale.AddListener(RemoveSelf);
			
			inbuiltClothes = containingAtom.transform.Find($"rescale2/geometry/FemaleClothes/FemaleClothingPrefab(Clone)");
			foreach (Transform clothing in inbuiltClothes)
			{
				var dci = clothing.GetComponent<DAZClothingItem>();
				var c = clothing;
				JSONStorableDynamic.OnLoaded action = () => OnInbuiltItemLoaded(c);
				dci.onLoadedHandlers += action;
				onDciLoadedActions[dci] = action;
			}
			CreateUI();
			// containingAtom.onToggleObjects.ToList().ForEach(x => x.name.Print());
			// foreach (Transform t in SuperController.singleton.GetAtomByUid("PlayerNavigationPanel").transform)
			// {
			// 	t.gameObject.layer.Print();
			// }
			//
			// foreach (var ctrl in atom.rigidbodies)
			// {
			// 	if(!ctrl.name.ToLower().Contains("hair")) continue;
			// 	$"{ctrl.name} {ctrl.gameObject.layer} {LayerMask.LayerToName(ctrl.gameObject.layer)}".Print();
			// }

			// pmc = containingAtom.presetManagerControls.First(x => x.name == "AppearancePresets");
			// presetManager = pmc.GetComponent<PresetManager>();
			// // presetManager.postLoadEvent.AddListener(PrintShit);
			// pmc.GetUrlParamNames().ForEach(x => x.Print());
			// pmc.GetActionNames().ForEach(x => x.Print());
			// var col = atom.GetComponentsInChildren<Collider>().FirstOrDefault(x => x.name == "AutoColliderJoint");
			// col.transform.parent.parent.Print();
		}

		// private DAZMorphBank morphBank;
		private DAZMorph testMorph;
		public static UnityEvent onMorphsDeactivated = new UnityEvent();

		private void ReActivateMorphs()
		{
			GetBulgeMorphs();
			testMorph = bellyBulgeMorphs[0];
			anus.GetMorphs();
			onMorphsDeactivated.Invoke();
		}
		
		private void GetBulgeMorphs()
		{
			string morphDir = packageUid + "Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/";
			bellyBulgeMorphs.Clear();
			for(int i=1; i<9; i++){
				bellyBulgeMorphs.Add(morphControl.GetMorphByUid(morphDir+"BellyBulge/BL_BellyBulge"+i+".vmi"));
			}
			throatBulgeMorphs.Clear();
			for (int i = 0; i < 3; i++)
			{
				throatBulgeMorphs.Add(morphControl.GetMorphByUid(morphDir+"ThroatBulge/BL_ThroatBulge"+i+".vmi"));
			}
			testMorph = bellyBulgeMorphs[0];
		}

		private Dictionary<DAZClothingItem, JSONStorableDynamic.OnLoaded> onDciLoadedActions = new Dictionary<DAZClothingItem, JSONStorableDynamic.OnLoaded>();

		private void OnInbuiltItemLoaded(Transform clothing)
		{
			foreach (var collider in clothing.GetComponentsInChildren<Collider>(true))
			{
				Physics.IgnoreCollision(collider, anus.enterTriggerCollider, true);
				Physics.IgnoreCollision(collider, vagina.enterTriggerCollider, true);
				foreach (var fuckable in fuckables)
				{
					Physics.IgnoreCollision(collider, fuckable.proximityTrigger, true);
				}
			}
		}

		public static bool snapping;
		public static Dictionary<Penetrator, StimReceiver> stimReceivers = new Dictionary<Penetrator, StimReceiver>();
		public static List<Person> persons = new List<Person>();
		public static List<Dildo> dildos = new List<Dildo>();
		public static AltFutaStim containingPerson;

		// public void BodyLanguageReceiveXrayStatus(bool val)
		// {
		// 	defereXrayInit = val;
		// 	$"{PoseMe.atom.name} received {val}".Print();
		// }

		public void AddPenetrator(Atom atom)
		{
			try
			{
				CapsuleCollider tip;
				switch (atom.type)
				{
					case "Dildo":
					{
						tip = atom.rigidbodies.First(x => x.name == "b3").transform.Find("_Collider4").GetComponent<CapsuleCollider>();
						// CapsulePenetrator penetrator = gameObject.AddComponent<CapsulePenetrator>().Init(tip);
						CapsulePenetrator penetrator = new CapsulePenetrator(tip);
						penetratorByTip[tip] = penetrator;
						var dildo = gameObject.AddComponent<Dildo>().Init(penetrator);
						stimReceivers[penetrator] = dildo;
						dildos.Add(dildo);
						break;
					}
					case "Person":
					{
						tip = atom.rigidbodies.First(x => x.name == "Gen3").transform
							.Find("AutoColliderGen3b/AutoColliderGen3bHard").GetComponent<CapsuleCollider>();
						if (tip == null) return;
						// CapsulePenetrator penetrator = gameObject.AddComponent<CapsulePenetrator>().Init(tip);
						CapsulePenetrator penetrator = new CapsulePenetrator(tip);
						penetratorByTip[tip] = penetrator;
						if (atom == containingAtom)
						{
							containingPerson = gameObject.AddComponent<AltFutaStim>().Init(penetrator);
							stimReceivers[penetrator] = containingPerson;
							ReadMyLips.altFutaStim = containingPerson;
							persons.Add(containingPerson);
							characterListener = containingPerson.characterListener;
							characterListener.OnChangedToFuta.AddListener(() => SetFuta(true));
							characterListener.OnChangedToFemale.AddListener(() => SetFuta(false));
							characterListener.OnChangedToMale.AddListener(RemoveSelf);
						}
						else
						{
							var person = gameObject.AddComponent<Person>().Init(penetrator);
							stimReceivers[penetrator] = person;
							persons.Add(person);
						}
						break;
					}
				}

				if (ReadMyLips.lastTabId >= 6 && ReadMyLips.lastMaleTabId == 0) ReadMyLips.singleton.UISelectMaleTab(0, ReadMyLips.lastTabId - 5);
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());
			}
		}

		public static void ResetPenetrators()
		{
			penetratorByTip.Clear();
			penetratorByCollider.Clear();
			for (int i = 0; i < persons.Count; i++)
			{
				var penetrator = persons[i].penetrator;
				penetrator.colliders.ForEach(x => penetratorByCollider[x] = penetrator);
				penetratorByTip[penetrator.tipCollider] = penetrator;
			}
			for (int i = 0; i < dildos.Count; i++)
			{
				var penetrator = dildos[i].penetrator;
				penetrator.colliders.ForEach(x => penetratorByCollider[x] = penetrator);
				penetratorByTip[penetrator.tipCollider] = penetrator;
			}
		}

		private void SetFuta(bool val)
		{
			isFuta = val;
			if(gameObject.GetComponent<AltFutaStim>() == null) AddPenetrator(atom);
			if (!val)
			{
				foreach (var uid in morphControl.GetMorphUids().Where(x => x.EndsWith("AltFuta Vagina Hide.vmi")))
				{
					morphControl.GetMorphByUid(uid).morphValue = 0f;
				}
			}
		}

		public static void OnForceChoiceChanged(string val)
		{
			int id = forceChooser.choices.IndexOf(val);
			Fuckable fuckable = null;
			Force force = null;
			if (id == 0)
			{
				fuckable = anus;
				force = anus.thrustForce;
			}
			else if (id == 1)
			{
				fuckable = anus;
				force = anus.maleForce;
			}
			else if (id == 2)
			{
				fuckable = vagina;
				force = vagina.thrustForce;
			}
			else if (id == 3)
			{
				fuckable = vagina;
				force = vagina.maleForce;
			}
			else if (id == 4)
			{
				fuckable = throat;
				force = throat.thrustForce;
			}
			else if (id == 5)
			{
				fuckable = throat;
				force = throat.maleForce;
			}
			else if (id == 6)
			{
				fuckable = hands[0];
				force = hands[0].thrustForce;
			}
			else if (id == 7)
			{
				fuckable = hands[0];
				force = hands[0].maleForce;
			}
			else if (id == 8)
			{
				fuckable = hands[1];
				force = hands[1].thrustForce;
			}
			else if (id == 9)
			{
				fuckable = hands[1];
				force = hands[1].maleForce;
			}
			else if (id == 10)
			{
				fuckable = cleavage;
				force = cleavage.thrustForce;
			}
			else if (id == 11)
			{
				fuckable = cleavage;
				force = cleavage.maleForce;
			}

			int menuId = (int)(.5f * id);
			currentFuckable = fuckable;
			lastMenuId = menuId;
			fuckable.CreateForceUINewPage(force);
		}
		
		public static void OnAtomToggled(bool val)
		{
			try
			{
				if (val) return;
				// "OnAtomToggled".Print();
				var thisPenetrators = penetratorByTip.Values.Where(x => x.atom.on == false);
				foreach (var penetrator in thisPenetrators)
				{
					if(penetrator.stimReceiver != null) penetrator.stimReceiver.cumshotHandler.Stop();
				}

				foreach (var orifice in orifices)
				{
					if (orifice.penetrator != null && !orifice.penetrator.atom.on)
					{
						orifice.ResetPenetration();
					}
					if (orifice.magnet.penetrator != null && !orifice.magnet.penetrator.atom.on)
					{
						orifice.proximityHandler.Reset();
					}
				}

				// foreach (var a in SuperController.singleton.GetAtoms())
				// {
				// 	if(!a.on) a.name.Print();
				// }
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());
			}
		}

		public static void OnPoseSnap()
		{
			try
			{
				// if(Pose.isApplying) return;
				// "OnPoseSnap".Print();
				snapping = true;
				waitForPose.Stop();
				waitForPose = null;
				ReadMyLips.ResetCumClothes();
				foreach (var orifice in orifices)
				{
					// orifice.ResetPenetration();
					if(!orifice.isPenetrated) orifice.StopForcesImmediate();
					else DeferredOrificeForceDisable(orifice).Start();
				}
				cleavage.enabled = false;
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());
			}
		}

		private void ResetHand()
		{
			if(!PoseMe.applyingHandjobPoseLeft) hands[0].Reset().Start();
			if (!PoseMe.applyingHandjobPoseRight) hands[1].Reset().Start();
		}

		private static IEnumerator DeferredOrificeForceDisable(Orifice orifice)
		{
			yield return new WaitForFixedUpdate();
			if(!orifice.isPenetrated) orifice.StopForcesImmediate();
		}

		public static void RegisterSnapCollision()
		{
			if (waitForPose == null) waitForPose = WaitForPose().Start();
		}

		private static IEnumerator WaitForPose()
		{
			yield return new WaitForSeconds(1f);
			anus.isPenetrated = false;
			vagina.isPenetrated = false;
			snapping = false;
			
			if (anus.GetPenetrator(anus.lastColliding) != vagina.GetPenetrator(vagina.lastColliding))
			{
				anus.RegisterCollision(anus.lastColliding);
				vagina.RegisterCollision(vagina.lastColliding);
				waitForPose = null;
				yield break;
			}
			var vagDist = vagina.GetRadialDistance();
			if(anus.GetRadialDistance() < vagDist) anus.RegisterCollision(anus.lastColliding);
			else if(vagDist < 100f) vagina.RegisterCollision(vagina.lastColliding);
			waitForPose = null;
		}

		// private void ResetOrifices()
		// {
		// 	orifices.ForEach(x =>
		// 	{
		// 		x.isPenetrated = false;
		// 	});
		// }

		public override void InitUI()
		{
			base.InitUI();
			if(UITransform == null || uiListener != null) return;
			uiListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
			uiListener.onEnabled.AddListener(() => UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements));
			uiListener.onEnabled.AddListener(() => Utils.OnInitUI(CreateUIElement));
			enabledJSON.toggle.interactable = false;
		}

		private void ResetMorphs()
		{
			var meshes = containingAtom.GetComponentsInChildren<DAZMesh>();
			foreach (var mesh in meshes)
			{
				if(mesh.morphBank == null) continue;
				foreach (var morph in mesh.morphBank.morphs)
				{
					if (morph.uid.Contains("BL_"))
					{
						morph.morphValue = 0f;
					}
				}
			}
		}

		private static void ToggleDepthMeters(bool b)
		{
			foreach (var orifice in orifices)
			{
				orifice.depthMeterRenderer.enabled = b && orifice.isPenetrated;
			}
		}

		// void CollisionToggleCallback(bool b)
		// {
		// 	"CollisionToggleCallback".Print();
		// 	if (b) CheckCollisionOnInit().Start();
		// }
		
		private void CreateOrificeTriggers()
		{
			foreach (var orifice in orifices)
			{
				orifice.entrance = new GameObject($"{orifice.name} Entrance");
				GameObject enterTriggerGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				// enterTriggerGO.SetActive(false);
				enterTriggerGO.transform.SetParent(orifice.entrance.transform, false);
				orifice.enterTriggerGO = enterTriggerGO;
				enterTriggerGO.name = $"{orifice.name} EnterTrigger";
				orifice.audioSource = enterTriggerGO.AddComponent<AudioSource>();
				orifice.audioSource.spatialBlend = 1f;
				orifice.audioSource.spatialize = true;
				orifice.enterPointTF = enterTriggerGO.transform;
				orifice.enterPointTF.localScale = new Vector3(.015f,.015f,.015f);
				
				orifice.enterTriggerCollider = enterTriggerGO.GetComponent<Collider>();
				
				orifice.enterTriggerCollider.contactOffset = 1e-9f;
				orifice.enterTriggerCollider.isTrigger = true;
				
				orifice.orificeTriggerHandler = enterTriggerGO.AddComponent<OrificeTriggerHandler>();
				orifice.orificeTriggerHandler.orifice = orifice;
				Renderer renderer = enterTriggerGO.GetComponent<Renderer>();
				var material = renderer.material;
				material.color = Color.yellow;
				material.shader = debugShader;
				material.SetFloat("_Offset", .5f);
				material.SetFloat("_MinAlpha", .1f);
				renderer.enabled = false;

				var RBs = atom.rigidbodies;
				Transform refTransform;
				if (orifice == anus)
				{
					refTransform = RBs.First(x => x.name == "_JointAr").transform;
					orifice.entrance.transform.position = refTransform.position;
					
					orifice.entrance.transform.parent = RBs.First(x => x.name == "pelvis").transform;
					Vector3 localPosition = orifice.entrance.transform.localPosition;
					orifice.entrance.transform.rotation = refTransform.rotation;
					orifice.entrance.transform.localPosition = new Vector3(0f, localPosition.y + .01f, localPosition.z);
					// orifice.entrance.transform.Draw();
				}
				else if (orifice == vagina)
				{
					refTransform = RBs.First(x => x.name == "LabiaTrigger").transform;
					orifice.entrance.transform.position = refTransform.position - refTransform.up * .013f;
					orifice.entrance.transform.parent = RBs.First(x => x.name == "pelvis").transform;
					orifice.entrance.transform.rotation = refTransform.rotation;
					orifice.entrance.transform.position += refTransform.up * .005f;
					orifice.entrance.transform.position += refTransform.forward * .01f;
					// orifice.enterPointTF.localScale = new Vector3(.015f,.015f,.015f);
					
				}
				else if (orifice == throat)
				{
					orifice.enterPointTF.localScale *= 1.5f;
					refTransform = RBs.First(x => x.name == "LipTrigger").transform;
					orifice.entrance.transform.position = refTransform.position  - refTransform.up *.01f;
					
					orifice.entrance.transform.parent = RBs.First(x => x.name == "head").transform;
					orifice.entrance.transform.rotation = refTransform.rotation;
					orifice.entrance.transform.Rotate(-75f, 0f, 0f);
					BJHelper.throatTrigger = orifice.entrance.transform;
				}

				orifice.enterTriggerBasePosition = orifice.entrance.transform.localPosition;
				orifice.SetupProximityTrigger();
				orifice.CreateParticleSystem();
			}
			gameObject.AddComponent<BJHelper>();
			foreach (var col in atom.GetComponentsInChildren<Collider>(true))
			{
				foreach (var orifice in orifices)
				{
					Physics.IgnoreCollision(orifice.enterTriggerCollider, col);
				}
			}
			foreach (var carpal in atom.rigidbodies.Where(x => x.name.Contains("Carpal")))
			{
				foreach (var col in carpal.GetComponentsInChildren<Collider>())
				{
					if(col.attachedRigidbody == carpal) continue;
					// col.transform.Draw();
					foreach (var orifice in orifices)
					{
						Physics.IgnoreCollision(orifice.enterTriggerCollider, col, false);
					}
				}
			}
			foreach (var thumb in atom.rigidbodies.Where(x => x.name.Contains("Thumb")))
			{
				foreach (var col in thumb.GetComponentsInChildren<Collider>())
				{
					foreach (var orifice in orifices)
					{
						Physics.IgnoreCollision(orifice.enterTriggerCollider, col, false);
					}
				}
			}
			foreach (var col in atom.rigidbodies.First(x => x.name == "Gen1").GetComponentsInChildren<Collider>(true))
			{
				Physics.IgnoreCollision(throat.enterTriggerCollider, col, false);
			}

			triggersCreated = true;
			OnPoseSnap();
		}

		IEnumerator CreateTriggersHandlers()
		{
			// yield return new WaitForFixedUpdate();
			yield return new WaitForSeconds(.5f);
			foreach (var orifice in orifices)
			{
				orifice.entrance = new GameObject();
				GameObject enterTriggerGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				enterTriggerGO.transform.SetParent(orifice.entrance.transform, false);
				orifice.enterTriggerGO = enterTriggerGO;
				enterTriggerGO.tag = "enterTrigger";
				orifice.audioSource = enterTriggerGO.AddComponent<AudioSource>();
				orifice.audioSource.spatialBlend = 1f;
				orifice.audioSource.spatialize = true;
				orifice.enterPointTF = enterTriggerGO.transform;
				orifice.enterPointTF.localScale = new Vector3(.015f,.015f,.015f);
				
				orifice.enterTriggerCollider = enterTriggerGO.GetComponent<Collider>();
				orifice.enterTriggerCollider.contactOffset = 1e-9f;
				orifice.enterTriggerCollider.isTrigger = true;
				
				orifice.orificeTriggerHandler = enterTriggerGO.AddComponent<OrificeTriggerHandler>();
				orifice.orificeTriggerHandler.orifice = orifice;
				Renderer renderer = enterTriggerGO.GetComponent<Renderer>();
				var material = renderer.material;
				material.color = Color.yellow;
				material.shader = debugShader;
				material.SetFloat("_Offset", .5f);
				material.SetFloat("_MinAlpha", .1f);
				renderer.enabled = false;

				var RBs = atom.rigidbodies;
				Transform refTransform;
				if (orifice == anus)
				{
					refTransform = RBs.First(x => x.name == "_JointAr").transform;
					orifice.entrance.transform.position = refTransform.position;
					
					orifice.entrance.transform.parent = RBs.First(x => x.name == "pelvis").transform;
					Vector3 localPosition = orifice.entrance.transform.localPosition;
					orifice.entrance.transform.rotation = refTransform.rotation;
					orifice.entrance.transform.localPosition = new Vector3(0f, localPosition.y + .01f, localPosition.z);

				}
				else if (orifice == vagina)
				{
					refTransform = RBs.First(x => x.name == "LabiaTrigger").transform;
					orifice.entrance.transform.position = refTransform.position - refTransform.up * .013f;// + refTransform.forward*.01f;
					orifice.entrance.transform.parent = RBs.First(x => x.name == "pelvis").transform;
					orifice.entrance.transform.rotation = refTransform.rotation;
					orifice.entrance.transform.position += refTransform.up * .005f;
					orifice.entrance.transform.position += refTransform.forward * .01f;
					// orifice.enterPointTF.localScale = new Vector3(.015f,.015f,.015f);
					
				}
				else if (orifice == throat)
				{
					refTransform = RBs.First(x => x.name == "LipTrigger").transform;
					orifice.entrance.transform.position = refTransform.position  - refTransform.up *.01f;
					orifice.entrance.transform.parent = RBs.First(x => x.name == "head").transform;
					orifice.entrance.transform.rotation = refTransform.rotation;
					orifice.entrance.transform.Rotate(-75f, 0f, 0f);
					BJHelper.throatTrigger = orifice.entrance.transform;
				}

				orifice.enterTriggerBasePosition = orifice.entrance.transform.localPosition;
				orifice.SetupProximityTrigger();
				gameObject.AddComponent<BJHelper>();
			}
			foreach (var col in atom.GetComponentsInChildren<Collider>())
			{
				foreach (var orifice in orifices)
				{
					Physics.IgnoreCollision(orifice.enterTriggerCollider, col);
				}
			}

			foreach (var hand in atom.rigidbodies.Where(x => x.name.Contains("Hand")))
			{
				foreach (var col in hand.GetComponentsInChildren<Collider>())
				{
					foreach (var orifice in orifices)
					{
						Physics.IgnoreCollision(orifice.enterTriggerCollider, col, false);
					}
				}
			}

			triggersCreated = true;
			presetSystem.ApplyLatestMatchingPreset();
			OnPoseSnap();
		}

		public void Update()
		{
			try
			{
				if (Input.GetKey(KeyCode.LeftControl))
				{
					if(Input.GetKeyDown(KeyCode.F)) ForceToggle.Toggle();
					// if (Input.GetKeyDown(KeyCode.X));
				}
				if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.X)) XRay.Toggle();
				if(isVar && !testMorph.isDemandActivated) ReActivateMorphs();
				orificeForceGroup.Update();
				handForceGroup.Update();
				UpdateBulge();
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());
			}
		}

		public static void UpdateBulge()
		{
			if (anus.isPenetrated && anus.bulgeScale.val > 0f)
			{
				for (var i = 0; i < bellyBulgeMorphs.Count; i++)
				{
					bellyBulgeMorphs[i].morphValue = anus.Bulge(i);
					if (i == 0) bellyBulgeMorphs[i].morphValue *= .75f;
				}
				if (vagina.isPenetrated && vagina.bulgeScale.val > 0f)
				{
					for (var i = 0; i < bellyBulgeMorphs.Count; i++)
						bellyBulgeMorphs[i].morphValue += vagina.Bulge(i) * .5f;
				}
			}
			else if (vagina.isPenetrated && vagina.bulgeScale.val > 0f)
			{
				for (var i = 0; i < bellyBulgeMorphs.Count; i++)
				{
					bellyBulgeMorphs[i].morphValue = vagina.Bulge(i);
				}
			}

			if (throat.isPenetrated)
			{
				for (var i = 0; i < throatBulgeMorphs.Count; i++)
				{
					throatBulgeMorphs[i].morphValue = throat.Bulge(i);
				}
			}
		}

		public static void PrintPenetrators()
		{
			var penetrators = penetratorByTip.Values.ToList();
			foreach (var penetrator in penetrators)
			{
				"-------------".Print();
				$"Atom: {penetrator.atom.name}".Print();
				if (penetrator.tipCollider.attachedRigidbody == null)
				{
					"Female (no AltFuta plugin)".Print();
				}
				else
				{
					$"Tip: {penetrator.tipCollider.attachedRigidbody.name}/{penetrator.tipCollider.name}".Print();
					"Additional colliders:".Print();
					foreach (var collider in penetrator.colliders)
					{
						var rb = collider.attachedRigidbody;
						var parent = rb != null ? rb.name : "";
						$"{parent}/{collider.name}".Print();
					}
				}
			}
		}

		public void RemoveSelf()
		{
			var id = name.Substring(0, name.IndexOf('_'));
			containingAtom.GetComponentInChildren<MVRPluginManager>().RemovePluginWithUID(id);
		}

		public void OnAtomRemove(Atom atom)
		{
			try
			{
				foreach (var orifice in orifices)
				{
					if (atom == penetratingAtoms[orifice])
					{
						orifice.CreateDepthMeter();
						orifice.orificeTriggerHandler.Reset();
						orifice.proximityHandler.Reset();
						penetratingAtoms[orifice] = null;
					}
				}
			
				// for (int i = 0; i < 2; i++)
				// {
				// 	var hand = hands[i];
				// 	if (hand.penetrator != null && hand.penetrator.atom == atom)
				// 	{
				// 		hand.triggerGO.GetComponent<HandJobTrigger>().Reset();
				// 	}
				// }

				var thisPenetrators = penetratorByTip.Values.Where(x => x.atom == atom).ToArray();
				foreach (var penetrator in thisPenetrators)
				{
					var capsulePenetrator = penetrator as CapsulePenetrator;
					
					if(capsulePenetrator != null && capsulePenetrator.fuckable) capsulePenetrator.fuckable.ResetPenetration();
					foreach (var orifice in orifices)
					{
						if (orifice.magnet.penetrator == penetrator)
						{
							orifice.proximityHandler.Reset();
						}
					}
					penetratorByTip.Remove(penetrator.tipCollider);
					foreach (var col in penetrator.colliders)
					{
						penetratorByCollider.Remove(col);
					}
				}

				var receiver = stimReceivers.Values.FirstOrDefault(x => x.penetrator.atom == atom);
				if(receiver != null)
				{
					ReadMyLips.singleton.DeregisterAction(receiver.orgasm);
					stimReceivers.Remove(receiver.penetrator);
					var person = receiver as Person;
					if (!ReferenceEquals(person, null))
					{
						persons.Remove(person);
						Destroy(person);
					}
					else
					{
						var dildo = receiver as Dildo;
						if (!ReferenceEquals(dildo, null)) dildos.Remove(dildo);
					}
				}
				if (ReadMyLips.lastTabId >= 6 && ReadMyLips.lastMaleTabId == 0) ReadMyLips.singleton.UISelectMaleTab(0, ReadMyLips.lastTabId - 5);
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());
				throw;
			}
		}
		
		private void OnAtomRename(string oldid, string newid)
		{
			anus.OnAtomRename();
			vagina.OnAtomRename();
			throat.OnAtomRename();
		}

		private void OnBeforeSceneSaved()
		{
			// foreach (var receiver in stimReceivers.Values.Where(x => x.type ==1))
			foreach (var person in persons)
			{
				person.Reset();
			}
			bellyBulgeMorphs.ForEach(x => x.morphValue = 0f);
			throatBulgeMorphs.ForEach(x => x.morphValue = 0f);
			ReadMyLips.Reset();
			isSavingScene = true;
		}

		private void OnSceneSaved()
		{
			if(Person.foreskiEnabled.val)
			{
				foreach (var person in persons)
				{
					person.ApplyForeskiBase();
				}
			}

			isSavingScene = false;
		}

		public static void ForeskiSetActive(bool val)
		{
			foreach (var person in persons)
			{
				person.ForeskiSetActive(val);
			}
		}

		private void OnDisable()
		{
			orifices.ForEach(x =>
			{
				x.enabled = false;
				x.DoReset();
			});
			bellyBulgeMorphs.ForEach(x => x.morphValue = 0f);
			throatBulgeMorphs.ForEach(x => x.morphValue = 0f);
			foreach (var person in persons)
			{
				person.Reset();
			}
		}

		public void OnDestroy()
		{
			Debug.Clear();
			SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemove;
			SuperController.singleton.onAtomAddedHandlers -= AddPenetrator;
			SuperController.singleton.onAtomUIDRenameHandlers -= OnAtomRename;
			SuperController.singleton.onBeforeSceneSaveHandlers -= OnBeforeSceneSaved;
			SuperController.singleton.onSceneSavedHandlers -= OnSceneSaved;
			Destroy(uiListener);
			characterListener.Destroy();
			var pmc = atom.presetManagerControls.First(x => x.name == "PosePresets");
			var pm = pmc.GetComponent<PresetManager>();
			pm.postLoadEvent.RemoveListener(OnPoseSnap);
			pm.postLoadEvent.RemoveListener(ResetHand);
			bellyBulgeMorphs.ForEach(x => x.morphValue = 0f);
			throatBulgeMorphs.ForEach(x => x.morphValue = 0f);
			foreach (Transform clothing in inbuiltClothes)
			{
				var dci = clothing.GetComponent<DAZClothingItem>();
				var c = clothing;
				dci.onLoadedHandlers -= onDciLoadedActions[dci];
			}

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				a.GetBoolJSONParam("on").setCallbackFunction -= OnAtomToggled;
			}

			foreach (var item in penetratorByTip)
			{
				Destroy(item.Value.tip.gameObject);
				
			}
			XRay.Destroy();
			connectToMaster.Stop();
		}

		private static int lastMenuId;
		public void CreateUI()
		{
			UIManager.SetScript(this, CreateUIElement, leftUIElements, rightUIElements);
			Utils.OnInitUI(CreateUIElement);
			presetSystem.CreateUI();
			// var twinButton = Utils.SetupTwinButton(this,
			// 	"Save preset", UISaveJSONDialog,
			// 	"Load preset", delegate {}, false);
			// loadURL.RegisterFileBrowseButton(twinButton.buttonRight);
			// Utils.SetupTwinButton(this,
			// 	"UserDefaults", () => ApplyFromUrl(saveDir+"UserDefaults.json"),
			// 	"FactoryDefaults", () => Load(factoryDefaults), true);
			regionTabbar = UIManager.CreateTabBar(new [] {"Anus", "Vagina", "Throat", "lHand", "rHand", "Cleavage"}, UISelectFuckable, script:this);
			settingsTabbar = UIManager.CreateTabBar(new [] {"Triggers & Info", "Settings", "XRay", "Bulge", "Forces", "Debug"}, UISelectSettings, script:this);
			regionTabbar.SelectTab(lastMenuId);
		}
		
		public static void SetPenetrated()
		{
			isPenetrated = orifices.Any(x => x.isPenetrated);
		}
		
		private void UISelectFuckable(int menuItem)
		{
			if(currentFuckable != null) currentFuckable.ClearUI();
			lastMenuId = menuItem;
			if(menuItem < 3) currentFuckable = orifices[menuItem];
			else if (menuItem < 5)
			{
				currentFuckable = hands[menuItem - 3];
			}
			else currentFuckable = cleavage;
			settingsTabbar.SelectLast();
		}

		public void UISelectSettings(int id)
		{
			currentFuckable.SelectTab(id);
		}

		private JSONClass Store(bool storeTriggers = true)
		{
			var jc = new JSONClass
			{
				[anus.name] = anus.Store(subScenePrefix, storeTriggers),
				[vagina.name] = vagina.Store(subScenePrefix, storeTriggers),
				[throat.name] = throat.Store(subScenePrefix, storeTriggers),
				[hands[0].name] = hands[0].Store(subScenePrefix, storeTriggers),
				[hands[1].name] = hands[1].Store(subScenePrefix, storeTriggers),
				[cleavage.name] = cleavage.Store(subScenePrefix, storeTriggers)
			};
			jc["OrificeForceGroup"] = orificeForceGroup.Store();
			jc["HandForceGroup"] = handForceGroup.Store();
			XRay.enabled.Store(jc, false);
			return jc;
		}

		private JSONClass StoreHandJobs()
		{
			var jc = new JSONClass();
			foreach (var hand in hands)
			{
				jc[hand.name] = hand.thrustForce.Store();
			}

			return jc;
		}

		public void Load(JSONClass jc)
		{
			anus.Load(jc, subScenePrefix);
			vagina.Load(jc, subScenePrefix);
			throat.Load(jc, subScenePrefix);
			hands.ForEach(x => x.Load(jc, subScenePrefix));
			cleavage.Load(jc, subScenePrefix);
			if (anus.bulgeScale.val == 0f || vagina.bulgeScale.val == 0f)
			{
				for (var i = 0; i < bellyBulgeMorphs.Count; i++)
				{
					bellyBulgeMorphs[i].morphValue = 0f;
				}
			}
			if (throat.bulgeScale.val == 0f)
			{
				for (var i = 0; i < throatBulgeMorphs.Count; i++)
				{
					throatBulgeMorphs[i].morphValue = 0f;
				}
			}
			if(jc.HasKey("OrificeForceGroup")) orificeForceGroup.Load(jc["OrificeForceGroup"].AsObject);
			if(jc.HasKey("HandForceGroup")) handForceGroup.Load(jc["HandForceGroup"].AsObject);
			XRay.enabled.Load(jc, false);
			if (!SuperController.singleton.IsMonitorOnly) XRay.enabled.val = false;
		}
		
		private void LoadHandJobs(JSONClass jc)
		{
			foreach (var hand in hands)
			{
				hand.thrustForce.Load(jc[hand.name].AsObject);
			}
		}

		public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
		{
			StartCoroutine(DeferredLateRestoreFromJSON(jc));
		}

		private IEnumerator DeferredLateRestoreFromJSON(JSONClass jc)
		{
			while (!triggersCreated) yield return new WaitForSeconds(.55f);
			base.LateRestoreFromJSON(jc);
			if (jc.HasKey("FillMeUp"))
			{
				var jc1 = jc["FillMeUp"].AsObject;
				Load(jc1);
				if(jc1.HasKey("ForceToggle")) ForceToggle.Load(jc1["ForceToggle"].AsObject);
			}
			else presetSystem.ApplyLatestMatchingPreset();
		}
		
		public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
		{
			JSONClass jc = base.GetJSON(includePhysical, includeAppearance, true);
			jc["FillMeUp"] = Store();
			jc["FillMeUp"]["ForceToggle"] = ForceToggle.Store();
			return jc;
		}
		
		public void RemoveUIElements(List<object> UIElements)
		{
			for (int i=0; i<UIElements.Count; ++i)
			{
				if(UIElements[i] == null) continue;
				if (UIElements[i] is JSONStorableParam)
				{
					JSONStorableParam jsp = UIElements[i] as JSONStorableParam;
					if (jsp is JSONStorableFloat)
						RemoveSlider(jsp as JSONStorableFloat);
					else if (jsp is JSONStorableBool)
						RemoveToggle(jsp as JSONStorableBool);
					else if (jsp is JSONStorableColor)
						RemoveColorPicker(jsp as JSONStorableColor);
					else if (jsp is JSONStorableString)
						RemoveTextField(jsp as JSONStorableString);
					else if (jsp is JSONStorableStringChooser)
					{
						// Workaround for VaM not cleaning its panels properly.
						JSONStorableStringChooser jssc = jsp as JSONStorableStringChooser;
						RectTransform popupPanel = jssc.popup?.popupPanel;
						RemovePopup(jssc);
						if (popupPanel != null)
							UnityEngine.Object.Destroy(popupPanel.gameObject);
					}
				}
				else if (UIElements[i] is UIDynamic)
				{
					UIDynamic uid = UIElements[i] as UIDynamic;
					if (uid is UIDynamicButton)
						RemoveButton(uid as UIDynamicButton);
                    else if (uid is UIDynamicSlider)
						RemoveSlider(uid as UIDynamicSlider);
					else if (uid is UIDynamicToggle)
						RemoveToggle(uid as UIDynamicToggle);
					else if (uid is UIDynamicColorPicker)
						RemoveColorPicker(uid as UIDynamicColorPicker);
					else if (uid is UIDynamicTextField)
						RemoveTextField(uid as UIDynamicTextField);
					else if (uid is UIDynamicPopup)
					{
						// Workaround for VaM not cleaning its panels properly.
						UIDynamicPopup uidp = uid as UIDynamicPopup;
						RectTransform popupPanel = uidp.popup?.popupPanel;
						RemovePopup(uidp);
						if (popupPanel != null)
							UnityEngine.Object.Destroy(popupPanel.gameObject);
					}
                    else if (uid is UIDynamicV3Slider)
                    {
                        var v3Slider = uid as UIDynamicV3Slider;
                        leftUIElements.Remove(v3Slider.transform);
                        rightUIElements.Remove(v3Slider.spacer.transform);
                        Destroy(v3Slider.spacer.gameObject);
                        Destroy(v3Slider.gameObject);
                    }
					else if (uid is UIDynamicV4Slider)
					{
						var v3Slider = uid as UIDynamicV4Slider;
						leftUIElements.Remove(v3Slider.transform);
						rightUIElements.Remove(v3Slider.spacer.transform);
						Destroy(v3Slider.spacer.gameObject);
						Destroy(v3Slider.gameObject);
					}
                    else if (uid is UIDynamicTabBar)
                    {
	                    var tabbar = uid as UIDynamicTabBar;
                        leftUIElements.Remove(tabbar.transform);
                        rightUIElements.Remove(tabbar.spacer.transform);
                        Destroy(tabbar.spacer.gameObject);
                        Destroy(tabbar.gameObject);
                    }
                    else if(uid is UIDynamicToggleArray)
					{
						// RemoveSpacer(uid);
						if (uid == null) return;
						this.rightUIElements.Remove(uid.transform);
						this.leftUIElements.Remove(uid.transform);
						DestroyImmediate(uid.gameObject);
					}
					else RemoveSpacer(uid);
				}
			}

			UIElements.Clear();
		}

		public void ClearUI()
		{
			RemoveUIElements(leftUIElements.Select(x => (object) x.GetComponent<UIDynamic>()).ToList());
			RemoveUIElements(rightUIElements.Select(x => (object) x.GetComponent<UIDynamic>()).ToList());
		}
		
		public static string GetLatestPackageUid(string packageUid){
			int version = FileManagerSecure.GetPackageVersion(packageUid+".latest");
			if (version == -1) return null;
			return $"{packageUid}.{version}";
		}
    }
}