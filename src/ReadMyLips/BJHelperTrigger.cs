using System;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class BJHelperTrigger : MonoBehaviour
    {
        public void OnTriggerEnter(Collider other)
        {
            if(other.isTrigger) return;
            
            Penetrator penetrator;
            if (FillMeUp.penetratorByCollider.TryGetValue(other, out penetrator))
            {
                if (BJHelper.penetrators.Add(penetrator) && !FillMeUp.throat.isPenetrated) BJHelper.singleton.enabled = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if(!BJHelper.penetrators.Select(x => x.tipCollider).Contains(other)) return;
            Penetrator penetrator;
            FillMeUp.penetratorByCollider.TryGetValue(other, out penetrator);
            BJHelper.penetrators.Remove(penetrator);
            if (BJHelper.penetrators.Count == 0) BJHelper.singleton.enabled = false;
        }
    }
}