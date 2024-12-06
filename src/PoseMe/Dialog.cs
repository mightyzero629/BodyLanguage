using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HelloMeow;
using MacGruber;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using UnityThreading;

namespace CheesyFX
{
    public class Dialog
    {
        private SpeechBubbleControl sbc;
        private AudioSourceControl headAudioControl;
        public Person person;
        public bool onEnter = true;
        public JSONStorableString text = new JSONStorableString("Text", "");
        public JSONStorableStringChooser personChooser = new JSONStorableStringChooser("Person",
            PoseMe.persons.Select(x => x.atom.uid).ToList(), PoseMe.atom.uid, "Person");
        public JSONStorableStringChooser occurrenceChooser = new JSONStorableStringChooser("Occurrence", 
            new List<string> { "OnEnter", "OnExit" }, "OnEnter", "Occurence");
        public JSONStorableBool isThinking = new JSONStorableBool("Thinking", false);
        public JSONStorableFloat lifeTime = new JSONStorableFloat("Life Time", 5f, .1f, 20f);
        public JSONStorableFloat delayMean = new JSONStorableFloat("Delay Mean", 1f, 0f, 20f, false);
        public JSONStorableFloat delayDelta = new JSONStorableFloat("Delay Delta", 0f, 0f, 20f);
        public JSONStorableBool delayOneSided = new JSONStorableBool("Delay OneSided", false);
        public JSONStorableBool playOnce = new JSONStorableBool("Play Only Once", false);
        public JSONStorableBool showBubble = new JSONStorableBool("Show Bubble", true);

        public JSONStorableBool isCamDialog = new JSONStorableBool("IsCamDialog", false);
        public bool isPoolDialog;
        public bool hasBeenPlayed;
        public static bool configureUIOpen;
        public UIDynamicBubbleItem uidItem;

        private UIDynamicButton addAudioButton;
        private UIDynamicButton playAudioButton;
        private UIDynamicButton stopAudioButton;
        private UIDynamicButton removeAudioButton;

        private List<object> UIElements = new List<object>();
        private static WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
        
        private static JSONStorableUrl loadURL = new JSONStorableUrl("loadURL", "Custom/Sounds", (string val) => { }, "wav|mp3", "Custom/Sounds/",true);

        private JSONStorableColor maxStimColor;
        
        public EventTrigger onDialogEnter;

        public static JSONClass cache;
        // public EventTrigger onDialogExit;

        public static List<Dialog> dialogs = new List<Dialog>();

        public static void InitLoadURL()
        {
            loadURL.hideExtension = false;
            loadURL.allowFullComputerBrowse = true;
            loadURL.allowBrowseAboveSuggestedPath = true;
            loadURL.SetFilePath("Custom/Sounds/");
        }
        
        public Dialog(bool isCamDialog = false, bool isPoolDialog = false)
        {
            SyncPerson();
            personChooser.setCallbackFunction += val => SyncPerson();
            occurrenceChooser.setCallbackFunction += val => onEnter = val == "OnEnter";
            isThinking.setCallbackFunction += val => SyncPerson();
            text.interactable = true;
            this.isCamDialog.val = isCamDialog;
            this.isPoolDialog = isPoolDialog;
            onDialogEnter = new EventTrigger(PoseMe.singleton, "On Dialog Enter");
            // onDialogExit = new EventTrigger(PoseMe.singleton, "On Dialog Exit");
            base64Clip.setCallbackFunction += LoadAudioFromBase64;
            dialogs.Add(this);
        }
        
        public Dialog(JSONClass jc)
        {
            personChooser.setCallbackFunction += val => SyncPerson();
            occurrenceChooser.setCallbackFunction += val => onEnter = val == "OnEnter";
            text.interactable = true;
            base64Clip.setCallbackFunction += LoadAudioFromBase64;
            onDialogEnter = new EventTrigger(PoseMe.singleton, "On Dialog Enter");
            // onDialogExit = new EventTrigger(PoseMe.singleton, "On Dialog Exit");
            Load(jc);
            SyncPerson();
            dialogs.Add(this);
        }

        public void SyncPerson()
        {
            person = FillMeUp.persons.FirstOrDefault(x => x.atom.uid == personChooser.val);
            if (person == null) return;
            if(isThinking.val) sbc = person.thoughtControl;
            else sbc = person.speechControl;
            headAudioControl = (AudioSourceControl)person.atom.GetStorableByID("HeadAudioSource");
            var newMaxStimColor = person.characterListener.gender == DAZCharacterSelector.Gender.Female
                ? ReadMyLips.femaleStimColor
                : ReadMyLips.maleStimColor;
            if (newMaxStimColor != maxStimColor)
            {
                if(configureUIOpen)
                {
                    PoseMe.singleton.RemoveUIElement(maxStimColor);
                    newMaxStimColor.CreateUI(UIElements);
                }
                maxStimColor = newMaxStimColor;
            }
        }

