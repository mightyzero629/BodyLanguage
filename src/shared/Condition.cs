using System.Collections.Generic;
using System.Linq;

namespace CheesyFX
{
    public class Condition
    {
        public JSONStorableStringChooser excludedChooser =
            new JSONStorableStringChooser("atomChooser", null, "", "... this Atom");
        public static HashSet<Atom> excludedAtoms = new HashSet<Atom>();
        public JSONStorableString excludedInfo = new JSONStorableString("excludeInfo", "<b>Excluded Atoms</b>\n");

        public Atom atom;

        public Condition()
        {
            excludedChooser.choices = SuperController.singleton.GetAtomUIDs();
        }

        public bool IsMet()
        {
            return !excludedAtoms.Contains(atom);
        }

        public void Exclude(string uid, bool val)
        {
            Atom atom = SuperController.singleton.GetAtomByUid(uid);
            if (atom == null) return;
            if (val)
            {
                if(excludedAtoms.Add(atom))
                {
                    excludedInfo.val += atom.uid + "\n";
                }
            }
            else if(excludedAtoms.Remove(atom))
            { 
                excludedInfo.val = "<b>Excluded Atoms</b>\n"+string.Join("\n", excludedAtoms.Select(x => x.uid).ToArray());
            }
        }
        
        // public void ResetExcluded()
        // {
        //     excludedAtoms.Clear();
        //     excludedInfo.val = "<b>Excluded Atoms</b>\n";
        // }
        //
        // public void AddExclusionChoice(Atom atom)
        // {
        //     if (excludedChooser.choices.Contains(atom.uid)) return;
        //     var choices = excludedChooser.choices;
        //     choices.Insert(0, atom.uid);
        //     excludedChooser.choices = new List<string>(choices);
        // }
        //
        // public void OnAtomRename()
        // {
        //     excludedChooser.choices = SuperController.singleton.GetAtomUIDs();
        //     var ignoredUids = excludedAtoms.Select(x => x.uid).ToArray();
        //     excludedInfo.val = "<b>Excluded Atoms</b>\n"+string.Join("\n", ignoredUids);
        // }
    }
}