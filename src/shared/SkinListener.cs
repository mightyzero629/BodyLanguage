using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CheesyFX
{
    public class SkinListener
    {
        public class SkinChangedToEvent : UnityEvent<string>{}

        public SkinChangedToEvent skinChangedToEvent = new SkinChangedToEvent();
        private List<UnityEventsListener> eventsListeners = new List<UnityEventsListener>();
        
        public SkinListener(Atom atom)
        {
            var container = atom.transform.Find("rescale2/geometry/MaleCharacters/MaleCharactersPrefab(Clone)");
            foreach (Transform child in container)
            {
                var listener = child.gameObject.AddComponent<UnityEventsListener>();
                eventsListeners.Add(listener);
                listener.onEnabled.AddListener(() => skinChangedToEvent.Invoke(child.name));
            }
            foreach (Transform child in atom.transform.Find("rescale2/geometry/FemaleCharacters/FemaleCharactersPrefab(Clone)"))
            {
                var listener = child.gameObject.AddComponent<UnityEventsListener>();
                eventsListeners.Add(listener);
                listener.onEnabled.AddListener(() => skinChangedToEvent.Invoke(child.name));
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < eventsListeners.Count; i++)
            {
                Object.Destroy(eventsListeners[i]);
            }
        }
    }
}