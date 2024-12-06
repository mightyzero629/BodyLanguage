using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace CheesyFX
{
    public static class ForceToggle
    {
        public static bool disabled;
        public static List<Force> forces = new List<Force>();
        private static List<bool> forceStates = new List<bool>();
        private static bool[] magnetStates = {true, true, true, true, true, true};
        private static bool[] correctives = {true, true, true};

        public static JSONClass Store()
        {
            var jc = new JSONClass();
            jc["disabled"].AsBool = disabled;
            for (int i = 0; i < forces.Count; i++)
            {
                jc[forces[i].name].AsBool = forceStates[i];
            }
            for (int i = 0; i < 6; i++)
            {
                jc[FillMeUp.fuckables[i].name + " magnetic"].AsBool = magnetStates[i];
                if(i<3) jc[FillMeUp.fuckables[i].name + " corrective"].AsBool = correctives[i];
            }
            return jc;
        }

        public static void Load(JSONClass jc)
        {
            disabled = jc["disabled"].AsBool;
            for (int i = 0; i < forces.Count; i++)
            {
                forceStates[i] = jc[forces[i].name].AsBool;
            }
            for (int i = 0; i < 6; i++)
            {
                magnetStates[i] = jc[FillMeUp.fuckables[i].name + " magnetic"].AsBool;
                if(i<3) correctives[i] = jc[FillMeUp.fuckables[i].name + " corrective"].AsBool;
            }
        }

        public static void Toggle()
        {
            if (disabled)
            {
                Enable();
                EnableMagnets();
                disabled = false;
            }
            else
            {
                GetStates();
                Disable();
                DisableMagnets();
            }
        }

        public static void GetStates()
        {
            for (int i = 0; i < forces.Count; i++)
            {
                forceStates[i] = forces[i].enabledJ.val;
            }

            for (int i = 0; i < 3; i++)
            {
                magnetStates[i] = FillMeUp.orifices[i].magnetic.val;
                correctives[i] = FillMeUp.orifices[i].correctiveTorqueEnabled.val;
            }

            magnetStates[3] = FillMeUp.hands[0].magnetic.val;
            magnetStates[4] = FillMeUp.hands[1].magnetic.val;
            // Store().ToString().Print();
            disabled = true;
        }

        private static void Enable()
        {
            for (int i = 0; i < forces.Count; i++)
            {
                forces[i].enabledJ.val = forceStates[i];
            }
            for (int i = 0; i < 3; i++)
            {
                FillMeUp.orifices[i].correctiveTorqueEnabled.val = correctives[i];
            }
        }

        private static void Disable()
        {
            for (int i = 0; i < forces.Count; i++)
            {
                forces[i].enabledJ.val = false;
            }
            for (int i = 0; i < 3; i++)
            {
                FillMeUp.orifices[i].correctiveTorqueEnabled.val = false;
            }
        }

        public static void AddForce(Force force)
        {
            forces.Add(force);
            forceStates.Add(true);
        }

        public static void EnableMagnets()
        {
            for (int i = 0; i < 3; i++)
            {
                FillMeUp.orifices[i].magnetic.val = magnetStates[i];
            }

            FillMeUp.hands[0].magnetic.val = magnetStates[3];
            FillMeUp.hands[1].magnetic.val = magnetStates[4];
        }
        
        public static void DisableMagnets()
        {
            for (int i = 0; i < 3; i++)
            {
                FillMeUp.orifices[i].magnetic.val = false;
            }

            FillMeUp.hands[0].magnetic.val = false;
            FillMeUp.hands[1].magnetic.val = false;
        }
    }
}