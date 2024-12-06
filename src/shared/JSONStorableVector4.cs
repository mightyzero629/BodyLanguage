using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace CheesyFX
{
    public class JSONStorableVector4 : MyJSONStorableVector3
    {
        private float _w;
        private float _defaultW;
        private float _minW;
        private float _maxW;

        public SetVector4Callback setCallbackFunction;
        public SetJSONVector4Callback setJSONCallbackFunction;
        
        protected new Vector4 _val {
            get{ return base._val.ToV4(_w);}
            set
            {
                base._val = value;
                _w = value[3];
            }
        }

        protected new Vector4 _defaultVal
        {
            get { return base._defaultVal.ToV4(_defaultW); }
            set
            {
                base._defaultVal = value;
                _defaultW = value[3];
            }
        }

        protected new Vector4 _min => base._min.ToV4(_minW);
        protected new Vector4 _max => base._max.ToV4(_maxW);
        protected Slider _sliderW;
        protected Slider _sliderWAlt;
        protected SliderControl _sliderWControl;
        protected SliderControl _sliderWControlAlt;

        public JSONStorableVector4(string label, Vector4 defaultValue, Vector4 minValue, Vector4 maxValue,
            SetVector4Callback callback = null) : base(label, defaultValue, minValue, maxValue)
        {
            _defaultW = defaultValue[3];
            _minW = minValue[3];
            _maxW = maxValue[3];
            val = defaultValue;
            setCallbackFunction = callback;
        }
        
        public new void SetValX(float f)
        {
            if (!sync) val = new Vector4(f, val.y, val.z, _w);
            else val = new Vector4(f, f, f, f);
        }
        public new void SetValY(float f)
        {
            if (!sync) val = new Vector4(val.x, f, val.z, _w);
            else val = new Vector4(f, f, f, f);
        }
        public new void SetValZ(float f)
        {
            if (!sync) val = new Vector4(val.x, val.y, f, _w);
            else val = new Vector4(f, f, f, f);
        }

        public void SetValW(float f)
        {
            if (!sync) val = new Vector4(val.x, val.y, val.z, f);
            else val = new Vector4(f, f, f, f);
        }
        
        public new Vector4 val
        {
            get { return _val;}
            set
            {
                InternalSetVal(value);
            }
        }
        
        public new Vector4 valNoCallback
        {
            set
            {
                InternalSetVal(value, false);
            }
        }
        
        public Vector4 min
        {
            get { return _min; }
            set
            {
                base.min = value;
                if (_minW == value[3]) return;
                _minW = value[3];
                if (_sliderW != null)
                    _sliderW.minValue = _minW;
                if (_sliderWAlt != null)
                    _sliderWAlt.minValue = _minW;
            }
        }
        
        public Vector4 max
        {
            get { return _max; }
            set
            {
                base.max = value;
                if (_maxW == value[3]) return;
                _maxW = value[3];
                if (_sliderW != null)
                    _sliderW.maxValue = _maxW;
                if (_sliderWAlt != null)
                    _sliderWAlt.maxValue = _maxW;
            }
        }

        public Vector4 defaultVal
        {
            get { return _defaultVal; }
            set
            {
                if (_defaultVal == value) return;
                _defaultVal = value;
                if (sliderW != null)
                {
                    SliderControl sliderControl = sliderW.GetComponent<SliderControl>();
                    if (sliderControl != null) sliderControl.defaultValue = _defaultW;
                }
            }
        }

        protected void InternalSetVal(Vector4 v, bool doCallback = true)
        {
            Vector3 oldVal = _val;
            base.InternalSetVal(v, false);
            float w = v[3];
            if (_constrained)
            {
                w = Mathf.Clamp(w, _minW, _maxW);
            }
            if (oldVal.ToV4(_w) == v) return;
            if(_w != w)
            {
                _w = w;
                bool flag1 = false;
                double m = min.w;
                if (min.w > _w)
                {
                    m = _w;
                    if (_sliderW != null && m > _sliderW.minValue)
                        m = _sliderW.minValue;
                }

                min = min.ToV3().ToV4((float)m);

                m = max.w;
                if (max.w < (double)_w)
                {
                    m = _w;
                    if (_sliderW != null && m < _sliderW.maxValue)
                        m = _sliderW.maxValue;
                    if (_sliderWAlt != null && m < _sliderWAlt.maxValue)
                        m = _sliderWAlt.maxValue;
                }

                max = max.ToV3().ToV4((float)m);

                if (_sliderW != null)
                    _sliderW.value = _w;
                if (_sliderWAlt != null)
                    _sliderWAlt.value = _w;
            }
            if (!doCallback)
                return;
            if (setCallbackFunction != null)
                setCallbackFunction(_val);
            if (setJSONCallbackFunction == null)
                return;
            setJSONCallbackFunction(this);
            
        }
        
        public void RegisterSliderW(Slider s, bool isAlt = false)
        {
            // if (isAlt)
            //     sliderWAlt = s;
            // else
                sliderW = s;
        }
        
        public Slider sliderW
        {
            get { return _sliderW; }
            set
            {
                if (_sliderW == value) return;
                if (_sliderW != null)
                {
                    _sliderW.interactable = true;
                    _sliderW.onValueChanged.RemoveListener(SetValW);
                }
                _sliderW = value;
                _sliderWControl = null;
                if (_sliderW == null) return;
                _sliderW.interactable = _interactable;
                _sliderWControl = _sliderW.GetComponent<SliderControl>();
                if (_sliderWControl != null)
                    _sliderWControl.defaultValue = _defaultW;
                _sliderW.minValue = _minW;
                _sliderW.maxValue = _maxW;
                _sliderW.value = _w;
                _sliderW.onValueChanged.AddListener(SetValW);
            }
        }

        public new bool constrained
        {
            get { return _constrained; }
            set
            {
                base.constrained = value;
                if(sliderW != null) sliderW.GetComponent<SliderControl>().clamp = value;
            }
        }

        public bool Store(JSONClass jc, bool forceStore = true)
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
                val = jc[name].AsArray.ToV4();
                return true;
            }
            return false;
        }
        
        public delegate void SetJSONVector4Callback(JSONStorableVector4 jf);

        public delegate void SetVector4Callback(Vector4 v);
    }
}