using IllusionPlugin;
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

namespace Intro_Skip
{
    public class Plugin : IPlugin
    {
        public string Name => "Intro Skip";
        public string Version => "0.0.1";
        private readonly string[] env = { "DefaultEnvironment", "BigMirrorEnvironment", "TriangleEnvironment", "NiceEnvironment" };
        public static bool skipIntro = false;
        public static bool isLevel = false;
        public static bool skipLongIntro = false;
        private MainGameSceneSetupData _mainGameSceneSetupData = null;
        public static float firstObjectTime = 0;
        private static AudioSource _songAudio;
        public static float introSkipTime = 0;
        public static AudioTimeSyncController AudioTimeSync { get; private set; }

        //Special Event stuffs
        public static bool specialEvent = true;
        System.Random rnd = new System.Random();
        PlatformLeaderboardsModel obj;
        string playerID;
        SoundPlayer simpleSound = new SoundPlayer(Properties.Resources.gnome);
        bool soundIsPlaying = false;
        bool songIsPaused = false;

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            skipLongIntro = ModPrefs.GetBool("IntroSkip", "skipLongIntro", false, true);
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {   //Handle quitting/restarting song mid special event
                if(soundIsPlaying == true)
                simpleSound.Stop();
            if (songIsPaused == true)
                _songAudio.UnPause();

            if (!_mainGameSceneSetupData)
            {
                _mainGameSceneSetupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().FirstOrDefault();
            }

            if(scene.name == "Menu")
            {

                firstObjectTime = 1000000;
                introSkipTime = 0;

                var skipOption = GameOptionsUI.CreateToggleOption("Skip Long Intros");
                skipOption.GetValue = ModPrefs.GetBool("IntroSkip", "skipLongIntro", false, true); 
                skipOption.OnToggle += (skipLongIntro) => { ModPrefs.SetBool("IntroSkip", "skipLongIntro", skipLongIntro); Log("Changed Modprefs value"); };

            }



            }
        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            skipLongIntro = ModPrefs.GetBool("IntroSkip", "skipLongIntro", false, true);

            if (scene.name == "GameCore")
                if (_mainGameSceneSetupData.gameplayOptions.validForScoreUse)
                    if (skipLongIntro == true)
                    {
                        obj = Resources.FindObjectsOfTypeAll<PlatformLeaderboardsModel>().FirstOrDefault();
                    if (obj != null)
                         playerID = ReflectionUtil.GetField<string>(obj, "_playerId");


                    specialEvent = ModPrefs.GetBool("IntroSkip", "specialEvent", true, true);
                    AudioTimeSync = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault();
                    if (AudioTimeSync != null)
                    {
                        _songAudio = AudioTimeSync.GetField<AudioSource>("_audioSource");
                        if (_songAudio != null)
                            Log("Audio not null");
                        Log("Object Found");
                    }
                    else
                        Log("Object is null");
                    Log("Level Found");
                    foreach (BeatmapLineData lineData in _mainGameSceneSetupData.difficultyLevel.beatmapData.beatmapLinesData)
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

                                }
                                else
                                {
                                    //  Console.WriteLine("Significant wall Found");
                                    //    Console.WriteLine(objectData.time);
                                    if (objectData.time < firstObjectTime)
                                        firstObjectTime = objectData.time;
                                }
                            }


                        }

                    }
                    if (firstObjectTime > 5)
                        skipIntro = true;
                    Log("First note is at " + firstObjectTime.ToString());
                    if (skipIntro == true)
                    {
                        Log("Will Skip Intro");
                        introSkipTime = firstObjectTime - 2;
                        SharedCoroutineStarter.instance.StartCoroutine(DelayedOnSceneLoaded(scene));
                        Log("Attempting to Skip Intro");

                        int chance = rnd.Next(1400, 1500);
                        if (playerID == "76561198055583703")
                            chance = rnd.Next(1400, 1425);
                        Log("Chance number is " + chance);
                        if (specialEvent == true)
                        {


                            if (chance == 1413 || playerID == "1870350353062945" || playerID == "76561197966357374")
                            {
                                Log("Speical Event activating");
                                SharedCoroutineStarter.instance.StartCoroutine(SpecialEvent());
                            }


                        }
                    }

                    else
                        Log("Will Not Skip Intro");


                }
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        { 
        }

        public void OnFixedUpdate()
        {
        }
        public static void Log(string message)
        {
            Console.WriteLine("[{0}] {1}", "IntroSkip", message);
        }


        private IEnumerator DelayedOnSceneLoaded(Scene scene)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            _songAudio.time = introSkipTime;
            Log("Intro Skipped");


        }

        private IEnumerator SpecialEvent()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            _songAudio.Pause();
            songIsPaused = true;
            simpleSound.Load();
            simpleSound.Play();
            soundIsPlaying = true;
            Log("Waiting");
            yield return new WaitForSecondsRealtime(16f);
            soundIsPlaying = false;
            _songAudio.UnPause();
            songIsPaused = false;
            Log("Unpaused");
            
        }

    }
}
