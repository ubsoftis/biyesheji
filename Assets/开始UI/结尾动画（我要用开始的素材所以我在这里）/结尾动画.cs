using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CubeAnimationController : MonoBehaviour
{
    // ===================================================================
    // ====== 制作人名单（新增）======
    // ===================================================================
    [Header("==== 制作人名单 ====")]
    [Tooltip("是否启用片头制作人名单（关闭后直接播放原动画）")]
    public bool enableCredits = true;

    [Tooltip("制作人名单总Canvas的CanvasGroup（用于整体显隐）")]
    public CanvasGroup creditsCanvasGroup;

    [Tooltip("名单各段（每段一个CanvasGroup，按播放顺序拖入）\n例如：特别感谢、游戏美术、游戏策划、游戏程序、感谢游玩")]
    public CanvasGroup[] creditsSegments;

    [Tooltip("每段淡入时间（秒）")]
    public float creditsFadeInTime = 0.8f;
    [Tooltip("每段完全显示停留时间（秒）")]
    public float creditsHoldTime = 2.6f;
    [Tooltip("每段淡出时间（秒）")]
    public float creditsFadeOutTime = 0.6f;
    [Tooltip("段与段之间的重叠时间（上一段还剩多少秒淡出时，下一段开始淡入；0=完全消失后再出现）")]
    public float creditsSegmentOverlap = 0f;

    [Tooltip("名单全部播放完后，整个Canvas淡出的时间（衔接到开场黑屏）")]
    public float creditsCanvasFadeOutTime = 0.5f;
    [Tooltip("名单结束后等待多少秒再进入原动画")]
    public float delayAfterCredits = 0.3f;

    [Header("==== 名单背景图 ====")]
    [Tooltip("名单期间的背景图（Image或RawImage，放在CreditsCanvas下作为子物体）")]
    public CanvasGroup creditsBackgroundGroup;
    [Tooltip("背景图从黑屏淡入的时间")]
    public float creditsBackgroundFadeInTime = 1.5f;

    [Header("==== 3D场景黑色遮罩（防止3D场景露出来）====")]
    [Tooltip("一个独立的黑色遮罩Canvas/Image的CanvasGroup，Sort Order应设为最低，覆盖在3D场景上层、CreditsCanvas下层")]
    public CanvasGroup blackMaskGroup;
    [Tooltip("遮罩是否在片尾结束时跟随淡出（一般不淡出，让原动画的blackOverlay接管）")]
    public bool fadeBlackMaskOnEnd = false;
    [Tooltip("如果上面勾选了，遮罩淡出的时间")]
    public float blackMaskFadeOutTime = 0.5f;

    [Header("==== 装饰白线（独立于段落，开头淡入结尾淡出）====")]
    [Tooltip("装饰白线的CanvasGroup（独立放在CreditsCanvas下，不在任何Segment里）")]
    public CanvasGroup decorationLinesGroup;
    [Tooltip("白线淡入时间（在第一段开始之前完成）")]
    public float decorationFadeInTime = 0.8f;
    [Tooltip("白线完全显示后，等待多少秒再开始第一段（让白线先稳定显示一会儿）")]
    public float decorationHoldBeforeFirstSegment = 0.5f;
    [Tooltip("白线淡出时间（与最后一段淡出同时进行）")]
    public float decorationFadeOutTime = 0.6f;

    [Header("==== 片尾开头黑屏 ====")]
    [Tooltip("片尾开始前先停留黑屏的时间（让玩家有一个'准备进入片尾'的过渡）")]
    public float creditsOpeningBlackHoldTime = 1.0f;

    [Header("==== 段落上浮入场 ====")]
    [Tooltip("【全局默认值】带 RiseOnFadeIn 组件的Text从下方多少像素的位置浮上来。\n每个RiseOnFadeIn组件可以单独覆盖这个值。\n不挂RiseOnFadeIn组件的Text将不会上浮，只淡入。")]
    public float segmentRiseDistance = 60f;

    [Tooltip("上浮文字完全到位后，等待多少秒再让段落里其他（不上浮的）文字淡入。\n0=同时出现；越大=层次感越强")]
    public float delayBeforeNonRisingFadeIn = 0.3f;

    [Header("==== 片尾期间隐藏的其他Canvas ====")]
    [Tooltip("片尾期间需要隐藏的其他Canvas（比如游戏中的UI Canvas），原动画开始时会自动显示")]
    public GameObject[] hideDuringCredits;

    [Header("==== 片尾BGM（新增）====")]
    [Tooltip("片尾曲BGM（贯穿整个片尾：名单→Cube动画→故障效果）")]
    public AudioClip creditsBGM;
    [Range(0f, 1f)]
    public float creditsBGMVolume = 0.8f;
    [Tooltip("BGM是否循环（一般关闭，让它自然播放完）")]
    public bool creditsBGMLoop = false;

    private AudioSource creditsBGMSource; // 自动创建
    // ===================================================================
    // ====== 制作人名单字段结束 ======
    // ===================================================================

    [Header("==== 摄像机设置 ====")]
    public Camera targetCamera;
    public Camera[] camerasToDisable;

    [Header("==== 正方体设置 ====")]
    public Transform cube;
    public Transform pos1;
    public Transform pos2;
    public Transform pos3;

    [Header("==== 移动时间（秒） ====")]
    public float moveTime3to2 = 3f;
    public float pauseAt2 = 1f;
    public float moveTime2to1 = 3f;
    [Tooltip("到达位置1后停留多少秒再开始故障效果")]
    public float pauseAt1 = 2f;

    [Header("==== 自转设置（仅3→2阶段，绕Z轴） ====")]
    public float rotateSpeed = 90f;
    public bool rotateDuringPause = true;

    [Header("==== UI Canvas（素材） ====")]
    public CanvasGroup uiCanvasGroup;
    public float uiFadeTime = 2f;
    public RectTransform[] uiElements;
    public float scatterDistance = 500f;

    [Header("==== 背景设置 ====")]
    public RawImage backgroundImage;
    public Texture newBackgroundTexture;
    public float bgFadeToBlackTime = 2f;
    public float bgFadeToNewTime = 2f;
    [Range(0f, 1f)]
    public float bgFadeToBlackStartAt = 0.5f;

    [Header("==== 开场黑屏（淡入） ====")]
    [Tooltip("是否启用开场黑屏淡入效果")]
    public bool enableOpeningFade = true;
    [Tooltip("开场黑屏淡出（从黑变亮）需要多少秒")]
    public float openingFadeTime = 2f;
    [Tooltip("黑屏结束后等多少秒再开始动画")]
    public float delayAfterOpeningFade = 0.5f;

    [Header("==== 音效设置 ====")]
    [Tooltip("正方体在position3时触发的音效（拖入MP3/WAV等）")]
    public AudioClip pos3SoundClip;
    [Tooltip("音效音量（0~1）")]
    [Range(0f, 1f)]
    public float pos3SoundVolume = 1f;
    [Tooltip("音效延迟播放（秒，0=立刻播放）")]
    public float pos3SoundDelay = 0f;
    [Tooltip("是否循环播放")]
    public bool pos3SoundLoop = false;

    [Header("==== 故障效果设置 ====")]
    [Tooltip("故障覆盖层Image（全屏，初始alpha=0）")]
    public Image glitchOverlay;
    [Tooltip("故障条纹覆盖层容器（空RectTransform，初始关闭）")]
    public RectTransform glitchStripContainer;
    [Tooltip("模糊覆盖层Image（全屏，半透明灰白,初始alpha=0）")]
    public Image glitchBlurOverlay;
    [Tooltip("模糊强度（0~1，越大画面越朦胧）")]
    [Range(0f, 1f)]
    public float blurIntensity = 0.25f;
    [Tooltip("故障效果持续时间")]
    public float glitchDuration = 2f;
    [Tooltip("故障强度（条纹数量、跳动频率），0~1")]
    [Range(0f, 1f)]
    public float glitchIntensity = 0.7f;
    [Tooltip("故障期间画面整体偏色（建议灰色调）")]
    public Color glitchTintColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("==== 黑屏设置（结尾，与故障同时进行）====")]
    [Tooltip("黑屏覆盖层Image（全屏黑色，初始alpha=0）")]
    public Image blackOverlay;
    [Tooltip("故障开始后多少秒，黑屏开始淡入")]
    public float blackFadeStartDelay = 0.5f;
    [Tooltip("黑屏淡入时间")]
    public float blackFadeTime = 2f;
    [Tooltip("最终黑屏程度（0=不变黑，1=全黑）")]
    [Range(0f, 1f)]
    public float blackFinalAlpha = 0.9f;

    [Header("==== 场景跳转 ====")]
    [Tooltip("跳转的场景序号（在Build Settings中的Index）")]
    public int sceneIndexToLoad = 1;
    [Tooltip("黑屏完成后等多久跳转")]
    public float waitBeforeLoadScene = 0.5f;
    public bool enableSceneJump = true;

    [Header("==== 开始播放 ====")]
    public bool playOnStart = true;

    private float accumulatedZ = 0f;
    private AudioSource pos3AudioSource; // 自动创建的AudioSource

    void Start()
    {
        SetupCameras();
        InitOverlays();
        InitAudio();
        InitCredits(); // ====== 制作人名单（新增）======

        if (cube != null && pos3 != null)
        {
            cube.position = pos3.position;
            cube.rotation = pos3.rotation;
            cube.localScale = pos3.localScale;
            accumulatedZ = pos3.eulerAngles.z;
        }

        if (playOnStart)
            PlayAnimation();
    }

    void SetupCameras()
    {
        if (targetCamera != null) targetCamera.enabled = true;
        if (camerasToDisable != null)
        {
            foreach (var cam in camerasToDisable)
                if (cam != null) cam.enabled = false;
        }
    }

    void InitOverlays()
    {
        if (glitchOverlay != null)
        {
            Color c = glitchOverlay.color; c.a = 0f;
            glitchOverlay.color = c;
        }
        if (glitchStripContainer != null)
            glitchStripContainer.gameObject.SetActive(false);
        if (glitchBlurOverlay != null)
        {
            Color c = glitchBlurOverlay.color; c.a = 0f;
            glitchBlurOverlay.color = c;
        }

        // 开场黑屏：初始化为全黑或透明
        if (blackOverlay != null)
        {
            Color c = blackOverlay.color;
            c.a = enableOpeningFade ? 1f : 0f;
            blackOverlay.color = c;
        }
    }

    void InitAudio()
    {
        // 自动添加AudioSource组件（如果没有）
        pos3AudioSource = GetComponent<AudioSource>();
        if (pos3AudioSource == null)
            pos3AudioSource = gameObject.AddComponent<AudioSource>();

        pos3AudioSource.playOnAwake = false;
    }

    /// <summary>
    /// 播放position3音效
    /// </summary>
    void PlayPos3Sound()
    {
        if (pos3SoundClip == null || pos3AudioSource == null) return;

        pos3AudioSource.clip = pos3SoundClip;
        pos3AudioSource.volume = pos3SoundVolume;
        pos3AudioSource.loop = pos3SoundLoop;

        if (pos3SoundDelay > 0)
            pos3AudioSource.PlayDelayed(pos3SoundDelay);
        else
            pos3AudioSource.Play();
    }

    public void PlayAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(AnimationSequence());
    }

    IEnumerator AnimationSequence()
    {
        // ====== 制作人名单（新增）======
        if (enableCredits)
        {
            yield return StartCoroutine(PlayCreditsSequence());
            yield return new WaitForSeconds(delayAfterCredits);
        }
        // ====== 制作人名单结束，以下为原动画逻辑，未做改动 ======

        // === 开场黑屏淡出 ===
        if (enableOpeningFade)
        {
            yield return StartCoroutine(FadeFromBlack());
            yield return new WaitForSeconds(delayAfterOpeningFade);
        }

        // === 🔊 正方体在position3，触发音效 ===
        PlayPos3Sound();

        // === UI 消散（与移动同时）===
        StartCoroutine(FadeAndScatterUI());

        // === 3 → 2 ===
        StartCoroutine(FadeBackgroundToBlackDelayed(moveTime3to2 * bgFadeToBlackStartAt));
        yield return StartCoroutine(MoveRotateScale_WithSpin(pos3, pos2, moveTime3to2));

        // === 停留2 ===
        float pauseTimer = 0f;
        while (pauseTimer < pauseAt2)
        {
            if (rotateDuringPause)
            {
                accumulatedZ += rotateSpeed * Time.deltaTime;
                ApplyRotation(pos2.eulerAngles.x, pos2.eulerAngles.y, accumulatedZ);
            }
            pauseTimer += Time.deltaTime;
            yield return null;
        }
        yield return StartCoroutine(AlignZRotation(pos2.eulerAngles.z, pos2.eulerAngles.x, pos2.eulerAngles.y));

        // === 2 → 1 ===
        StartCoroutine(FadeBackgroundToNewImage());
        yield return StartCoroutine(SmoothTransition(pos2, pos1, moveTime2to1));

        // === 停留1 ===
        yield return new WaitForSeconds(pauseAt1);

        // === 故障效果 + 黑屏（同时进行）===
        StartCoroutine(FadeToBlackDelayed(blackFadeStartDelay));
        yield return StartCoroutine(GlitchEffect());

        float blackTotalTime = blackFadeStartDelay + blackFadeTime;
        float remainTime = blackTotalTime - glitchDuration;
        if (remainTime > 0)
            yield return new WaitForSeconds(remainTime);

        // === 跳转场景 ===
        if (enableSceneJump)
        {
            yield return new WaitForSeconds(waitBeforeLoadScene);
            SceneManager.LoadScene(sceneIndexToLoad);
        }

        Debug.Log("✅ 动画播放完成！");
    }

    // ============ 开场黑屏淡出（从黑到透明）============
    IEnumerator FadeFromBlack()
    {
        if (blackOverlay == null) yield break;

        Color startColor = blackOverlay.color;
        startColor.a = 1f;
        Color endColor = blackOverlay.color;
        endColor.a = 0f;

        float timer = 0f;
        while (timer < openingFadeTime)
        {
            float t = timer / openingFadeTime;
            blackOverlay.color = Color.Lerp(startColor, endColor, t);
            timer += Time.deltaTime;
            yield return null;
        }
        blackOverlay.color = endColor;
    }

    // ============ 故障效果（黑白复古版 + 模糊）============
    IEnumerator GlitchEffect()
    {
        if (glitchOverlay == null && glitchStripContainer == null)
        {
            yield return new WaitForSeconds(glitchDuration);
            yield break;
        }

        if (glitchOverlay != null)
            glitchOverlay.color = glitchTintColor;

        if (glitchBlurOverlay != null)
        {
            Color c = glitchBlurOverlay.color;
            c.a = blurIntensity;
            glitchBlurOverlay.color = c;
        }

        if (glitchStripContainer != null)
            glitchStripContainer.gameObject.SetActive(true);

        float timer = 0f;
        float nextStripTime = 0f;

        while (timer < glitchDuration)
        {
            if (timer >= nextStripTime)
            {
                if (glitchStripContainer != null)
                    RefreshGlitchStrips();
                nextStripTime = timer + Random.Range(0.03f, 0.15f) * (1.1f - glitchIntensity);
            }

            if (glitchOverlay != null)
            {
                Color c = glitchTintColor;
                c.a = glitchTintColor.a + Random.Range(-0.1f, 0.1f);
                if (Random.value < 0.05f * glitchIntensity)
                    c.a = Mathf.Min(1f, c.a + 0.3f);
                glitchOverlay.color = c;
            }

            if (glitchBlurOverlay != null)
            {
                Color c = glitchBlurOverlay.color;
                c.a = blurIntensity + Random.Range(-0.05f, 0.05f);
                glitchBlurOverlay.color = c;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (glitchStripContainer != null)
            glitchStripContainer.gameObject.SetActive(false);
        if (glitchOverlay != null)
        {
            Color c = glitchOverlay.color; c.a = 0f;
            glitchOverlay.color = c;
        }
        if (glitchBlurOverlay != null)
        {
            Color c = glitchBlurOverlay.color; c.a = 0f;
            glitchBlurOverlay.color = c;
        }
    }

    void RefreshGlitchStrips()
    {
        foreach (Transform child in glitchStripContainer)
            Destroy(child.gameObject);

        float screenW = ((RectTransform)glitchStripContainer.parent).rect.width;
        float screenH = ((RectTransform)glitchStripContainer.parent).rect.height;

        int bigStripCount = Mathf.RoundToInt(Random.Range(2, 8) * glitchIntensity + 1);
        for (int i = 0; i < bigStripCount; i++)
            CreateStrip(screenW, screenH, false);

        int scanLineCount = Mathf.RoundToInt(Random.Range(15, 40) * glitchIntensity);
        for (int i = 0; i < scanLineCount; i++)
            CreateStrip(screenW, screenH, true);
    }

    void CreateStrip(float screenW, float screenH, bool isScanLine)
    {
        GameObject strip = new GameObject(isScanLine ? "ScanLine" : "GlitchStrip",
            typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(glitchStripContainer, false);

        RectTransform rt = strip.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);

        float w, h, x, y;
        Image img = strip.GetComponent<Image>();

        if (isScanLine)
        {
            w = screenW * Random.Range(0.8f, 1.1f);
            h = Random.Range(1f, 2.5f);
            x = Random.Range(-screenW * 0.05f, 0f);
            y = Random.Range(-screenH * 0.5f, screenH * 0.5f);

            float gray = Random.Range(0f, 0.3f);
            img.color = new Color(gray, gray, gray, Random.Range(0.15f, 0.4f));
        }
        else
        {
            w = Random.Range(screenW * 0.2f, screenW * 1.3f);
            h = Random.Range(3f, 35f);
            x = Random.Range(-screenW * 0.2f, screenW * 0.5f);
            y = Random.Range(-screenH * 0.5f, screenH * 0.5f);

            float[] grayPalette = { 0f, 0.1f, 0.2f, 0.5f, 0.8f, 0.95f, 1f };
            float gray = grayPalette[Random.Range(0, grayPalette.Length)];

            float r = gray, g = gray, b = gray;
            if (Random.value < 0.15f)
            {
                float tint = Random.Range(-0.1f, 0.1f);
                r = Mathf.Clamp01(gray + tint);
                b = Mathf.Clamp01(gray - tint);
            }

            img.color = new Color(r, g, b, Random.Range(0.5f, 0.95f));
        }

        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    // ============ 黑屏（带延迟，结尾用）============
    IEnumerator FadeToBlackDelayed(float delay)
    {
        if (blackOverlay == null) yield break;

        yield return new WaitForSeconds(delay);

        Color startColor = blackOverlay.color;
        startColor.a = 0f;
        Color endColor = blackOverlay.color;
        endColor.a = blackFinalAlpha;

        float timer = 0f;
        while (timer < blackFadeTime)
        {
            float t = timer / blackFadeTime;
            blackOverlay.color = Color.Lerp(startColor, endColor, t);
            timer += Time.deltaTime;
            yield return null;
        }
        blackOverlay.color = endColor;
    }

    // ============ 移动+自转（3→2）============
    IEnumerator MoveRotateScale_WithSpin(Transform from, Transform to, float duration)
    {
        Vector3 startPos = from.position;
        Vector3 endPos = to.position;
        Vector3 startScale = from.localScale;
        Vector3 endScale = to.localScale;

        Vector3 startEuler = from.eulerAngles;
        Vector3 endEuler = to.eulerAngles;

        float deltaX = Mathf.DeltaAngle(startEuler.x, endEuler.x);
        float deltaY = Mathf.DeltaAngle(startEuler.y, endEuler.y);

        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cube.position = Vector3.Lerp(startPos, endPos, smoothT);
            cube.localScale = Vector3.Lerp(startScale, endScale, smoothT);

            float currentX = startEuler.x + deltaX * smoothT;
            float currentY = startEuler.y + deltaY * smoothT;

            accumulatedZ += rotateSpeed * Time.deltaTime;
            ApplyRotation(currentX, currentY, accumulatedZ);

            timer += Time.deltaTime;
            yield return null;
        }

        cube.position = endPos;
        cube.localScale = endScale;

        yield return StartCoroutine(AlignZRotation(endEuler.z, endEuler.x, endEuler.y));
    }

    // ============ 平滑过渡（2→1）============
    IEnumerator SmoothTransition(Transform from, Transform to, float duration)
    {
        Vector3 startPos = from.position;
        Vector3 endPos = to.position;
        Vector3 startScale = from.localScale;
        Vector3 endScale = to.localScale;
        Quaternion startRot = cube.rotation;
        Quaternion endRot = to.rotation;

        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cube.position = Vector3.Lerp(startPos, endPos, smoothT);
            cube.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            cube.rotation = Quaternion.Slerp(startRot, endRot, smoothT);

            timer += Time.deltaTime;
            yield return null;
        }

        cube.position = endPos;
        cube.localScale = endScale;
        cube.rotation = endRot;
        accumulatedZ = to.eulerAngles.z;
    }

    // ============ Z 轴对齐 ============
    IEnumerator AlignZRotation(float targetZ, float lockX, float lockY)
    {
        if (Mathf.Abs(rotateSpeed) < 0.0001f)
        {
            ApplyRotation(lockX, lockY, targetZ);
            accumulatedZ = targetZ;
            yield break;
        }

        float currentZ = accumulatedZ;
        float targetAccumulated;

        if (rotateSpeed > 0)
        {
            float currentMod = Mod360(currentZ);
            float diff = targetZ - currentMod;
            if (diff < 0) diff += 360f;
            if (diff < 0.01f) diff = 0f;
            targetAccumulated = currentZ + diff;
        }
        else
        {
            float currentMod = Mod360(currentZ);
            float diff = currentMod - targetZ;
            if (diff < 0) diff += 360f;
            if (diff < 0.01f) diff = 0f;
            targetAccumulated = currentZ - diff;
        }

        float alignDuration = Mathf.Abs((targetAccumulated - accumulatedZ) / rotateSpeed);

        float timer = 0f;
        while (timer < alignDuration)
        {
            accumulatedZ += rotateSpeed * Time.deltaTime;
            ApplyRotation(lockX, lockY, accumulatedZ);
            timer += Time.deltaTime;
            yield return null;
        }

        accumulatedZ = targetAccumulated;
        ApplyRotation(lockX, lockY, targetZ);
    }

    void ApplyRotation(float x, float y, float z)
    {
        cube.rotation = Quaternion.Euler(x, y, z);
    }

    float Mod360(float angle)
    {
        float r = angle % 360f;
        if (r < 0) r += 360f;
        return r;
    }

    // ============ UI 消散 ============
    IEnumerator FadeAndScatterUI()
    {
        if (uiCanvasGroup == null) yield break;

        Vector3[] originalPositions = null;
        Vector3[] scatterTargets = null;
        if (uiElements != null && uiElements.Length > 0)
        {
            originalPositions = new Vector3[uiElements.Length];
            scatterTargets = new Vector3[uiElements.Length];
            for (int i = 0; i < uiElements.Length; i++)
            {
                originalPositions[i] = uiElements[i].localPosition;
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir == Vector2.zero) dir = Vector2.up;
                scatterTargets[i] = originalPositions[i] + new Vector3(dir.x, dir.y, 0) * scatterDistance;
            }
        }

        float timer = 0f;
        while (timer < uiFadeTime)
        {
            float t = timer / uiFadeTime;
            uiCanvasGroup.alpha = 1f - t;

            if (uiElements != null)
            {
                for (int i = 0; i < uiElements.Length; i++)
                {
                    uiElements[i].localPosition = Vector3.Lerp(originalPositions[i], scatterTargets[i], t);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }
        uiCanvasGroup.alpha = 0f;
    }

    IEnumerator FadeBackgroundToBlackDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (backgroundImage == null) yield break;

        Color startColor = backgroundImage.color;
        Color blackColor = Color.black;

        float timer = 0f;
        while (timer < bgFadeToBlackTime)
        {
            float t = timer / bgFadeToBlackTime;
            backgroundImage.color = Color.Lerp(startColor, blackColor, t);
            timer += Time.deltaTime;
            yield return null;
        }
        backgroundImage.color = blackColor;
    }

    IEnumerator FadeBackgroundToNewImage()
    {
        if (backgroundImage == null || newBackgroundTexture == null) yield break;

        backgroundImage.texture = newBackgroundTexture;
        Color startColor = Color.black;
        Color endColor = Color.white;

        float timer = 0f;
        while (timer < bgFadeToNewTime)
        {
            float t = timer / bgFadeToNewTime;
            backgroundImage.color = Color.Lerp(startColor, endColor, t);
            timer += Time.deltaTime;
            yield return null;
        }
        backgroundImage.color = endColor;
    }

    // ===================================================================
    // ====== 制作人名单（新增）======
    // ===================================================================

    /// <summary>
    /// 初始化：把名单Canvas设为完全显示，但每个段落初始alpha=0（不可见）
    /// 创建独立的AudioSource用于播放片尾BGM
    /// </summary>
    void InitCredits()
    {
        // 名单总Canvas初始化为可见（alpha=1），等需要时再整体淡出
        if (creditsCanvasGroup != null)
        {
            creditsCanvasGroup.alpha = enableCredits ? 1f : 0f;
            creditsCanvasGroup.gameObject.SetActive(enableCredits);
        }

        // 每个段落初始全部隐藏
        if (creditsSegments != null)
        {
            foreach (var seg in creditsSegments)
            {
                if (seg != null) seg.alpha = 0f;
            }
        }

        // 预处理：所有RiseOnFadeIn组件在游戏开始时强制配置CanvasGroup
        // 这样即使没在Inspector里勾ignoreParentGroups，运行时也会正确
        if (creditsSegments != null)
        {
            foreach (var seg in creditsSegments)
            {
                if (seg == null) continue;
                RiseOnFadeIn[] risers = seg.GetComponentsInChildren<RiseOnFadeIn>(true);
                foreach (var r in risers)
                {
                    if (r == null) continue;
                    var cg = r.GetComponent<CanvasGroup>();
                    if (cg == null) cg = r.gameObject.AddComponent<CanvasGroup>();
                    cg.ignoreParentGroups = true;
                    cg.alpha = 0f; // 初始隐藏
                }
            }
        }

        // 背景图初始为透明（从黑屏淡入）
        if (creditsBackgroundGroup != null)
            creditsBackgroundGroup.alpha = 0f;

        // 3D场景黑色遮罩：片尾开始就保持全黑显示，挡住3D场景
        if (enableCredits && blackMaskGroup != null)
        {
            blackMaskGroup.gameObject.SetActive(true);
            blackMaskGroup.alpha = 1f;
        }

        // 装饰白线：初始为透明，等第一段开始时一起淡入
        if (decorationLinesGroup != null)
            decorationLinesGroup.alpha = 0f;

        // 片尾期间需要隐藏的其他Canvas
        if (enableCredits && hideDuringCredits != null)
        {
            foreach (var go in hideDuringCredits)
            {
                if (go != null) go.SetActive(false);
            }
        }

        // 创建独立的BGM AudioSource（与原pos3AudioSource分开，互不干扰）
        creditsBGMSource = gameObject.AddComponent<AudioSource>();
        creditsBGMSource.playOnAwake = false;
    }

    /// <summary>
    /// 播放整个制作人名单序列：BGM启动 + 各段依次淡入淡出 + Canvas整体淡出
    /// </summary>
    IEnumerator PlayCreditsSequence()
    {
        // 临时关闭blackOverlay（让背景图能透过来），名单结束后会恢复
        // 这样不影响原动画的"开场黑屏淡出"逻辑
        float savedBlackAlpha = 0f;
        if (blackOverlay != null)
        {
            savedBlackAlpha = blackOverlay.color.a;
            // 先保持黑屏，等"片尾开头黑屏"时间过完再开始
            Color c = blackOverlay.color; c.a = 1f;
            blackOverlay.color = c;
        }

        // 片尾开头黑屏停留（让玩家有一个进入片尾的过渡）
        yield return new WaitForSeconds(creditsOpeningBlackHoldTime);

        // 停留结束，关闭黑屏让背景能透过来
        if (blackOverlay != null)
        {
            Color c = blackOverlay.color; c.a = 0f;
            blackOverlay.color = c;
        }

        // 启动BGM（贯穿整个片尾）
        if (creditsBGM != null && creditsBGMSource != null)
        {
            creditsBGMSource.clip = creditsBGM;
            creditsBGMSource.volume = creditsBGMVolume;
            creditsBGMSource.loop = creditsBGMLoop;
            creditsBGMSource.Play();
        }

        // 背景图淡入（与白线/第一段同时进行，让画面不空）
        if (creditsBackgroundGroup != null)
            StartCoroutine(FadeCreditsBackground());

        // 装饰白线先淡入并停留一会儿，再开始播放段落
        if (decorationLinesGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(decorationLinesGroup, 0f, 1f, decorationFadeInTime));
            yield return new WaitForSeconds(decorationHoldBeforeFirstSegment);
        }

        // 各段依次播放（带重叠）
        if (creditsSegments != null && creditsSegments.Length > 0)
        {
            for (int i = 0; i < creditsSegments.Length; i++)
            {
                CanvasGroup current = creditsSegments[i];
                if (current == null) continue;

                bool isLast = (i == creditsSegments.Length - 1);

                // 启动当前段落的淡入→停留→淡出
                StartCoroutine(FadeSegment(current));

                // 最后一段：在它开始淡出时同步淡出白线
                if (isLast && decorationLinesGroup != null)
                {
                    StartCoroutine(FadeDecorationOutDelayed());
                }

                // 计算下一段开始的等待时间
                // 段落总时长 = 上浮阶段（如果有上浮文字）+ 延迟 + 淡入 + 停留 + 淡出
                // 上浮阶段时长用全局淡入时间近似（如果有自定义时长更长的情况，可能略有偏差，但通常足够）
                bool hasRisers = (current.GetComponentsInChildren<RiseOnFadeIn>(false).Length > 0);
                float risingPhase = hasRisers ? (creditsFadeInTime + delayBeforeNonRisingFadeIn) : 0f;
                float segmentTotal = risingPhase + creditsFadeInTime + creditsHoldTime + creditsFadeOutTime;
                float waitBeforeNext = segmentTotal - creditsSegmentOverlap;

                if (isLast)
                {
                    yield return new WaitForSeconds(segmentTotal);
                }
                else
                {
                    yield return new WaitForSeconds(waitBeforeNext);
                }
            }
        }

        // 名单整体Canvas淡出，衔接到原动画的开场黑屏
        if (creditsCanvasGroup != null)
        {
            float timer = 0f;
            float startAlpha = creditsCanvasGroup.alpha;
            while (timer < creditsCanvasFadeOutTime)
            {
                float t = timer / creditsCanvasFadeOutTime;
                creditsCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                timer += Time.deltaTime;
                yield return null;
            }
            creditsCanvasGroup.alpha = 0f;
            creditsCanvasGroup.gameObject.SetActive(false); // 彻底关掉，避免挡住后面
        }

        // 恢复blackOverlay到原始alpha，让原动画的"开场黑屏淡出"正常工作
        if (blackOverlay != null)
        {
            Color c = blackOverlay.color; c.a = savedBlackAlpha;
            blackOverlay.color = c;
        }

        // 恢复之前隐藏的Canvas，让它们能在原动画开始时显示
        if (hideDuringCredits != null)
        {
            foreach (var go in hideDuringCredits)
            {
                if (go != null) go.SetActive(true);
            }
        }

        // 黑色遮罩：默认保持显示，让原动画的blackOverlay接管；勾选了fadeBlackMaskOnEnd则淡出
        if (blackMaskGroup != null)
        {
            if (fadeBlackMaskOnEnd)
            {
                yield return StartCoroutine(FadeCanvasGroup(blackMaskGroup, blackMaskGroup.alpha, 0f, blackMaskFadeOutTime));
                blackMaskGroup.gameObject.SetActive(false);
            }
            // 不勾选则保持全黑，作为底板继续遮挡3D场景，原动画的blackOverlay淡出时仍能看到黑色
        }
    }

    /// <summary>
    /// 背景图从黑屏（alpha=0）淡入到完全显示
    /// 名单结束时会跟随CreditsCanvasGroup整体淡出，不需要单独淡出
    /// </summary>
    IEnumerator FadeCreditsBackground()
    {
        if (creditsBackgroundGroup == null) yield break;

        float timer = 0f;
        while (timer < creditsBackgroundFadeInTime)
        {
            creditsBackgroundGroup.alpha = timer / creditsBackgroundFadeInTime;
            timer += Time.deltaTime;
            yield return null;
        }
        creditsBackgroundGroup.alpha = 1f;
    }

    /// <summary>
    /// 通用的CanvasGroup淡入淡出协程
    /// </summary>
    IEnumerator FadeCanvasGroup(CanvasGroup cg, float fromAlpha, float toAlpha, float duration)
    {
        if (cg == null) yield break;
        if (duration <= 0f)
        {
            cg.alpha = toAlpha;
            yield break;
        }

        float timer = 0f;
        cg.alpha = fromAlpha;
        while (timer < duration)
        {
            cg.alpha = Mathf.Lerp(fromAlpha, toAlpha, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        cg.alpha = toAlpha;
    }

    /// <summary>
    /// 装饰白线延迟淡出：等最后一段进入"淡出阶段"时再开始淡出
    /// 时机 = 最后一段的（上浮阶段 +）淡入时间 + 停留时间（也就是它开始淡出的那一刻）
    /// </summary>
    IEnumerator FadeDecorationOutDelayed()
    {
        if (decorationLinesGroup == null) yield break;

        // 检查最后一段是否有上浮文字，决定要不要等上浮阶段
        float risingPhase = 0f;
        if (creditsSegments != null && creditsSegments.Length > 0)
        {
            CanvasGroup last = creditsSegments[creditsSegments.Length - 1];
            if (last != null && last.GetComponentsInChildren<RiseOnFadeIn>(false).Length > 0)
                risingPhase = creditsFadeInTime + delayBeforeNonRisingFadeIn;
        }

        yield return new WaitForSeconds(risingPhase + creditsFadeInTime + creditsHoldTime);
        yield return StartCoroutine(FadeCanvasGroup(decorationLinesGroup, 1f, 0f, decorationFadeOutTime));
    }

    /// <summary>
    /// 单个段落的播放流程：
    /// 1. 段落整体先alpha=0（所有不上浮的Text隐藏）
    /// 2. 上浮文字（ignoreParentGroups=true）单独淡入+上浮
    /// 3. 等上浮完成 + 延迟
    /// 4. 段落整体淡入（带动所有不上浮的Text显示出来，上浮文字因为ignoreParentGroups不受影响）
    /// 5. 停留
    /// 6. 段落整体淡出（同时手动淡出上浮文字）
    /// </summary>
    IEnumerator FadeSegment(CanvasGroup seg)
    {
        // 查找当前段落下所有带 RiseOnFadeIn 标记的子物体
        RiseOnFadeIn[] risers = seg.GetComponentsInChildren<RiseOnFadeIn>(false);

        // 关键：在运行时强制确保每个 RiseOnFadeIn 的 CanvasGroup 配置正确
        // 不依赖 Inspector 手动勾选 ignoreParentGroups
        foreach (var r in risers)
        {
            if (r == null) continue;
            var cg = r.GetComponent<CanvasGroup>();
            if (cg == null) cg = r.gameObject.AddComponent<CanvasGroup>();
            cg.ignoreParentGroups = true; // 强制：不受Segment整体CanvasGroup影响
            cg.alpha = 0f;                 // 先隐藏，等下统一淡入
        }

        // 段落整体alpha=0：所有不上浮的Text都隐藏
        // 上浮文字因为ignoreParentGroups=true，alpha由它们自己的CanvasGroup控制（已经是0）
        seg.alpha = 0f;

        // 把所有上浮文字移到下方起始位置
        foreach (var r in risers)
        {
            if (r == null) continue;
            RectTransform rt = r.GetComponent<RectTransform>();
            if (rt != null)
            {
                float dist = (r.riseDistance > 0.01f) ? r.riseDistance : segmentRiseDistance;
                if (dist > 0.01f)
                    rt.anchoredPosition = rt.anchoredPosition + new Vector2(0, -dist);
            }
        }

        // 步骤1：上浮文字单独淡入+上浮
        float maxRiseDuration = 0f;
        foreach (var r in risers)
        {
            if (r == null) continue;
            StartCoroutine(RiseOneItem(r));
            float d = r.useGlobalDuration ? creditsFadeInTime : r.customRiseDuration;
            if (d > maxRiseDuration) maxRiseDuration = d;
        }

        // 步骤2：等上浮全部完成
        if (risers.Length > 0)
        {
            yield return new WaitForSeconds(maxRiseDuration);
            // 步骤2b：上浮完成后停顿一下，再让非上浮文字出现
            yield return new WaitForSeconds(delayBeforeNonRisingFadeIn);
        }

        // 步骤3：段落整体淡入
        // 上浮文字因为ignoreParentGroups=true，不会被这次淡入再次"亮起来"
        // 这次淡入只影响那些"没挂RiseOnFadeIn"的Text
        float timer = 0f;
        while (timer < creditsFadeInTime)
        {
            seg.alpha = timer / creditsFadeInTime;
            timer += Time.deltaTime;
            yield return null;
        }
        seg.alpha = 1f;

        // 步骤4：停留
        yield return new WaitForSeconds(creditsHoldTime);

        // 步骤5：淡出
        // 段落整体淡出（带动非上浮文字）
        // 同时手动淡出上浮文字（因为它们ignoreParentGroups不受段落alpha影响）
        timer = 0f;
        while (timer < creditsFadeOutTime)
        {
            float a = 1f - (timer / creditsFadeOutTime);
            seg.alpha = a;

            foreach (var r in risers)
            {
                if (r == null) continue;
                var cg = r.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = a;
            }

            timer += Time.deltaTime;
            yield return null;
        }
        seg.alpha = 0f;
        foreach (var r in risers)
        {
            if (r == null) continue;
            var cg = r.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
        }
    }

    /// <summary>
    /// 让单个带 RiseOnFadeIn 标记的物体从下方上浮到原位
    /// 注意：起始位置已经在 FadeSegment 里设置过了，这里直接从当前位置浮到目标位置
    /// </summary>
    IEnumerator RiseOneItem(RiseOnFadeIn riser)
    {
        RectTransform rt = riser.GetComponent<RectTransform>();
        if (rt == null) yield break;

        // 决定距离
        float distance = (riser.riseDistance > 0.01f) ? riser.riseDistance : segmentRiseDistance;

        // 决定时长
        float duration = riser.useGlobalDuration ? creditsFadeInTime : riser.customRiseDuration;

        var cg = riser.GetComponent<CanvasGroup>();

        // 如果距离≈0，只淡入不上浮
        if (distance < 0.01f)
        {
            float t = 0f;
            while (t < duration)
            {
                if (cg != null) cg.alpha = t / duration;
                t += Time.deltaTime;
                yield return null;
            }
            if (cg != null) cg.alpha = 1f;
            yield break;
        }

        // 起始位置：当前位置（FadeSegment已经把它移到下方了）
        Vector2 startPos = rt.anchoredPosition;
        // 目标位置：当前位置 + 距离（往上）
        Vector2 originalPos = startPos + new Vector2(0, distance);

        if (duration <= 0f)
        {
            rt.anchoredPosition = originalPos;
            if (cg != null) cg.alpha = 1f;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            rt.anchoredPosition = Vector2.Lerp(startPos, originalPos, smoothT);
            if (cg != null) cg.alpha = t; // 同步淡入
            timer += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = originalPos;
        if (cg != null) cg.alpha = 1f;
    }

    // ===================================================================
    // ====== 制作人名单结束 ======
    // ===================================================================
}