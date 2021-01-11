using TMPro;
using Zenject;
using System.Linq;
using UnityEngine;
using IPA.Utilities;
using System.Collections;
using BeatSaberMarkupLanguage;

namespace IntroSkip
{
    public class SkipBehavior : MonoBehaviour
    {
        private bool _init = false;
        private AudioSource _songAudio;
        private TextMeshProUGUI _skipPrompt;
        private IVRPlatformHelper _vrPlatformHelper;
        private BeatmapObjectCallbackController _callbackController;
        private AudioTimeSyncController _audioTimeSyncController;
        private VRControllersInputManager _vrControllersInputManager;

        private bool _skippableOutro = false;
        private bool _skippableIntro = false;
        private float _introSkipTime = -1f;
        private float _outroSkipTime = -1f;
        private float _lastObjectSkipTime = -1f;

        [Inject]
        public void Construct(IVRPlatformHelper vrPlatformHelper, BeatmapObjectCallbackController callbackController, AudioTimeSyncController audioTimeSyncController, VRControllersInputManager vrControllersInputManager)
        {
            _vrPlatformHelper = vrPlatformHelper;
            _callbackController = callbackController;
            _audioTimeSyncController = audioTimeSyncController;
            _vrControllersInputManager = vrControllersInputManager;
        }

        public void Start()
        {
            CreatePrompt();
            _songAudio = _audioTimeSyncController.GetField<AudioSource, AudioTimeSyncController>("_audioSource");
            ReadMap();
        }

        public void ReInit()
        {
            _init = false;
            _skippableIntro = false;
            _skippableOutro = false;
            _introSkipTime = -1;
            _outroSkipTime = -1;
            _lastObjectSkipTime = -1;
            ReadMap();
        }

        public void ReadMap()
        {
         //   yield return new WaitForSeconds(1f);
            var lineData = _callbackController.GetField<IReadonlyBeatmapData, BeatmapObjectCallbackController>("_beatmapData").beatmapLinesData;
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
            if (!(_skippableIntro || _skippableOutro))
            {
                if (_skipPrompt.gameObject.activeSelf) _skipPrompt.gameObject.SetActive(false);
                return;
            }

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
            if ( _audioTimeSyncController.state == AudioTimeSyncController.State.Playing && (_vrControllersInputManager.TriggerValue(UnityEngine.XR.XRNode.LeftHand) >= .8 || _vrControllersInputManager.TriggerValue(UnityEngine.XR.XRNode.RightHand) >= .8 || Input.GetKey(KeyCode.I)))
            {
                Logger.log.Debug("Skip Triggered At:" + time);
                _vrPlatformHelper.TriggerHapticPulse(UnityEngine.XR.XRNode.LeftHand, 0.1f, 0.2f, 1);
                _vrPlatformHelper.TriggerHapticPulse(UnityEngine.XR.XRNode.RightHand, 0.1f, 0.2f, 1);
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

    }
}