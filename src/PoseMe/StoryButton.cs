using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheesyFX
{
    public class StoryButton
    {
        private StoryLevel level;
        private RectTransform rtLeft;
        private RectTransform rtRight;
        public RectTransform rtMiddle;
        private UIDynamicButton buttonMiddle;
        private UIDynamicButton buttonLeft;
        private UIDynamicButton buttonRight;
        private ClickListener clickListener;
        private static string[] symbols = {"‹","›","«","»", "‹«","»›"};
        private static string[] labels = {"C","P","L"};
        private int id;
        public Text label;
        private LayoutElement layout;

        public StoryButton(StoryLevel level, int i)
        {
            id = i;
            this.level = level;
            
            rtMiddle = Object.Instantiate(PoseMe.singleton.manager.configurableButtonPrefab, level.buttonPanel) as RectTransform;
            rtMiddle.name = $"m{i}";
            // Object.Destroy(rtMiddle.GetComponentInChildren<UIDynamicButton>());
            // Object.Destroy(rtMiddle.GetComponentInChildren<Button>());
            // rtLabel = new GameObject("StoryButtonLabel").AddComponent<RectTransform>();
            rtMiddle.SetParent(level.buttonPanel, false);
            rtMiddle.anchorMax = rtMiddle.anchorMin = Vector2.one;
            rtMiddle.pivot = new Vector2(1, 1);
            if (id > 0)
            {
                rtMiddle.anchorMax = new Vector2(.5f, 1f);
                rtMiddle.anchorMin = new Vector2(.5f, 1f);
                rtMiddle.pivot = new Vector2(.5f, .5f);
            }
            var csf = rtMiddle.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // rtLabel.offsetMax = rtLabel.offsetMin = Vector2.zero;
            rtMiddle.offsetMax = new Vector2(-125, -120 * i);
            rtMiddle.offsetMin = new Vector2(-155,-120 * (i+1));
            label = rtMiddle.gameObject.GetComponentInChildren<Text>();
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            layout = rtMiddle.GetComponent<LayoutElement>();
            // Object.Destroy(le);
            label.text = "Level\n0/10";//"<b>L</b>";
            label.fontSize = 40;
            layout.preferredWidth = label.preferredWidth;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            // var image = rtLabel.GetComponent<Image>();
            // var image = rtMiddle.gameObject.GetComponentInChildren<Image>();
            // image.color = new Color(1f, 1f, 1f, 0.3f);
            // image.color = Color.clear;
            buttonMiddle = rtMiddle.GetComponent<UIDynamicButton>();
            buttonMiddle.button.image.color = Color.clear;
            buttonMiddle.button.onClick.AddListener(PoseMe.TogglePoseButtons);
            
            rtRight = Object.Instantiate(PoseMe.singleton.manager.configurableButtonPrefab, rtMiddle, false) as RectTransform;
            rtRight.name = $"r{i}";
            rtRight.anchorMax = new Vector2(1f, 1f);
            rtRight.anchorMin = new Vector2(1f, .15f);
            rtRight.offsetMax = rtRight.offsetMin = Vector2.zero;
            var textRT = rtRight.Find("Text") as RectTransform;
            textRT.offsetMax = new Vector2(0f, 8);
            csf = textRT.gameObject.AddComponent<ContentSizeFitter>();
            textRT.pivot = new Vector2(0, .5f);
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            buttonRight = rtRight.GetComponent<UIDynamicButton>();
            buttonRight.label = "»";//‹ › » «
            buttonRight.buttonText.fontSize = 152;
            buttonRight.buttonText.color = Color.white;
            buttonRight.button.image.color = Color.clear;
            // buttonRight.button.image.color = new Color(1f, 1f, 1f, 0.5f);
            buttonRight.buttonText.alignment = TextAnchor.MiddleLeft;
            // buttonRight.button.onClick.AddListener(level.Next);
            
            rtLeft = Object.Instantiate(PoseMe.singleton.manager.configurableButtonPrefab, rtMiddle, false) as RectTransform;
            rtLeft.name = $"l{i}";
            rtLeft.anchorMax = new Vector2(0f, 1f);
            rtLeft.anchorMin = new Vector2(0f, .15f);
            rtLeft.offsetMax = rtLeft.offsetMin = Vector2.zero;
            textRT = rtLeft.Find("Text") as RectTransform;
            textRT.offsetMax = new Vector2(0f, 8);
            csf = textRT.gameObject.AddComponent<ContentSizeFitter>();
            textRT.pivot = new Vector2(1f, .5f);
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            buttonLeft = rtLeft.GetComponent<UIDynamicButton>();
            buttonLeft.label = "«";//‹ › » «
            buttonLeft.buttonText.fontSize = 152;
            buttonLeft.buttonText.color = Color.white;
            buttonLeft.button.image.color = Color.clear;
            // buttonLeft.button.image.color = new Color(1f, 1f, 1f, 0.3f);
            buttonLeft.buttonText.alignment = TextAnchor.MiddleRight;
            // buttonLeft.button.onClick.AddListener(level.Previous);

            SetCallbacks(id);
        }

        public void SetCallbacks(int id)
        {
            switch (id)
            {
                case 0:
                {
                    buttonLeft.button.onClick.AddListener(Story.Previous);
                    buttonRight.button.onClick.AddListener(Story.Next);
                    break;
                }
                case 1:
                {
                    buttonLeft.button.onClick.AddListener(level.Previous);
                    buttonRight.button.onClick.AddListener(level.Next);
                    break;
                }
                case 2:
                {
                    buttonLeft.button.onClick.AddListener(() => PoseMe.currentPose?.ApplyPreviousCamAngle());
                    buttonRight.button.onClick.AddListener(() => PoseMe.currentPose?.ApplyNextCamAngle());
                    break;
                }
            }
        }

        public string SyncText()
        {
            string newText = "";
            switch (id)
            {
                case 0:
                {
                    newText = label.text = $"{level.name.val}\n{Story.levels.IndexOf(level)}/{Story.levels.Count-1}";
                    // PoseMe.worldCanvas.SyncLevelNavText(0, label.text);
                    break;
                }
                case 1:
                {
                    newText = label.text = $"Pose\n{level.currentPoseId}/{level.poseCount-1}";
                    // PoseMe.worldCanvas.SyncLevelNavText(1, label.text);
                    break;
                }
                case 2:
                {
                    if(PoseMe.currentPose == null) newText = label.text = $"Cam\n-/-";
                    else if(PoseMe.currentPose.currentCam == null) newText = label.text = $"Cam\n-/{PoseMe.currentPose.camAngles.Count-1}";
                    else newText = label.text = $"Cam\n{PoseMe.currentPose.currentCam.id}/{PoseMe.currentPose.camAngles.Count-1}";
                    break;
                }
            }
            layout.preferredWidth = label.preferredWidth;
            return newText;
        }
    }
}