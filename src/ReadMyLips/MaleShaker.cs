using System;
using System.Linq;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class MaleShaker : Shaker
    {
        private FreeControllerV3 hipCtrl;

        public MaleShaker Init(Atom atom)
        {
            RB = atom.rigidbodies.FirstOrDefault(x => x.name == "hip");
            hipCtrl = atom.freeControllers.FirstOrDefault(x => x.name == "hipControl");
            baseForceFactor = 500f;
            applyForceOnReturn = false;
            enabled = false;
            return this;
        }
        
        public override void Randomize()
        {
            forceQuickness = Random.Range(1f, 2f);
            randomForceFactor = Random.Range(forceFactor*.8f, forceFactor*1.5f);
            period = Random.Range(.3f, .5f);
            periodRatio = Random.Range(.2f, .7f);
        }
        
        public override void SetForce(float percent) {
            targetForce = percent * randomForceFactor * hipCtrl.transform.forward;
        }
    }
}