using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace CheesyFX
{
    public class MyJSONStorableVector3 : JSONStorableVector3
    {
        public bool sync { get; set; }
        // public UIDynamicV3Slider uid;
        public MyJSONStorableVector3(string label, Vector3 defaultValue, Vector3 minValue, Vector3 maxValue,
            SetVector3Callback callback = null) : base(label, defaultValue, callback, minValue, maxValue)
        {
            _constrained = false;
        }

        public void SetValX(float f)
        {
            if (!sync) val = new Vector3(f, val.y, val.z);
            else val = new Vector3(f, f, f);
        }
        public void SetValY(float f)
        {
            if (!sync) val = new Vector3(val.x, f, val.z);
            else val = new Vector3(f, f, f);
        }
        public void SetValZ(float f)
        {
            if (!sync) val = new Vector3(val.x, val.y, f);
            else val = new Vector3(f, f, f);
        }

        public new bool constrained
        {
            get { return _constrained; }
            set
            {
                if (_constrained == value) return;
                _constrained = value;
                Slider[] sliders = { sliderX, sliderY, sliderZ };
                for (int i = 0; i < 3; i++)
                {
                    if(sliders[i] != null) sliders[i].GetComponent<SliderControl>().clamp = value;
                }
            }
        }

        public Vector3 valAndDefault
        {
            set { val = defaultVal = value; }
        }
        
        public bool Store(JSONClass jc, bool forceStore = false)
        {
            if (_val != _defaultVal || forceStore)
            {
                jc[name] = _val.ToJA();
                return true;
            }
            return false;
        }

        public bool Load(JSONClass jc)
        {
            if (jc.HasKey(name))
            {
                valNoCallback = jc[name].AsArray.ToV3();
                return true;
            }
            return false;
        }
    }
}