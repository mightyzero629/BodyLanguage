using System.Collections.Generic;
using System.Linq;
using MacGruber;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace CheesyFX
{
    public class ActorMapping
    {
        public string storedUid;
        private JSONClass actorPose;
        public bool insane;
        public JSONStorableString mapping;
        private Color color = Color.white;
        private bool isContainingAtom;
        

        public ActorMapping(string storedUid, JSONClass actorPose)
        {
            this.storedUid = storedUid;
            this.actorPose = actorPose;
            
            mapping = new JSONStorableString("", MapName(), val => SanityCheck(val, true));
            CreateUI();
        }

        public void CreateUI()
        {
            var line = Utils.SetupInfoOneLine(PoseMe.singleton, storedUid, false);
            line.ForceHeight(50f);
            line.text.alignment = TextAnchor.MiddleLeft;
            PoseMe.UIElements.Add(line);
            var input = Utils.SetupTextInput(PoseMe.singleton, ">>", mapping, true);
            PoseMe.UIElements.Add(input);
            mapping.inputField.GetComponentInChildren<Image>().color = color;
            // input.input.image.color = color;
            if (isContainingAtom)
            {
                mapping.interactable = false;
                color = mapping.inputField.GetComponentInChildren<Image>().color = color * .75f;
            }
        }
        
        private string MapName()
        {
            string mappedName = "";
            if (actorPose["containingAtom"].AsBool)
            {
                mappedName = PoseMe.atom.uid;
                PoseExtractor.mappedAtoms.Add(PoseMe.atom);
                isContainingAtom = true;
            }
            else
            {
                if (actorPose["type"].Value == "Person")
                {
                    var candidates = PoseMe.persons.Where(x => 
                        !PoseExtractor.mappedAtoms.Contains(x.atom) && 
                        x.atom.type == actorPose["type"].Value && 
                        x.characterListener.dcs.gender.ToString() == actorPose["gender"].Value &&
                        x.characterListener.isFuta == actorPose["futa"].AsBool).ToArray();
                    if (candidates.Length == 1)
                    {
                        var person = candidates[0];
                        mappedName = person.atom.uid;
                        PoseExtractor.mappedAtoms.Add(person.atom);
                    }
                    else if (candidates.Length > 1)
                    {
                        var uidMatching = candidates.FirstOrDefault(x => x.uid == storedUid);
                        if (uidMatching != null)
                        {
                            mappedName = uidMatching.atom.uid;
                            PoseExtractor.mappedAtoms.Add(uidMatching.atom);
                        }
                        else
                        {
                            var person = candidates[0];
                            mappedName = person.atom.uid;
                            PoseExtractor.mappedAtoms.Add(person.atom);
                        }
                    }
                }
                else
                {
                    var candidates = SuperController.singleton.GetAtoms().Where(x => 
                        !PoseExtractor.mappedAtoms.Contains(x) && 
                        x.type == actorPose["type"].Value).ToArray();
                    if (candidates.Length == 1)
                    {
                        var atom = candidates[0];
                        mappedName = atom.uid;
                        PoseExtractor.mappedAtoms.Add(atom);
                    }
                    else if(candidates.Length > 1)
                    {
                        var uidMatching = candidates.FirstOrDefault(x => x.uid == storedUid);
                        if (uidMatching != null)
                        {
                            mappedName = uidMatching.uid;
                            PoseExtractor.mappedAtoms.Add(uidMatching);
                        }
                        else
                        {
                            var person = candidates[0];
                            mappedName = person.uid;
                            PoseExtractor.mappedAtoms.Add(person);
                        }
                    }
                }
            }
            return mappedName;
        }
        
        private void SanityCheck(string val, bool checkOthers)
        {
            insane = false;
            if(isContainingAtom) color = mapping.inputField.GetComponentInChildren<Image>().color = Color.white * .75f;
            else color = mapping.inputField.GetComponentInChildren<Image>().color = Color.white;
            if (val == "") return;
            if (PoseExtractor.actorMappings.Any(x => x != this && x.mapping.val == val))
            {
                insane = true;
                "Error: You are not allowed to map two different actors in the pose to the same actor in the scene.".Print();
            }
            else
            {
                Atom atom = SuperController.singleton.GetAtomByUid(val);
                if (atom == null)
                {
                    insane = true;
                    $"Error: Atom with uid '{val}' does not exist in the scene.".Print();
                }
                else
                {
                    if (atom.type != actorPose["type"].Value)
                    {
                        insane = true;
                        $"Error: Atom '{val}' is of different type than the one you want to map it to.".Print();
                    }
                    else if (atom.type == "Person")
                    {
                        if (PoseMe.persons.First(x => x.atom.name == mapping.val).characterListener.dcs.gender.ToString() !=
                            actorPose["gender"].Value)
                        {
                            $"Warning: Gender missmatch. Atom '{val}' has a different gender than the person you want to map it to.\n Hand poses will not be restored.".Print();
                            mapping.inputField.GetComponentInChildren<Image>().color = PoseMe.warningColor;
                        }
                    }
                }
            }
            if(insane) color = mapping.inputField.GetComponentInChildren<Image>().color = PoseMe.severeWarningColor;
            if(!checkOthers) return;
            foreach (var actorMapping in PoseExtractor.actorMappings.Where(x => x != this))
            {
                actorMapping.SanityCheck(actorMapping.mapping.val, false);
            }
            
        }
    }
}