using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class PenisDisabler : MVRScript
    {
        private List<Collider> colliders;

        private void OnEnable()
        {
            if(colliders != null) colliders.ForEach(x => x.enabled = false);
        }

        public override void Init()
        {
            var gen1 = containingAtom.rigidbodies.First(x => x.name == "Gen1");
            colliders = gen1.gameObject.GetComponentsInChildren<Collider>().ToList();
            colliders.ForEach(x => x.enabled = false);
        }

        private void OnDestroy()
        {
            colliders.ForEach(x => x.enabled = true);
        }

        private void OnDisable()
        {
            colliders.ForEach(x => x.enabled = true);
        }
    }
}