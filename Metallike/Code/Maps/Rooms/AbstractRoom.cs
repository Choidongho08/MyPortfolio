using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps.Rooms;
using Core.EventBus;
using GondrLib.ObjectPool.RunTime;
using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    [Serializable]
    public struct EnterDirectionPos
    {
        public EnterDirection Dir;
        public Transform Trm;

        public EnterDirectionPos(EnterDirection dir, Transform trm)
        {
            Dir = dir;
            Trm = trm;
        }
    }

    public abstract class AbstractRoom : MonoBehaviour, IRoom
    {
        public Action OnClear { get; set; }
        public Vector2Int GridPos { get; set; }
        [field: SerializeField] public PoolItemSO PoolItem { get; private set; }
        [field: SerializeField] public Button Button { get; private set; }
        [field: SerializeField] public RoomType RoomType { get; private set; }
        [field: SerializeField] public Transform Center { get; private set; }

        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public Dictionary<EnterDirection, Vector3> EnterPosDict { get; private set; } = new();
        [SerializeField] private List<EnterDirectionPos> enterPosList;

        public Dictionary<EnterDirection, Vector3> DoorPosDict { get; private set; } = new();

        public Vector2 Size { get; private set; }

        public Dictionary<int, GameObject> BreakingObstacles { get; private set; } = new();

        [field: SerializeField] public List<Transform> Vertices { get; private set; }

        [SerializeField] protected List<EnterDirectionPos> doorPosList;

        private Pool myPool;

        private void Awake()
        {
            Bus<RoomClearEvent>.OnEvent += HandleRoomClearEvent;

            foreach (var obs in GetComponentsInChildren<BreakingObstacle>())
            {
                BreakingObstacles.TryAdd(obs.Id, obs.gameObject);
            }

            CalculateRoomSize();
        }

        // 방 크기 계산 로직 분리
        private void CalculateRoomSize()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
            {
                // 1. 첫 번째 렌더러를 기준으로 영역을 잡습니다.
                Bounds combinedBounds = renderers[0].bounds;

                // 2. 나머지 모든 렌더러를 포함하도록 영역을 확장(Encapsulate)합니다.
                foreach (Renderer render in renderers)
                {
                    // (옵션) 파티클이나 트레일은 방 크기 계산에서 제외하고 싶으면 주석 해제
                    // if (render is ParticleSystemRenderer || render is TrailRenderer) continue;

                    combinedBounds.Encapsulate(render.bounds);
                }

                // 3. 최종적으로 합쳐진 영역의 가로/세로 크기를 저장
                Size = new Vector2(combinedBounds.size.x, combinedBounds.size.z);
            }
            else
            {
                Size = Vector2.zero;
            }
        }

        public virtual void Initialize()
        {
            foreach (var kv in doorPosList)
            {
                DoorPosDict[kv.Dir] = kv.Trm.position;
            }
            foreach (var kv in enterPosList)
            {
                EnterPosDict[kv.Dir] = kv.Trm.position;
            }
        }

        protected virtual void OnDestroy()
        {
            Bus<RoomClearEvent>.OnEvent -= HandleRoomClearEvent;
        }

        private void HandleRoomClearEvent(RoomClearEvent evt)
        {
            if (evt.ClearedRoom.Equals(this))
            {
                ThisRoomClear();
            }
        }

        public virtual void FirstEnterRoom()
        {
            EnterRoom();
        }

        public virtual void EnterRoom()
        {
        }

        public virtual void ThisRoomClear()
        {
            OnClear?.Invoke();
        }

        public void SetUpPool(Pool pool)
        {
            myPool = pool;
        }

        public void ResetItem()
        {
        }

        public virtual void PushRoom()
        {
            myPool.Push(this);
        }

        protected void FastClear()
        {
            Bus<StartAddRoomEvent>.OnEvent?.Invoke(new());
        }

        protected void EnemySpawn(IMonsterRoom monsterRoom)
        {
            Bus<EnemySpawnEvent>.OnEvent?.Invoke(new(monsterRoom));
        }
    }
}