        public string GetTypeInfo()
        {
            var type = isThinking.val? "thinking" : "saying";
            return $"<b>{person.uid} {type}:</b>";
        }

        public void OnPersonRenamed(string oldUid, string newUid)
        {
            personChooser.SetChoices(PoseMe.persons.Select(x => x.atom.uid));
            if (personChooser.val == oldUid) personChooser.valNoCallback = newUid;
            if(uidItem != null) uidItem.configureButtonLabel.text = GetTypeInfo();
            onDialogEnter.SyncAtomNames();
        }

        public void CreateUIItem()
        {
            PoseMe.CreateDialogUIItem(this);
        }

        public void CreateConfigureUI()
        {
            PoseMe.singleton.ClearUI();
            UIElements.Clear();
            var button = PoseMe.singleton.CreateButton("Return");
            button.buttonColor = PoseMe.navColor;
            if(isPoolDialog) button.button.onClick.AddListener(CloseConfigureUIPool);
            else button.button.onClick.AddListener(CloseConfigureUI);
            UIElements.Add(button);
            isThinking.CreateUI(UIElements, true);
            personChooser.CreateUI(UIElements);
            occurrenceChooser.CreateUI(UIElements, true);
            delayMean.CreateUI(UIElements);
            delayDelta.CreateUI(UIElements, true);
            lifeTime.CreateUI(UIElements);
            delayOneSided.CreateUI(UIElements);
            playOnce.CreateUI(UIElements);
            showBubble.CreateUI(UIElements);
            var tf = PoseMe.singleton.CreateTextField(text, true);
            var inputField = tf.gameObject.AddComponent<InputField>();
            inputField.textComponent = tf.UItext;
            text.inputField = inputField;
            tf.ForceHeight(120f);
            PoseMe.singleton.CreateTextField(clipName, true).ForceHeight(115f);

            loadURL.setCallbackFunction = LoadAudioFromFile;
            addAudioButton = PoseMe.singleton.SetupButton("Add Audio", true);
            loadURL.RegisterFileBrowseButton(addAudioButton.button);
            playAudioButton = PoseMe.singleton.SetupButton("Play Audio", true,() => PlayAudio(true));
            stopAudioButton = PoseMe.singleton.SetupButton("Stop Audio", true, StopAudio);
            removeAudioButton = PoseMe.singleton.SetupButton("Remove Audio",RemoveAudio, PoseMe.severeWarningColor,rightSide:true);
            SyncAudioButtons();
            button = PoseMe.singleton.SetupButton("On Dialog Enter", true, onDialogEnter.OpenPanel);
            button.buttonColor = new Color(0.45f, 1f, 0.45f);
            if(person.characterListener.gender == DAZCharacterSelector.Gender.Female) ReadMyLips.femaleStimColor.CreateUI(UIElements);
            else ReadMyLips.maleStimColor.CreateUI(UIElements);
            PoseMe.dialogColorGain.CreateUI(UIElements);
            PoseMe.singleton.SetupButton("Copy", true, StoreToCache);
            PoseMe.singleton.SetupButton("Paste", true, () => Load(cache));
            configureUIOpen = true;
        }

        private void SyncAudioButtons()
        {
            if(addAudioButton == null) return;
            if (nClip == null)
            {
                addAudioButton.label = "Add Audio";
                playAudioButton.label = "Play Audio";
                stopAudioButton.label = "Stop Audio";
                removeAudioButton.label = "Remove Audio";
                playAudioButton.button.interactable = false;
                stopAudioButton.button.interactable = false;
                removeAudioButton.button.interactable = false;
            }
            else
            {
                addAudioButton.label = "♫ Update Audio";
                playAudioButton.label = "♫ Play Audio";
                stopAudioButton.label = "♫ Stop Audio";
                removeAudioButton.label = "♫ Remove Audio";
                playAudioButton.button.interactable = true;
                stopAudioButton.button.interactable = true;
                removeAudioButton.button.interactable = true;
            }
        }

        // private AudioClip clip;
        private JSONStorableString clipName = new JSONStorableString("clipName", "");
        private JSONStorableString base64Clip = new JSONStorableString("clip", "");
        public NamedAudioClip nClip;
        private static bool importingOGG;
        private void LoadAudioFromFile(string path)
        {
            var bytes = FileManagerSecure.ReadAllBytes(path);
            clipName.val = path.Substring(path.LastIndexOf("/")+1);
            base64Clip.valNoCallback = Convert.ToBase64String(bytes);
            AudioClip clip;
            if(path.EndsWith(".wav")) clip = NAudioPlayer.AudioClipFromWAV(new WAV(bytes));
            else if(path.EndsWith("mp3")) clip = NAudioPlayer.AudioClipFromMp3Data(bytes);
            else
            {
                if (importingOGG)
                {
                    SuperController.LogError("Import process already running. Try again later.");
                    return;
                }
                ImportOGG(path).Start();
                return;
            }
            nClip = new NamedAudioClip {sourceClip = clip};
            SyncAudioButtons();
            PlayAudio(true);
            
        }

