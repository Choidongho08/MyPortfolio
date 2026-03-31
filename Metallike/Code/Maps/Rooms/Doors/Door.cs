using DG.Tweening;
using EPOOutline;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Work.CDH.Code.Maps.Rooms.Doors
{
    public class Door : MonoBehaviour
    {
        public Action<EnterDirection> OnEnter;
        public Action<EnterDirection> OnUnlock;
        public Action OnDoorOpen;

        [field: SerializeField] public EnterDirection Dir { get; private set; }

        public bool IsEnterable { get; set; } = false;
        public bool IsBattle { get; set; }

        [Header("Settings")]
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Transform startTrm;
        [SerializeField] private Transform endTrm;
        [SerializeField] private Outlinable outlinable;
        [SerializeField] private Color outlineColor;
        [SerializeField] private float animDuration = 1f;
        [SerializeField] private Vector3 doorOpenCheckSize;
        [SerializeField] private LayerMask playerLayer;

        [Header("Interaction Settings")]
        [SerializeField] private InteractionImageText interactionImageText;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Vector2 spriteSize;
        [SerializeField] private float fontSize;

        private Collider[] colliders;
        private bool isOpenDoor; // 문 오브젝트(패널)이 열리고 닫힐 때 사용
        private bool canUnlock;

        // [추가] 중복 입장 방지용 플래그
        private bool isEntering;

        private Vector3 startPos;
        private Vector3 endPos;

        private void Awake()
        {
            colliders = new Collider[1];
            Initialize();

            // [추가] 시작 시 위치 데이터 저장
            if (doorPanel != null)
            {
                startPos = startTrm.localPosition;
                // print(startPos);
            }

            if (endTrm != null)
            {
                endPos = endTrm.localPosition;
                // print(endPos);
            }
        }

        public void Initialize()
        {
            // [수정 1] 실행 중인 모든 트윈을 즉시 중단해야 위치 초기화가 먹힙니다.
            if (doorPanel != null)
            {
                doorPanel.DOKill();
                doorPanel.localPosition = startPos;
            }

            // [수정 2] 상태값들을 명확하게 초기화
            isOpenDoor = false;
            IsEnterable = false;
            canUnlock = false; // canUnlock도 초기화해주는 것이 안전합니다.
            isEntering = false; // [추가] 입장 플래그 초기화

            interactionImageText.Init();
            IconSetting(InteractionIconEnum.LockIcon, InteractionIconEnum.LockIcon);

            Color c = outlineColor;
            c.a = 0f;
            outlinable.BackParameters.Color = c;
            outlinable.FrontParameters.Color = c;
        }

        private void Update()
        {
            // 1. 전투 중이면 아무것도 하지 않음
            if (IsBattle)
                return;

            // 2. 플레이어 감지 (Physics.OverlapBoxNonAlloc은 GC 할당이 없어 Update에 적합)
            int hitCount = Physics.OverlapBoxNonAlloc(transform.position, doorOpenCheckSize, colliders, transform.rotation, playerLayer);
            bool isPlayerDetected = hitCount > 0;

            // 3. 상황별 처리 분기
            if (isPlayerDetected)
            {
                HandlePlayerDetected();
            }
            else
            {
                HandlePlayerExit();
            }
        }

        // 플레이어가 범위 안에 있을 때의 로직
        private void HandlePlayerDetected()
        {
            interactionImageText.Show();

            // Case A: 진입 불가능한 상태 (잠김 등)
            if (!IsEnterable)
            {
                // Input: 잠금 해제 시도
                if (Keyboard.current.eKey.wasPressedThisFrame && canUnlock)
                {
                    canUnlock = false;
                    OnUnlock?.Invoke(Dir);
                    IsEnterable = true;
                    IconSetting(InteractionIconEnum.EIcon, InteractionIconEnum.EIcon);
                }
                return; // 진입 불가능하므로 아래 로직(문 열기/입장)은 실행하지 않음
            }

            // Case B: 진입 가능한 상태

            // Logic: 닫혀 있다면 자동으로 염
            if (!isOpenDoor)
            {
                Open();
                OnDoorOpen?.Invoke();
                isOpenDoor = true;
            }

            // Input: 입장 시도
            // [수정] 이미 입장 중(isEntering)이면 입력을 무시
            if (Keyboard.current.eKey.wasPressedThisFrame && !isEntering)
            {
                isEntering = true; // [추가] 입장 시작 플래그 설정
                OnEnter?.Invoke(Dir);
            }
        }

        // 플레이어가 범위 밖으로 나갔을 때의 로직
        private void HandlePlayerExit()
        {
            interactionImageText.Hide();

            // 열려 있다면 자동으로 닫음
            if (isOpenDoor)
            {
                Close();
                isOpenDoor = false;
            }
        }

        public void Open()
        {
            // print("Open");
            // [추가] DOTween 이동 로직
            if (doorPanel != null)
            {
                // 기존 애니메이션 제거 (빠르게 왔다 갔다 할 때 꼬임 방지)
                doorPanel.DOKill();
                // endTrm 위치(endPos)로 이동
                doorPanel.DOLocalMoveY(endPos.y, animDuration).SetEase(Ease.OutQuad);
            }
        }

        public void Close()
        {
            // print("close");
            // [추가] DOTween 이동 로직
            if (doorPanel != null)
            {
                doorPanel.DOKill();
                // 원래 위치(startPos)로 이동
                doorPanel.DOLocalMoveY(startPos.y, animDuration).SetEase(Ease.OutQuad);
            }
        }

        public void Battle()
        {
            IsBattle = true;
            if (isOpenDoor)
            {
                Close();
                isOpenDoor = false;
            }

            interactionImageText.IconSetting(InteractionIconEnum.BattleMode, InteractionIconEnum.BattleMode, spriteSize, fontSize, true);
        }

        public void BattleEnd()
        {
            IsBattle = false;
            interactionImageText.IconSetting(InteractionIconEnum.BattleMode, InteractionIconEnum.BattleMode, spriteSize, fontSize, false);
        }

        public void CanUnlockState()
        {
            if (!IsEnterable)
            {
                Color c = outlineColor;
                c.a = 255f;
                outlinable.BackParameters.Color = c;
                outlinable.FrontParameters.Color = c;
            }
            canUnlock = true;
        }

        public void CantUnlockState()
        {
            Color c = outlineColor;
            c.a = 0f;
            outlinable.BackParameters.Color = c;
            outlinable.FrontParameters.Color = c;
            canUnlock = false;
        }

        public void IconSetting(InteractionIconEnum iconType, InteractionIconEnum colorType)
        {
            interactionImageText.IconSetting(iconType, colorType, spriteSize, fontSize);
        }

        public void SetEnterable()
        {
            IsEnterable = true;
            IconSetting(InteractionIconEnum.EIcon, InteractionIconEnum.EIcon);
        }
    }
}