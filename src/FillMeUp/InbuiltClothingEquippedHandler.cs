using System;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class InbuiltClothingEquippedHandler : MonoBehaviour
    {
        private void Start()
        {
            OnEnable();
        }

        private void OnEnable()
        {
            transform.Print(); 
            var dci = transform.GetComponent<DAZClothingItem>();
            // dci.onLoadedHandlers.Print();
            
            foreach (var collider in gameObject.GetComponentsInChildren<Collider>(true))
            {
                collider.Print();
                if(FillMeUp.ignoredClothingColliders.Contains(collider)) continue;
                Physics.IgnoreCollision(collider, FillMeUp.anus.enterTriggerCollider, true);
                Physics.IgnoreCollision(collider, FillMeUp.vagina.enterTriggerCollider, true);
                foreach (var fuckable in FillMeUp.fuckables)
                {
                    Physics.IgnoreCollision(collider, fuckable.proximityTrigger, true);
                }
                FillMeUp.ignoredClothingColliders.Add(collider);
            }
            // FillMeUp.ignoredClothingColliders.ForEach(x => x.Print());
        }
    }
}