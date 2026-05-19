using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace NodeCanvas.DialogueTrees.UI.Examples
{

    public class DialogueUGUI : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>由主工程脚本订阅（NodeCanvas 程序集无法引用 Assembly-CSharp 里的 AudioManager）。</summary>
        public static System.Action<AudioClip, float> TypingSoundPlayHandler;

        public Locales language;

        [System.Serializable]
        public class SubtitleDelays
        {
            public float characterDelay = 0.05f;
            public float sentenceDelay = 0.5f;
            public float commaDelay = 0.1f;
            public float finalDelay = 1.2f;
        }

        //Options...
        [Header("Input Options")]
        public bool skipOnInput;
        public bool waitForInput;

        //Group...
        [Header("Subtitles")]
        public RectTransform subtitlesGroup;
        public Text actorSpeech;
        public Text actorName;
        public Image actorPortrait;
        public RectTransform waitInputIndicator;
        public SubtitleDelays subtitleDelays = new SubtitleDelays();
        public List<AudioClip> typingSounds;
        private AudioSource playSource;

        //Group...
        [Header("Multiple Choice")]
        public RectTransform optionsGroup;
        public Button optionButton;
        private Dictionary<Button, int> cachedButtons;
        private Vector2 originalSubsPosition;
        private bool isWaitingChoice;

        private AudioSource _localSource;
        private AudioSource localSource => _localSource != null ? _localSource : _localSource = gameObject.AddComponent<AudioSource>();

        /// <summary>头像与 portraitAnimSource 的逐帧同步；新字幕开始前必须停掉旧的，否则会串到上一位演员的 Sprite。</summary>
        Coroutine _portraitSyncRoutine;

        private bool anyKeyDown;
        public void OnPointerClick(PointerEventData eventData) => anyKeyDown = true;
        void LateUpdate() => anyKeyDown = false;


        void Awake() { Subscribe(); Hide(); }
        void OnEnable() { UnSubscribe(); Subscribe(); }
        void OnDisable() { UnSubscribe(); }

        void Subscribe() {
            DialogueTree.OnDialogueStarted += OnDialogueStarted;
            DialogueTree.OnDialoguePaused += OnDialoguePaused;
            DialogueTree.OnDialogueFinished += OnDialogueFinished;
            DialogueTree.OnSubtitlesRequest += OnSubtitlesRequest;
            DialogueTree.OnMultipleChoiceRequest += OnMultipleChoiceRequest;
        }

        void UnSubscribe() {
            DialogueTree.OnDialogueStarted -= OnDialogueStarted;
            DialogueTree.OnDialoguePaused -= OnDialoguePaused;
            DialogueTree.OnDialogueFinished -= OnDialogueFinished;
            DialogueTree.OnSubtitlesRequest -= OnSubtitlesRequest;
            DialogueTree.OnMultipleChoiceRequest -= OnMultipleChoiceRequest;
        }

        void Hide() {
            subtitlesGroup.gameObject.SetActive(false);
            optionsGroup.gameObject.SetActive(false);
            optionButton.gameObject.SetActive(false);
            waitInputIndicator.gameObject.SetActive(false);
            originalSubsPosition = subtitlesGroup.transform.position;
        }

        void OnDialogueStarted(DialogueTree dlg) {
            //nothing special...
        }

        void OnDialoguePaused(DialogueTree dlg) {
            subtitlesGroup.gameObject.SetActive(false);
            optionsGroup.gameObject.SetActive(false);
            StopAllCoroutines();
            _portraitSyncRoutine = null;
            playSource?.Stop();
        }

        void OnDialogueFinished(DialogueTree dlg) {
            subtitlesGroup.gameObject.SetActive(false);
            optionsGroup.gameObject.SetActive(false);
            if ( cachedButtons != null ) {
                foreach ( var tempBtn in cachedButtons.Keys ) {
                    if ( tempBtn != null ) {
                        Destroy(tempBtn.gameObject);
                    }
                }
                cachedButtons = null;
            }
            StopAllCoroutines();
            _portraitSyncRoutine = null;
            playSource?.Stop();
        }

        ///----------------------------------------------------------------------------------------------

        void OnSubtitlesRequest(SubtitlesRequestInfo info) {
            StartCoroutine(Internal_OnSubtitlesRequestInfo(info));
        }

        IEnumerator Internal_OnSubtitlesRequestInfo(SubtitlesRequestInfo info) {
            DialogueTree dialogueTree = DialogueTree.currentDialogue;

            var text = info.statement.GetLocalizedText(language);
            var audio = info.statement.GetLocalizedAudio(language);
            var actor = info.actor;

            subtitlesGroup.gameObject.SetActive(true);
            subtitlesGroup.position = originalSubsPosition;
            actorSpeech.text = "";

            actorName.text = actor.name;
            actorSpeech.color = actor.dialogueColor;

            // actorPortrait.gameObject.SetActive(actor.portraitSprite != null);
            //actorPortrait.sprite = actor.portraitSprite;//核心删除的两行
            //接下来的是我黏贴的
            // 修复版：动画连续播+对话框文字正常显示
            DialogueActor curActor = info.actor as DialogueActor;
            actorPortrait.gameObject.SetActive(true);
            SpriteRenderer animSourceRender = null;
            Animator animSourceAnim = null;

            // 初始化动画源组件
            if (curActor != null && curActor.portraitAnimSource != null)
            {
                GameObject animSource = curActor.portraitAnimSource;
                animSource.SetActive(true);
                animSourceRender = animSource.GetComponent<SpriteRenderer>();
                animSourceAnim = animSource.GetComponent<Animator>();
                // 对话触发时强制动画从头播放
                if (animSourceAnim != null) animSourceAnim.Play(0, -1, 0f);
            }

            // 必须停掉上一条字幕的同步协程，否则会沿用上一演员的 SpriteRenderer，出现「同事说话播主角动画」
            if ( _portraitSyncRoutine != null ) {
                StopCoroutine(_portraitSyncRoutine);
                _portraitSyncRoutine = null;
            }
            if ( animSourceRender != null )
                _portraitSyncRoutine = StartCoroutine(RunPortraitSpriteSync(animSourceRender));

            // 无动画/非主角：恢复原版静态头像逻辑
            if (animSourceRender == null && curActor != null)
            {
                actorPortrait.sprite = curActor.portraitSprite;
                actorPortrait.gameObject.SetActive(curActor.portraitSprite != null);
            }

            //上面是我黏贴的

            if ( audio != null ) {
                var actorSource = actor.transform?.GetComponent<AudioSource>();
                playSource = actorSource != null ? actorSource : localSource;
                playSource.clip = audio;
                playSource.Play();
                actorSpeech.text = text;
                var timer = 0f;
                while ( timer < audio.length ) {
                    if ( skipOnInput && anyKeyDown ) {
                        playSource.Stop();
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            if ( audio == null ) {
                var tempText = string.Empty;
                var inputDown = false;
                if ( skipOnInput ) {
                    StartCoroutine(CheckInput(() => { inputDown = true; }));
                }

                for ( int i = 0; i < text.Length; i++ ) {

                    if ( skipOnInput && inputDown ) {
                        actorSpeech.text = text;
                        yield return null;
                        break;
                    }

                    if ( subtitlesGroup.gameObject.activeSelf == false ) {
                        if ( _portraitSyncRoutine != null ) {
                            StopCoroutine(_portraitSyncRoutine);
                            _portraitSyncRoutine = null;
                        }
                        yield break;
                    }

                    char c = text[i];
                    tempText += c;
                    yield return StartCoroutine(DelayPrint(subtitleDelays.characterDelay));
                    PlayTypeSound();
                    if ( c == '.' || c == '!' || c == '?' ) {
                        yield return StartCoroutine(DelayPrint(subtitleDelays.sentenceDelay));
                        PlayTypeSound();
                    }
                    if ( c == ',' ) {
                        yield return StartCoroutine(DelayPrint(subtitleDelays.commaDelay));
                        PlayTypeSound();
                    }

                    actorSpeech.text = tempText;
                }

                if ( !waitForInput ) {
                    yield return StartCoroutine(DelayPrint(subtitleDelays.finalDelay));
                }
            }

            if ( waitForInput ) {
                waitInputIndicator.gameObject.SetActive(true);
                while ( !Input.anyKeyDown ) {
                    yield return null;
                }
                waitInputIndicator.gameObject.SetActive(false);
            }

            yield return null;
            if ( _portraitSyncRoutine != null ) {
                StopCoroutine(_portraitSyncRoutine);
                _portraitSyncRoutine = null;
            }
            subtitlesGroup.gameObject.SetActive(false);
            if ( dialogueTree != null && dialogueTree.isRunning && !dialogueTree.isPaused ) {
                info.Continue();
            }
        }

        IEnumerator RunPortraitSpriteSync(SpriteRenderer sourceRender) {
            try {
                while ( subtitlesGroup.gameObject.activeSelf && sourceRender != null ) {
                    actorPortrait.sprite = sourceRender.sprite;
                    yield return null;
                }
            } finally {
                _portraitSyncRoutine = null;
            }
        }

        void PlayTypeSound() {
            if ( typingSounds.Count > 0 ) {
                var sound = typingSounds[Random.Range(0, typingSounds.Count)];
                if ( sound != null ) {
                    float vol = Random.Range(0.6f, 1f);
                    if ( TypingSoundPlayHandler != null )
                        TypingSoundPlayHandler(sound, vol);
                    else
                        localSource.PlayOneShot(sound, vol);
                }
            }
        }

        IEnumerator CheckInput(System.Action Do) {
            while (!Input.anyKeyDown) {
                yield return null;
            }
            Do();
        }

        IEnumerator DelayPrint(float time) {
            var timer = 0f;
            while ( timer < time ) {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        ///----------------------------------------------------------------------------------------------

        void OnMultipleChoiceRequest(MultipleChoiceRequestInfo info) {

            optionsGroup.gameObject.SetActive(true);
            var buttonHeight = optionButton.GetComponent<RectTransform>().rect.height;
            optionsGroup.sizeDelta = new Vector2(optionsGroup.sizeDelta.x, ( info.options.Values.Count * buttonHeight ) + 20);

            cachedButtons = new Dictionary<Button, int>();
            int i = 0;

            foreach ( KeyValuePair<IStatement, int> pair in info.options ) {
                var btn = (Button)Instantiate(optionButton);
                btn.gameObject.SetActive(true);
                btn.transform.SetParent(optionsGroup.transform, false);
                btn.transform.localPosition = (Vector3)optionButton.transform.localPosition - new Vector3(0, buttonHeight * i, 0);
                btn.GetComponentInChildren<Text>().text = pair.Key.GetLocalizedText(language);
                cachedButtons.Add(btn, pair.Value);
                btn.onClick.AddListener(() => { Finalize(info, cachedButtons[btn]); });
                i++;
            }

            if ( info.showLastStatement ) {
                subtitlesGroup.gameObject.SetActive(true);
                var newY = optionsGroup.anchoredPosition.y + optionsGroup.sizeDelta.y + 1;
               // subtitlesGroup.anchoredPosition = new Vector2(subtitlesGroup.anchoredPosition.x, newY);
            }

            if ( info.availableTime > 0 ) {
                StartCoroutine(CountDown(info));
            }
        }

        IEnumerator CountDown(MultipleChoiceRequestInfo info) {
            isWaitingChoice = true;
            var timer = 0f;
            while ( timer < info.availableTime ) {
                if ( isWaitingChoice == false ) {
                    yield break;
                }
                timer += Time.deltaTime;
                SetMassAlpha(optionsGroup, Mathf.Lerp(1, 0, timer / info.availableTime));
                yield return null;
            }

            if ( isWaitingChoice ) {
                Finalize(info, info.options.Values.Last());
            }
        }

        void Finalize(MultipleChoiceRequestInfo info, int index) {
            isWaitingChoice = false;
            SetMassAlpha(optionsGroup, 1f);
            optionsGroup.gameObject.SetActive(false);
            subtitlesGroup.gameObject.SetActive(false);
            foreach ( var tempBtn in cachedButtons.Keys ) {
                Destroy(tempBtn.gameObject);
            }
            DialogueTree dialogueTree = DialogueTree.currentDialogue;
            if ( dialogueTree != null && dialogueTree.isRunning && !dialogueTree.isPaused ) {
                info.SelectOption(index);
            }
        }

        void SetMassAlpha(RectTransform root, float alpha) {
            foreach ( var graphic in root.GetComponentsInChildren<CanvasRenderer>() ) {
                graphic.SetAlpha(alpha);
            }
        }
    }
}