        private IEnumerator ImportOGG(string path)
        {
            importingOGG = true;
            var bi = SuperController.singleton.GetComponentInChildren<BassImporter>();
            bi.Import(path);
            while (!bi.isLoaded && !bi.isError)
            {
                // bi.progress.Print();
                yield return null;
            }

            if (!bi.isError)
            {
                nClip = new NamedAudioClip {sourceClip = bi.audioClip};
                SyncAudioButtons();
                PlayAudio(true);
            }
            else SuperController.LogError("Error during ogg import " + bi.error);
            importingOGG = false;
        }

        private void LoadAudioFromBase64(string val)
        {
            if(val == "") return;
            var bytes = Convert.FromBase64String(val);
            AudioClip clip = null;
            if(clipName.val == "")
            {
                try
                {
                    clip = NAudioPlayer.AudioClipFromWAV(new WAV(bytes));
                }
                catch (Exception e)
                {
                    clip = NAudioPlayer.AudioClipFromMp3Data(bytes);
                }
            }
            else
            {
                var extension = clipName.val.Substring(clipName.val.LastIndexOf(".") + 1);
                if (extension == "wav")
                {
                    clip = NAudioPlayer.AudioClipFromWAV(new WAV(bytes));
                }
                else if(extension == "mp3")
                {
                    clip = NAudioPlayer.AudioClipFromMp3Data(bytes);
                }
                else if(extension == "ogg")
                {
                    clip = NAudioPlayer.AudioClipFromWAV(new WAV(bytes));
                }
            }
            nClip = new NamedAudioClip
            {
                sourceClip = clip
            };
            SyncAudioButtons();
        }

        private void RemoveAudio()
        {
            nClip = null;
            base64Clip.val = "";
            SyncAudioButtons();
        }

        private void PlayAudio(bool immediate = false)
        {
            if (nClip == null) return;
            if(immediate) headAudioControl.PlayNow(nClip);
            else headAudioControl.PlayNextClearQueue(nClip);
        }
        
        private void StopAudio()
        {
            if (headAudioControl.playingClip != nClip) return;
            headAudioControl.Stop();
        }

        public static void CloseConfigureUI()
        {
            // if(!PoseMe.singleton.UITransform.gameObject.activeSelf) return;
            configureUIOpen = false;
            PoseMe.singleton.ClearUI();
            if (PoseMe.singleton.UITransform.gameObject.activeSelf) PoseMe.singleton.CreateUI();
            else PoseMe.needsUIRefresh = true;
        }
        
        public static void CloseConfigureUIPool()
        {
            configureUIOpen = false;
            PoseMe.singleton.ClearUI();
            DialogPool.CreateUI();
        }

        public void Invoke()
        {
            if (person == null || !person.atom.on || (playOnce.val && hasBeenPlayed)) return;
            if(playOnce.val) hasBeenPlayed = true;
            if (delayMean.val == 0f && delayDelta.val == 0f)
            {
                if (showBubble.val && text.val != "")
                {
                    sbc.UpdateText(text.val, lifeTime.val);
                    if(maxStimColor.val.ToRGB() != Color.white) person.speechRoutine = SyncBubbleColor().Start();
                }
                PlayAudio();
                if(!PoseMe.ignoreTriggers.val) onDialogEnter.Trigger();
            }
            else
            {
                float del = NormalDistribution.GetValue(delayMean.val, delayDelta.val, onesided: delayOneSided.val);
                if(isThinking.val) person.thoughtRoutine = InvokeCo(del).Start();
                else person.speechRoutine = InvokeCo(del).Start();
            }
        }

        private IEnumerator InvokeCo(float delay)
        {
            yield return new WaitForSeconds(delay);
            PlayAudio();
            if (showBubble.val && text.val != "")
            {
                sbc.UpdateText(text.val, lifeTime.val);
                if(!PoseMe.ignoreTriggers.val) onDialogEnter.Trigger();
                if(maxStimColor.val.ToRGB() == Color.white) yield break;
                var timer = lifeTime.val;
                float colorSpeed = PoseMe.dialogColorGain.val;
                if (person.atom != PoseMe.atom) colorSpeed *= .25f;
                while (timer > 0f)
                {
                    timer -= Time.fixedDeltaTime;
                    sbc.bubbleImage.color = Color.Lerp(Color.white, maxStimColor.val.ToRGB(), colorSpeed*(person.dynamicStimGain - person.stimGain));
                    yield return waitForFixedUpdate;
                }
            }
        }

