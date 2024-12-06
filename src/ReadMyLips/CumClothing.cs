using System.Collections;
using GPUTools.Cloth.Scripts;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace CheesyFX
{
    public class CumClothing
    {
        private Person person;
        private DAZClothingItem item;
        private ClothSimControl simControl;
        private JSONStorableVector3 force;
        private JSONStorableFloat stiffness;
        public JSONStorableFloat detachTreshold;
        // private JSONStorableFloat friction;
        // private ClothSettings clothSettings;
        private JSONStorableFloat alphaJ;
        public bool isPermament;
        public IEnumerator remove;
        public bool active => item.active;
        
        private static WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
        public static WaitForSeconds waitForFadeOut = new WaitForSeconds(Person.clothingFadeTime.val);

        private bool isShooting;
        private float alpha;

        public CumClothing(DAZClothingItem item, Person person)
        {
            this.item = item;
            this.person = person;
            simControl = item.GetComponentInChildren<ClothSimControl>(true);
            // item.uid.Print();
            if(simControl == null) DeferredInit().Start();
            else Init();
            // male.dcs.SetActiveClothingItem(item, true, true);
            this.item.PostLoadJSONRestore();
        }

        private void Init()
        {
            person.dcs.SetActiveClothingItem(item, false);
            simControl.simEnabledJSON.val = true;
            force = simControl.GetVector3JSONParam("force");
            stiffness = simControl.GetFloatJSONParam("stiffness");
            detachTreshold = simControl.GetFloatJSONParam("detachThreshold");
            simControl.clothSettings.Stiffness = stiffness.defaultVal;
            // detachTreshold.val = .001f;
            // simControl.clothSettings.BreakThreshold
            force.val = Vector3.zero;
            // force = simControl.GetVector3JSONParam("friction");
            // clothSettings = simControl.clothSettings;
            // simControl.containingAtom.GetStorableIDs().ForEach(x => x.Print());
            // simControl.containingAtom.GetStorableByID(item.internalUid+"Material").NullCheck();
            var matOptions = item.GetComponentInChildren<MaterialOptions>(true);
            // matOptions.GetFloatParamNames().ForEach(x => x.Print());
            alphaJ = matOptions.GetFloatJSONParam("Alpha Adjust");
            alphaJ.val = -1f;
            alpha = alphaJ.defaultVal;
            simControl.clothSettings.Friction = 1f;
            if (item.internalUid.EndsWith("cum string 4") || item.internalUid.EndsWith("CumString04_new")) isPermament = true;
            Person.SyncClothSetting(this);
        }

        private IEnumerator DeferredInit()
        {
            while (simControl == null)
            {
                person.dcs.SetActiveClothingItem(item, true, false);
                simControl = item.GetComponentInChildren<ClothSimControl>(true);
                yield return null;
            }
            Init();
        }

        public void ShotStart(float forceFactor)
        {
            if(isShooting) return;
            isShooting = true;
            alphaJ.val = -1f;
            force.val = Quaternion.Euler(0f, Random.Range(-5f, 5f) ,Random.Range(-5f, 5f)) * person.penetrator.tip.forward * forceFactor;
            alpha = Random.Range(-.1f, alphaJ.defaultVal);
            // alpha.Print();
            // alpha = -1f;
            simControl.clothSettings.Stiffness = Random.Range(.01f, stiffness.defaultVal);
            detachTreshold.val = Random.Range(0f, .03f);
            person.dcs.SetActiveClothingItem(item, true);
            simControl.Reset();
            // simControl.clothSettings.Friction = 0f;
            
            // if(!isPermament) remove.Stop();
        }

        public void ShotEnd()
        {
            force.val = Vector3.zero;
            isShooting = false;
            if(!isPermament) remove = Remove().Start();
        }

        public void BlendAlpha(float val)
        {
            alphaJ.val = Mathf.Lerp(-1f, alpha, val);
        }

        private IEnumerator Remove()
        {
            yield return waitForFadeOut;
            float timer = 0f;
            while (timer < 5f)
            {
                alphaJ.val = Mathf.Lerp(alpha, -1f, timer*.2f);
                timer += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            alphaJ.val = -1;
            // simControl.simEnabledJSON.val = false;
            person.dcs.SetActiveClothingItem(item, false);
        }

        public void Reset()
        {
            person.dcs.SetActiveClothingItem(item, false);
            remove.Stop();
        }
    }
}