using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheesyFX
{
    public class Movement
    {
	    // public string name;
	    public Pose pose;
	    public JSONStorableBool enabledJ = new JSONStorableBool("Movement Enabled", true);
	    public JSONStorableBool cycleForcesEnabled = new JSONStorableBool("CycleForces Enabled", true);
	    public Atom atom;
	    public Rigidbody rb;
	    private JSONStorableStringChooser personChooser = new JSONStorableStringChooser("Person", new List<string>(), null, "Person");
        private JSONStorableStringChooser rbChooser = new JSONStorableStringChooser("Target RB", new List<string>(), null, "Target RB");
        private JSONStorableStringChooser spaceChooser = new JSONStorableStringChooser("spaceChooser",
	        new List<string>{"Local", "Person Root"}, "Local", "Transform Space");

        public bool isLocalSpace;

        private readonly Force[] forces = new Force[6];
        public readonly CircularForce circularForce;

        public UIDynamicMovement uid;
        private UIDynamicTabBarWithBG tabbar;
        private int lastTabId;
        public static bool configureUIOpen;
        private static List<object> UIElements = new List<object>();

        private static string[] rbPriority = { "hip", "chest", "head", "lFoot", "rFoot", "lHand", "rHand" };

        public Movement(Pose pose, string atomUid = null, string rbName = null)
        {
	        this.pose = pose;
	        if (atomUid != null) atom = SuperController.singleton.GetAtomByUid(atomUid);
	        else atom = PoseMe.atom;
	        if (rbName == null)
		        rbName = rbPriority.FirstOrDefault(x => PoseMe.currentPose.movements.All(y => y.atom != atom || y.rb.name != x));
	        var fc = atom.forceReceivers.FirstOrDefault(x => x.name == rbName);
		    rb = fc.GetComponent<Rigidbody>();
	        // name = $"{atom.name}:{rb.name} Movement";
	        SyncRBChoices();
	        SyncAtomChoices();
            rbChooser.setCallbackFunction += SyncRB;
            personChooser.setCallbackFunction += SyncAtom;
            enabledJ.setCallbackFunction += val =>
            {
	            for (int i = 0; i < 6; i++)
	            {
		            forces[i].SetActive(val && cycleForcesEnabled.val && forces[i].enabledJ.val);
	            }
	            circularForce.SetActive(val && circularForce.enabledJ.val);
            };
            cycleForcesEnabled.setCallbackFunction += val =>
            {
	            for (int i = 0; i < 6; i++)
	            {
		            forces[i].SetActive(val && forces[i].enabledJ.val);
	            }
            };
            personChooser.valNoCallback = atom.uid;
            rbChooser.valNoCallback = rb.name;

            for (int i = 0; i < 6; i++)
            {
	            string type = i < 3 ? "Force" : "Torque";
	            var id = i % 3;
	            var dir = id == 0 ? "X" : (id == 1 ? "Y" : "Z");
                if (id == 0)
                {
                    if(i<3) forces[i] = PoseMe.singleton.gameObject.AddComponent<Force>().Init($"{type}{dir}", rb, () => Vector3.right);
                    else forces[i] = PoseMe.singleton.gameObject.AddComponent<Torque>().Init($"{type}{dir}", rb, () => Vector3.right);
                }
                else if (id == 1)
                {
	                if(i<3) forces[i] = PoseMe.singleton.gameObject.AddComponent<Force>().Init($"{type}{dir}", rb, () => Vector3.up);
	                else forces[i] = PoseMe.singleton.gameObject.AddComponent<Torque>().Init($"{type}{dir}", rb, () => Vector3.up);
                }
                else if (id == 2)
                {
	                if(i<3) forces[i] = PoseMe.singleton.gameObject.AddComponent<Force>().Init($"{type}{dir}", rb, () => Vector3.forward);
	                else forces[i] = PoseMe.singleton.gameObject.AddComponent<Torque>().Init($"{type}{dir}", rb, () => Vector3.up);
                }
				
                var j = i;
                forces[i].enabledJ.setCallbackFunction += val =>
                {
	                forces[j].SetActive(val);
                };
                forces[i].amplitude.mean.setCallbackFunction += val => SyncForceEnabled(j);
                forces[i].amplitude.delta.setCallbackFunction += val => SyncForceEnabled(j);
                forces[i].enabledJ.SetWithDefault(false);
            }

            circularForce = PoseMe.singleton.gameObject.AddComponent<CircularForce>().Init(this);
            spaceChooser.AddCallback(SyncSpace);
        }

        public Movement(Pose pose, JSONClass jc, bool enabled = true) : this(pose, jc["atom"].Value, jc["rb"].Value)
        {
	        Load(jc);
	        SetActive(enabled);
        }

        private static Vector3[] directions = { Vector3.right, Vector3.up, Vector3.forward };
        private void SyncSpace(string val)
        {
	        switch (val)
	        {
		        case "Local":
		        {
			        isLocalSpace = true;
			        for (int i = 0; i < 6; i++)
			        {
				        Vector3 dir = directions[i % 3];
				        forces[i].GetDirection = () => rb.transform.TransformDirection(dir);
			        }
			        break;
		        }
		        case "Person Root":
		        {
			        isLocalSpace = false;
			        for (int i = 0; i < 6; i++)
			        {
				        Vector3 dir = directions[i % 3];
				        forces[i].GetDirection = () => atom.mainController.transform.TransformDirection(dir);
			        }
			        break;
		        }
	        }
	        circularForce.SyncAxis(circularForce.rotateAround.val);
        }

        private void SyncAtom(string uid)
        {
	        atom = SuperController.singleton.GetAtomByUid(uid);
	        SyncRB(rbChooser.val);
        }

        private void SyncAtomChoices()
        {
	        personChooser.SetChoices(PoseMe.persons.Select(x => x.atom.uid));
        }

        private void SyncRB(string rbName)
        {
	        if(string.IsNullOrEmpty(rbName)) return;
	        rb = atom.forceReceivers.FirstOrDefault(x => x.name == rbName).GetComponent<Rigidbody>();
	        for (int i = 0; i < 6; i++)
	        {
		        forces[i].rb = rb;
	        }

	        if(configureUIOpen)
	        {
		        Debug.Clear();
		        rb.transform.Draw();
	        }
        }

        private void SyncRBChoices()
        {
	        rbChooser.SetChoices(atom.forceReceivers
		        // .Where(x => PoseMe.currentPose == null || PoseMe.currentPose.movements.All(y => y.rb.name != x.name))
		        .Select(x => x.name));
        }

        private void SyncForceEnabled(int i)
        {
	        var force = forces[i];
	        force.OnAmpChanged();
	        if(tabbar == null) return;
	        tabbar.bgImages[i].gameObject.SetActive(force.enabled);
	        if(tabbar == null) return;
	        SyncButtons(i);
        }

        private void SyncButtons(int i)
        {
	        var force = forces[i];
	        tabbar.bgImages[i].gameObject.SetActive(force.enabled);
        }

        public void SetActive(bool val)
        {
	        if(!enabledJ.val) return;
	        if(cycleForcesEnabled.val)
	        {
		        for (int i = 0; i < 6; i++)
		        {
			        forces[i].SetActive(val && forces[i].enabledJ.val);
		        }
	        }
	        circularForce.SetActive(val && circularForce.enabledJ.val);
        }
        
        public void SetActiveImmediate(bool val)
        {
	        if(!enabledJ.val) return;
	        if(cycleForcesEnabled.val)
	        {
		        for (int i = 0; i < 6; i++)
		        {
			        forces[i].SetActiveImmediate(val && forces[i].enabledJ.val);
		        }
	        }
	        circularForce.SetActiveImmediate(val && circularForce.enabledJ.val);
        }

        public void CreateUI()
        {
	        PoseMe.singleton.ClearUI();
	        UIElements.Clear();
	        // PoseMe.singleton.RemoveUIElements(UIElements);
	        var button = PoseMe.singleton.CreateButton("Return");
	        button.buttonColor = PoseMe.navColor;
	        button.button.onClick.AddListener(CloseUI);
	        enabledJ.CreateUI(rightSide: true);
	        cycleForcesEnabled.CreateUI(UIElements);
	        circularForce.enabledJ.CreateUI(UIElements);
	        PoseMe.singleton.SetupButton("Configure Cycle Forces", true, CreateForceUI);
	        PoseMe.singleton.SetupButton("Configure Circular Force", true, circularForce.CreateUI);
	        personChooser.CreateUI(UIElements, chooserType:2);
	        rbChooser.CreateUI(UIElements, true, chooserType:2);
	        spaceChooser.CreateUI(UIElements, false, chooserType:0);
	        
	        configureUIOpen = true;
	        rb.transform.Draw();
        }
	
        public void CreateForceUI()
        {
	        PoseMe.singleton.ClearUI();
	        var button = PoseMe.singleton.CreateButton("Return");
	        button.buttonColor = PoseMe.navColor;
	        button.button.onClick.AddListener(CreateUI);
	        cycleForcesEnabled.CreateUI(rightSide: true);

	        tabbar = UIManager.CreateTabBarWithBG(new[] { "ForceX", "ForceY", "ForceZ", "TorqueX", "TorqueY", "TorqueZ" }, SelectForceTab, bgColor:Color.green);
	        
	        tabbar.SelectTab(lastTabId);
	        for (int i = 0; i < 6; i++)
	        {
		        SyncButtons(i);
	        }
        }

        public static void CloseUI()
        {
	        configureUIOpen = false;
	        PoseMe.singleton.ClearUI();
	        UIElements.Clear();
	        if (PoseMe.singleton.UITransform.gameObject.activeSelf) PoseMe.singleton.CreateUI();
	        else PoseMe.needsUIRefresh = true;
	        Debug.Clear();
        }
        
        private void SelectForceTab(int id)
        {
	        PoseMe.singleton.RemoveUIElements(UIElements);
	        forces[lastTabId].paramControl.UIOpen = false;
	        UIElements = forces[id].paramControl.CreateUI(PoseMe.singleton);
	        lastTabId = id;
        }

        public void Destroy()
        {
	        for (int i = 0; i < 6; i++)
	        {
		        Object.Destroy(forces[i]);
	        }
	        Object.Destroy(circularForce);
        }

        public void OnAtomAdded(Atom atom)
        {
	        SyncAtomChoices();
        }

        public void OnAtomRenamed(string oldUid, string newUid)
        {
	        SyncAtomChoices();
	        if (personChooser.val == oldUid)
	        {
		        if(uid) uid.label.text = $"<b>{newUid}:{rb.name}</b>";
		        personChooser.valNoCallback = newUid;
		        // name = $"{atom.name}:{rb.name} Movement";
	        }
	        
        }
        
        public void OnAtomRemoved(Atom atom)
        {
	        SyncAtomChoices();
	        if (this.atom == atom)
	        {
		        Destroy();
		        // if(PoseMe.singleton.UITransform.gameObject.activeSelf && PoseMe.currentTab == 5 && !configureUIOpen) pose.CreateMovementItems();
		        PoseMe.singleton.RemoveUIElement(uid);
	        }
        }

        public JSONClass Store()
        {
	        var jc = new JSONClass();
	        enabledJ.Store(jc, false);
	        jc["atom"] = atom.uid;
	        jc["rb"] = rb.name;
	        for (int i = 0; i < 6; i++)
	        {
		        jc[forces[i].name] = forces[i].Store(false);
	        }

	        jc["CircularForce"] = circularForce.Store();
	        return jc;
        }

        public void Load(JSONClass jc)
        {
	        atom = SuperController.singleton.GetAtomByUid(jc["atom"].Value);
	        rb = atom.forceReceivers.First(x => x.name == jc["rb"].Value).GetComponent<Rigidbody>();
	        personChooser.valNoCallback = atom.uid;
	        rbChooser.valNoCallback = rb.name;
	        for (int i = 0; i < 6; i++)
	        {
		        forces[i].Load(jc[forces[i].name].AsObject, true);
	        }
	        if(jc.HasKey("CircularForce")) circularForce.Load(jc["CircularForce"].AsObject);
	        enabledJ.Load(jc, true);
        }

        public static GameObject uidPrefab;
        
        public static UIDynamicMovement CreateUIDynamicMovement(Movement movement, bool rightSide = false)
        {
	        if (uidPrefab == null) CreateUidPrefab();
	        Transform t = PoseMe.MyCreateUIElement(Movement.uidPrefab.transform, rightSide);
	        UIDynamicMovement uid = t.gameObject.GetComponent<UIDynamicMovement>();
	        uid.label.text = movement.rb.name;
	        if (!movement.enabledJ.val) uid.toggleText.text = "";
	        uid.activeToggle.isOn = movement.enabledJ.val;
	        // uid.toggleText.text = movement.enabledJ.val? "✓" : "";
	        uid.activeToggle.onValueChanged.AddListener(val =>
	        {
		        uid.toggleText.text = val ? "✓" : "";
		        movement.enabledJ.val = val;
	        });
	        uid.deleteButton.onClick.AddListener(() =>
	        {
		        movement.pose.movements.Remove(movement);
		        movement.Destroy();
		        PoseMe.singleton.RemoveUIElement(uid);
	        });
	        uid.configureButton.button.onClick.AddListener(movement.CreateUI);
	        uid.configureButton.label = $"<b>{movement.atom.name}:{movement.rb.name}</b>";
	        movement.uid = uid;
	        if(PoseMe.UIElements != null) PoseMe.UIElements.Add(uid);
	        return uid;
        }
        
        public static void CreateUidPrefab()
        {
			if (uidPrefab == null)
			{
                uidPrefab = new GameObject("UIDynamicGazeTarget");
                uidPrefab.SetActive(false);
				RectTransform rt = uidPrefab.AddComponent<RectTransform>();
				rt.anchorMax = new Vector2(0, 1);
				rt.anchorMin = new Vector2(0, 1);
				rt.offsetMax = new Vector2(535, -500);
				rt.offsetMin = new Vector2(10, -600);
				LayoutElement le = uidPrefab.AddComponent<LayoutElement>();
				le.flexibleWidth = 1;
				le.minHeight = 50;
				le.minWidth = 350;
				le.preferredHeight = 50;
				le.preferredWidth = 500;

				RectTransform backgroundTransform = PoseMe.singleton.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
				backgroundTransform = Object.Instantiate(backgroundTransform, uidPrefab.transform);
				backgroundTransform.name = "Background";
				backgroundTransform.anchorMax = new Vector2(1, 1);
				backgroundTransform.anchorMin = new Vector2(0, 0);
				backgroundTransform.offsetMax = new Vector2(0, 0);
				backgroundTransform.offsetMin = new Vector2(0, 0);
                backgroundTransform.GetComponent<Image>().color = new Color(0.839f, .839f, .839f);
                
                RectTransform buttonPrefab = PoseMe.singleton.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
                var buttonTransform = Object.Instantiate(buttonPrefab, uidPrefab.transform);
                Object.DestroyImmediate(buttonTransform.GetComponent<Button>());
                buttonTransform.name = "ActiveToggle";
                buttonTransform.anchorMax = new Vector2(0, 1);
                buttonTransform.anchorMin = new Vector2(0, 1);
                buttonTransform.offsetMax = new Vector2(50, 0);
                buttonTransform.offsetMin = new Vector2(0, -50);
                var activeToggle = buttonTransform.gameObject.AddComponent<Toggle>();
                var activeToggleText = buttonTransform.Find("Text").GetComponent<Text>();
                activeToggleText.text = "✓";
                activeToggleText.fontSize = 28;
                activeToggle.isOn = true;

                var configureButtonTransform = Object.Instantiate(PoseMe.singleton.manager.configurableButtonPrefab.transform, uidPrefab.transform) as RectTransform;
                configureButtonTransform.name = "ConfigureButton";
                configureButtonTransform.anchorMax = new Vector2(1, 1);
                configureButtonTransform.anchorMin = new Vector2(0, 0);
                configureButtonTransform.offsetMax = new Vector2(-50, 0);
                configureButtonTransform.offsetMin = new Vector2(50, 0);
                var label = configureButtonTransform.GetComponentInChildren<Text>();
                label.text = "Configure: <b>hip</b>";
                label.color = Color.black;

                buttonTransform = Object.Instantiate(buttonPrefab, uidPrefab.transform);
				buttonTransform.name = "Button";
				buttonTransform.anchorMax = new Vector2(1, 1);
				buttonTransform.anchorMin = new Vector2(1, 0);
				buttonTransform.offsetMax = new Vector2(0, 0);
				buttonTransform.offsetMin = new Vector2(-50, 0);
				var deleteButton = buttonTransform.GetComponent<Button>();
                var deleteButtonText = buttonTransform.Find("Text").GetComponent<Text>();
                deleteButtonText.fontSize = 28;
                deleteButtonText.text = "<b>X</b>";
                deleteButtonText.color = Color.white;
                buttonTransform.GetComponent<Image>().color = PoseMe.severeWarningColor;

                UIDynamicMovement uid = uidPrefab.AddComponent<UIDynamicMovement>();
                uid.activeToggle = activeToggle;
                uid.toggleText = activeToggleText;
                uid.deleteButton = deleteButton;
                uid.configureButton = configureButtonTransform.GetComponent<UIDynamicButton>();
                uid.label = label;
            }
		}
    }
}