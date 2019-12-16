using IPA;
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
using System.Drawing;
namespace Intro_Skip
{
    public class Plugin : IBeatSaberPlugin
    {

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
        private static BS_Utils.Gameplay.LevelData _mainGameSceneSetupData = null;
        private static AudioSource _songAudio;
        GameObject promptObject;
        TextMeshProUGUI _skipPrompt;
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
        internal static bool isIsolated = false;
        public static BS_Utils.Utilities.Config ModPrefs = new BS_Utils.Utilities.Config("IntroSkip");
        public void OnApplicationStart()
        {
            allowIntroSkip = ModPrefs.GetBool("IntroSkip", "skipLongIntro", true, true);
            allowOutroSkip = ModPrefs.GetBool("IntroSkip", "allowOutroSkip", true, true);
        }


        public void OnSceneLoaded(Scene scene, LoadSceneMode arg1)
        {

            if (scene.name == "MenuCore")
            {

                CreateUI();
            }
        }

        public void OnActiveSceneChanged(Scene arg0, Scene scene)
        {   //Handle quitting/restarting song mid special event

            hasSkippedIntro = false;
            hasSkippedOutro = false;
            promptPlayer = false;
            isLevel = false;
            ReadPreferences();
            if (scene.name == "MenuViewControllers")
            {

                firstObjectTime = 1000000;
                introSkipTime = 0;

            }
            if (scene.name == "GameCore")
            {
                isIsolated = BS_Utils.Gameplay.Gamemode.IsIsolatedLevel;
                if (_mainGameSceneSetupData == null)
                {
                    _mainGameSceneSetupData = BS_Utils.Plugin.LevelData;
                }
             //   Log("Game scene");
                skipIntro = false;
                skipOutro = false;
                isLevel = true;
                allowedToSkipIntro = false;
                allowedToSkipOutro = false;
                if (_mainGameSceneSetupData != null)
                {
              //      if (!isIsolated)
                //    {
                        if (allowIntroSkip == true || allowOutroSkip == true)
                            InitIntroSkip();
                  //  }
               //     else
                //        Log("Isolated");


                }
                else
                {
                    isLevel = false;
                    DestroyPrompt();
                }

                SharedCoroutineStarter.instance.StartCoroutine(DelayedSetSkip());
                SharedCoroutineStarter.instance.StartCoroutine(DelayedCheckOutro());
                // _mainGameSceneSetupData is StandardLevelSceneSetupDataSO
                if (_mainGameSceneSetupData.GameplayCoreSceneSetupData.practiceSettings != null)
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

        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {

            if (true)
            {
                if (soundIsPlaying == true && _songAudio != null)
                {
                    Log("Pausing");
                    _songAudio.pitch = 0f;
                 //   AudioTimeSync.forcedAudioSync = true;
                }

                if (isLevel == true && (allowIntroSkip == true || allowOutroSkip == true) && _songAudio != null)
                {

                    if (skipIntro == true && _songAudio.time < introSkipTime && hasSkippedIntro == false && allowedToSkipIntro == true)
                    {
                        if (leftController.triggerValue >= .8 || rightController.triggerValue >= .8 || Input.GetKeyDown(KeyCode.I))
                        {
                            Skip();
                            DestroyPrompt();
                            hasSkippedIntro = true;
                            Log("Attempting Haptics");
                            SharedCoroutineStarter.instance.StartCoroutine(OneShotRumbleCoroutine(leftController, 0.2f, 1));
                            SharedCoroutineStarter.instance.StartCoroutine(OneShotRumbleCoroutine(rightController, 0.2f, 1));
                        }

                    }
                    if (skipOutro == true && _songAudio.time >= lastObjectTime && allowedToSkipOutro)
                    {
                        if (promptPlayer == true)
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
        public void InitIntroSkip()
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
        //    Log("Left:" + leftController.ToString());
          //  Log("Right: " + rightController.ToString());

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
         //   Log("Level Found");
            CheckSkip();

        }

        public void CheckSkip()
        {
            foreach (BeatmapLineData lineData in _mainGameSceneSetupData.GameplayCoreSceneSetupData.difficultyBeatmap.beatmapData.beatmapLinesData)
            {
             //   Log("Parsing Line");
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


        }

        public IEnumerator DelayedCheckOutro()
        {
            yield return new WaitForSeconds(1f);
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
       //     Log("Creating Prompt");
            promptObject = new GameObject("Prompt");
            promptObject.transform.position = new Vector3(-2.5f, 2.1f, 7.0f);
            promptObject.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);

            Canvas _canvas = promptObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(100, 50);

            _skipPrompt = CreateText(_canvas.transform as RectTransform, "Press Trigger To Skip", new Vector2(0, 10));
            rectTransform = _skipPrompt.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = new Vector2(100, 20);
            _skipPrompt.fontSize = 15f;
            _canvas.enabled = true;

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
            /*
            if (_introSkipIcon == null)
                _introSkipIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("Intro_Skip.Resources.IntroSkip.png");

            var introSkipMenu = GameplaySettingsUI.CreateSubmenuOption(GameplaySettingsPanels.ModifiersLeft, "Intro Skip", "MainMenu", "IntroSkip", "Intro Skip Settings", _introSkipIcon);

            var introSkipOption = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.ModifiersLeft, "Intro Skipping", "IntroSkip", "Gives Option to skip sufficiently long empty song intro");
            introSkipOption.GetValue = ModPrefs.GetBool("IntroSkip", "allowIntroSkip", true, true);
            introSkipOption.OnToggle += (value) => { ModPrefs.SetBool("IntroSkip", "allowIntroSkip", value); Log("Changed Modprefs value"); };

            var outroSkipOption = GameplaySettingsUI.CreateToggleOption(GameplaySettingsPanels.ModifiersLeft, "Outro Skipping", "IntroSkip", "Gives Option to skip sufficiently long empty song outro");
            outroSkipOption.GetValue = ModPrefs.GetBool("IntroSkip", "allowOutroSkip", true, true);
            outroSkipOption.OnToggle += (value) => { ModPrefs.SetBool("IntroSkip", "allowOutroSkip", value); Log("Changed Modprefs value"); };
            */
        }

        public IEnumerator OneShotRumbleCoroutine(VRController controller, float duration, float impulseStrength, float intervalTime = 0f)
        {
            VRPlatformHelper vr = Resources.FindObjectsOfTypeAll<VRPlatformHelper>().First();
            YieldInstruction waitForIntervalTime = new WaitForSeconds(intervalTime);
            float time = Time.time + 0.1f;
            while (Time.time < time && isLevel == true)
            {
                vr.TriggerHapticPulse(controller.node, impulseStrength);
                yield return intervalTime > 0 ? waitForIntervalTime : null;
            }
        }

        public static TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 anchoredPosition)
        {
            return CreateText(parent, text, anchoredPosition, new Vector2(60f, 10f));
        }

        /// <summary>
        /// Creates a TextMeshProUGUI component.
        /// </summary>
        /// <param name="parent">Thet transform to parent the new TextMeshProUGUI component to.</param>
        /// <param name="text">The text to be displayed.</param>
        /// <param name="anchoredPosition">The position the text component should be anchored to.</param>
        /// <param name="sizeDelta">The size of the text components RectTransform.</param>
        /// <returns>The newly created TextMeshProUGUI component.</returns>
        public static TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject gameObj = new GameObject("CustomUIText");
            gameObj.SetActive(false);

            TextMeshProUGUI textMesh = gameObj.AddComponent<TextMeshProUGUI>();
            textMesh.font = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = UnityEngine.Color.white;

            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.sizeDelta = sizeDelta;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;

            gameObj.SetActive(true);
            return textMesh;
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }
    }
}
