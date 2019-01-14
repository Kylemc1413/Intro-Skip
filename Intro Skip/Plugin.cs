using IllusionPlugin;
using IllusionInjector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using System.Media;
using TMPro;
using UnityEngine.XR;
using CustomUI.GameplaySettings;
using System.Drawing;
namespace Intro_Skip
{
    public class Plugin : IPlugin
    {
        public string Name => "Intro Skip";
        public string Version => "2.1.1";
        private readonly string[] env = { "DefaultEnvironment", "BigMirrorEnvironment", "TriangleEnvironment", "NiceEnvironment" };

        public static bool skipIntro = false;
        public static bool skipOutro = false;
        public static bool isLevel = false;
        public static bool allowIntroSkip = false;
        public static bool allowOutroSkip = false;
        public static bool promptPlayer = false;
        public static bool hasSkippedIntro = false;
        public static bool hasSkippedOutro = false;
        public static bool allowedToSkipIntro = false;
        public static bool allowedToSkipOutro = false;
        public static float firstObjectTime = 0;
        public static float lastObjectTime = 0;
        public static float introSkipTime = 0;
        public static float outroSkipTime = 0;
        private static StandardLevelSceneSetupDataSO _mainGameSceneSetupData = null;
        private static AudioSource _songAudio;
        GameObject promptObject;
        TextMeshPro _skipPrompt;
        public static AudioTimeSyncController AudioTimeSync { get; private set; }
        private static Sprite _introSkipIcon;
        VRController leftController;
        VRController rightController;
        //Special Event stuffs
        public static bool specialEvent = true;
        PlatformLeaderboardsModel obj;
        string playerID;
        bool soundIsPlaying = false;
        bool firstCreate = false;
        int i = 0;
        public static bool multiActive = false;
        public void OnApplicationStart()
        {

            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            allowIntroSkip = ModPrefs.GetBool("IntroSkip", "skipLongIntro", true, true);

        }
      

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {

            if (scene.name == "Menu")
            {

                CreateUI();
            }
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {   //Handle quitting/restarting song mid special event

            hasSkippedIntro = false;
            hasSkippedOutro = false;
            promptPlayer = false;
            isLevel = false;
            ReadPreferences();
            if (scene.name == "Menu")
            {

                firstObjectTime = 1000000;
                introSkipTime = 0;

            }
            if (scene.name == "GameCore")
            {
                if (_mainGameSceneSetupData == null)
                {
                    _mainGameSceneSetupData = Resources.FindObjectsOfTypeAll<StandardLevelSceneSetupDataSO>().FirstOrDefault();
                }
                Log("Game scene");
                skipIntro = false;
                skipOutro = false;
                isLevel = true;
                allowedToSkipIntro = false;
                allowedToSkipOutro = false;
                if (_mainGameSceneSetupData != null)
                {
                    if (PluginManager.Plugins.Any(x => x.Name == "Beat Saber Multiplayer"))
                    {
                        GameObject client = GameObject.Find("MultiplayerClient");
                        if (client != null)
                        {
                            multiActive = true;
                            Log("Found MultiplayerClient game object!");

                        }
                        else
                        {
                            multiActive = false;
                            Log(" MultiplayerClient game object not found!");
                        }
                    }
                    if (multiActive == false)
                        if (allowIntroSkip == true || allowOutroSkip == true)
                            Init();

                }
                else
                {
                    isLevel = false;
                    DestroyPrompt();
                }

                SharedCoroutineStarter.instance.StartCoroutine(DelayedSetSkip());
                // _mainGameSceneSetupData is StandardLevelSceneSetupDataSO
                if (_mainGameSceneSetupData.gameplayCoreSetupData.practiceSettings != null)
                    Log("Practice mode on");
            }
        }

        public static byte[] GetResource(string ResourceName)
        {
            System.Reflection.Assembly asm = Assembly.GetExecutingAssembly();
            System.IO.Stream stream = asm.GetManifestResourceStream(ResourceName);
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            return data;
        }



        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {

            if (multiActive == false)
            {
                if (soundIsPlaying == true && _songAudio != null)
                {
                    Log("Pausing");
                    _songAudio.pitch = 0f;
                    AudioTimeSync.forcedAudioSync = true;
                }

                if (isLevel == true && (allowIntroSkip == true || allowOutroSkip == true) && _songAudio != null)
                {

                    if (skipIntro == true && _songAudio.time < introSkipTime && hasSkippedIntro == false && allowedToSkipIntro == true)
                    {
                        if (leftController.triggerValue >= .8 || rightController.triggerValue >= .8)
                        {
                            Skip();
                            DestroyPrompt();
                            hasSkippedIntro = true;
                            Log("Attempting Haptics");
                            SharedCoroutineStarter.instance.StartCoroutine(OneShotRumbleCoroutine(leftController, 0.2f, 1));
                            SharedCoroutineStarter.instance.StartCoroutine(OneShotRumbleCoroutine(rightController, 0.2f, 1));
                        }
                        //Hec U voolas
                        if (Name.Contains( "Voolas"))
                            Application.Quit();
                    }
                    if(skipOutro == true && _songAudio.time >= lastObjectTime && allowedToSkipOutro)
                    {
                        if(promptPlayer == true)
                        {
                            CreateSkipPrompt(true);
                            promptPlayer = false;
                        }
                        if (promptPlayer == false)
                        {
                            if (leftController.triggerValue >= .8 || rightController.triggerValue >= .8 && !hasSkippedOutro)
                            {
                                DestroyPrompt();
                                Skip();
                                skipOutro = false;
                                Log("Attempting Haptics");
                                SharedCoroutineStarter.instance.StartCoroutine(OneShotRumbleCoroutine(leftController, 0.2f, 1));
                                SharedCoroutineStarter.instance.StartCoroutine(OneShotRumbleCoroutine(rightController, 0.2f, 1));
                            }
                        }
                    }
                    if (_songAudio.time > introSkipTime && skipIntro == true && !(_songAudio.time >= lastObjectTime))
                    {
                        DestroyPrompt();
                    }
                }
            }


        }

        public void OnFixedUpdate()
        {
        }

        public static void Log(string message)
        {
            Console.WriteLine("[{0}] {1}", "IntroSkip", message);
        }

        public void DestroyPrompt()
        {
            var obj = GameObject.Find("Prompt");
            if (obj != null)
                GameObject.Destroy(obj);
        }
        public void Init()
        {
            var controllers = Resources.FindObjectsOfTypeAll<VRController>();
            foreach (VRController controller in controllers)
            {
                //        Log(controller.ToString());
                if (controller.ToString() == "ControllerLeft (VRController)")
                    leftController = controller;
                if (controller.ToString() == "ControllerRight (VRController)")
                    rightController = controller;
            }
            Log("Left:" + leftController.ToString());
            Log("Right: " + rightController.ToString());

            obj = Resources.FindObjectsOfTypeAll<PlatformLeaderboardsModel>().FirstOrDefault();
            if (obj != null)
                playerID = ReflectionUtil.GetField<string>(obj, "_playerId");

            specialEvent = ModPrefs.GetBool("IntroSkip", "specialEvent", true, true);
            AudioTimeSync = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault();
            if (AudioTimeSync != null)
            {
                _songAudio = AudioTimeSync.GetField<AudioSource>("_audioSource");
                if (_songAudio != null)
                {
                    Log("Audio not null");
                    Log("Object Found");
                }
            }
            else
                Log("Object is null");
            Log("Level Found");
            CheckSkip();

        }

        public void CheckSkip()
        {
            foreach (BeatmapLineData lineData in _mainGameSceneSetupData.difficultyBeatmap.beatmapData.beatmapLinesData)
            {
                Log("Parsing Line");
                foreach (BeatmapObjectData objectData in lineData.beatmapObjectsData)
                {
                        if (objectData.beatmapObjectType == BeatmapObjectType.Note)
                    {
                        //   Console.WriteLine("Note or Bomb found");
                        //  Console.WriteLine(objectData.time);
                        if (objectData.time < firstObjectTime)
                            firstObjectTime = objectData.time;
                        if (objectData.time > lastObjectTime)
                            lastObjectTime = objectData.time;


                    }
                    else if (objectData.beatmapObjectType == BeatmapObjectType.Obstacle)
                    {
                        ObstacleData obstacle = (ObstacleData)objectData;
                        if (obstacle.lineIndex == 0 && obstacle.width == 1)
                        {
                            //  Console.WriteLine("skipping side wall");

                        }
                        else if (obstacle.lineIndex == 3 && obstacle.width == 1)
                        {
                            //  Console.WriteLine("skipping side wall");

                        }
                        else if (obstacle.duration <= 0 || obstacle.width <= 0)
                        {
                            //   Console.WriteLine("fake wall Found");
                            //   Console.WriteLine(objectData.time);
                            if (objectData.time < firstObjectTime)
                                firstObjectTime = objectData.time;
                            if (objectData.time > lastObjectTime)
                                lastObjectTime = objectData.time;

                        }
                        else
                        {
                            //  Console.WriteLine("Significant wall Found");
                            //    Console.WriteLine(objectData.time);
                            if (objectData.time > lastObjectTime)
                                lastObjectTime = objectData.time;
                            if (objectData.time < firstObjectTime)
                                firstObjectTime = objectData.time;
                        }
                    }
                }
            }
            Log("First note is at " + firstObjectTime.ToString());
            Log("Last object is at " + lastObjectTime.ToString());
            if (firstObjectTime > 5)
                skipIntro = true;
            SharedCoroutineStarter.instance.StartCoroutine(DelayedCheckOutro());


        }

        public IEnumerator DelayedCheckOutro()
        {
            yield return new WaitForSeconds(5f);
            if (SceneManager.GetActiveScene().name != "GameCore") yield break;
            if ((_songAudio.clip.length - lastObjectTime) >= 5)
                skipOutro = true;

            if (skipOutro == true && allowOutroSkip)
            {
                Log("Skippable Outro");
                outroSkipTime = _songAudio.clip.length - 1;
                allowedToSkipOutro = true;
                promptPlayer = true;

            }
            else
                Log("Will Not Skip Outro");
        }
        public void CreateSkipPrompt(bool outro)
        {
            Log("Creating Prompt");
            promptObject = new GameObject("Prompt");
            _skipPrompt = promptObject.AddComponent<TextMeshPro>();
            _skipPrompt.text = "Press Trigger To Skip";
            _skipPrompt.fontSize = 4;
            _skipPrompt.color = UnityEngine.Color.white;
            _skipPrompt.font = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
            _skipPrompt.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 5f);
            _skipPrompt.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1f);
            _skipPrompt.rectTransform.position = new Vector3(-2.5f, 2.1f, 7.0f);
        }