        private IEnumerator SyncBubbleColor()
        {
            var timer = lifeTime.val;
            float colorSpeed = PoseMe.dialogColorGain.val;
            if (person.atom != PoseMe.atom) colorSpeed *= .25f;
            while (timer > 0f)
            {
                timer -= Time.fixedDeltaTime;
                sbc.bubbleImage.color = Color.Lerp(Color.white, maxStimColor.val.ToRGB(), colorSpeed*(person.dynamicStimGain - person.stimGain));
                yield return waitForFixedUpdate;
            }
        }

        public void StoreToCache()
        {
            cache = Store();
            "Dialog stored to cache.".Print();
        }

        public JSONClass Store()
        {
            var jc = new JSONClass();
            personChooser.Store(jc);
            text.Store(jc);
            occurrenceChooser.Store(jc);
            isThinking.Store(jc);
            lifeTime.Store(jc);
            delayMean.Store(jc);
            delayDelta.Store(jc);
            delayOneSided.Store(jc);
            playOnce.Store(jc);
            isCamDialog.Store(jc);
            clipName.Store(jc);
            base64Clip.Store(jc);
            jc[onDialogEnter.Name] = onDialogEnter.GetJSON(PoseMe.singleton.subScenePrefix);
            return jc;
        }

        public void Load(JSONClass jc)
        {
            personChooser.Load(jc);
            text.Load(jc);
            occurrenceChooser.Load(jc);
            isThinking.Load(jc);
            lifeTime.Load(jc);
            delayMean.Load(jc);
            delayDelta.Load(jc);
            delayOneSided.Load(jc);
            playOnce.Load(jc);
            isCamDialog.Load(jc);
            clipName.Load(jc);
            base64Clip.Load(jc);
            onDialogEnter.RestoreFromJSON(jc, PoseMe.singleton.subScenePrefix, false, true);
        }

        public void RegisterUid(UIDynamicBubbleItem uid)
        {
            uid.speech.text = text.val;
            text.inputField = uid.inputField;
            SyncUid(uid);
            uid.deleteButton.onClick.AddListener(() =>
            {
                dialogs.Remove(this);
                if (isPoolDialog)
                {
                    DialogPool.RemoveDialog(this);
                }
                else
                {
                    if (!isCamDialog.val)
                    {
                        PoseMe.currentPose.dialogs.Remove(this);
                        PoseMe.singleton.RemoveUIElement(uid);
                    }
                    else
                    {
                        PoseMe.currentPose.currentCam.dialogs.Remove(this);
                        PoseMe.singleton.RemoveUIElement(uid);
                    }
                }
            });
            uid.configureButton.onClick.AddListener(CreateConfigureUI);
            uid.copyButton.onClick.AddListener(StoreToCache);
            uid.personButton.onClick.AddListener(() =>
            {
                personChooser.val =
                    personChooser.choices[
                        (personChooser.choices.IndexOf(personChooser.val) + 1) % personChooser.choices.Count];
                SyncUid(uid);
            });
            uid.typeButton.onClick.AddListener(() =>
            {
                isThinking.val = !isThinking.val;
                SyncUid(uid);
            });
            uid.occurenceButton.onClick.AddListener(() =>
            {
                occurrenceChooser.val = onEnter ? "OnExit" : "OnEnter";
                SyncUid(uid);
            });
            uid.increaseDelayButton.onClick.AddListener(() =>
            {
                delayMean.val += 1f;
                SyncUid(uid);
            });
            uid.decreaseDelayButton.onClick.AddListener(() =>
            {
                if(delayMean.val >= 1f) delayMean.val -= 1f;
                SyncUid(uid);
            });
            uidItem = uid;
        }
        
        public void SyncUid(UIDynamicBubbleItem uid)
        {
            string type;
            if (isThinking.val)
            {
                type = "thinking";
                uid.typeText.text = "<b>T</b>";
            }
            else
            {
                type = "saying";
                uid.typeText.text = "<b>S</b>";
            }
            var notes = nClip == null ? "" : "♫ ";
            uid.configureButtonLabel.text = $"<b>{notes}{personChooser.val} {type}:</b>";
            uid.occurenceButton.GetComponentInChildren<Text>().text = onEnter? "»":"«";
            uid.delayInfo.text = delayMean.val.ToString();
            uid.configureButton.GetComponent<Image>().color = person.characterListener.gender == DAZCharacterSelector.Gender.Male?
                ReadMyLips.maleStimColor.val.ToRGB():ReadMyLips.femaleStimColor.val.ToRGB();
        }
    }
}