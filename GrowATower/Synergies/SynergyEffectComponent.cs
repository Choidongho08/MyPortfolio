using Assets._01.Member.CDH.Code.EventBus;
using Assets._01.Member.CDH.Code.Events;
using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Synergies
{
    public abstract class SynergyEffectComponent : MonoBehaviour
    {
        [SerializeField] protected SynergySO targetSynergySO;

        protected int targetSynergyId;

        protected virtual void Awake()
        {
            targetSynergyId = GetSynergyId(targetSynergySO.synergy);
            Bus<SynergyActiveEvent>.OnEvent += HandleSynergyActiveEvent;
        }

        protected virtual void OnDestroy()
        {
            Bus<SynergyActiveEvent>.OnEvent -= HandleSynergyActiveEvent;
        }

        protected void HandleSynergyActiveEvent(SynergyActiveEvent _evt)
        {
            int evtSynergyId = GetSynergyId(_evt.Synergy);
            if (evtSynergyId != targetSynergyId) // targetSynergy와 다르면 아무처리 안함
                return;

            SynergyActiveMethod(_evt.Synergy);
        }

        protected abstract void SynergyActiveMethod(Synergy synergy);

        protected virtual int GetSynergyId(Synergy synergy)
        {
            // 1. 원본 문자열 준비 (공백 제거 및 소문자 변환)
            string source = synergy.name.Trim().ToLowerInvariant();

            // 2. SHA256 해시 계산
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));

                // 3. 해시의 첫 4바이트(32비트)를 int로 변환
                // BitConverter.ToInt32는 바이트 배열의 0번 인덱스부터 4바이트를 읽어
                // 32비트 정수(int)로 변환합니다.
                int unitId = BitConverter.ToInt32(hash, 0);

                return unitId;
            }
        }

    }
}
