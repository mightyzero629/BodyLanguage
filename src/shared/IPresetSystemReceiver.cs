using System;
using SimpleJSON;

namespace shared
{
    public interface IPresetSystemReceiver
    {
        JSONClass Store();
        void Load(JSONClass jc);
    }
}