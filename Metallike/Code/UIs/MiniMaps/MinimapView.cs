using Assets.Work.CDH.Code.Maps;
using DG.Tweening;
using GondrLib.Dependencies;
using GondrLib.ObjectPool.RunTime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public class MinimapView : MonoBehaviour, IMinimapView
    {
        [SerializeField] private Transform playerTrm;
        [SerializeField] private Transform rootTrm;
        [SerializeField] private GameObject minimapPlayerIcon;
        [SerializeField] private GameObject playerViewObj;
        [SerializeField] private float addAngle;
        [Header("UI")]
        [SerializeField] private RectTransform imagesRoot = null!;
        [SerializeField] private PoolItemSO entityIconItem;
        [SerializeField] private Color playerColor;
        [SerializeField] private Color enemyColor;
        [SerializeField] private Color notFoundRoomColor;
        [SerializeField] private MinimapBackground[] roomIcons;
        [SerializeField] private float radius;
        [SerializeField] private Image battleBackground;
        [SerializeField] private float duration;

        [Header("Battle Zoom (Icon Only)")]
        [SerializeField] private float zoomDuration = 0.25f;
        [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Inject] private PoolManagerMono poolManager;

        private readonly Dictionary<Vector2Int, MinimapBackground> icons = new();
        private readonly Dictionary<Vector2Int, RectTransform> iconRects = new();

        private bool isBattleMode;

        private MinimapEntityIcon playerIcon;
        private List<Entity> enemyList;
        private List<MinimapEntityIcon> enemyIcons;
        private IRoomDef curRoom;

        private void Awake()
        {
            enemyList = new();
            enemyIcons = new();
        }

        private void Update()
        {
            UpdateEntityPositions();
            UpdatePlayerView();
        }

        public void Initializer()
        {
            // [중요] 재시작 시 기존 아이콘들이 남아있지 않도록 정리
            foreach (var rect in iconRects.Values)
            {
                if (rect != null) Destroy(rect.gameObject);
            }
            icons.Clear();
            iconRects.Clear();
            isBattleMode = false;

            foreach (var icon in roomIcons)
            {
                if (icon.transform.localPosition == Vector3.zero)
                {
                    icons[Vector2Int.zero] = icon;
                    iconRects[Vector2Int.zero] = icon.Rect;
                }
                if (icon.transform.localPosition.x < 0 && icon.transform.localPosition.y < 0)
                {
                    icons[Vector2Int.up] = icon;
                    iconRects[Vector2Int.up] = icon.Rect;
                }
                if (icon.transform.localPosition.x > 0 && icon.transform.localPosition.y < 0)
                {
                    icons[Vector2Int.left] = icon;
                    iconRects[Vector2Int.left] = icon.Rect;
                }
                if (icon.transform.localPosition.x < 0 && icon.transform.localPosition.y > 0)
                {
                    icons[Vector2Int.right] = icon;
                    iconRects[Vector2Int.right] = icon.Rect;
                }
                if (icon.transform.localPosition.x > 0 && icon.transform.localPosition.y > 0)
                {
                    icons[Vector2Int.down] = icon;
                    iconRects[Vector2Int.down] = icon.Rect;
                }
            }

            imagesRoot.gameObject.SetActive(true);


            minimapPlayerIcon.gameObject.SetActive(true);
            minimapPlayerIcon.transform.SetAsLastSibling();
        }

        public void Show()
        {
            imagesRoot.gameObject.SetActive(true);
        }

        Vector2Int[] dirs = new Vector2Int[]
        {
           new Vector2Int(1, 0),
           new Vector2Int(-1, 0),
           new Vector2Int(0, 1),
           new Vector2Int(0, -1)
        };

        public void SetMinimap(List<IRoomUIData> datas, Vector2Int centerPos)
        {
            if (datas == null || datas.Count == 0) return;

            int minX = datas.Min(d => d.Pos.x);
            int minY = datas.Min(d => d.Pos.y);
            int maxX = datas.Max(d => d.Pos.x);
            int maxY = datas.Max(d => d.Pos.y);

            Vector2Int minPos = new Vector2Int(minX, minY);
            Vector2Int maxPos = new Vector2Int(maxX, maxY);

            foreach (Vector2Int dir in dirs)
            {
                var targetPos = centerPos - dir;
                var iconKey = dir;

                bool found = false;
                IRoomUIData foundRoom = default;

                if (targetPos.x >= minPos.x && targetPos.x <= maxPos.x && targetPos.y >= minPos.y && targetPos.y <= maxPos.y)
                {
                    foreach (IRoomUIData data in datas)
                    {
                        if (data.Pos == targetPos)
                        {
                            found = true;
                            foundRoom = data;

                            // 발견 여부에 따른 색상 변경
                            if (icons.TryGetValue(iconKey, out var image))
                            {
                                image.mainImage.color = foundRoom.IsDiscovered ? Color.white : notFoundRoomColor;
                            }
                            break;
                        }
                    }
                }

                if (!icons.ContainsKey(iconKey)) continue;

                // 배틀 모드일 때는 아이콘의 활성/비활성 상태를 변경하지 않고 이미지만 업데이트
                if (isBattleMode)
                {
                    // if (iconKey == Vector2Int.zero && found)
                    //     icons[iconKey].mainImage.sprite = foundRoom.Sprite;
                    continue;
                }

                // 일반 모드: 방이 있으면 켜고, 없으면 끈다
                GameObject iconObject = icons[iconKey].gameObject;

                if (found)
                {
                    if (!iconObject.activeSelf) iconObject.SetActive(true);
                    icons[iconKey].SettingSprite(foundRoom.SubSprite);
                }
                else
                {
                    if (iconObject.activeSelf) iconObject.SetActive(false);
                }
            }
        }

        public void SetEnemies(List<Entity> enemies)
        {
            // [수정 1] 플레이어 아이콘은 '없을 때만' 생성해야 합니다.
            // 기존 코드는 이 함수가 불릴 때마다(적이 죽을 때마다) 플레이어 아이콘을 계속 새로 만들어서
            // 화면에 플레이어 색깔 점이 계속 쌓이는 버그가 있었습니다.
            if (playerIcon == null)
            {
                playerIcon = poolManager.Pop<MinimapEntityIcon>(entityIconItem);
                playerIcon.gameObject.SetActive(true);
                playerIcon.transform.SetParent(imagesRoot);
                playerIcon.transform.localScale = Vector3.one; // 스케일 초기화 권장
                playerIcon.SetColor(playerColor);
                playerIcon.SetAsLastSibling();
            }

            // [수정 2] 죽은 적 제거 로직 (역방향 for문 사용)
            // 기존 코드: foreach 도중 리스트를 수정하면 에러가 발생하며, 무조건 0번 아이콘을 지우는 문제가 있었습니다.
            // 리스트의 뒤에서부터 돌며 검사해야 안전하게 삭제됩니다.
            for (int i = enemyList.Count - 1; i >= 0; i--)
            {
                var existingEnemy = enemyList[i];

                // 새 리스트(enemies)에 기존 적(existingEnemy)이 포함되어 있지 않다면 -> 죽은 적
                if (!enemies.Contains(existingEnemy))
                {
                    // 1. 아이콘 풀 반환 (인덱스를 맞춰서 해당 아이콘 제거)
                    if (i < enemyIcons.Count)
                    {
                        enemyIcons[i].PushItem();
                        enemyIcons.RemoveAt(i);
                    }
                    // 2. 데이터 리스트에서 제거
                    enemyList.RemoveAt(i);
                }
            }

            // [수정 3] 새로운 적 추가 로직 (처음 시작하거나, 도중에 적이 추가될 경우 대응)
            // 기존 코드는 enemyList.Count < 1 일 때만 추가해서, 이미 전투 중일 때 추가된 적을 처리 못할 수 있음
            foreach (var newEnemy in enemies)
            {
                if (!enemyList.Contains(newEnemy))
                {
                    enemyList.Add(newEnemy);

                    var enemyIcon = poolManager.Pop<MinimapEntityIcon>(entityIconItem);
                    enemyIcon.transform.SetParent(imagesRoot);
                    enemyIcon.transform.localScale = Vector3.one;
                    enemyIcon.SetColor(enemyColor);
                    enemyIcon.SetAsLastSibling();
                    enemyIcons.Add(enemyIcon);
                }
            }
            playerIcon.SetAsLastSibling();
        }

        private void UpdateEntityPositions()
        {
            // 방 정보가 없거나, 미니맵 센터 UI가 없으면 계산 불가
            if (curRoom == null) return;
            if (!iconRects.TryGetValue(Vector2Int.zero, out var centerRect)) return;

            // 방의 월드 중심 좌표 및 사이즈
            Vector3 roomPos = curRoom.WorldPos;
            Vector2 roomSize = curRoom.RoomSize;

            // UI 기준 데이터 (수정하신 부분 적용)
            Vector2 currentUiSize = new Vector2(radius, radius);
            Vector2 centerUiPos = Vector2.zero; // Vector3.zero 대신 RectTransform용 Vector2 권장

            // 원형 미니맵의 반지름 계산
            float uiRadius = Mathf.Min(currentUiSize.x, currentUiSize.y) * 0.5f;

            // 방의 절반 크기 (거리 정규화 계산용)
            float halfRoomX = roomSize.x * 0.5f;
            float halfRoomY = roomSize.y * 0.5f;

            // -----------------------------------------------------
            // Player Logic Update
            // -----------------------------------------------------
            if (playerIcon != null && playerIcon.gameObject.activeSelf && playerTrm != null)
            {
                // 1. 방 중심(roomPos)을 기준으로 -1 ~ 1 사이의 정규화된 좌표 구하기 (Y 대신 Z)
                float u = (playerTrm.position.x - roomPos.x) / halfRoomX;
                float v = (playerTrm.position.z - roomPos.z) / halfRoomY;

                // 방 밖으로 나갔을 때를 대비해 -1 ~ 1 사이로 클램프
                u = Mathf.Clamp(u, -1f, 1f);
                v = Mathf.Clamp(v, -1f, 1f);

                // 2. 사각형 -> 원형 왜곡 매핑 (Elliptical Grid Mapping)
                float mappedX = u * Mathf.Sqrt(1f - (v * v) * 0.5f);
                float mappedY = v * Mathf.Sqrt(1f - (u * u) * 0.5f);

                // 3. 왜곡된 좌표에 UI 반지름(uiRadius)을 곱해 최종 위치 결정
                if (playerIcon.transform is RectTransform playerRect)
                {
                    playerRect.anchoredPosition = centerUiPos + new Vector2(mappedX * uiRadius, mappedY * uiRadius);
                }
            }

            // -----------------------------------------------------
            // Enemy Logic Update
            // -----------------------------------------------------
            int count = Mathf.Min(enemyList.Count, enemyIcons.Count);
            for (int i = 0; i < count; i++)
            {
                var enemy = enemyList[i];
                var icon = enemyIcons[i];

                if (enemy == null || icon == null || !icon.gameObject.activeSelf) continue;

                // 1. 적 위치를 -1 ~ 1 로 정규화
                float u = (enemy.transform.position.x - roomPos.x) / halfRoomX;
                float v = (enemy.transform.position.z - roomPos.z) / halfRoomY;

                u = Mathf.Clamp(u, -1f, 1f);
                v = Mathf.Clamp(v, -1f, 1f);

                // 2. 사각형 -> 원형 왜곡 매핑
                float mappedX = u * Mathf.Sqrt(1f - (v * v) * 0.5f);
                float mappedY = v * Mathf.Sqrt(1f - (u * u) * 0.5f);

                // 3. UI 적용
                if (icon.transform is RectTransform iconRect)
                {
                    iconRect.anchoredPosition = centerUiPos + new Vector2(mappedX * uiRadius, mappedY * uiRadius);
                }
            }
        }

        private void UpdatePlayerView()
        {
            if (isBattleMode)
            {
                if (playerIcon == null && playerViewObj.activeSelf)
                {
                    playerViewObj.SetActive(false);
                    return;
                }
                else if (playerIcon != null && !playerViewObj.activeSelf)
                {
                    playerViewObj.SetActive(true);
                }

                if (playerIcon != null)
                    playerViewObj.transform.localPosition = playerIcon.transform.localPosition;
            }
            else
            {
                if (playerViewObj.transform.localPosition != Vector3.zero)
                    playerViewObj.transform.localPosition = Vector3.zero;
            }

            Vector3 euler = playerViewObj.transform.eulerAngles;
            float playerY = playerTrm.eulerAngles.y;

            if (!isBattleMode)
                playerY += addAngle;

            euler.z = -playerY;

            playerViewObj.transform.rotation = Quaternion.Euler(euler);
        }

        public void RollbackToDefaultSetting()
        {
            if (!isBattleMode) return;
            isBattleMode = false;

            // 기존 플레이어 화살표 아이콘 복구
            minimapPlayerIcon.gameObject.SetActive(true);
            playerIcon.gameObject.SetActive(false);

            // 배틀용 플레이어 점 아이콘 반환
            if (playerIcon != null)
            {
                poolManager.Push(playerIcon);
                playerIcon = null; // 참조 끊기
            }

            // 에너미 정리
            // 에너미 아이콘들도 모두 Push 해야 함
            foreach (var enemyIcon in enemyIcons)
            {
                poolManager.Push(enemyIcon);
            }

            enemyList.Clear();
            enemyIcons.Clear();

            // 아이콘 활성 상태 원복
            foreach (var kv in icons)
            {
                kv.Value.gameObject.SetActive(kv.Value);
            }

            battleBackground.DOKill();
            Color endColor = battleBackground.color;
            endColor.a = 0f;
            battleBackground.DOColor(endColor, duration);
        }

        public void SetBattleSetting()
        {
            if (isBattleMode) return;

            // [방어 코드 1] minimapPlayerIcon이 연결 안 되어 있으면 중단
            if (minimapPlayerIcon == null)
            {
                Debug.LogWarning("[MinimapView] Player Icon is missing!");
                return;
            }

            isBattleMode = true;
            minimapPlayerIcon.gameObject.SetActive(false);

            // 현재 활성화 상태 캐싱 및 주변 아이콘 숨기기
            foreach (var kv in icons)
            {
                // [방어 코드 2] 딕셔너리 내부의 오브젝트가 파괴되었는지 확인
                if (kv.Value == null || kv.Value.gameObject == null)
                    continue;

                kv.Value.gameObject.SetActive(false);
            }

            battleBackground.DOKill();
            Color endColor = battleBackground.color;
            endColor.a = 255f;
            battleBackground.DOColor(endColor, duration);
        }

        public void SetCurRoom(IRoomDef roomDef)
        {
            curRoom = roomDef;
        }

        public void SetActive(bool active)
        {
            rootTrm.gameObject.SetActive(active);
        }
    }
}