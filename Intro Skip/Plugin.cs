using IPA;
using UnityEngine;
using SiraUtil.Zenject;
using IntroSkip.Installers;

namespace IntroSkip
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {

        [Init]
        public void Init(IPA.Logging.Logger logger, Zenjector zenjector)
        {
            Logger.log = logger;
            zenjector.OnGame<IntroSkipGameInstaller>();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Config.Read();
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += BSEvents_gameSceneLoaded;
            BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup.instance.AddTab("Intro Skip", "IntroSkip.UI.BSML.modifierUI.bsml", UI.ModifierUI.instance);

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