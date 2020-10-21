using IPA;
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
            zenjector.OnGame<IntroSkipGameInstaller>().ShortCircuitForMultiplayer().ShortCircuitForTutorial();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Config.Read();
            BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup.instance.AddTab("Intro Skip", "IntroSkip.UI.BSML.modifierUI.bsml", UI.ModifierUI.instance);
        }

        [OnExit]
        public void OnApplicationQuit()
        {

        }
    }
}