using System.Collections;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public static class Gagger
    {
        public static JSONStorableBool enabled = new JSONStorableBool("Gagging Enabled", true);
        public static JSONStorableFloat gagThreshold = new JSONStorableFloat("Gag Threshold", .12f, .03f, .2f);
        public static JSONStorableFloat gagScale = new JSONStorableFloat("Gag Scale", 1f, 0f, 5f);
        private static Rigidbody chest = FillMeUp.atom.rigidbodies.First(x => x.name == "chest");
        private static Rigidbody head = FillMeUp.atom.rigidbodies.First(x => x.name == "head");
        private static float timer;
        private static int amount;
        public static IEnumerator gag;
        public static bool shutDown;
        private static float headFactor;
        private static float gagTime;

        public static IEnumerator Gag()
        {
            timer = 1f;
            amount = 1;
            headFactor = 0f;
            gagTime = Random.Range(.1f, .75f);
            var gagStrength = Random.Range(.3f, 1f);
            FillMeUp.eyelidBehavior.blinkSpaceMax = 20f;
            FillMeUp.eyelidBehavior.blinkSpaceMin = 20f;
            FillMeUp.eyelidBehavior.blinkTimeMin = .75f;
            FillMeUp.eyelidBehavior.blinkTimeMax = 3f;
            FillMeUp.eyelidBehavior.blinkDownUpRatio = .1f;
            if (Random.Range(0f, 1f) > .15f) FillMeUp.eyelidBehavior.Blink();
            FillMeUp.throat.relaxation.val += .1f;
            while (timer > 0f || headFactor > .01f)
            {
                if(timer > 0f)
                {
                    headFactor = Mathf.Lerp(headFactor, timer, 6f * Time.fixedDeltaTime);
                    if (shutDown) timer -= 2f * Time.fixedDeltaTime;
                    else timer -= Time.fixedDeltaTime;
                    if(!SuperController.singleton.freezeAnimation) chest.AddTorque(50f * timer * gagStrength * chest.transform.right, ForceMode.Force);
                    // head.AddTorque(-15f * headFactor * head.transform.right, ForceMode.Force);
                    // head.AddForce(-150f * timer * head.transform.forward, ForceMode.Force);
                    if (!shutDown && amount < 4 && timer < gagTime)
                    {
                        if (Random.Range(0f, 1f) > .2f + amount*.1f)
                        {
                            timer = 1f;
                            gagTime = Random.Range(.1f, .5f-amount*.1f);
                            amount++;
                            if (Random.Range(0f, 1f) > .25f) FillMeUp.eyelidBehavior.Blink();
                            FillMeUp.throat.relaxation.val += .1f/amount;
                        }
                        else shutDown = true;
                    }
                }
                else
                {
                    headFactor = Mathf.Lerp(headFactor, 0f, 6f * Time.fixedDeltaTime);
                    // head.AddTorque(-15f * headFactor * head.transform.right, ForceMode.Force);
                    // head.AddForce(-150f * timer * head.transform.forward, ForceMode.Force);
                }
                if(!SuperController.singleton.freezeAnimation)
                {
                    head.AddTorque(-15f * headFactor * gagScale.val * gagStrength * head.transform.right,
                        ForceMode.Force);
                    head.AddForce(-150f * timer * gagScale.val * gagStrength * head.transform.forward, ForceMode.Force);
                }
                yield return new WaitForFixedUpdate();
            }
            gag = null;
            shutDown = false;
            RestoreBlink();
        }

        // public static void ShutDown()
        // {
        //     shutDown = true;
        // }

        public static void Update(float depth, float lastDepth, JSONStorableFloat relaxation)
        {
            if (!enabled.val) return;
            if (depth > gagThreshold.val && lastDepth <= gagThreshold.val && gag == null && Random.Range(0f, 1f) > relaxation.val)
            {
                gag = Gag().Start();
            }
            if (gag != null && depth < gagThreshold.val - .02f)
            {
                shutDown = true;
            }
        }

        public static void RestoreBlink()
        {
            FillMeUp.eyelidBehavior.blinkTimeMin = .25f;
            FillMeUp.eyelidBehavior.blinkTimeMax = .35f;
            FillMeUp.eyelidBehavior.blinkSpaceMax = 7f;
            FillMeUp.eyelidBehavior.blinkSpaceMin = 1f;
            FillMeUp.eyelidBehavior.blinkDownUpRatio = .4f;
        }
    }
}