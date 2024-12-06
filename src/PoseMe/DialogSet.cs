using System;
using System.Collections.Generic;
using System.Net;

namespace CheesyFX
{
    public class DialogSet : List<Dialog>
    {
        private bool sorted;
        private static List<Dialog> delayGroup = new List<Dialog>();
        private static List<Dialog> available = new DialogSet();
        private Dialog last;
        public new void Add(Dialog dialog)
        {
            base.Add(dialog);
            dialog.delayMean.setCallbackFunction += val => sorted = false;
            sorted = false;
            Sort();
        }

        public void Invoke(bool onEnter)
        {
            if(Count == 0) return;
            // return;
            Sort();
            available.Clear();
            for (int j = 0; j < Count; j++)
            {
                var dialog = this[j];
                if(dialog.onEnter == onEnter && (!dialog.playOnce.val || !dialog.hasBeenPlayed)) available.Add(dialog);
            }
            if(available.Count == 0) return;
            var delay = available[0].delayMean.val;
            int i = 0;
            {
                while (true)
                {
                    while (i<available.Count && Math.Abs(available[i].delayMean.val - delay) < .1f)
                    {
                        delayGroup.Add(available[i]);
                        i++;
                    }

                    last = delayGroup.TakeRandom(last);
                    last?.Invoke();
                    delayGroup.Clear();
                    if (i < available.Count && i + 1 == available.Count)
                    {
                        available[i].Invoke();
                        break;
                    }
                    if (i < available.Count)
                    {
                        delay = available[i].delayMean.val;
                        // i++;
                    }
                    else break;
                }
            }
        }

        public new void Sort()
        {
            if(sorted) return;
            Sort((x, y) => x.delayMean.val.CompareTo(y.delayMean.val));
            sorted = true;
        }

        public void OnPersonRenamed(string oldUid, string newUid)
        {
            foreach (var dialog in this)
            {
                dialog.OnPersonRenamed(oldUid, newUid);
            }
        }
    }
}