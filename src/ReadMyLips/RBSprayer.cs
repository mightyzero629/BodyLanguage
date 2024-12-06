using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class RBSprayer : MonoBehaviour
    {
        private GameObject parent;
        private GameObject prefab;
        
        public RBSprayer Init()
        {
            prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.transform.localScale = new Vector3(.01f, .01f, .01f);
            var rb = prefab.AddComponent<Rigidbody>();
            rb.drag = 20f;
            rb.mass = .1f;
            // Destroy(prefab);
            return this;
        }
        
        public void Run()
        {
            InvokeRepeating(nameof(Spray), 0f, .075f);
        }
        
        public void ShutDown()
        {
            CancelInvoke();
        }
        
        private void Spray()
        {
            GameObject item = Instantiate(prefab);
            item.transform.position = gameObject.transform.position;
            Vector3 sprayDirection = Random.insideUnitSphere*5f;
            item.GetComponent<Rigidbody>().velocity = sprayDirection;
            
            Destroy(item, 2f);
        }
        
    }
}