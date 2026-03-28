using Ami.BroAudio;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _01.Develop.LSW._01._Scripts.UI.InGame
{
    public class DiagonalStarTransition : MonoBehaviour
    {
        [Header("Sound")]
        [SerializeField] private SoundID startSoundID;   
        [SerializeField] private SoundID endSoundID;   

        [Header("References")]
        [SerializeField] private RectTransform transitionRoot;
        [SerializeField] private RectTransform starContainer;
        [SerializeField] private DiagonalFallingStar starPrefab;

        [Header("Settings")]
        [SerializeField] private float starSize = 128f;
        [SerializeField] private float spacingMultiplier = 0.7f;
        [SerializeField] private Vector2 sizeScaleRange = new(0.95f, 1.15f);
        [SerializeField] private float endPositionJitter = 0f;
        [SerializeField] private float startPositionJitter = 30f;
        [SerializeField] private Vector2 durationRange = new(0.8f, 1.2f);

        [Header("Sequential Fill (BottomRight -> TopLeft)")]
        [SerializeField] private float totalStartStaggerTime = 1.2f;

        [Header("After Scene Load (Outro Fall)")]
        [SerializeField] private bool persistAcrossScenes = true;       // ✅ 체크되어 있어야 outro 보임
        [SerializeField] private float outroTotalStaggerTime = 0.8f;
        [SerializeField] private Vector2 outroDurationRange = new(0.45f, 0.65f);
        [SerializeField] private float outroTravelMultiplier = 1.35f;
        [SerializeField] private float afterActivateHold = 0.05f;

        [Header("Canvas Safety")]
        [SerializeField] private int forceSortingOrder = 9999;          // 항상 맨 위에 뜨게

        private static DiagonalStarTransition instance;

        private readonly List<DiagonalFallingStar> stars = new();
        private readonly List<Tween> starTweens = new();
        private readonly List<DiagonalFallingStar> fillOrderStars = new();

        private int totalStars;
        private int completedStars;
        private bool isPlaying;

        private Canvas[] cachedCanvases;
        private GameObject persistentRoot; // DontDestroyOnLoad로 넘길 루트(캔버스 포함)

        private void Awake()
        {
            // 중복 생성 방지 (씬마다 프리팹 또 있으면 바로 사라지는 원인 됨)
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (transitionRoot == null)
            {
                Debug.LogError("[DiagonalStarTransition] transitionRoot is null.");
                return;
            }

            // ✅ 화면에 실제로 보이는 루트(transitionRoot의 최상단)를 보존해야 함
            persistentRoot = transitionRoot.root.gameObject;

            cachedCanvases = persistentRoot.GetComponentsInChildren<Canvas>(true);

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(persistentRoot);

                // 씬 바뀔 때 ScreenSpace-Camera 캔버스 카메라 재연결
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
            }

            ApplyCanvasSafety();

            transitionRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            // 씬 바뀌면 Camera가 바뀌는 경우가 많아서 캔버스 카메라 다시 잡아줌
            ApplyCanvasSafety();
        }

        private void ApplyCanvasSafety()
        {
            if (cachedCanvases == null) return;

            var cam = Camera.main; // 새 씬 카메라
            for (int i = 0; i < cachedCanvases.Length; i++)
            {
                var c = cachedCanvases[i];
                if (c == null) continue;

                c.sortingOrder = forceSortingOrder;

                // ScreenSpace-Camera/WorldSpace면 camera가 필요할 수 있음
                if (c.renderMode == RenderMode.ScreenSpaceCamera || c.renderMode == RenderMode.WorldSpace)
                {
                    // 새 씬에서 기존 카메라가 파괴되어 null일 수 있음
                    if (c.worldCamera == null)
                        c.worldCamera = cam;
                }
            }
        }

        [ContextMenu("Play Transition")]
        public void Play() => Play("MainGameScene");

        public void Play(string nextScene)
        {
            if (isPlaying) return;
            isPlaying = true;

            BroAudio.Play(startSoundID);
            transitionRoot.gameObject.SetActive(true);
            transitionRoot.SetAsLastSibling();

            Canvas.ForceUpdateCanvases();
            ApplyCanvasSafety();

            SpawnStarsSequential();
            StartCoroutine(TransitionSequence(nextScene));
        }

        private struct SpawnInfo
        {
            public int c, r;
            public Vector2 startPos;
            public Vector2 endPos;
        }

        private void SpawnStarsSequential()
        {
            ClearStars();
            starTweens.Clear();
            fillOrderStars.Clear();
            completedStars = 0;

            float screenW = starContainer.rect.width;
            float screenH = starContainer.rect.height;
            float diagonal = Mathf.Sqrt(screenW * screenW + screenH * screenH);

            float step = starSize * spacingMultiplier;

            int cols = Mathf.CeilToInt(diagonal / step) + 4;
            int rows = Mathf.CeilToInt(diagonal / step) + 4;
            totalStars = cols * rows;

            Vector2 gridOrigin = new Vector2(-(cols - 1) * step * 0.5f, (rows - 1) * step * 0.5f);

            // 왼쪽 위(화면 밖)에서 날아오게
            Vector2 startOffset = new Vector2(-diagonal * 1.25f, diagonal * 1.25f);

            List<SpawnInfo> infos = new List<SpawnInfo>(totalStars);

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    float targetX = gridOrigin.x + (c * step) + Random.Range(-endPositionJitter, endPositionJitter);
                    float targetY = gridOrigin.y - (r * step) + Random.Range(-endPositionJitter, endPositionJitter);
                    Vector2 endPos = new Vector2(targetX, targetY);

                    Vector2 randomizedOffset = startOffset + new Vector2(
                        Random.Range(-startPositionJitter, startPositionJitter),
                        Random.Range(-startPositionJitter, startPositionJitter)
                    );
                    Vector2 startPos = endPos + randomizedOffset;

                    infos.Add(new SpawnInfo { c = c, r = r, startPos = startPos, endPos = endPos });
                }
            }

            // 오른쪽 아래부터 순차 정렬
            infos.Sort((a, b) =>
            {
                int dxA = (cols - 1) - a.c;
                int dyA = (rows - 1) - a.r;
                int stepA = dxA + dyA;

                int dxB = (cols - 1) - b.c;
                int dyB = (rows - 1) - b.r;
                int stepB = dxB + dyB;

                int cmp = stepA.CompareTo(stepB);
                if (cmp != 0) return cmp;

                cmp = dxA.CompareTo(dxB);
                if (cmp != 0) return cmp;

                return dyA.CompareTo(dyB);
            });

            float perStarDelay = (totalStars <= 1) ? 0f : (totalStartStaggerTime / (totalStars - 1));

            for (int i = 0; i < infos.Count; i++)
            {
                var info = infos[i];

                var star = Instantiate(starPrefab, starContainer);
                stars.Add(star);
                fillOrderStars.Add(star);

                RectTransform rt = star.rect;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

                float randomScale = Random.Range(sizeScaleRange.x, sizeScaleRange.y);
                rt.sizeDelta = new Vector2(starSize * randomScale, starSize * randomScale);

                float duration = Random.Range(durationRange.x, durationRange.y);
                float delay = i * perStarDelay;

                Tween tween = star.Fall(info.startPos, info.endPos, duration, delay);
                tween.OnComplete(() =>
                {
                    completedStars++;
                    if (rt != null) rt.DOPunchScale(Vector3.one * 0.06f, 0.12f, 1, 0.6f);
                });

                starTweens.Add(tween);
            }
        }

        private IEnumerator TransitionSequence(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null) op.allowSceneActivation = false;

            // 화면 꽉 찰 때까지
            while (completedStars < totalStars)
                yield return null;

            // 씬 활성화
            if (op != null)
            {
                while (op.progress < 0.9f) yield return null;
                op.allowSceneActivation = true;
                while (!op.isDone) yield return null;
            }

            // 씬 바뀐 직후 캔버스 카메라/정렬 재보정
            ApplyCanvasSafety();

            yield return new WaitForSecondsRealtime(afterActivateHold);

            BroAudio.Play(endSoundID);
            // ✅ outro: 오른쪽 아래 별부터 오른쪽 아래 방향으로 떨어짐
            yield return PlayOutroFall();

            ClearStars();
            starTweens.Clear();
            transitionRoot.gameObject.SetActive(false);
            isPlaying = false;
        }

        private IEnumerator PlayOutroFall()
        {
            Canvas.ForceUpdateCanvases();

            float screenW = starContainer.rect.width;
            float screenH = starContainer.rect.height;
            float diagonal = Mathf.Sqrt(screenW * screenW + screenH * screenH);

            Vector2 outVector = new Vector2(diagonal * outroTravelMultiplier, -diagonal * outroTravelMultiplier);

            int count = fillOrderStars.Count;
            if (count == 0) yield break;

            float perDelay = (count <= 1) ? 0f : (outroTotalStaggerTime / (count - 1));
            int finished = 0;

            // 기존 트윈 정리
            foreach (var t in starTweens)
                if (t != null && t.IsActive()) t.Kill(false);
            starTweens.Clear();

            for (int i = 0; i < count; i++)
            {
                var star = fillOrderStars[i];
                if (star == null) { finished++; continue; }

                RectTransform rt = star.rect;

                Vector2 start = rt.anchoredPosition;
                Vector2 end = start + outVector + new Vector2(Random.Range(-25f, 25f), Random.Range(-25f, 25f));

                float duration = Random.Range(outroDurationRange.x, outroDurationRange.y);
                float delay = i * perDelay;

                Tween tw = rt.DOAnchorPos(end, duration)
                    .SetDelay(delay)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => finished++);

                rt.DORotate(new Vector3(0, 0, rt.localEulerAngles.z + Random.Range(120f, 240f)), duration, RotateMode.FastBeyond360)
                    .SetDelay(delay)
                    .SetEase(Ease.InQuad);

                starTweens.Add(tw);
            }

            while (finished < count)
                yield return null;
        }

        private void ClearStars()
        {
            foreach (var t in starTweens)
                if (t != null && t.IsActive()) t.Kill(false);

            foreach (var s in stars)
                if (s != null) Destroy(s.gameObject);

            stars.Clear();
            fillOrderStars.Clear();
        }
    }
}
