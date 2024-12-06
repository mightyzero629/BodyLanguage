using System;
using System.Collections;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CheesyFX
{
    public class SlapTarget
    {
        public bool hasTarget;
        public Atom targetAtom;
        public GameObject target;
        private Vector3 pos;
        private Quaternion rot;
        private Rigidbody hand;
        public Vector3 controllerPos;
        public Quaternion controllerRot = Quaternion.identity;
        public Transform transform => target.transform;
        public JSONStorableFloat intensityMultiplier = new JSONStorableFloat("Intensity Multiplier", 1f, 0f, 4f);
        public JSONStorableBool allowSlaps = new JSONStorableBool("Allow Slaps", true);
        public JSONStorableBool allowPushes = new JSONStorableBool("Allow Pushes", true);

        public SlapTarget(FreeControllerV3 handCtrl)
        {
            try
            {
                hand = handCtrl.followWhenOffRB;
                Transform t;
                if (!FireSphereRay(-2, out t))
                {
                    if (!FireSphereRay(2, out t))
                    {
                        if(!FireSphereRay(hand.name.Substring(0,1) == "l"? -1 : 1, out t))
                        {
                            SuperController.LogError("PoseMe: No target detected. Move the hand closer to a target");
                            return;
                        }
                    }
                }

                if (t == null)
                {
                    SuperController.LogError("Raycast: No target hit");
                    hasTarget = false;
                    return;
                }
                target = new GameObject("SlapTarget");
                target.transform.SetParent(t, false);
                target.transform.position = hand.transform.position;
                target.transform.rotation = hand.transform.rotation;
                pos = target.transform.localPosition;
                rot = target.transform.localRotation;
                targetAtom = t.gameObject.GetAtom();
                target.transform.Draw();
                controllerPos = target.transform.InverseTransformPoint(handCtrl.transform.position);
                controllerRot = Quaternion.Inverse(target.transform.rotation) * handCtrl.transform.rotation;
                hasTarget = true;
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        public SlapTarget(Rigidbody hand, JSONClass jc)
        {
            this.hand = hand;
            Load(jc);
        }

        public void Sync()
        {
            target.transform.localPosition = pos;
            target.transform.localRotation = rot;
        }

        private bool FireSphereRay(int axis, out Transform closest)
        {
            // axis.Print();
            var handChilds = hand.GetComponentsInChildren<Transform>();
            string side = hand.name.Substring(0, 1);
            var t = hand.transform.Find($"{side}Carpal1/{side}Mid1");
            int hits = Physics.SphereCastNonAlloc(t.position, .05f, Mathf.Sign(axis) * hand.transform.Axis(Mathf.Abs(axis) - 1), Slap.rayCastBuffer,.1f);
            float minDist = Mathf.Infinity;
            // $"hits {hits}".Print();
            closest = null;
            for (int i = 0; i < hits; i++)
            {
                var hit = Slap.rayCastBuffer[i];
                if(hit.rigidbody == null) continue;
                var transform = hit.transform;
                // transform.name.Print();
                if(transform.name.Contains("Control") || transform.name.Contains("Link")) continue;
                var dist = (transform.position - t.transform.position).sqrMagnitude;
                if (dist < minDist)
                {
                    if(handChilds.Contains(transform)) continue;
                    closest = transform;
                    minDist = dist;
                }
                // transform.name.Print();
            }
            // 
            return closest != null;
        }

        private bool FireRay(int axis, out Transform closest)
        {
            var handChilds = hand.GetComponentsInChildren<Transform>();
            string side = hand.name.Substring(0, 1);
            var t = hand.transform.Find($"{side}Carpal1/{side}Mid1");
            int hits = Physics.RaycastNonAlloc(t.position, Mathf.Sign(axis) * hand.transform.Axis(Mathf.Abs(axis) - 1), Slap.rayCastBuffer,.1f);
            float minDist = Mathf.Infinity;
            closest = null;
            for (int i = 0; i < hits; i++)
            {
                if(Slap.rayCastBuffer[i].rigidbody.name.Contains("Control")) continue;
                var dist = (Slap.rayCastBuffer[i].transform.position - t.transform.position).sqrMagnitude;
                if (dist < minDist)
                {
                    var transform = Slap.rayCastBuffer[i].transform;
                    if(handChilds.Contains(transform)) continue;
                    closest = transform;
                    minDist = dist;
                    
                }
            }
            return hits > 0;
        }
        
        public JSONClass Store()
        {
            var jc = new JSONClass();
            if (hasTarget)
            {
                jc["targetAtom"] = targetAtom.uid;
                jc["parent"] = target.transform.parent.name;
                jc["pos"] = target.transform.localPosition.ToJA();
                jc["rot"] = target.transform.localRotation.ToJA();
                jc["controllerPos"] = controllerPos.ToJA();
                jc["controllerRot"] = controllerRot.ToJA();
            }
            return jc;
        }

        public void Load(JSONClass jc)
        {
            if(jc.HasKey("targetAtom")) 
            {
                target = new GameObject("SlapTarget");
                targetAtom = SuperController.singleton.GetAtomByUid(jc["targetAtom"].Value);
                var parent = targetAtom.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(x => x.name == jc["parent"].Value);
                if (parent == null)
                {
                    // $"{jc["targetAtom"]} {jc["parent"]} {targetAtom.GetComponentsInChildren<Transform>(true).Length}".Print();
                    deferredTargetLoad = DeferredTargetLoad(jc).Start();
                    return;
                }
                transform.parent = parent;
                transform.localPosition = pos = jc["pos"].AsArray.ToV3();
                transform.localRotation = rot = jc["rot"].AsArray.ToQuat();
                if (jc.HasKey("controllerPos"))
                {
                    controllerPos = jc["controllerPos"].AsArray.ToV3();
                    controllerRot = jc["controllerRot"].AsArray.ToQuat();
                }
                hasTarget = true;
            }
        }

        private IEnumerator deferredTargetLoad;
        private IEnumerator DeferredTargetLoad(JSONClass jc)
        {
            var t = 0f;
            var parentName = jc["parent"].Value;
            while ((transform.parent = targetAtom.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(x => x.name == parentName)) == null)
            {
                t += Time.deltaTime;
                if (t > 60f)
                {
                    SuperController.LogError($"SlapTarget {targetAtom.uid}/{parentName} could not be restored.");
                    yield break;
                }
                yield return null;
            }
            transform.localPosition = pos = jc["pos"].AsArray.ToV3();
            transform.localRotation = rot = jc["rot"].AsArray.ToQuat();
            if (jc.HasKey("controllerPos"))
            {
                controllerPos = jc["controllerPos"].AsArray.ToV3();
                controllerRot = jc["controllerRot"].AsArray.ToQuat();
            }
            hasTarget = true;
            // parentName.Print();
            // t.Print();
        }

        public void Destroy()
        {
            Object.Destroy(target);
            deferredTargetLoad.Stop();
        }
    }
}