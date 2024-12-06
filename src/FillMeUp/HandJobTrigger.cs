using System;
using System.Linq;
using System.Security;
using UnityEngine;
using UnityEngine.Serialization;

namespace CheesyFX
{
    public class HandJobTrigger : MonoBehaviour
    {
        protected int collisions;
        public Hand hand;
        public CapsuleCollider lastCollider;
        
        public void OnTriggerEnter(Collider col)
        {
            if(col.isTrigger || !hand.magnetic.val) return;
            Penetrator penetrator;
            if (FillMeUp.penetratorByCollider.TryGetValue(col, out penetrator))
            {
                if(penetrator.type == 0) return;
                if (penetrator != hand.penetrator)
                {
                    // if (hand.penetrator == null || hand.depth.val > hand.GetDistance(penetrator.tip))
                    // {
                    //     hand.penetrator = (CapsulePenetrator)penetrator;
                    //     collisions = 0;
                    //     hand.OnEnable();
                    //     // penetrator.tipCollider.Print();
                    // }
                    hand.SetPenetrator((CapsulePenetrator)penetrator);
                    collisions = 0;
                } 
                collisions++;
                lastCollider = (CapsuleCollider)col;
                if(!hand.enabled) hand.enabled = true;
            }
        }

        public void OnTriggerExit(Collider col)
        {
            // if(!HandJob.penetrators.Select(x => x.collider).Contains(other)) return;
            // handJob.penetrator.colliders.Contains(other).Print();
            // "OnTriggerExit".Print();
            if (col.isTrigger || hand.penetrator == null || !hand.penetrator.colliders.Contains(col)) return;
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
            hand.enabled = false;
            hand.penetrator = null;
        }

        private Collider closest;

        public CapsuleCollider closestCollider
        {
            get
            {
                var colliders = hand.penetrator.colliders;
                var dist = 100f;
                Collider closest = null;
                for (int i = 0; i < colliders.Count; i++)
                {
                    var col = colliders[i];
                    var d = (col.transform.position - hand.enterPointTF.position).sqrMagnitude;
                    // $"{col.name} {d}".Print();
                    if (d < dist)
                    {
                        closest = col;
                        dist = d;
                    }
                }
                // if(this.closest != closest) $"{closest.name} {((CapsuleCollider)closest).direction}".Print();
                this.closest = closest;
                
                return (CapsuleCollider)closest;
            }
        }
    }
}