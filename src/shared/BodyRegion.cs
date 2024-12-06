using System.Collections.Generic;
using System.Linq;
using MacGruber;

namespace CheesyFX
{
    public class BodyRegion
    {
        public string name;
		public List<BodyRegion> children = new List<BodyRegion>();
		public List<BodyRegion> parents = new List<BodyRegion>();
		public BodyRegion topParent;
		private BodyRegion _parent;
		public BodyRegion parent{
			get{return _parent;}
			set{_parent = value;
				value.children.Add(this);
				value.children.AddRange(this.children);
			}
		}
		public JSONStorableBool enabled = new JSONStorableBool("enabled", true);
		public JSONStorableBool isSelfColliding = new JSONStorableBool("selfCollision", false);

		public JSONStorableBool onLookEnabled = new JSONStorableBool("Look Reaction Enabled", true);
		public JSONStorableBool touchEnabled = new JSONStorableBool("Touch Reaction Enabled", true);
		public JSONStorableBool slapEnabled = new JSONStorableBool("Slap Enabled", true);
		public JSONStorableBool slapVocalReactionEnabled = new JSONStorableBool("Slap Vocal Reaction", true);
		public JSONStorableFloat slapSensitivity = new JSONStorableFloat("Slap Sensitivity", 1f,0f,5f);
		public JSONStorableFloat slapVolumeFactor = new JSONStorableFloat("Slap Volume Factor", 1f,0f, 5f);
		public JSONStorableFloat slapPitchFactor = new JSONStorableFloat("Slap Pitch Factor", 1f,0f,5f);
		public JSONStorableFloat slapReactionTimeout = new JSONStorableFloat("Slap Reaction Timeout", 2f,0f,5f);
		public JSONStorableFloat touchArousalRate = new JSONStorableFloat("Touch Arousal Rate", 0f,0f,1f);
		public JSONStorableFloat slapArousalRate = new JSONStorableFloat("Slap Arousal Rate", 1f,0f,10f);
		public JSONStorableFloat lookAtArousalRate = new JSONStorableFloat("LookAt Arousal Rate", .01f,0f,1f);

		public JSONStorableFloat maxReactionProb = new JSONStorableFloat("Max Reaction Chance", .5f,0f,.5f);

		List<JSONStorableBool> boolParams = new List<JSONStorableBool>();
		List<JSONStorableFloat> floatParams = new List<JSONStorableFloat>();

		public float timeTouched;
		public float timeWatched;
		public float gazeTimeout;
		public float interest;
		public float reactionChance;

		public int numLookAtColliders;
		public JSONStorableFloat onLookPriority = new JSONStorableFloat("OnLookAt Priority", 1f,0f,10f);
		public JSONStorableFloat onLookMaxRepetitions = new JSONStorableFloat("Trigger Priority", 10f,0f,10f);
		public Dictionary<int, ClipLibrary> onSlapLibraries = new Dictionary<int, ClipLibrary>();
		public bool hasCollisionClips;

		public BodyRegion(string region){
			name = region;

			// GetClipLibraries();

			boolParams.Add(enabled);
			boolParams.Add(isSelfColliding);
			boolParams.Add(onLookEnabled);
			boolParams.Add(touchEnabled);
			boolParams.Add(slapEnabled);
			boolParams.Add(slapVocalReactionEnabled);

			floatParams.Add(slapSensitivity);
			floatParams.Add(slapVolumeFactor);
			floatParams.Add(slapPitchFactor);
			floatParams.Add(slapReactionTimeout);
			floatParams.Add(touchArousalRate);
			floatParams.Add(slapArousalRate);
			floatParams.Add(lookAtArousalRate);
			floatParams.Add(maxReactionProb);

			foreach(JSONStorableBool json in boolParams){
				json.setJSONCallbackFunction = BoolJSONCB;
				json.name = region+" "+json.name;
				// UIManager.script.RegisterBool(json);
			}

			foreach(JSONStorableFloat json in floatParams){
				json.setJSONCallbackFunction = FloatJSONCB;
				json.name = region+" "+json.name;
				// UIManager.script.RegisterFloat(json);
			}
		}

		void FloatJSONCB(JSONStorableFloat fJSON){
			foreach(BodyRegion child in children){
				child.floatParams[floatParams.IndexOf(fJSON)].valNoCallback = fJSON.val;
			}
		}

		void BoolJSONCB(JSONStorableBool bJSON){
			foreach(BodyRegion child in children){
				boolParams.First(x => x.name == bJSON.name).val = bJSON.val;
			}
		}

		public bool ShouldReact(){
			float rand = UnityEngine.Random.Range(0f, 1f);
			return rand < reactionChance;
		}

		public void GetParents(){
			parents.Add(this);
			BodyRegion parent = this.parent;
			topParent = this;
			while(parent != null)
			{
				topParent = parent;
				parents.Add(parent);
				// parent.children.Add(this);
				parent = parent.parent;
			}

			// (name + " " +topParent.name).Print();
		}

		public void GetChildren(){
			foreach(BodyRegion child in children){

				children = child.children;
				this.children.AddRange(children);
			}
		}

		// public void GetClipLibraries(){
			// clipLibraries.Clear();
			// foreach(string e in BodyRegionManager.events){
			// 	// clipLibraries[e] = new ClipLibrary(bodyRegionManager.audioManager.audioSourceControl, AudioManager.ImportFolder("Custom/Sounds/CheesyFX/BodyLanguage/Voices/"+bodyRegionManager.voice.val+"/BodyRegions/"+name+"/"+e), name+"/"+e);
			// 	clipLibraries[e] = bodyRegionManager.audioManager.GetBodyRegionClipLibrary(name, e);
			// 	if(!hasCollisionClips && clipLibraries[e].clips.Count > 0){
			// 		hasCollisionClips = true;
			// 	}
			// }
		// }

		// public void CreateViewScanUI(MVRScript script, List<object> UIElements){
		// 	interest.CreateUI(UIElements, false);
		// 	reactionChance.CreateUI(UIElements, true);
		// }

		// public override void CreateUI(){
		// 	UIDynamicTextInfo infoline;
		// 	List<object> UIElements = new List<object>();
		//
		// 	UIManager.CreateButton("Return", ()=>UIManager.Return(), UIElements, rightSide:true);
		//
		// 	infoline = Utils.SetupInfoOneLine(UIManager.script, name, false);
		// 	UIElements.Add(infoline);
		// 	infoline.height = 50f;
		//
		// 	for(int i=0; i< boolParams.Count; i++) boolParams[i].CreateUI(UIElements, i%2 == 1);
		// 	for(int i=0; i< floatParams.Count; i++) floatParams[i].CreateUI(UIElements, i%2 == 1);
		//
		// 	string childrenNames = "Children:\n";
		// 	children.ForEach(x => childrenNames += x.name+"\n");
		// 	infoline = Utils.SetupInfoOneLine(UIManager.script, childrenNames, true);
		// 	UIElements.Add(infoline);
		// 	infoline.height = 120f;
		//
		//
		// 	UIManager.newWindowElements.AddRange(UIElements);
		// 	// collisionListener.CreateUI();
		// }
    }
}