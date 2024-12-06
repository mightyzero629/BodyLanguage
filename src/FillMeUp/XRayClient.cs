using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace CheesyFX
{
    public class XRayClient
    {
        public StimReceiver stimReceiver;
        private Atom atom => stimReceiver.atom;
        private DAZSkinV2 skin;
        public GameObject meshContainer;
        private MeshFilter meshFilter;
        private MeshRenderer renderer;
        private Material material;
        private Material[] oldMaterials;
        private Material discard = new Material(Shader.Find("Custom/Discard"));
        private MaterialOptionsWrapper matOptions;
        public float maxAlpha = -.5f;
        public bool useAngleScaling = true;
        public bool useOcclusionScaling = true;
        private float alphaTarget;

        private IEnumerator syncSkinCo;
        private bool syncingSkin;
        public XRayAlphaDriver xRayAlphaDriver;
        private IEnumerator deferredInit;

        private bool initialized;
        
        private static readonly Shader shader = Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff");


        public float alpha
        {
            get { return material.GetFloat("_AlphaAdjust");}
            set
            {
                // material.SetFloat("_AlphaAdjust", value);
                matOptions.SetAlpha(value);
            }
        }

        public XRayClient(StimReceiver stimReceiver)
        {
            this.stimReceiver = stimReceiver;
            deferredInit = DeferredInit().Start();
        }

        private IEnumerator DeferredInit()
        {
            
            yield return new WaitUntil(() => !SuperController.singleton.isLoading);
            meshContainer = new GameObject("XRayMesh");
            meshContainer.transform.parent = atom.transform;
            xRayAlphaDriver = meshContainer.AddComponent<XRayAlphaDriver>().Init(this, XRay.overlayCamera.transform);
            // meshContainer = atom.transform.Find("XRayMesh")?.gameObject;
            // if (meshContainer == null)
            // {
            //     meshContainer = new GameObject("XRayMesh");
            //     meshContainer.transform.parent = atom.transform;
            //     xRayAlphaDriver = meshContainer.AddComponent<XRayAlphaDriver>().Init(this, XRay.overlayCamera.transform);
            // }
            // else
            // {
            //     "meshContainer exists".Print();
            //     xRayAlphaDriver = meshContainer.GetComponent<XRayAlphaDriver>();
            //     yield break;
            // }
            meshContainer.layer = XRay.layer; // This layer otherwise appears unused
            renderer = meshContainer.AddComponent<MeshRenderer>();
            meshFilter = meshContainer.AddComponent<MeshFilter>();
            
            matOptions = meshContainer.AddComponent<MaterialOptionsWrapper>();
            matOptions.materialContainer = meshContainer.transform;
            matOptions.textureGroup1 = new MaterialOptionTextureGroup();
            // matOptions.SetCustomTextureFolder("Custom/Atom/Person/Textures");
            matOptions.materialForDefaults = material;
		
            if(stimReceiver is Person) matOptions.paramMaterialSlots = new int[]{29};
            else matOptions.paramMaterialSlots = new int[]{0};
            matOptions.textureGroup1.materialSlots = matOptions.paramMaterialSlots;
            
            SyncSkin();
            meshContainer.gameObject.SetActive(false);
        }

        public void SyncSkin()
        {
            try
            {
                if(syncingSkin) return;
                // "syncingSkin".Print();
                syncSkinCo.Stop();
                
                syncSkinCo = SyncSkinCo().Start();
                syncingSkin = true;
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
            }
        }

        private IEnumerator SyncSkinCo()
        {
            initialized = false;
            
            yield return new WaitUntil(() => 
                !SuperController.singleton.loadingIcon.gameObject.activeSelf && 
                !SuperController.singleton.loadingUI.gameObject.activeSelf &&
                skin != ((DAZCharacterMaterialOptions)atom.GetStorableByID("skin")).skin && 
                FillMeUp.processesSorted
            );
            int p = Mathf.RoundToInt(FillMeUp.instanceId.val);
            // $"{PoseMe.atom.name} process {p}".Print();
            for (int l = 0; l < FillMeUp.otherUidsWithBL.Count + 1; l++)
            {
                FillMeUp.levels.val = FillMeUp.levels.val.SetComponent(p, l);
                FillMeUp.lastToEnter.val = FillMeUp.lastToEnter.val.SetComponent(l, p);
                // $"{PoseMe.atom.name} {l} {FillMeUp.lastToEnter.val[l]} {FillMeUp.levels.val.MaxExcluding(p)} {FillMeUp.levels.val}".Print();
                while (Mathf.RoundToInt(FillMeUp.lastToEnter.val[l]) == p && FillMeUp.levels.val.MaxExcluding(p) >= l)
                {
                    // $"{l} {FillMeUp.lastToEnter.val[l]} {FillMeUp.levels.val.MaxExcluding(p)}".Print();
                    // $"{PoseMe.atom.name} > {stimReceiver.atom.name} waiting".Print();
                    // $"{PoseMe.atom.name} {l} {FillMeUp.singleton.lastToEnter.val[l]} {FillMeUp.singleton.levels.val.MaxExcluding(p)} {FillMeUp.singleton.levels.val}".Print();
                    yield return null;
                }
            }

            FillMeUp.WaitForThreadRace(SkinDance()).Start();
            // $"{PoseMe.atom.name} > {stimReceiver.atom.name} started".Print();
            // skin = ((DAZCharacterMaterialOptions)atom.GetStorableByID("skin")).skin;
            // bool matEnabled = skin.materialsEnabled[29];
            // skin.materialsEnabled[29] = true;
            // meshFilter.sharedMesh = skin.Mesh;
            // oldMaterials = skin.GPUmaterials;
            // material = new Material(skin.GPUmaterials[29])
            // {
            //     shader = shader
            // };
            // if(stimReceiver.fuckable != null) SetAlphaTexture(stimReceiver.fuckable.xrayAlphaTexture);
            // mats = new Material[skin.GPUmaterials.Length];
            //
            // // TODO: Add a no-cull shader option
            // //Shader transparent = Shader.Find("Custom/Subsurface/TransparentGlossNMNoCullSeparateAlphaComputeBuff");
            // //Shader transparent = Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff");
            // for (int i = 0; i < mats.Length; i++)
            // {
            //     mats[i] = discard;
            // }
            // mats[29] = material;
            // renderer.sharedMaterials = mats;
            // renderer.materials = mats;
            // renderer.enabled = true;
            // skin.GPUmaterials = mats;
            // skin.FlushBuffers();
            // yield return new WaitForEndOfFrame();
            // bool proceeed = false;
            // int j = 0;
            // while (!proceeed)
            // {
            //     
            //     proceeed = true;
            //     if (oldMaterials == null)
            //     {
            //         // "om".Print();
            //         proceeed = false;
            //     }
            //     if (skin.GPUmaterials != mats)
            //     {
            //         // "gpu".Print();
            //         proceeed = false;
            //         
            //     }
            //     if (!material.HasProperty("verts"))
            //     {
            //         // "verts".Print();
            //         // var curSkin = ((DAZCharacterMaterialOptions)atom.GetStorableByID("skin")).skin;
            //         var curSkin = atom.GetComponentInChildren<DAZMergedSkinV2>();
            //         if (curSkin != skin)
            //         {
            //             // "Skin changed".Print();
            //             syncingSkin = false;
            //             SyncSkin();
            //             yield break;
            //         }
            //         proceeed = false;
            //     }
            //     // if(proceeed) break;
            //     j++;
            //     if (j == 120)
            //     {
            //         SuperController.LogError("XRay: SyncSkin failed");
            //         break;
            //     }
            //     yield return new WaitForEndOfFrame();
            // }
            //
            // skin.GPUmaterials = oldMaterials;
            // skin.FlushBuffers();
            // matOptions.SetParams();
            // initialized = true;
            // if (stimReceiver.fuckable != null)
            // {
            //     Enable(stimReceiver.fuckable);
            // }
            // skin.materialsEnabled[29] = matEnabled;
            // syncingSkin = false;
            //
            // // $"{PoseMe.atom.name} > {stimReceiver.atom.name} finished".Print();
            // FillMeUp.levels.val = FillMeUp.levels.val.SetComponent(p, -1);
        }

        private IEnumerator SkinDance()
        {
            // $"{PoseMe.atom.name} > {stimReceiver.atom.name} started".Print();
            skin = ((DAZCharacterMaterialOptions)atom.GetStorableByID("skin")).skin;
            bool matEnabled = skin.materialsEnabled[29];
            skin.materialsEnabled[29] = true;
            meshFilter.sharedMesh = skin.Mesh;
            oldMaterials = skin.GPUmaterials;
            material = new Material(skin.GPUmaterials[29])
            {
                shader = shader
            };
            if(stimReceiver.fuckable != null) SetAlphaTexture(stimReceiver.fuckable.xrayAlphaTexture);
            mats = new Material[skin.GPUmaterials.Length];
			
            // TODO: Add a no-cull shader option
            //Shader transparent = Shader.Find("Custom/Subsurface/TransparentGlossNMNoCullSeparateAlphaComputeBuff");
            //Shader transparent = Shader.Find("Custom/Subsurface/TransparentGlossNMSeparateAlphaComputeBuff");
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = discard;
            }
            mats[29] = material;
            renderer.sharedMaterials = mats;
            renderer.materials = mats;
            renderer.enabled = true;
            skin.GPUmaterials = mats;
            skin.FlushBuffers();
            yield return new WaitForEndOfFrame();
            bool proceeed = false;
            int j = 0;
            while (!proceeed)
            {
                
                proceeed = true;
                if (oldMaterials == null)
                {
                    // "om".Print();
                    proceeed = false;
                }
                if (skin.GPUmaterials != mats)
                {
                    // "gpu".Print();
                    proceeed = false;
                    
                }
                if (!material.HasProperty("verts"))
                {
                    // "verts".Print();
                    // var curSkin = ((DAZCharacterMaterialOptions)atom.GetStorableByID("skin")).skin;
                    var curSkin = atom.GetComponentInChildren<DAZMergedSkinV2>();
                    if (curSkin != skin)
                    {
                        // "Skin changed".Print();
                        syncingSkin = false;
                        SyncSkin();
                        yield break;
                    }
                    proceeed = false;
                }
                // if(proceeed) break;
                j++;
                if (j == 120)
                {
                    SuperController.LogError("XRay: SyncSkin failed");
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
            
            skin.GPUmaterials = oldMaterials;
            skin.FlushBuffers();
            matOptions.SetParams();
            initialized = true;
            if (stimReceiver.fuckable != null)
            {
                Enable(stimReceiver.fuckable);
            }
            skin.materialsEnabled[29] = matEnabled;
            syncingSkin = false;
            // $"{PoseMe.atom.name} > {stimReceiver.atom.name} finished".Print();
        }

        private Material[] mats;

        public void Deregister()
        {
            Destroy();
            XRay.clients.Remove(this);
        }

        public void Disable()
        {
            meshContainer.gameObject.SetActive(false);
            XRay.LogOutClient();
        }

        public void ShutDown()
        {
            xRayAlphaDriver.blendTarget = 0f;
        }
        
        public void Enable(Fuckable fuckable)
        {
            if(!initialized || !XRay.enabled.val || !fuckable.xrayEnabled.val) return;
            maxAlpha = -fuckable.xrayTransparency.val;
            useAngleScaling = fuckable.xrayUseAngleScaling.val;
            useOcclusionScaling = fuckable.xrayUseOcclusionScaling.val;
            if (useAngleScaling || useOcclusionScaling) xRayAlphaDriver.enabled = true;
            SetAlphaTexture(fuckable.xrayAlphaTexture);
            // matOptions.SyncAllParameters();
            XRay.overlayCamera.enabled = true;
            xRayAlphaDriver.blendFactor = 0f;
            xRayAlphaDriver.blendTarget = 1f;
            
            // if (fuckable is Throat)
            // {
            //     xRayAlphaDriver.occlusionOffset = 5;
            //     xRayAlphaDriver.occlusionStrength = .85f;
            // }
            // else
            // {
            //     xRayAlphaDriver.occlusionOffset = 10;
            //     xRayAlphaDriver.occlusionStrength = .95f;
            // }

            xRayAlphaDriver.occlusionDecay = fuckable.xrayOcclusionDecay.val;
            xRayAlphaDriver.occlusionOffset = (int)fuckable.xrayOcclusionOffset.val;
            xRayAlphaDriver.obstruction = fuckable.obstructionInfo;
            meshContainer.gameObject.SetActive(true);
        }

        public void Destroy()
        {
            if(stimReceiver is Person)
            {
                syncSkinCo.Stop();
                deferredInit.Stop();
                if (skin != null && oldMaterials != null && skin.GPUmaterials != oldMaterials)
                {
                    skin.GPUmaterials = oldMaterials;
                    skin.FlushBuffers();
                }
                Object.DestroyImmediate(meshContainer);
                Object.DestroyImmediate(discard);
                Object.DestroyImmediate(material);
            }
            else
            {
                foreach(Renderer renderer in stimReceiver.atom.GetComponentsInChildren<Renderer>())
                {
                    renderer.gameObject.layer = 0;
                }
            }
        }

        public void SetAlphaTexture(Texture2D tex) => matOptions.SetAlphaTexture(tex);

        public class MaterialOptionsWrapper : MaterialOptions
        {
            public void SetParams(){
                this.SetAllParameters();
            }

            public float renderQueue
            {
                get { return renderQueueJSON.val; }
                set { SetMaterialRenderQueue(Mathf.FloorToInt(value)); }
            }
            
            public void SetAlpha(float val)
            {
                renderers[0].materials[29].SetFloat("_AlphaAdjust", val);
            }
            
            public void SetAlphaTexture(Texture2D tex)
            {
                customTexture4IsNull = false;
                customTexture4 = tex;
                SetTextureGroupSet(this.textureGroup1, this.currentTextureGroup1Set, 3, tex, this.customTexture4IsNull);
            }
            }
        }

        public class XRayAlphaDriver : MonoBehaviour
        {
            private XRayClient xRayClient;
            private Transform[] references = new Transform[3];
            private Transform camera;
            public float blendTarget;
            public float blendFactor;
            private bool initialized;
            RaycastHit[] hits = new RaycastHit[100];
            private float depthFactor;
            public int occlusionOffset = 10;
            public float occlusionDecay = .9f;
            public JSONStorableFloat obstruction = new JSONStorableFloat("Obstruction", 0f, 0f, 100f, true, false);
            
            private Vector3 meanForward
            {
                get
                {
                    Vector3 sum = Vector3.zero;
                    for (int i = 0; i < 3; i++)
                    {
                        sum += references[i].forward;
                    }
                    return sum * .33333f;
                }
            }


            public XRayAlphaDriver Init(XRayClient client, Transform camera)
            {
                xRayClient = client;
                this.camera = camera;
                // references[0] = client.person.atom.forceReceivers.FirstOrDefault(x => x.name == "pelvis").transform.Find("Gen1");
                // references[1] = references[0].Find("Gen2");
                references[0] = client.stimReceiver.penetrator.rigidbodies[0].transform;
                references[1] = client.stimReceiver.penetrator.rigidbodies[0].transform;
                references[2] = client.stimReceiver.penetrator.tip;
                enabled = false;
                initialized = true;
                // "XRayAlphaDriver Init".Print();
                return this;
            }

            public void OnEnable()
            {
                // blendFactor = 0f;
                blendTarget = 1f;
                // dynamicMaxAlpha = xRayClient.maxAlpha;
                // xRayClient.alpha = -1f;
            }

            private float timer = .1f;
            private int size;
            public void Update()
            {
                if (xRayClient.useOcclusionScaling)
                {
                    bool obstructedByOthers = false;
                    timer -= Time.deltaTime;
                    if (timer < 0f)
                    {
                        timer = 0f;
                        var camPos = Camera.main.transform.position;
                        Ray ray = new Ray(camPos, references[2].position - camPos);
                        size = Physics.RaycastNonAlloc(ray, hits, (references[2].position - camPos).magnitude);
                        
                        // size.Print();
                        
                        
                        for (int i = 0; i < Math.Min(size, 5); i++)
                        {
                            // i.Print();
                            var collider = hits[i].collider;
                            if (!xRayClient.stimReceiver.penetrator.colliders.Contains(collider)
                                && !collider.name.Contains("Control") && !collider.name.StartsWith("CheesyFX"))
                            {
                                var atom = collider.GetAtom();
                                // atom.type.Print();
                                if(atom == FillMeUp.atom || atom.type == "Glass" || atom.type.Contains("Slate") || atom.type.Contains("Panel")) continue;
                                // $"{atom.type}/{collider.name} {i}".Print();
                                obstructedByOthers = true;
                                break;
                            }
                        }
                    }

                    if (obstructedByOthers)
                    {
                        depthFactor = Mathf.Lerp(depthFactor, 0f, Time.deltaTime);
                    }
                    else
                    {
                        if (size < occlusionOffset)
                        {
                            // dynamicMaxAlpha = xRayClient.maxAlpha;
                            depthFactor = Mathf.Lerp(depthFactor, 1f, Time.deltaTime);
                        }
                        else
                        {
                            // depthFactor = Mathf.Lerp(depthFactor, Mathf.Pow(occlusionDecay, size-occlusionOffset), Time.deltaTime);
                            depthFactor = Mathf.Lerp(depthFactor, 1f - (size - occlusionOffset) * occlusionDecay,
                                Time.deltaTime);
                            // depthFactor = Mathf.Lerp(depthFactor, Mathf.Max(0f, 1f - (size-occlusionOffset) * .05f),
                            //     3f * Time.deltaTime);
                        }
                    }
                    obstruction.val = 1f-depthFactor;
                }
                
                var delta = Mathf.Abs(blendFactor - blendTarget);
                if (delta < .01f)
                {
                    if (blendTarget == 0f)
                    {
                        xRayClient.Disable();
                        obstruction.val = 0f;
                        return;
                    }
                    blendFactor = 1f;
                    if (!xRayClient.useAngleScaling && !xRayClient.useOcclusionScaling) enabled = false;
                }
                else blendFactor = Mathf.Lerp(blendFactor, blendTarget, .5f*Time.deltaTime);
                if(xRayClient.useAngleScaling)
                {
                    var angle = 1f - 1.1f * Mathf.Abs(Vector3.Dot(camera.forward, meanForward));
                    if (xRayClient.useOcclusionScaling) angle *= depthFactor;
                    xRayClient.alpha = Mathf.Lerp(-1f, xRayClient.maxAlpha, angle * blendFactor);
                }
                else
                {
                    var lambda = blendFactor;
                    if (xRayClient.useOcclusionScaling) lambda *= depthFactor;
                    xRayClient.alpha = Mathf.Lerp(-1f, xRayClient.maxAlpha, lambda);
                }
                // xRayClient.alpha.Print();
            }
        }
}