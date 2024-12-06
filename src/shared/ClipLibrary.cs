using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;

namespace CheesyFX
{
    public class ClipLibrary
    {
        public string name;
        public List<AudioClip> clips = new List<AudioClip>();
        private List<AudioClip> remainingClips = new List<AudioClip>();
        private System.Random rnd = new System.Random();
        private bool hasClips;
        

        public ClipLibrary(string name)
        {
            this.name = name;
        }

        public void AddClip(AudioClip clip)
        {
            clips.Add(clip);
            remainingClips.Add(clip);
            hasClips = true;
        }

        public void Clear()
        {
            clips.Clear();
            remainingClips.Clear();
            hasClips = false;
        }

        public void Play(AudioSource audioSource)
        {
            if (!hasClips) return;
            audioSource.clip = GetRandomClip();
            audioSource.Play();
        }
        
        public AudioClip GetRandomClip()
        {
            if (!hasClips) return null;
            AudioClip clip;
            if(clips.Count == 1) clip = clips[0];
            else{
                int id = rnd.Next(0, remainingClips.Count);
                clip = remainingClips[id];
                remainingClips.Remove(clip);
                if (remainingClips.Count == 0)
                {
                    remainingClips.Clear();
                    remainingClips.AddRange(clips);
                }
            }
            return clip;
        }
    }
}