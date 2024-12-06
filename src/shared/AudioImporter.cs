using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MeshVR;
using SimpleJSON;
using UnityEngine;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;

namespace CheesyFX
{
    public class AudioImporter
    {
        private static List<Request> bundles = new List<Request>();
        public static Request moanBundle;
        private static bool moanBundleReady;

        public static void GetClipsFromAssetBundle(List<ClipLibrary> clipLibraries, string bundlePath)
        {
            Request request = new AssetLoader.AssetBundleFromFileRequest{
                path = bundlePath,
                callback = val => OnBundleLoaded(val, clipLibraries)};
            AssetLoader.QueueLoadAssetBundleFromFile(request);
        }

        public static void GetClipsFromAssetBundle(ClipLibrary clipLibrary, string bundlePath)
        {
            var clipLibraries = new List<ClipLibrary> { clipLibrary };
            GetClipsFromAssetBundle(clipLibraries, bundlePath);
        }

        private static void OnBundleLoaded(Request request, List<ClipLibrary> clipLibraries){
            try{
                bundles.Add(request);
                if (request.path.Contains("VAMMoan/audio/voices.voicebundle"))
                {
                    moanBundle = request;
                    ReadMoanBundle();
                    moanBundleReady = true;
                }
                else ReadBundle(request, clipLibraries);
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        public static void ReadBundle(Request bundle, List<ClipLibrary> clipLibraries)
        {
            foreach (var clipLibrary in clipLibraries)
            {
                clipLibrary.Clear();
                switch (clipLibrary.name)
                {
                    case "slaps":
                    {
                        var paths = bundle.assetBundle.GetAllAssetNames().Where(x => x.Contains("/slaps/"));
                        foreach (string path in paths) {clipLibrary.AddClip(bundle.assetBundle.LoadAsset<AudioClip>(path));}
                        break;
                    }
                    case "sexslaps":
                    {
                        var paths = bundle.assetBundle.GetAllAssetNames().Where(x => x.Contains("/sexslaps"));
                        foreach (string path in paths) clipLibrary.AddClip(bundle.assetBundle.LoadAsset<AudioClip>(path));
                        break;
                    }
                    case "buttslaps":
                    {
                        var paths = bundle.assetBundle.GetAllAssetNames().Where(x => x.Contains("/buttslaps"));
                        foreach (string path in paths) clipLibrary.AddClip(bundle.assetBundle.LoadAsset<AudioClip>(path));
                        break;
                    }
                    case "squishes":
                    {
                        var paths = bundle.assetBundle.GetAllAssetNames().Where(x => x.Contains("squishes/sq0"));
                        foreach (string path in paths)
                        {
                            clipLibrary.AddClip(bundle.assetBundle.LoadAsset<AudioClip>(path));
                        }
                        break;
                    }
                    case "blowjobs":
                    {
                        var paths = bundle.assetBundle.GetAllAssetNames().Where(x => x.Contains("blowjob/f-bj0"));
                        foreach (string path in paths)
                        {
                            clipLibrary.AddClip(bundle.assetBundle.LoadAsset<AudioClip>(path));
                        }
                        break;
                    }
                }
            }
        }

        public static void ReadMoanBundle()
        {
            int lastIdWithFiles = 0;
            string[] paths;
            for (int i = 0; i < 5; i++)
            {
                int id = i;
                paths = moanBundle.assetBundle.GetAllAssetNames().Where(x => x.Contains(ReadMyLips.voice.val.ToLower() + $"/m{id}-")).ToArray();
                if (paths.Length == 0)
                {
                    ReadMyLips.moanLibrary[id] = ReadMyLips.moanLibrary[lastIdWithFiles];
                    continue;
                }
                lastIdWithFiles = i;
                ReadMyLips.moanLibrary[id] = new ClipLibrary($"moans{id}");
                foreach (string path in paths)
                {
                    ReadMyLips.moanLibrary[id].AddClip(moanBundle.assetBundle.LoadAsset<AudioClip>(path));
                }
            }
            ReadMyLips.moanLibrary[5] = new ClipLibrary($"moans5");
            foreach (string path in moanBundle.assetBundle.GetAllAssetNames().Where(x => x.Contains(ReadMyLips.voice.val.ToLower()+$"/o-")))
            {
                ReadMyLips.moanLibrary[5].AddClip(moanBundle.assetBundle.LoadAsset<AudioClip>(path));
            }
        }
        
        public static IEnumerator DeferredReadMoanBundle()
        {
            int i = 0;
            while (!moanBundleReady && i < 10)
            {
                yield return new WaitForSeconds(.5f);
                i++;
            }
            ReadMoanBundle();
        }

        public static void UnloadBundles()
        {
            bundles.ForEach(x => AssetLoader.DoneWithAssetBundleFromFile(x.path));
        }
    }
}