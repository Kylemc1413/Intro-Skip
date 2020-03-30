using IPA;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BeatSaberMarkupLanguage;
using TMPro;
namespace IntroSkip
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {

        [OnStart]
        public void OnApplicationStart()
        {
            Config.Read();
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += BSEvents_gameSceneLoaded;
            BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup.instance.AddTab("Intro Skip", "IntroSkip.UI.BSML.modifierUI.bsml", UI.ModifierUI.instance);

        }

        [Init]
        public void Init(IPA.Logging.Logger logger)
        {
            Logger.log = logger;
        }
        private void BSEvents_gameSceneLoaded()
        {
            Config.Read();
            if(Config.AllowIntroSkip || Config.AllowOutroSkip)
            new GameObject("IntroSkip Behavior").AddComponent<SkipBehavior>();
        }

        [OnExit]
        public void OnApplicationQuit()
        {

        }

    
    }
}
