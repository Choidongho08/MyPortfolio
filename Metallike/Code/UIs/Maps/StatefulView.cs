using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public abstract class StatefulView<TState> : MonoBehaviour where TState : struct
    {
        protected TState? prevState;
        protected bool isInit => !prevState.HasValue;

        /// <summary>
        /// 외부(Presenter나 부모 View)에서 호출하는 상태 업데이트 진입점입니다.
        /// </summary>
        public virtual void UpdateState(in TState newState)
        {
            // 상태 안변하면 리턴
            if (prevState != null && prevState.Value.Equals(newState))
                return;

            // Update 실행
            OnUpdateState(newState);

            // 상태 갱신
            prevState = newState;
        }

        /// <summary>
        /// UI Update 로직
        /// </summary>
        /// <param name="state"></param>
        protected abstract void OnUpdateState(TState state);

        protected void Bind<TValue>(TValue oldVal, TValue newVal, Action<TValue> updateAction)
        {
            if (isInit || !EqualityComparer<TValue>.Default.Equals(oldVal, newVal))
            {
                updateAction(newVal);
            }
        }
    }
}
