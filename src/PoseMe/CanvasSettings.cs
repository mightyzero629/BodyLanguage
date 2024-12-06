using SimpleJSON;

namespace CheesyFX
{
    public class CanvasSettings
    {
        public static bool allowUpdates = true;
        protected string type;
        public JSONStorableFloat maxRows = new JSONStorableFloat("MaxRows", 20f, 1f, 100f, false);
        public JSONStorableFloat buttonSize = new JSONStorableFloat("Button Size", 200f, 0f, 1000f, false);
        public JSONStorableFloat buttonSpacing = new JSONStorableFloat("Button Spacing", .005f, 0f, 1000f, false);
        protected JSONStorableFloat buttonTransparency = new JSONStorableFloat("Button Transparency", 0.5f, 0f, 1f);
        
        public CanvasSettings(string type)
        {
            this.type = type;
        }
        
        public virtual void Update()
        {
            if(!allowUpdates) return;
            maxRows.min = PoseMe.maxRows.min;
            maxRows.max = PoseMe.maxRows.max;
            maxRows.val = PoseMe.maxRows.val;
            buttonSize.val = PoseMe.buttonSizeJ.val;
            buttonSpacing.val = PoseMe.buttonSpacing.val;
            buttonTransparency.val = PoseMe.buttonTransparency.val;
        }

        public void Apply()
        {
            PoseMe.maxRows.min = maxRows.min;
            PoseMe.maxRows.max = maxRows.max;
            PoseMe.maxRows.val = maxRows.val;
            PoseMe.buttonSizeJ.val = buttonSize.val;
            PoseMe.buttonSpacing.val = buttonSpacing.val;
            PoseMe.buttonTransparency.val = buttonTransparency.val;
        }

        public virtual void Store(JSONClass parent)
        {
            JSONClass jc = new JSONClass();
            maxRows.StoreWithMinMax(jc);
            buttonSize.Store(jc, false);
            buttonSpacing.Store(jc, false);
            buttonTransparency.Store(jc, false);
            parent[type] = jc;
        }
        
        public virtual void Load(JSONClass parent)
        {
            if(!parent.HasKey(type)) return;
            var jc = parent[type].AsObject;
            maxRows.LoadWithMinMax(jc);
            buttonSize.Load(jc, true);
            buttonSpacing.Load(jc, true);
            buttonTransparency.Load(jc, true);
        }
    }
}