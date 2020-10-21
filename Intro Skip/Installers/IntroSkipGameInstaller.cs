using SiraUtil;
using Zenject;

namespace IntroSkip.Installers
{
    public class IntroSkipGameInstaller : Installer
    {
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;

        public IntroSkipGameInstaller(GameplayCoreSceneSetupData gameplayCoreSceneSetupData)
        {
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
        }

        public override void InstallBindings()
        {
            Config.Read();
            if ((Config.AllowIntroSkip || Config.AllowOutroSkip) && _gameplayCoreSceneSetupData.practiceSettings == null && !BS_Utils.Gameplay.Gamemode.IsIsolatedLevel)
            {
                Container.Bind<SkipBehavior>().FromNewComponentOnNewGameObject("IntroSkip Behavior").AsSingle().NonLazy();
            }
        }
    }
}