        public void Skip()
        {
            
            SharedCoroutineStarter.instance.StartCoroutine(SkipToTime());
            Log("Attempting to Skip Intro");

            
        }

        private IEnumerator SkipToTime()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            if (_songAudio.time > introSkipTime && allowedToSkipOutro && !hasSkippedOutro)
            {
                _songAudio.time = outroSkipTime;
                hasSkippedOutro = true;
            }

                if (_songAudio.time < introSkipTime && allowedToSkipIntro)
            _songAudio.time = introSkipTime;

            Log("Intro Skipped");


        }

        private IEnumerator DelayedSetSkip()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            if (skipIntro == true && allowIntroSkip)
            {
                Log("Skippable Intro");
                introSkipTime = firstObjectTime - 2;
                promptPlayer = true;
                CreateSkipPrompt(false);
                allowedToSkipIntro = true;

            }
            else
                Log("Will Not Skip Intro");

            
        }

        public static void ReadPreferences()
        {
            allowIntroSkip = ModPrefs.GetBool("IntroSkip", "allowIntroSkip", true, true);
            allowOutroSkip = ModPrefs.GetBool("IntroSkip", "allowOutroSkip", true, true);

        }

        public static void CreateUI()
        {
            if (_introSkipIcon == null)
                _introSkipIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("Intro_Skip.Resources.IntroSkip.png");

            var introSkipMenu = GameplaySettingsUI.CreateSubmenuOption(GameplaySettingsPanels.ModifiersLeft, "Intro Skip", "MainMenu", "IntroSkip", "Intro Skip Settings", _introSkipIcon);

            var introSkipOption = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.ModifiersLeft, "Intro Skipping", "IntroSkip", "Gives Option to skip sufficiently long empty song intro");
            introSkipOption.GetValue = ModPrefs.GetBool("IntroSkip", "allowIntroSkip", true, true);
            introSkipOption.OnToggle += (value) => { ModPrefs.SetBool("IntroSkip", "allowIntroSkip", value); Log("Changed Modprefs value"); };

            var outroSkipOption = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.ModifiersLeft, "Outro Skipping", "IntroSkip","Gives Option to skip sufficiently long empty song outro");
            outroSkipOption.GetValue = ModPrefs.GetBool("IntroSkip", "allowOutroSkip", true, true);
            outroSkipOption.OnToggle += (value) => { ModPrefs.SetBool("IntroSkip", "allowOutroSkip", value); Log("Changed Modprefs value"); };
        }

        public IEnumerator OneShotRumbleCoroutine(VRController controller, float duration, float impulseStrength, float intervalTime = 0f)
        {
            VRPlatformHelper vr = VRPlatformHelper.instance;
            YieldInstruction waitForIntervalTime = new WaitForSeconds(intervalTime);
            float time = Time.time + 0.1f;
            while (Time.time < time && isLevel == true)
            {
                vr.TriggerHapticPulse(controller.node, impulseStrength);
                yield return intervalTime > 0 ? waitForIntervalTime : null;
            }
        }

    }
}
