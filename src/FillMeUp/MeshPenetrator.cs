using UnityEngine;

namespace CheesyFX
{
    public class MeshPenetrator : Penetrator
    {
        public MeshCollider meshCollider;

        public MeshPenetrator (Collider meshCollider) : base(meshCollider)
        {
            this.meshCollider = (MeshCollider)meshCollider;
        }

        public void SetTip()
        {
            tip.localPosition = Vector3.zero;
            width = .4f;
        }
    }
}