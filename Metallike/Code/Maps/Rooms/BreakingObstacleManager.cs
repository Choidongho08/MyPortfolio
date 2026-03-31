using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;
using DG.Tweening;
using GondrLib.Dependencies;
using GondrLib.ObjectPool.RunTime;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class BreakingObstacleManager : MonoBehaviour
    {
        [Header("파편 설정")]
        [Tooltip("파편 개수")]
        [SerializeField] private int minDebrisCount = 3;
        [SerializeField] private int maxDebrisCount = 3;

        [Tooltip("파편 크기 비율 (기본 1.0)")]
        [SerializeField] private float debrisSize = 1.0f;

        [Header("물리 설정 (Pop Up 느낌)")]
        [SerializeField] private float minUpForce = 5f;
        [SerializeField] private float maxUpForce = 10f;
        [SerializeField] private float sideSpread = 2f;
        [SerializeField] private float lifeTime = 4.0f;

        [Header("풀링 아이템")]
        [SerializeField] private PoolItemSO debrisItem; // 인스펙터에서 할당 필수

        [Inject] private PoolManagerMono poolManager;

        private void Awake()
        {
            Bus<BreakObstacleEvent>.OnEvent += HandleBreakingObstacleEvent;
            Bus<BreakObstacleVFXEvent>.OnEvent += HandleBreakObstacleVFXEvent;
        }

        private void OnDestroy()
        {
            Bus<BreakObstacleEvent>.OnEvent -= HandleBreakingObstacleEvent;
            Bus<BreakObstacleVFXEvent>.OnEvent -= HandleBreakObstacleVFXEvent;
        }

        private void HandleBreakObstacleVFXEvent(BreakObstacleVFXEvent evt)
        {
            var effect = poolManager.Pop<PoolingEffect>(evt.VfxItem);
            effect.PlayVFX(evt.Pos, evt.Rot);
            PushObstacleEffect(evt.Duration, effect);
        }

        private void PushObstacleEffect(float duration, PoolingEffect effect)
        {
            DOVirtual.DelayedCall(duration, () => poolManager.Push(effect));
        }

        private void HandleBreakingObstacleEvent(BreakObstacleEvent evt)
        {
            var data = evt.DebrisData;
            // data안에 위치 있음.

            int debrisCount = Random.Range(minDebrisCount, maxDebrisCount);
            for (int i = 0; i < debrisCount; i++)
            {
                SpawnDebrisFromPool(data);
            }
        }

        private void SpawnDebrisFromPool(BreakingObstacleEventData data)
        {
            // 1. 풀에서 가져오기
            var debris = poolManager.Pop<IDebris>(debrisItem);
            if (debris == null) return;

            // 2. 위치 랜덤 설정 (Scale 정보 활용)
            Vector3 randomOffset = new Vector3(
                Random.Range(-data.Size.x / 2, data.Size.x / 2),
                Random.Range(-data.Size.y / 2, data.Size.y / 2),
                Random.Range(-data.Size.z / 2, data.Size.z / 2)
            );

            // Debris 위치/회전 세팅
            debris.Transform.position = data.Position + randomOffset;
            debris.Transform.rotation = Random.rotation;

            // 3. 크기 설정 (약간의 랜덤)
            float finalSize = debrisSize * Random.Range(0.8f, 1.2f);
            debris.Transform.localScale = Vector3.one * finalSize;

            // 4. 재질 설정
            debris.SetMaterial(data.Material);

            // 5. 물리 힘 계산 (위로 솟구치고 옆으로 살짝 퍼짐)
            float upForce = Random.Range(minUpForce, maxUpForce);
            float xForce = Random.Range(-sideSpread, sideSpread);
            float zForce = Random.Range(-sideSpread, sideSpread);

            Vector3 finalForce = new Vector3(xForce, upForce, zForce);

            // 6. 힘 적용 (Debris 내부 메서드 호출)
            debris.Eject(finalForce);
            debris.SetLifeTime(lifeTime);
        }
    }
}