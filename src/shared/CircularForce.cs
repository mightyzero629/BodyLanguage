using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Leap;
using SimpleJSON;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class CircularForce : MonoBehaviour
    {
        private bool initialized;
        public JSONStorableBool enabledJ = new JSONStorableBool("Circular Force Enabled", false);

        private ForceParam radius;
        private ForceParam speed;
        private ForceParam excentricity;
        public ForceParam[] parameters;

        public JSONStorableStringChooser rotateAround = new JSONStorableStringChooser("Rotate Around",
            new List<string> { "X", "Y", "Z" }, "Z", "Rotate Around");

        public JSONStorableFloat flipChance = new JSONStorableFloat("Flip Chance", .5f, 0f, 1f, true);

        private Movement movement;
        public CircularForceParamControl paramControl; 
        
        private Vector3 force;
        private float angle;
        private bool shutDown;
        private ForceSync sync;
        protected StringBuilder sb = new StringBuilder();

        private Func<float, Vector3> GetForce;

        private List<object> UIElements = new List<object>();
        
        public JSONClass Store(bool forceStore = false)
        {
            var jc = paramControl.Store();
            enabledJ.Store(jc, forceStore);
            rotateAround.Store(jc, forceStore);
            flipChance.Store(jc, forceStore);
            return jc;
        }
        
        public void Load(JSONClass jc, bool setMissingToDefault = true)
        {
            enabledJ.Load(jc, setMissingToDefault);
            rotateAround.Load(jc, setMissingToDefault);
            flipChance.Load(jc, setMissingToDefault);
            paramControl.Load(jc, setMissingToDefault);
        }

        public CircularForce Init(Movement movement)
        {
            enabled = false;
            this.movement = movement;
            radius = new ForceParam("Radius", 50f, 0f);
            speed = new ForceParam("Speed", 1f, 0f);
            excentricity = new ForceParam("Excentricity", 1f, 0f);
            radius.mean.SetWithDefault(80f);
            radius.delta.SetWithDefault(40f);
            radius.mean.max = 200f;
            radius.delta.max = 200f;
            speed.mean.SetWithDefault(360f);
            speed.delta.SetWithDefault(40f);
            speed.mean.max = 1000f;
            speed.mean.min = -1000f;
            speed.delta.max = 1000f;
            speed.delta.min = -1000f;
            speed.transitionQuicknessMean.SetWithDefault(2f);
            speed.onGetNewTarget.AddListener(UpdateFlip);
            excentricity.mean.max = 1f;
            excentricity.mean.min = -1f;
            excentricity.mean.SetWithDefault(0f);
            excentricity.mean.constrained = true;
            excentricity.delta.max = 1f;
            excentricity.delta.min = -1f;
            excentricity.delta.SetWithDefault(.3f);
            excentricity.delta.constrained = true;
            parameters = new [] { radius, speed, excentricity };
            paramControl = new CircularForceParamControl(this);
            enabledJ.setCallbackFunction += val => enabled = val;
            rotateAround.AddCallback(SyncAxis);
            initialized = true;
            return this;
        }

        public void SyncAxis(string val)
        {
            var t = movement.isLocalSpace? movement.rb.transform : FillMeUp.atom.mainController.transform;
            if (val == "X") GetForce = angle =>
            {
                var v = Quaternion.AngleAxis(angle, t.right) * t.up;
                if (excentricity.current == 0f) return v;
                if (excentricity.current > 0f)
                    v = v.ScaleComponentsAlongUnit(t.up, t.forward, 1f, 1f-excentricity.current);
                else
                    v = v.ScaleComponentsAlongUnit(t.forward, t.up, 1f, 1f+excentricity.current);
                return v;
            };
            else if (val == "Y") GetForce = angle =>
            {
                var v = Quaternion.AngleAxis(angle, t.up) * t.forward;
                if (excentricity.current == 0f) return v;
                if (excentricity.current > 0f)
                    v = v.ScaleComponentsAlongUnit(t.forward, t.right, 1f, 1f-excentricity.current);
                else
                    v = v.ScaleComponentsAlongUnit(t.right, t.forward, 1f, 1f+excentricity.current);
                return v;
            };
            else if (val == "Z") GetForce = angle =>
            {
                var v = Quaternion.AngleAxis(angle, t.forward) * t.up;
                if (excentricity.current == 0f) return v;
                if (excentricity.current > 0f)
                    v = v.ScaleComponentsAlongUnit(t.up, t.right, 1f, 1f-excentricity.current);
                else
                    v = v.ScaleComponentsAlongUnit(t.right, t.up, 1f, 1f+excentricity.current);
                return v;
            };
        }

        private float maxAngle = 90f;
        private bool maxAngleExceeded;
        private void FixedUpdate()
        {
            UpdateParams();
            angle += Time.fixedDeltaTime * speed.current;
            // if (Mathf.Abs(angle) > maxAngle)
            // {
            //     if (!maxAngleExceeded)
            //     {
            //         maxAngleExceeded = true;
            //         Flip();
            //         $"{angle} {maxAngleExceeded}".Print();
            //     }
            // }
            // else if(maxAngleExceeded)
            // {
            //     maxAngleExceeded = false;
            //     $"{angle} {maxAngleExceeded}".Print();
            // }
            if (Mathf.Abs(angle) > 360f) angle = 0f;
            force = radius.current * GetForce(angle);
            if(!Pose.isApplying && !SuperController.singleton.freezeAnimation && !movement.atom.mainController.isGrabbing) movement.rb.AddForce(force);
            if (paramControl.UIOpen && (FillMeUp.singleton.UITransform.gameObject.activeSelf || PoseMe.singleton.UITransform.gameObject.activeSelf))
            {
                sb = sb.Clear(87);
                    // .AppendLine($"<b>{movement.rb.name}</b>" + (sync != null && sync.enabled && sync.driver
                    //     ? $" (<color=#ff0000>Synced</color> to {sync.driver.name})"
                    //     : " (Master)"));
                for (int i = 0; i < parameters.Length; i++)
                {
                    sb.Append($"{parameters[i].name} ").AppendFormat("{0:F1}", parameters[i].current).Append("\n");
                }

                // sb.Append("Angle ").AppendFormat("{0:F1}", angle);
                paramControl.info.val = sb.ToString();
            }
        }
        
        public void UpdateParams()
        {
            if(!shutDown) radius.Update();
            else radius.LerpToTarget();
            speed.Update();
            excentricity.Update();
            if(excentricity.target > 1f) excentricity.target = 1f;
            else if (excentricity.target < -1f) excentricity.target = -1f;
        }

        private void UpdateFlip()
        {
            if(flipChance.val == 0f || (flipChance.val < 1f && Random.Range(0f, 1f) > flipChance.val)) return;
            Flip();
        }

        private void Flip()
        {
            // flip *= -1f;
            // speed.target *= flip;
            speed.target = -speed.target;
            speed.atTarget = false;
        }

       public void Enable()
        {
            if(!initialized || !enabledJ.val) return;
            shutDown = false;
            radius.GetNewTarget();
            enabled = true;
            // for (var index = 0; index < forceGroups.Count; index++)
            // {
            //     forceGroups[index].MarkEnabled(this);
            // }
        }
        
        public void SetActive(bool val)
        {
            if(val) Enable();
            else ShutDown(4f);
        }
        
        public void SetActiveImmediate(bool val)
        {
            if(val) Enable();
            else ShutDownImmediate();
        }
        
        public void ShutDown(float quickness = 0f)
        {
            if (!enabled) return;
            shutDown = true;
            radius.target = 0f;
            if (radius.atTarget) enabled = false;
            // for (var index = 0; index < forceGroups.Count; index++)
            // {
            //     forceGroups[index].MarkDisabled(this);
            // }
        }
        
        public void ShutDownImmediate()
        {
            if (!enabled) return;
            radius.target = radius.current = 0f;
            shutDown = false;
            enabled = false;
            // for (var index = 0; index < forceGroups.Count; index++)
            // {
            //     forceGroups[index].MarkDisabled(this);
            // }
        }

        public void CreateUI()
        {
            UIElements.Clear();
            PoseMe.singleton.ClearUI();
            var button = PoseMe.singleton.CreateButton("Return");
            button.buttonColor = PoseMe.navColor;
            button.button.onClick.AddListener(movement.CreateUI);
            enabledJ.CreateUI(UIElements);
            paramControl.CreateUI(PoseMe.singleton);
        }
    }
}