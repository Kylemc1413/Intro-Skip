using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntroSkip
{

    public static class Config
    {
        public static BS_Utils.Utilities.Config ModPrefs = new BS_Utils.Utilities.Config("IntroSkip");
        public static bool AllowIntroSkip = true;
        public static bool AllowOutroSkip = true;


        public static void Read()
        {
            AllowIntroSkip = ModPrefs.GetBool("IntroSkip", "allowIntroSkip", true, true);
            AllowOutroSkip = ModPrefs.GetBool("IntroSkip", "allowOutroSkip", true, true);
        }
        public static void Write()
        {
            ModPrefs.SetBool("IntroSkip", "allowIntroSkip", AllowIntroSkip);
            ModPrefs.SetBool("IntroSkip", "allowOutroSkip", AllowOutroSkip);
        }
    }
}
