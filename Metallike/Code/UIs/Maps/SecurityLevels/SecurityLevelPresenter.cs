using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps;
using Core.EventBus;
using System;
using System.Runtime.Serialization;
using Unity.AppUI.Redux;

namespace Assets.Work.CDH.Code.UIs.Maps.SecurityLevels
{
    public readonly record struct SecurityLevelViewState(
        int SecurityLevel,
        float SecurityValue
    );

    public interface ISecurityLevelView : IUpdatable<SecurityLevelViewState>
    {
        void Initialize();
    }

    public readonly record struct SecurityLevelPresenterInitData(
        IMapDataProvider Model,
        ISecurityLevelView SecurityLevelView
    );

    public class SecurityLevelPresenter
    {
        IMapDataProvider model;
        private ISecurityLevelView securityLevelView;
        private SecurityLevelViewState securityLevelViewState;

        private int currentSecurityLevel;
        private float currentSecurityValue;

        public SecurityLevelPresenter(SecurityLevelPresenterInitData initData)
        {
            model = initData.Model;
            securityLevelView = initData.SecurityLevelView;
        }

        public void Initialize()
        {
            SecurityViewInits();

            SubscribeEvents();
        }

        private void SecurityViewInits()
        {
            securityLevelView.Initialize();
            currentSecurityLevel = 0;
            currentSecurityValue = 0f;
            UpdateSecurityLevelView();
        }

        public void Release()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            Bus<SecurityLevelUpdateEvent>.OnEvent += HandleSecurityLevelUpdateEvent;
        }

        private void UnsubscribeEvents()
        {
            Bus<SecurityLevelUpdateEvent>.OnEvent -= HandleSecurityLevelUpdateEvent;
        }

        private void HandleSecurityLevelUpdateEvent(SecurityLevelUpdateEvent evt)
        {
            currentSecurityLevel = evt.Level;
            currentSecurityValue = evt.Value;
            UpdateSecurityLevelView();
        }

        private void UpdateSecurityLevelView()
        {
            securityLevelViewState = securityLevelViewState with
            {
                SecurityLevel = currentSecurityLevel,
                SecurityValue = currentSecurityValue,
            };
            securityLevelView.UpdateState(securityLevelViewState);
        }
    }
}
