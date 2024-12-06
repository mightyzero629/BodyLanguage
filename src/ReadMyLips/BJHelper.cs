using System;
using System.Collections.Generic;
using System.Linq;
using MacGruber;
using UnityEngine;
using Object = System.Object;

namespace CheesyFX
{
    public class BJHelper : MonoBehaviour
    {
        private bool initialized;
        public static HashSet<Penetrator> penetrators = new HashSet<Penetrator>();
        private static Penetrator closest;
        public static GameObject triggerGO;
        private static CapsuleCollider trigger;
        public static BJHelper singleton;
        public static JSONStorableBool enabledJ = new JSONStorableBool("Pre BJ Reactions Enabled", true);
        private static JSONStorableBool openMouth = new JSONStorableBool("Open Mouth", true);
        private static JSONStorableBool morphLips = new JSONStorableBool("Morph Lips", true);
        private static JSONStorableBool morphTongue = new JSONStorableBool("Morph Tongue", true);
        public static JSONStorableFloat distance = new JSONStorableFloat("Distance", 0f, 0f, .15f, true, false);
        public static JSONStorableFloat speed = new JSONStorableFloat("Speed", 0f, -1f, 1f, true, false);
        public static JSONStorableString info = new JSONStorableString("info", "");
        // private static JSONStorableBool debug = new JSONStorableBool("Debug", false, val => trigger.gameObject.GetComponent<Renderer>().enabled = val);

        private static MyJSONStorableVector3 offset =
            new MyJSONStorableVector3("Offset", new Vector3(), new Vector3(), new Vector3(.1f, .1f, .1f), Offset);
        
        private float lastDistance;
        public static Transform throatTrigger;
        
        private static LerpingMorph mouthNarrow;
        private static LerpingMorph mouthBlow;
        private static LerpingMorph tongueOut;
        private static LerpingMorph tongueUp;

        private void Start()
        {
            singleton = this;
            trigger = FillMeUp.throat.proximityTrigger;
            trigger.gameObject.AddComponent<BJHelperTrigger>();
            // foreach (var col in FillMeUp.atom.rigidbodies.First(x => x.name == "Gen1").GetComponentsInChildren<Collider>(true))
            // {
            //     Physics.IgnoreCollision(trigger, col, false);
            // }
            mouthBlow = gameObject.AddComponent<LerpingMorph>().Init(ReadMyLips.packageUid+"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/BJ/BL_Mouth Blow.vmi");
            // mouthOpen = gameObject.AddComponent<LerpingMorph>().Init("Mouth Open Wide 2");
            mouthNarrow = gameObject.AddComponent<LerpingMorph>().Init(ReadMyLips.packageUid+"Custom/Atom/Person/Morphs/female/CheesyFX/BodyLanguage/BJ/BL_MouthNarrow.vmi");
            tongueOut = gameObject.AddComponent<LerpingMorph>().Init("Tongue In-Out");
            tongueUp = gameObject.AddComponent<LerpingMorph>().Init("Tongue Raise-Lower");

            enabledJ.setCallbackFunction += SetActive;
            enabled = false;
            initialized = true;
        }

        private void SetActive(bool val)
        {
            enabled = false;
            Throat.mouthOpen.target = mouthBlow.target = 0f;
            mouthNarrow.target = 0f;
            tongueOut.target = 1f;
            info.val = val? "":"Disabled";
        }

        private void OnEnable()
        {
            if (!initialized) return;
            distance.val = penetrators.Min(x => GetDistance(x.tip));
            if (!enabledJ.val)
            {
                enabled = false;
                return;
            }
            GetClosestPenetrator();
            info.val = $"{closest.atom.name}/{closest.tipCollider.name}";
        }

        private void OnDestroy()
        {
            // Destroy(triggerGO);
            Throat.mouthOpen.morphVal = 0f;
            mouthNarrow.morphVal = 0f;
        }

