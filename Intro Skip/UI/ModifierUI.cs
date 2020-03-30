using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Attributes;
namespace IntroSkip.UI
{
    public class ModifierUI : NotifiableSingleton<ModifierUI>
    {
        [UIValue("introSkipToggle")]
        public bool introSkipToggle
        {
            get => Config.AllowIntroSkip;
            set
            {
                Config.AllowIntroSkip = value;
                Config.Write();
            }
        }
        [UIAction("setIntroSkipToggle")]
        void SetIntro(bool value)
        {
            introSkipToggle = value;
        }
        [UIValue("outroSkipToggle")]
        public bool outroSkipToggle
        {
            get => Config.AllowOutroSkip;
            set
            {
                Config.AllowOutroSkip = value;
                Config.Write();
            }
        }
        [UIAction("setOutroSkipToggle")]
        void SetOutro(bool value)
        {
            outroSkipToggle = value;
        }
      

    }
}
