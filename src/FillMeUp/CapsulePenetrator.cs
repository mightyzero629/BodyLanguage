using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Battlehub.RTSaveLoad.PersistentObjects;
using UnityEngine;
using UnityEngine.Serialization;

namespace CheesyFX
{
    public class CapsulePenetrator : Penetrator
    {
        public CapsuleCollider capsule;
        public bool isFucking;
        public Fuckable fuckable;

        private bool collidersModified;

        public CapsulePenetrator(CapsuleCollider capsule) : base(capsule)
        {
            this.capsule = capsule;
            if (atom.type == "Dildo") RegisterDildo();
            if (capsule.name == "AutoColliderGen3bHard") RegisterPenis();
            SetTipAndWith();
            // SetTipAndWith();
        }
        
        private void RegisterPenis()
        {
            type = 1;
            root = atom.rigidbodies.First(x => x.name == "Gen1");
            colliders = rootTransform.GetComponentsInChildren<Collider>(true).Where(x => !x.name.StartsWith("_")).ToList();
            rigidbodies = rootTransform.GetComponentsInChildren<Rigidbody>(true).ToList();
            colliders.ForEach(x => FillMeUp.penetratorByCollider[x] = this);
            // rigidbodies.ForEach(x => x.transform.Draw());
            for (int i = 0; i < colliders.Count; i++)
            {
                var c = colliders[i];
                if (i < 2) forwardByCollider[c] = () => c.transform.up;
                else forwardByCollider[c] = () => c.transform.right;
            }
            forward = () => capsule.transform.right;
            // tipCollider.transform.GetAllChildren().ForEach(x => x.name.Print());
            // radii = colliders.Select(x => ((CapsuleCollider)x).radius).ToArray();
            
            // baseRadii => autoColliders.Select(x => x.colliderRadius);
            // for (int i = 0; i < autoColliders.Count; i++)
            // {
            //     autoColliders[i].AutoColliderSizeSet();
            // }
            // bones = root.GetComponentsInChildren<DAZBone>();
            // bones[0].baseJointRotation = new Vector3(0f, 0f, 0f);
            // if (atom != PoseMe.atom)
            // {
            //     bones[1].baseJointRotation = new Vector3(0f, 0f, 0f);
            //     bones[2].baseJointRotation = new Vector3(0f, 0f, 0f);
            // }
            // GetColliderDefaults();
        }

        private void RegisterDildo()
        {
            type = 2;
            root = atom.rigidbodies.First(x => x.name == "b1");
            rootTransform = root.transform;
            colliders = rootTransform.GetComponentsInChildren<Collider>(true).Where(x => x.attachedRigidbody != null).ToList();
            // colliders.ForEach(x => x.transform.Draw());
            rigidbodies = rootTransform.GetComponentsInChildren<Rigidbody>(true).ToList();
            colliders.ForEach(x => FillMeUp.penetratorByCollider[x] = this);
            for (int i = 0; i < colliders.Count; i++)
            {
                var c = (CapsuleCollider)colliders[i];
                forwardByCollider[c] = () => c.transform.up;
            }
            forward = () => capsule.transform.up;
            atom.mainController.RBHoldPositionSpring = 1e5f;
            // capsule.transform.Draw();
            // rigidbodies.ForEach(x => x.transform.Draw());
        }

        public override void SetTipAndWith()
        {
            var scale = GetScale();
            switch (capsule.direction)
            {
                case 0:
                    tip.localPosition = new Vector3(0f,capsule.radius * scale,0f);
                    width = capsule.height * scale*9f;
                    break;
                case 1:
                    tip.localPosition = new Vector3(0f,capsule.height * .5f * scale,0f);
                    width = capsule.radius * scale*18f;
                    break;
                case 2:
                    tip.localPosition = new Vector3(capsule.radius * scale, 0f, 0f);
                    width = capsule.height * scale*9f;
                    break;
            }

            if (type > 0)
            {
                length = Vector3.Distance(tip.position, root.position);
                // GetColliderDefaults();
            }
            // $"{atom.name} : {capsule.direction} {capsule.transform.lossyScale} {width}".Print();
            foreach (var orifice in FillMeUp.orifices)
            {
                if (orifice.penetrator == this) orifice.penetratorWidth = width;
            }
            // $"{atom.name} {capsule.name} {width} {capsule.direction}".Print();
            // tip.Draw();
            // width.Print();
        }

        // private void OnEnable()
        // {
        //     SetTipAndWith();
        //     foreach (var orifice in FillMeUp.orifices)
        //     {
        //         if (orifice.penetrator == this) orifice.penetratorWidth = width;
        //     }
        // }
    }
}