        private void OnDisable()
        {
            // "OnDisable".Print();
            distance.val = lastDistance = 0f;
            speed.val = 0f;
            if (FillMeUp.throat.isPenetrated)
            {
                info.val = enabledJ.val? $"Paused: Sucking {FillMeUp.throat.penetrator.atom.name}":"Disabled";
            }
            else
            {
                Throat.mouthOpen.target = mouthBlow.target = 0f;
                info.val = enabledJ.val? "":"Disabled";
            }
            mouthNarrow.target = 0f;
            tongueOut.target = 1f;
        }

        private void Update()
        {
            // distance.val = penetrators.Min(x => GetDistance(x.tip));
            GetClosestPenetrator();
            // info.val = closest.tipCollider.name;
            GetSpeed();
            lastDistance = distance.val;
            Throat.mouthOpen.quicknessIn = mouthNarrow.quicknessIn = mouthBlow.quicknessIn = tongueUp.quicknessIn = 500f * (distance.val-.1f) * (distance.val-.1f) * 50f*speed.val;
            Throat.mouthOpen.quicknessOut = mouthNarrow.quicknessOut = mouthBlow.quicknessOut = tongueUp.quicknessOut = tongueOut.quicknessIn = 1f + 4f * distance.val;
            tongueOut.quicknessOut = Throat.mouthOpen.quicknessIn * .25f;
            
            var target = closest.width - closest.width * 10f * (distance.val - .03f);
            if (speed.val > .005f)
            {
                Throat.mouthOpen.target = target;
                mouthNarrow.target = target * .7f;
                mouthBlow.target = Mathf.Lerp(.5f, 0f, distance.val*20f);
                tongueOut.target = -1f;
                tongueUp.target = .15f;
            }
            else
            {
                Throat.mouthOpen.target = 0f;
                mouthNarrow.target = 0f;
                mouthBlow.target = 0f;
                tongueOut.target = 1f;
                tongueUp.target = 0f;
            }
            
        }

        private void GetClosestPenetrator()
        {
            distance.val = 10f;
            float dist;
            foreach (var penetrator in penetrators)
            {
                dist = GetDistance(penetrator.tip);
                if (dist < distance.val)
                {
                    distance.val = dist;
                    closest = penetrator;
                }
            }
        }

        private float GetDistance(Transform point)
        {
            return Mathf.Max(0f, Vector3.Dot(throatTrigger.position - point.position, trigger.transform.up));
        }

        private void GetSpeed()
        {
            var newSpeed = (lastDistance - distance.val) / Time.fixedDeltaTime;
            speed.val = Mathf.Lerp(speed.val, newSpeed, 5f * Time.fixedDeltaTime);
        }
        
        public static void CreateUI(List<object> UIElements)
        {
            distance.CreateUI(UIElements);
            speed.CreateUI(UIElements, rightSide:true);
            var infoLine = Utils.SetupInfoOneLine(FillMeUp.singleton, info, false);
            infoLine.ForceHeight(50f);
            UIElements.Add(infoLine);
            enabledJ.CreateUI(UIElements);
            // openMouth.CreateUI(UIElements);
            // morphLips.CreateUI(UIElements);
            // morphTongue.CreateUI(UIElements);
            FillMeUp.singleton.CreateStaticInfo(
                "This feature drives facial reactions based on the distance and speed at which a penetrator is approaching the mouth. It is independent of the auto BJ feature of FillMeUp.",
                170f, UIElements, true);
        }
        
        public static void Offset(Vector3 v3)
        {
            // triggerGO.transform.localPosition = lipTrigger.InverseTransformDirection(triggerGO.transform.TransformDirection(v3 + new Vector3(0f, .04f, -.04f)));
            triggerGO.transform.localPosition = Quaternion.Euler(15f, 0f, 0f) * (v3 + new Vector3(0f, -.01f, .04f));
        }
    }
}