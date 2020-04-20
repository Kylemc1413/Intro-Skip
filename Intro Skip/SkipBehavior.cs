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
    public class SkipBehavior : MonoBehaviour
    {
        private bool _init = false;
        private TextMeshProUGUI _skipPrompt;
        private BeatmapObjectCallbackController _callbackController;
        private AudioSource _songAudio;
        private VRController _leftController = null;
        private VRController _rightController = null;

        
        private bool _skippableOutro = false;
        private bool _skippableIntro = false;
        private float _introSkipTime = -1f;
        private float _outroSkipTime = -1f;
        private float _lastObjectSkipTime = -1f;
        public void Awake()
        {
            if (!(Config.AllowIntroSkip || Config.AllowOutroSkip)) return;
            bool practice = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.practiceSettings != null;
            if(practice || BS_Utils.Gameplay.Gamemode.IsIsolatedLevel) return;

            CreatePrompt();
            var controllers = Resources.FindObjectsOfTypeAll<VRController>();
            foreach (VRController controller in controllers)
            {
                if (_leftController == null && controller.node == UnityEngine.XR.XRNode.LeftHand)
                    _leftController = controller;
                if (_rightController == null && controller.node == UnityEngine.XR.XRNode.RightHand)
                    _rightController = controller;
            }
            _callbackController = Resources.FindObjectsOfTypeAll<BeatmapObjectCallbackController>().FirstOrDefault();
            var audioTimeSync = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault();
            if (audioTimeSync != null)
            {
                _songAudio = audioTimeSync.GetField<AudioSource>("_audioSource");
            }
            StartCoroutine(ReadMap());
        }

        public IEnumerator ReadMap()
        {
            yield return new WaitForSeconds(0.1f);
            var lineData = _callbackController.GetField<BeatmapData>("_beatmapData").beatmapLinesData;
            float firstObjectTime = _songAudio.clip.length;
            float lastObjectTime = -1f;
            foreach(var line in lineData)
            {
                foreach(var beatmapObject in line.beatmapObjectsData)
                {
                    switch(beatmapObject.beatmapObjectType)
                    {
                        case BeatmapObjectType.Note:
                            if (beatmapObject.time < firstObjectTime)
                                firstObjectTime = beatmapObject.time;
                            if (beatmapObject.time > lastObjectTime)
                                lastObjectTime = beatmapObject.time;
                            break;
                        case BeatmapObjectType.Obstacle:
                            ObstacleData obstacle = beatmapObject as ObstacleData;
                            if(!(obstacle.lineIndex == 0 && obstacle.width == 1) && !(obstacle.lineIndex == 3 && obstacle.width == 1))
                            {
                                if (beatmapObject.time < firstObjectTime)
                                    firstObjectTime = beatmapObject.time;
                                if (beatmapObject.time > lastObjectTime)
                                    lastObjectTime = beatmapObject.time;
                            }
                            break;
                    }
                }
            }
            if (firstObjectTime > 5f)
            {
                _skippableIntro = Config.AllowIntroSkip;
                _introSkipTime = firstObjectTime - 2f;
            }

            if ((_songAudio.clip.length - lastObjectTime) >= 5f)
            {
                _skippableOutro = Config.AllowOutroSkip;
                _outroSkipTime = _songAudio.clip.length - 1.5f;
                _lastObjectSkipTime = lastObjectTime + 0.5f;
            }
            _init = true;
            Logger.log.Debug($"Skippable Intro: {_skippableIntro} | Skippable Outro: {_skippableOutro}");
            Logger.log.Debug($"First Object Time: {firstObjectTime} | Last Object Time: {lastObjectTime}");
            Logger.log.Debug($"Intro Skip Time: {_introSkipTime} | Outro Skip Time: {_outroSkipTime}");
        }


        private void CreatePrompt()
        {
            var skipPromptObject = new GameObject("IntroSkip Prompt");
            skipPromptObject.transform.position = new Vector3(-2.5f, 2.1f, 7.0f);
            skipPromptObject.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);

            Canvas _canvas = skipPromptObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(100, 50);

            _skipPrompt = BeatSaberUI.CreateText(_canvas.transform as RectTransform, "Press Trigger To Skip", new Vector2(0, 10));
            rectTransform = _skipPrompt.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = new Vector2(100, 20);
            _skipPrompt.fontSize = 15f;
            _canvas.enabled = true;
            _skipPrompt.gameObject.SetActive(false);
        }
        public void Update()
        {
            if (!_init || _songAudio == null) return;
            if (!(_skippableIntro || _skippableOutro)) return;
            float time = _songAudio.time;
            bool introPhase = (time < _introSkipTime) && _skippableIntro;
            bool outroPhase = (time > _lastObjectSkipTime && time < _outroSkipTime) && _skippableOutro;
            if (introPhase || outroPhase)
            {
                if (!_skipPrompt.gameObject.activeSelf)
                    _skipPrompt.gameObject.SetActive(true);
            }
            else 
            {
                if (_skipPrompt.gameObject.activeSelf)
                    _skipPrompt.gameObject.SetActive(false);
                return;
            }
            if (_leftController.triggerValue >= .8 || _rightController.triggerValue >= .8 || Input.GetKey(KeyCode.I))
            {
               StartCoroutine(OneShotRumbleCoroutine(_leftController, 0.2f, 1));
               StartCoroutine(OneShotRumbleCoroutine(_rightController, 0.2f, 1));
                if (introPhase)
                {
                    _songAudio.time = _introSkipTime;
                    _skippableIntro = false;
                }

                else if (outroPhase)
                {
                    _songAudio.time = _outroSkipTime;
                    _skippableOutro = false;
                }


            }


        }


        public IEnumerator OneShotRumbleCoroutine(VRController controller, float duration, float impulseStrength, float intervalTime = 0f)
        {
            VRPlatformHelper vr = Resources.FindObjectsOfTypeAll<VRPlatformHelper>().First();
            YieldInstruction waitForIntervalTime = new WaitForSeconds(intervalTime);
            float time = Time.time + 0.1f;
            while (Time.time < time)
            {
                vr.TriggerHapticPulse(controller.node, impulseStrength);
                yield return intervalTime > 0 ? waitForIntervalTime : null;
            }
        }
    }
}
