using UnityEngine;

namespace CheesyFX
{
    public class TitJobTrigger : MonoBehaviour
    {
        protected int collisions;
        public Cleavage cleavage;
        protected Penetrator penetrator;
        
        public void OnTriggerEnter(Collider col)
        {
            if(col.isTrigger) return;
            
            if (FillMeUp.penetratorByCollider.TryGetValue(col, out penetrator))
            {
                // if(penetrator.type == 0) return;
                if (penetrator != cleavage.penetrator)
                {
                    // if (hand.penetrator == null || hand.depth.val > hand.GetDistance(penetrator.tip))
                    // {
                    //     hand.penetrator = (CapsulePenetrator)penetrator;
                    //     collisions = 0;
                    //     hand.OnEnable();
                    //     // penetrator.tipCollider.Print();
                    // }
                    cleavage.SetPenetrator((CapsulePenetrator)penetrator);
                    collisions = 0;
                    // PoseMe.gaze.Focus(col);
                    // $"in: {col.attachedRigidbody}".Print();
                } 
                collisions++;
                if(!cleavage.enabled) cleavage.enabled = true;
            }
        }

        public void OnTriggerExit(Collider col)
        {
            // if(!HandJob.penetrators.Select(x => x.collider).Contains(other)) return;
            // handJob.penetrator.colliders.Contains(other).Print();
            // "OnTriggerExit".Print();
            // cleavage.penetrator.atom.Print();
            // $"out: {col.attachedRigidbody}".Print();
            // cleavage.penetrator.colliders.Contains(col).Print();
            if (col.isTrigger || cleavage.penetrator == null || !cleavage.penetrator.colliders.Contains(col)) return;
            collisions--;
            if (collisions == 0)
            {
                Reset();
                // hand.markedForDisable = true;
            }
        }

        public void Reset()
        {
            collisions = 0;
            cleavage.enabled = false;
            cleavage.penetrator = null;
        }
    }
}