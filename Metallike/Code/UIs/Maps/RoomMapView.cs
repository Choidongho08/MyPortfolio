using Assets.Work.CDH.Code.Maps;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public interface IRoomMapView : IUpdatable<RoomMapState>, IFadeInOutable, IHaveTargetMapView
    {
        event Action OnChangeView;
        event Action OnNextButtonClicked;
        event Action<Vector2Int> OnSelectRoom;
        event Action<float, Vector2> OnRegionZoomRequested;

        bool IsShow { get; }

        // 초기화 및 상태
        void Initialize(List<RoomIconInitData> roomPositions);

        void EnableView();
        void DisableView();
        // 방 상태 업데이트용 함수들
        void UpdateAllRooms(IReadOnlyDictionary<Vector2Int, RoomIconState> roomStates);
        void UpdateSingleRoom(Vector2Int pos, RoomIconState state);
        Vector2 GetRoomUIPosition(Vector2Int gridPos);
        void SetMappingTextVisible(bool showMapping);
    }

    public class RoomMapView : StatefulView<RoomMapState>, IRoomMapView, IFadeInOutable
    {
        public event Action<Vector2Int> OnSelectRoom;
        public event Action OnNextButtonClicked;
        public event Action OnChangeView;

        public event Action<float, Vector2> OnRegionZoomRequested;

        [Header("UI References")]
        [SerializeField, Tooltip("내가 보여줄 내용을 관리하는 캔버스 그룹")] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration;
        CanvasGroup IFadeInOutable.FadeGroup => canvasGroup;

        public bool IsShow { get; private set; } = false;

        [SerializeField] private RectTransform mapView;
        [SerializeField] private RectTransform roomIconsBackground;
        [SerializeField] private RectTransform playerIconTrm;
        [SerializeField] private GameObject roomUIPrefab;

        [Header("Data & Texts")]
        [SerializeField] private GameObject dataAndTextsRoot;
        #region LeftUI
        [SerializeField] private RoomInfoSO infoSO;
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private TextMeshProUGUI roomDescText;
        #endregion
        #region Other
        [SerializeField] private TextMeshProUGUI canMappingText;
        #endregion
        [SerializeField] private TextMeshProUGUI securityLevelText;
        #region Test
        [SerializeField] private Button backToRegionUIBtn;
        #endregion

        [Header("Map Settings")]
        [Tooltip("방 크기 대비 간격의 비율 (예: 0.15 = 방 크기의 15%)")]
        [SerializeField] private float spaceRatio = 0.15f;
        [SerializeField] private float mapPaddingBound;

        private const float TextFadeDuration = 0.5f;

        // --- 내부 상태 변수들 ---
        private float roomUISize;
        private float actualSpace;
        private Vector2 mapViewSize;
        private Vector2 gridSize;
        private Vector2 mapContentMin;
        private Vector2 mapContentMax;
        private IRoomIcon prevRoomIcon;
        private Dictionary<Vector2Int, IRoomIcon> roomIconDict = new();
        private List<IRoomIcon> interactions = new();

        private void MappingTextViewlessImmediatly()
        {
            canMappingText.alpha = 0;
        }

        public void Initialize(List<RoomIconInitData> roomInitDataList)
        {
            this.FadeOut(0);

            ClearAll();
            mapViewSize = mapView.rect.size;
            MappingTextViewlessImmediatly();
            CreateRoomIcons(roomInitDataList);
            backToRegionUIBtn.onClick.AddListener(HandleBackToRegionUIBtnClick);
        }

        private void OnDestroy()
        {
            backToRegionUIBtn.onClick.RemoveListener(HandleBackToRegionUIBtnClick);
        }

        private void HandleBackToRegionUIBtnClick()
        {
            OnChangeView?.Invoke();
        }

        private void ClearAll()
        {
            foreach (var interaction in interactions)
            {
                interaction.OnRoomIconClick -= HandleRoomClick;
            }
            interactions.Clear();

            foreach (var icon in roomIconDict.Values)
            {
                if (icon is Component comp)
                {
                    Destroy(comp.gameObject);
                }
            }
            roomIconDict.Clear();

            if (playerIconTrm != null)
                playerIconTrm.gameObject.SetActive(false);
        }

        private void GetRoomUISize(List<RoomIconInitData> roomDefs)
        {
            if (roomDefs == null || !roomDefs.Any())
            {
                roomUISize = 0f;
                gridSize = Vector2.zero;
                return;
            }

            int minX = roomDefs.Min(r => r.GridPos.x);
            int maxX = roomDefs.Max(r => r.GridPos.x);
            int minY = roomDefs.Min(r => r.GridPos.y);
            int maxY = roomDefs.Max(r => r.GridPos.y);

            int gridColumns = maxX - minX + 1;
            int gridRows = maxY - minY + 1;

            float innerW = mapViewSize.x - (mapPaddingBound * 2f);
            float innerH = mapViewSize.y - (mapPaddingBound * 2f);

            float totalWidthRatio = gridColumns + ((gridColumns - 1) * spaceRatio);
            float totalHeightRatio = gridRows + ((gridRows - 1) * spaceRatio);

            float maxRoomWidth = innerW / totalWidthRatio;
            float maxRoomHeight = innerH / totalHeightRatio;

            roomUISize = Mathf.Min(maxRoomWidth, maxRoomHeight);
            actualSpace = roomUISize * spaceRatio;

            gridSize = new Vector2(
                (roomUISize * gridColumns) + (actualSpace * (gridColumns - 1)),
                (roomUISize * gridRows) + (actualSpace * (gridRows - 1))
            );
        }

        private void CreateRoomIcons(List<RoomIconInitData> roomInitDataList)
        {
            if (roomInitDataList == null || roomInitDataList.Count == 0) return;

            int minX = roomInitDataList.Min(r => r.GridPos.x);
            int minY = roomInitDataList.Min(r => r.GridPos.y);
            int maxX = roomInitDataList.Max(r => r.GridPos.x);
            int maxY = roomInitDataList.Max(r => r.GridPos.y);

            GetRoomUISize(roomInitDataList);

            float step = roomUISize + actualSpace;
            Vector2 startPos = -gridSize * 0.5f + Vector2.one * (roomUISize * 0.5f);

            var bossRooms = roomInitDataList.Where(r => r.RoomType == RoomType.BossRoom).ToList();
            var normalRooms = roomInitDataList.Where(r => r.RoomType != RoomType.BossRoom).ToList();

            roomIconDict.Clear();

            foreach (var room in normalRooms)
            {
                var obj = Instantiate(roomUIPrefab, mapView);
                var roomIcon = obj.GetComponent<IRoomIcon>();

                roomIcon.SetGridPos(room.GridPos);
                RectTransform rect = roomIcon.Rect;

                Vector2 normalizedGridPos = new Vector2(room.GridPos.x - minX, room.GridPos.y - minY);
                Vector2 pos = startPos + normalizedGridPos * step;

                rect.anchoredPosition = pos;

                float baseWidth = rect.sizeDelta.x != 0 ? rect.sizeDelta.x : 100f;
                float scaleFactor = roomUISize / baseWidth;
                rect.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

                roomIconDict[room.GridPos] = roomIcon;
            }

            if (bossRooms.Any())
            {
                int bossMinX = bossRooms.Min(r => r.GridPos.x);
                int bossMinY = bossRooms.Min(r => r.GridPos.y);
                int bossMaxX = bossRooms.Max(r => r.GridPos.x);
                int bossMaxY = bossRooms.Max(r => r.GridPos.y);

                int colCount = bossMaxX - bossMinX + 1;
                int rowCount = bossMaxY - bossMinY + 1;

                float bossUIWidth = (colCount * roomUISize) + ((colCount - 1) * actualSpace);
                float bossUIHeight = (rowCount * roomUISize) + ((rowCount - 1) * actualSpace);

                float centerGridX = (bossMinX + bossMaxX) / 2f;
                float centerGridY = (bossMinY + bossMaxY) / 2f;

                Vector2 normalizedCenterPos = new Vector2(centerGridX - minX, centerGridY - minY);
                Vector2 bossUIPos = startPos + normalizedCenterPos * step;

                var obj = Instantiate(roomUIPrefab, mapView);
                var roomIcon = obj.GetComponent<IRoomIcon>();

                roomIcon.SetGridPos(new Vector2Int(bossMinX, bossMinY));
                RectTransform rect = roomIcon.Rect;
                rect.anchoredPosition = bossUIPos;
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(bossUIWidth, bossUIHeight);

                foreach (var bRoom in bossRooms)
                {
                    roomIconDict[bRoom.GridPos] = roomIcon;
                }
            }

            playerIconTrm.SetAsLastSibling();
            playerIconTrm.sizeDelta = new Vector2(roomUISize / 2f, roomUISize / 2f);
            playerIconTrm.gameObject.SetActive(false);

            CalculateMapBounds();
        }

        private void CalculateMapBounds()
        {
            if (roomIconDict.Count == 0) return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var icon in roomIconDict.Values)
            {
                Vector2 pos = icon.Rect.anchoredPosition;
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }

            float halfSize = roomUISize * 0.5f;
            mapContentMin = new Vector2(minX - halfSize, minY - halfSize);
            mapContentMax = new Vector2(maxX + halfSize, maxY + halfSize);
        }

        private void HandleRoomClick(Vector2Int roomPos)
        {
            foreach (var interaction in interactions)
            {
                interaction.OnRoomIconClick -= HandleRoomClick;
            }

            interactions.Clear();
            OnSelectRoom?.Invoke(roomPos);
        }

        #region Update UI Logic

        public void UpdateAllRooms(IReadOnlyDictionary<Vector2Int, RoomIconState> roomStates)
        {
            foreach (var kvp in roomStates)
            {
                UpdateSingleRoom(kvp.Key, kvp.Value);
            }
        }

        public void UpdateSingleRoom(Vector2Int pos, RoomIconState state)
        {
            if (roomIconDict.TryGetValue(pos, out IRoomIcon icon))
            {
                icon.UpdateState(state);
                RoomIconEventSubscribe(state, icon);
            }
        }

        protected override void OnUpdateState(RoomMapState state)
        {
            if (prevState != null && prevState.Value.Equals(state))
                return;

            var prev = prevState.GetValueOrDefault();

            Bind(prev.SecurityLevel, state.SecurityLevel, UpdateSecurityLevel);
            Bind(prev.IsPlayerVisible, state.IsPlayerVisible, UpdatePlayerVisible);
            Bind(prev.PlayerPos, state.PlayerPos, UpdatePlayerPos);
            Bind(prev.RoomInfo, state.RoomInfo, UpdateTextHandle);
            // Bind(prev.IsShow, state.IsShow, UpdateViewShow);

            // 🎯 방 구역을 포커싱 해야 할 때 데이터 동기화
            Bind(prev.RoomMapRegionData, state.RoomMapRegionData, UpdateZoomView);

            prevState = state;
        }

        private void UpdateZoomView(RoomMapRegionData data)
        {
            if (data.MinRoomPos == Vector2Int.zero && data.MaxRoomPos == Vector2Int.zero) return;

            if (!roomIconDict.TryGetValue(data.MinRoomPos, out IRoomIcon minRoom) ||
                !roomIconDict.TryGetValue(data.MaxRoomPos, out IRoomIcon maxRoom))
            {
                return;
            }

            Vector2 minAnchor = minRoom.Rect.anchoredPosition;
            Vector2 maxAnchor = maxRoom.Rect.anchoredPosition;

            float halfRoomSize = roomUISize * 0.5f;
            Vector2 regionMin = new Vector2(
                Mathf.Min(minAnchor.x, maxAnchor.x) - halfRoomSize,
                Mathf.Min(minAnchor.y, maxAnchor.y) - halfRoomSize);

            Vector2 regionMax = new Vector2(
                Mathf.Max(minAnchor.x, maxAnchor.x) + halfRoomSize,
                Mathf.Max(minAnchor.y, maxAnchor.y) + halfRoomSize);

            float regionWidth = regionMax.x - regionMin.x;
            float regionHeight = regionMax.y - regionMin.y;
            Vector2 regionCenter = (regionMin + regionMax) * 0.5f;

            float viewWidth = roomIconsBackground.rect.width - (mapPaddingBound * 2f);
            float viewHeight = roomIconsBackground.rect.height - (mapPaddingBound * 2f);

            float scaleX = viewWidth / Mathf.Max(regionWidth, 0.1f);
            float scaleY = viewHeight / Mathf.Max(regionHeight, 0.1f);
            float targetScale = Mathf.Min(scaleX, scaleY);

            Vector2 targetPos = regionCenter;

            OnRegionZoomRequested?.Invoke(targetScale, targetPos);
        }

        public void UpdateTextHandle(RoomType roomInfo)
        {
            roomDescText.text = infoSO.GetDescInfo(roomInfo);
            roomNameText.text = infoSO.GetNameInfo(roomInfo);
        }

        public void SetMappingTextVisible(bool showMapping)
        {
            float endValue = showMapping ? 1f : 0f;
            canMappingText.DOKill();
            canMappingText.DOFade(endValue, TextFadeDuration);
        }

        private void UpdatePlayerPos(Vector2Int playerPos)
        {
            if (roomIconDict.TryGetValue(playerPos, out IRoomIcon room))
            {
                if (prevRoomIcon != null)
                {
                    playerIconTrm.SetParent(mapView);
                }

                playerIconTrm.SetParent(room.Rect);
                playerIconTrm.anchoredPosition = Vector2.zero;
                prevRoomIcon = room;
            }
        }

        private void UpdatePlayerVisible(bool isPlayerVisible)
        {
            playerIconTrm.gameObject.SetActive(isPlayerVisible);
        }

        private void UpdateSecurityLevel(int securityLevel)
        {
            string text = $"보안등급 : {securityLevel}";
            securityLevelText.SetText(text);
        }

        private void RoomIconEventSubscribe(RoomIconState state, IRoomIcon icon)
        {
            if (state.CanInteractable)
            {
                if (!interactions.Contains(icon))
                {
                    interactions.Add(icon);
                    icon.OnRoomIconClick += HandleRoomClick;
                }
            }
            else
            {
                if (interactions.Contains(icon))
                {
                    interactions.Remove(icon);
                    icon.OnRoomIconClick -= HandleRoomClick;
                }
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (mapView == null || roomIconsBackground == null) return;

            Gizmos.matrix = roomIconsBackground.localToWorldMatrix;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(roomIconsBackground.rect.center, roomIconsBackground.rect.size);

            if (mapContentMin != Vector2.zero || mapContentMax != Vector2.zero)
            {
                Gizmos.matrix = mapView.localToWorldMatrix;
                Gizmos.color = Color.green;

                Vector2 size = mapContentMax - mapContentMin;
                Vector2 center = (mapContentMin + mapContentMax) * 0.5f;

                Gizmos.DrawWireCube(center, size);
            }
        }

#endif
        public Vector2 GetRoomUIPosition(Vector2Int gridPos)
        {
            // 딕셔너리에서 해당 그리드의 방 UI를 찾아서 위치를 반환
            if (roomIconDict.TryGetValue(gridPos, out IRoomIcon icon))
            {
                return icon.Rect.anchoredPosition;
            }

            // 혹시라도 못 찾으면 그냥 정중앙(0,0) 반환
            return Vector2.zero;
        }

        public void EnableView()
        {
            if (IsShow)
                return;

            IsShow = true;
            this.FadeIn(fadeDuration);
            dataAndTextsRoot.SetActive(true);
        }

        public void DisableView()
        {
            if (!IsShow)
                return;

            IsShow = false;
            this.FadeOut(fadeDuration);
            dataAndTextsRoot.SetActive(false);
        }
    }
}