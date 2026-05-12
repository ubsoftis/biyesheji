using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CubeAnimationController : MonoBehaviour
{
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

    [Header("==== 故障效果设置 ====")]
    [Tooltip("故障覆盖层Image（全屏，初始alpha=0）")]
    public Image glitchOverlay;
    [Tooltip("故障条纹覆盖层容器（空RectTransform，初始关闭）")]
    public RectTransform glitchStripContainer;
    [Tooltip("模糊覆盖层Image（全屏，半透明灰白，初始alpha=0）")]
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

    void Start()
    {
        SetupCameras();
        InitOverlays();

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
            c.a = enableOpeningFade ? 1f : 0f;  // 启用开场则一开始全黑
            blackOverlay.color = c;
        }
    }

    public void PlayAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(AnimationSequence());
    }

    IEnumerator AnimationSequence()
    {
        // === 开场黑屏淡出 ===
        if (enableOpeningFade)
        {
            yield return StartCoroutine(FadeFromBlack());
            yield return new WaitForSeconds(delayAfterOpeningFade);
        }

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
        // 启动黑屏（带延迟）但不等待，与故障并行
        StartCoroutine(FadeToBlackDelayed(blackFadeStartDelay));
        yield return StartCoroutine(GlitchEffect());

        // 确保黑屏完成（如果故障比黑屏短）
        // 计算黑屏总用时（延迟 + 淡入）
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

        // 清理（注意：黑屏不清理，因为可能还在淡入或者已经达到目标）
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
}