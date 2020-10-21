using SiraUtil;
using Zenject;

namespace IntroSkip.Installers
{
    public class IntroSkipGameInstaller : Installer
    {
        public override void InstallBindings()
        {
            Config.Read();
            if (Config.AllowIntroSkip || Config.AllowOutroSkip)
            {
                Container.Bind<SkipBehavior>().FromNewComponentOnNewGameObject("IntroSkip Behavior").AsSingle();
            }
        }
    }
}