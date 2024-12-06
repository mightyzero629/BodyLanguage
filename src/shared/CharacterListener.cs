using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace CheesyFX
{
    public class CharacterListener
    {
        public Atom atom;
        public DAZCharacterSelector dcs;
        public DAZCharacterSelector.Gender gender;
        public bool isFuta;
        private UnityEventsListener genderListener;
        private SkinListener skinListener;
        public readonly UnityEvent OnGenderChanged = new UnityEvent();
        public readonly UnityEvent OnChangedToFemale = new UnityEvent();
        public readonly UnityEvent OnChangedToMale = new UnityEvent();
        public readonly UnityEvent OnChangedToFuta = new UnityEvent();

        public SkinListener.SkinChangedToEvent onSkinChangedTo => skinListener.skinChangedToEvent;

        public CharacterListener(Atom atom)
        {
            this.atom = atom;
            dcs = atom.GetStorableByID("geometry") as DAZCharacterSelector;
            gender = dcs.gender;
            var gen1Object = atom.GetStorableByID("PhysicsModel").transform.GetAllChildren()
                .FirstOrDefault(x => x.name == "Gen1").gameObject;
            isFuta = gen1Object.activeSelf && dcs.gender == DAZCharacterSelector.Gender.Female;
            genderListener = gen1Object.AddComponent<UnityEventsListener>();
            genderListener.onEnabled.AddListener(OnGen1Enabled);
            genderListener.onDisabled.AddListener(OnGen1Disabled);
            skinListener = new SkinListener(atom);
            onSkinChangedTo.AddListener(PrintInfo);
            // foreach (Transform child in atom.transform.Find("rescale2/geometry/MaleCharacters/MaleCharactersPrefab(Clone)"))
            // {
            //     // child.NullCheck();
            //     skinListener = child.gameObject.AddComponent<UnityEventsListener>();
            //     skinListener.onEnabled.AddListener(() => $"{this.atom.name} {child.name}".Print());
            //     skinListener.onEnabled.AddListener(OnSkinChanged.Invoke);
            // }
            // foreach (Transform child in atom.transform.Find("rescale2/geometry/FemaleCharacters/FemaleCharactersPrefab(Clone)"))
            // {
            //     var skinListener = child.gameObject.AddComponent<UnityEventsListener>();
            //     skinListener.onEnabled.AddListener(() => child.name.Print());
            //     this.skinListener.onEnabled.AddListener(OnSkinChanged.Invoke);
            // }
        }

        private void PrintInfo(string skin)
        {
            $"{atom.name} > {skin}".Print();
        }

        private void OnGen1Enabled()
        {
            if (dcs.gender == DAZCharacterSelector.Gender.Female)
            {
                OnChangedToFuta.Invoke();
                isFuta = true;
            }
            else
            {
                OnChangedToMale.Invoke();
                isFuta = false;
            }
            if(dcs.gender != gender) OnGenderChanged.Invoke();
            gender = dcs.gender;
        }

        public void OnGen1Disabled()
        {
            // "OnGen1Disabled".Print();
            // dcs.gender.Print();
            // dcs.containingAtom.Print();
            // genderListener.transform.parent.name.Print();
            // if (!genderListener.transform.parent.gameObject.activeInHierarchy)
            // {
            //     "666".Print();
            //     return;
            // }
            if (dcs.gender == DAZCharacterSelector.Gender.Male) return;
            OnChangedToFemale.Invoke();
            if(dcs.gender != gender) OnGenderChanged.Invoke();
            gender = dcs.gender;
            isFuta = false;
        }

        public void Destroy()
        {
            Object.Destroy(genderListener);
            skinListener.Destroy();
        }
    }
}