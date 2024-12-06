using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CheesyFX
{
    public class PoseTransition
    {
        private Pose from;
        private Pose to;
        private Atom atom;

        private List<FreeControllerV3> ctrlsToLerp = new List<FreeControllerV3>();
        private List<Vector3> targetPositions = new List<Vector3>();
        private List<Quaternion> targetRotations = new List<Quaternion>();

        public PoseTransition(Atom atom, Pose to)
        {
            var personJc = to.pose["pose"][atom.uid].AsObject;
            foreach (var ctrl in atom.freeControllers)
            {
                var jc = personJc.Childs.First(x => x["id"].Value == ctrl.name);
                if(jc["positionState"].Value == "Off" && jc["rotationState"].Value == "Off")  continue;
                ctrlsToLerp.Add(ctrl);
                targetPositions.Add(jc["localPosition"].AsObject.ToV3());
                targetRotations.Add(Quaternion.Euler(jc["localRotation"].AsObject.ToV3()));
            }
        }

    }
}