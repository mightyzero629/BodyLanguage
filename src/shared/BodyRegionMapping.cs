using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public static class BodyRegionMapping
    {
	    public static List<string> touchZoneNames = new List<string>{
		    "Abdomen", "Pelvis",
		    "lGlutes","rGlutes","lThigh","rThigh","lShin","rShin",
		    "Chest","lBreast","rBreast","lAreola","rAreola","lNipple","rNipple",
		    "Head", "Eyes", "Lips", "Mouth", "Throat",
		    "Labia","Anus",
		    "Penis", "Testes",
			
		    "lFoot","lBigToe", "lSmallToe1", "lSmallToe2", "lSmallToe3", "lSmallToe4",
		    "rFoot","rBigToe", "rSmallToe1", "rSmallToe2", "rSmallToe3", "rSmallToe4",
		    "lShoulder","rShoulder","lForeArm","rForeArm",
		    "lPalm","lThumb","lIndex","lMid","lRing","lPinky",
		    "rPalm","rThumb","rIndex","rMid","rRing","rPinky",
	    };
	    public static List<string> bodyRegionNames = new List<string>{
		    "Legs", "lLeg", "rLeg", "Thighs", "Shins",
		    "Crotch", "Glutes", "Breasts", "Nipples",
		    "Feet", "Toes", "lToes", "rToes",
		    "Arms", "lArm", "rArm", "Shoulders", "ForeArms",
		    "lHand", "rHand", "Hands","lFingers", "rFingers", "Fingers",
	    };
	    public static List<string> cumRegionNames = new List<string>{
		    "lLeg", "rLeg", "Thighs", "Shins",
		    "Crotch", "Glutes", "lBreast", "rBeast", "Abdomen", "Pelvis",
		    "Feet", "Toes", "lToes", "rToes",
		    // "lArm", "rArm", "Shoulders", "ForeArms",
		    "lHand", "rHand",
		    "Eyes", "Lips", "Mouth"
	    };
	    public static List<string> topRegionNames = new List<string>{
		    "Legs", "Crotch", "Glutes", "Breasts", "Feet", "Arms", "Hands", "Head"
	    };
	    
	    public static Dictionary<string, TouchZone> touchZones = new Dictionary<string, TouchZone>();
	    public static Dictionary<string, BodyRegion> bodyRegions = new Dictionary<string, BodyRegion>();
	    public static Dictionary<string, BodyRegion> regionsByLowerName;
	    public static List<BodyRegion> topRegions = new List<BodyRegion>();
	    
	    public static Dictionary<string, string> bodyRegionByRigidbodyName = new Dictionary<string, string>();
	    public static Dictionary<string, string[]> parentNamesByTouchZoneName = new Dictionary<string, string[]>();

	    public static void Init(Atom atom)
	    {
		    foreach (var rb in atom.GetComponentsInChildren<Rigidbody>(true))
		    {
			    // if(rb.name.StartsWith("PhysicsMesh")) rb.name.Print();
			    bodyRegionByRigidbodyName[rb.name] = MapToRegion(rb);
		    }
		    MapMiscRegions();
		    
		    foreach(string name in bodyRegionNames){
			    BodyRegion region = new BodyRegion(name);
			    bodyRegions[name] = region;
			    if (topRegionNames.Contains(name))
			    {
				    topRegions.Add(region);
			    }
		    }
		    foreach(string name in touchZoneNames){
			    TouchZone touchZone = new TouchZone(name);
			    touchZones[name] = touchZone;
			    if (topRegionNames.Contains(name)) topRegions.Add(touchZone);
			    // touchZone.GetCumRegion();
		    }
		    BuildHierarchy();
		    // SuperController.singleton.ClearMessages();
		    // var lip = atom.GetComponentsInChildren<Rigidbody>().First(x => x.name == "PhysicsMeshJoint0");
		    // // lip.transform.parent.parent.parent.parent.name.Print();
		    
		    // foreach (var t in atom.transform.Find("rescale2/geometry/FemaleMorphers/MouthPhysicsMesh").GetComponentsInChildren<Rigidbody>())
		    // {
			   //  t.name.Print();
		    // }
		    //432.1704f
		    //5.832772 6.006286
		    // var cj = atom.transform.Find("rescale2/geometry/FemaleMorphers/BreastPhysicsMesh/PhysicsMeshleftnipple/PhysicsMeshJointleftnipple0").GetComponent<ConfigurableJoint>();
		    
		    // var xDrive = cj.xDrive;
		    // var yDrive = cj.yDrive;
		    // var zDrive = cj.zDrive;
		    // xDrive.positionSpring = yDrive.positionSpring = zDrive.positionSpring = 432f;
		    // xDrive.positionDamper = yDrive.positionDamper = zDrive.positionDamper = 6f;
		    // xDrive.positionSpring.Print();
		    // cj.xDrive = xDrive;
		    // cj.yDrive = yDrive;
		    // cj.zDrive = zDrive;
	    }

	    public static string GetRegionName(string rbName)
	    {
		    string region;
		    bodyRegionByRigidbodyName.TryGetValue(rbName, out region);
		    return region;
	    }

	    public static string GetRegionName(this Rigidbody rb)
	    {
		    return GetRegionName(rb.name);
	    }
	    
	    public static TouchZone GetRegion(this Rigidbody rb)
	    {
		    string region = rb.GetRegionName();
		    if (region == null) return null;
		    TouchZone tz;
		    touchZones.TryGetValue(rb.GetRegionName(), out tz);
		    return tz;
	    }

	    public static string MapToRegion(Rigidbody rb)
	    {
		    return MapToRegion(rb.name);
	    }

	    public static string MapToRegion(string rbName)
		{
			string output = null;
			if (rbName.Contains("Link") || rbName.Contains("Control")) return null;

			if (rbName.Contains("Lip")) output = "Lips";
			else if (rbName.Contains("FaceHardLeft5") || rbName.Contains("FaceHardRight5"))
			{
				output = "Eyes";
			}
			else if (rbName.Contains("head")) output = "Head";
			else if (rbName.Contains("chest")) output = "Chest";
			else if (rbName.Contains("pelvis")) output = "Pelvis";
			else if (rbName.Contains("rThigh")) output = "rThigh";
			else if (rbName.Contains("lThigh")) output = "lThigh";
			else if (rbName.Contains("rShin")) output = "rShin";
			else if (rbName.Contains("lShin")) output = "lShin";
			else if (rbName.Contains("rFoot")) output = "rFoot";
			else if (rbName.Contains("rBigToe")) output = "rBigToe";
			else if (rbName.Contains("rSmallToe1")) output = "rSmallToe1";
			else if (rbName.Contains("rSmallToe2")) output = "rSmallToe2";
			else if (rbName.Contains("rSmallToe3")) output = "rSmallToe3";
			else if (rbName.Contains("rSmallToe4")) output = "rSmallToe4";
			else if (rbName.Contains("lFoot")) output = "lFoot";
			else if (rbName.Contains("lBigToe")) output = "lBigToe";
			else if (rbName.Contains("lSmallToe1")) output = "lSmallToe1";
			else if (rbName.Contains("lSmallToe2")) output = "lSmallToe2";
			else if (rbName.Contains("lSmallToe3")) output = "lSmallToe3";
			else if (rbName.Contains("lSmallToe4")) output = "lSmallToe4";
			else if (rbName.Contains("PhysicsMeshJointleft glute")) output = "lGlutes";
			else if (rbName.Contains("PhysicsMeshJointright glute")) output = "rGlutes";
			else if (rbName.Contains("AutoColliderFemaleAutoColliderslPectoral")) output = "lBreast";
			else if (rbName.Contains("AutoColliderFemaleAutoCollidersrPectoral")) output = "rBreast";
			else if (rbName.Contains("PhysicsMeshJointleftareola")) output = "lAreola";
			else if (rbName.Contains("PhysicsMeshJointrightareola")) output = "rAreola";
			else if (rbName.Contains("PhysicsMeshJointleftnipple")) output = "lNipple";
			else if (rbName.Contains("PhysicsMeshJointrightnipple")) output = "rNipple";
			else if (rbName.Contains("PhysicsMeshJointleft")) output = "lBreast";
			else if (rbName.Contains("PhysicsMeshJointright")) output = "rBreast";
			else if (rbName=="lNipple") output = "lNipple";
			else if (rbName=="rNipple") output = "rNipple";
			else if (rbName.Contains("JointG")) output = "Labia";
			else if (rbName.Contains("PhysicsMeshJointgen")) output = "Labia";
			else if (rbName.Contains("PhysicsMeshJointlab")) output = "Labia";
			else if (rbName.Contains("JointA")) output = "Anus";
			else if (rbName.Contains("PhysicsMeshJointan")) output = "Anus";
			else if (rbName.Contains("PhysicsMeshJointbridge")) output = "Anus";
			else if (rbName.Contains("PhysicsMeshJoint")) output = "Lips";
			else if (rbName.Contains("chest")) output = "Chest";
			else if (rbName.Contains("abdomen")) output = "Abdomen";
			else if (rbName.Contains("pelvis")) output = "Pelvis";
			else if (rbName.Contains("lForeArm") || rbName.Contains("lShldr")) output = "lArm";
			else if (rbName.Contains("rForeArm") || rbName.Contains("rShldr")) output = "rArm";
			else if (rbName.Contains("lHand") || rbName.Contains("lCarpal")) output = "lPalm";
			else if (rbName.Contains("lThumb")) output = "lThumb";
			else if (rbName.Contains("lIndex")) output = "lIndex";
			else if (rbName.Contains("lMid")) output = "lMid";
			else if (rbName.Contains("lRing")) output = "lRing";
			else if (rbName.Contains("lPinky")) output = "lPinky";
			else if (rbName.Contains("rHand") || rbName.Contains("rCarpal")) output = "rPalm";
			else if (rbName.Contains("rThumb")) output = "rThumb";
			else if (rbName.Contains("rIndex")) output = "rIndex";
			else if (rbName.Contains("rMid")) output = "rMid";
			else if (rbName.Contains("rRing")) output = "rRing";
			else if (rbName.Contains("rPinky")) output = "rPinky";
			
			else if (rbName.Contains("Gen")) output = "Penis";
			else if (rbName == "Testes") output = "Testes";
			// else if (name == "sack") output = "DildoSack";
			// else if (name == "b1" || name == "b2" || name == "b3") output = "DildoPenis";
			// else if (name == "object") output = "Object";

			// if (name=="lNipple")
			// {
			// 	rb.name.Print();
			// 	rb.GetComponents<Collider>().ToList().ForEach(x => x.isTrigger.Print());
			// }
			return output;
		}

        private static void MapMiscRegions()
        {
	        bodyRegionByRigidbodyName["sack"] = "DildoSack";
	        bodyRegionByRigidbodyName["b1"] = "DildoPenis";
	        bodyRegionByRigidbodyName["b2"] = "DildoPenis";
	        bodyRegionByRigidbodyName["b3"] = "DildoPenis";
	        // bodyRegionByRigidbodyName["Gen1"] = "Penis";
	        // bodyRegionByRigidbodyName["Gen2"] = "Penis";
	        // bodyRegionByRigidbodyName["Gen3"] = "Penis";
	        // bodyRegionByRigidbodyName["Testes"] = "Testes";
        }
        
        public static void BuildHierarchy(){
			touchZones["Anus"].parent = bodyRegions["Crotch"];
			touchZones["Labia"].parent = bodyRegions["Crotch"];

			foreach(string s in new []{"l", "r"}){
				touchZones[s+"Thigh"].parent = bodyRegions["Thighs"];
				touchZones[s+"Shin"].parent = bodyRegions["Shins"];

				touchZones[s+"Shoulder"].parent = bodyRegions["Shoulders"];
				touchZones[s+"ForeArm"].parent = bodyRegions["ForeArms"];

				touchZones[s+"Glutes"].parent = bodyRegions["Glutes"];

				touchZones[s+"Breast"].parent = bodyRegions["Breasts"];
				touchZones[s+"Areola"].parent = touchZones[s+"Breast"];
				touchZones[s+"Nipple"].parent = bodyRegions["Nipples"];

				foreach(string finger in new []{"Thumb","Index","Mid","Ring","Pinky"})
				{
					touchZones[s + finger].parent = bodyRegions[s+"Fingers"];
				}
				bodyRegions[s+"Hand"].parent = bodyRegions["Hands"];
				touchZones[s+"Palm"].parent = bodyRegions[s+"Hand"];
				bodyRegions[s+"Fingers"].parent = bodyRegions[s+"Hand"];

				
				foreach(string toe in new string[]{"BigToe","SmallToe1","SmallToe2","SmallToe3","SmallToe4"}){
					touchZones[s+toe].parent = bodyRegions[s+"Toes"];
				}
				bodyRegions[s+"Toes"].parent = touchZones[s+"Foot"];
				touchZones[s+"Foot"].parent = bodyRegions["Feet"];
			}
			bodyRegions["Thighs"].parent = bodyRegions["Legs"];
			bodyRegions["Shins"].parent = bodyRegions["Legs"];
			
			bodyRegions["Shoulders"].parent = bodyRegions["Arms"];
			bodyRegions["ForeArms"].parent = bodyRegions["Arms"];

			bodyRegions["Nipples"].parent = bodyRegions["Breasts"];
			// foreach(BodyRegion bodyRegion in allRegions){
			// 	BodyRegion parent = bodyRegion.parent;
			// 	int i=0;
			// 	while(parent != null && i<10){
			// 		parent.children.Add(bodyRegion);
			// 		parent = parent.parent;
			// 		i+=1;
			// 	}
			// 	// if(bodyRegion.parent != null) bodyRegion.parent.children.Add(bodyRegion);
			// }
			foreach(TouchZone touchZone in touchZones.Values.ToList()){
				touchZone.GetParents();
				parentNamesByTouchZoneName[touchZone.name] = touchZone.parents.Select(x => x.name).ToArray();
			}
			// bodyRegions["Legs"].children.ForEach(x => x.name.Print());
		}

        // public static string[] GetParentNames(string region)
        // {
	       //  if (string.IsNullOrEmpty(region)) return null;
	       //  return parentNamesByTouchZoneName[region];
        // }
        //
        // public static string[] GetParentNames(Rigidbody rb)
        // {
	       //  var region = GetRegionName(rb.name);
	       //  if (region == null) return null;
	       //  string[] parents;
	       //  parentNamesByTouchZoneName.TryGetValue(region, out parents);
	       //  return parents;
        // }

        public static bool IsInRegion(this Rigidbody rb, string region, bool exact = false)
        {
	        var reg = GetRegionName(rb.name);
	        if (reg == null) return false;
	        if ((!exact && reg.Contains(region)) || reg == region) return true;
	        string[] parents;
	        if (parentNamesByTouchZoneName.TryGetValue(reg, out parents))
	        {
		        if(!exact) return parents.Any(x => x.Contains(region));
		        return parents.Contains(region);
	        }
	        return false;
        }
        
        public static bool IsInRegion(string name, string region, bool exact = false)
        {
	        var reg = GetRegionName(name);
	        if (reg == null) return false;
	        if ((!exact && reg.Contains(region)) || reg == region) return true;
	        string[] parents;
	        if (parentNamesByTouchZoneName.TryGetValue(reg, out parents))
	        {
		        if(!exact) return parents.Any(x => x.Contains(region));
		        return parents.Contains(region);
	        }
	        return false;
        }
    }
}