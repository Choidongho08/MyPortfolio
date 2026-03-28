using Assets._01.Member.CDH.Code.EventBus;
using Assets._01.Member.CDH.Code.Events;
using Jsons;
using System;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Turns
{
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO turnManagerEventChannel;
        [SerializeField] private EventChannelSO uiEventChannel;
        [SerializeField] private EventChannelSO timeManagerEventChannel;
        [SerializeField] private CardStat watering;
        [SerializeField] private float waitingTime;
        [SerializeField] private float breakTime;
        [SerializeField] private float waveTime;

        private bool isWaitingTime;
        private bool isBreakTime;
        private bool isGameOver;

        private void Awake()
        {
            isGameOver = false;
            turnManagerEventChannel.AddListener<WaveEndEvent>(HandleWaveEnd);
            turnManagerEventChannel.AddListener<DrawUnitCardsEndEvent>(HandleUnitDrawCardsEnds);
            turnManagerEventChannel.AddListener<DrawItemCardsEndEvent>(HandleItemDrawCardsEnds);
            turnManagerEventChannel.AddListener<TimeSkipEvent>(HandleTimeSkipEvent);

            timeManagerEventChannel.AddListener<FinishTimerEvent>(HandleFinishTimer);
            Bus<YggdrasilDeadEvent>.OnEvent += HandleYggdrasilDead;
        }


        private void OnDestroy()
        {
            turnManagerEventChannel.RemoveListener<WaveEndEvent>(HandleWaveEnd);
            turnManagerEventChannel.RemoveListener<DrawUnitCardsEndEvent>(HandleUnitDrawCardsEnds);
            turnManagerEventChannel.RemoveListener<DrawItemCardsEndEvent>(HandleItemDrawCardsEnds);
            turnManagerEventChannel.RemoveListener<TimeSkipEvent>(HandleTimeSkipEvent);
            Bus<YggdrasilDeadEvent>.OnEvent -= HandleYggdrasilDead;
        }

        private void HandleYggdrasilDead(YggdrasilDeadEvent _evt)
        {
            isGameOver = true;
            Destroy(this);
        }

        private void HandleFinishTimer(FinishTimerEvent evt)
        {
            if (isGameOver)
                return;
            if (isWaitingTime)
            {
                StartWave();
            }
            if (isBreakTime)
            {
                DrawCards();
            }
        }

        private void HandleWaveEnd(WaveEndEvent evt)
        {
            if (isGameOver)
                return;
            // 휴식시간 시작
            Initialize();
            isBreakTime = true;
            timeManagerEventChannel.Invoke(TimeManagerEvents.EndTimerEvent.Initializer());
            timeManagerEventChannel.Invoke(TimeManagerEvents.SetTimerEvent.Initializer(breakTime));
            turnManagerEventChannel.Invoke(TurnManagerEvents.BreakTimeStartEvent.Initializer());
            turnManagerEventChannel.Invoke(TimeManagerEvents.StopStopWatch.Initializer((timer) =>
            turnManagerEventChannel.Invoke(TurnManagerEvents.WaveClearTimeEvent.Initializer(timer))));
        }

        private void HandleUnitDrawCardsEnds(DrawUnitCardsEndEvent evt)
        {
            if (isGameOver)
                return;
            Initialize();
            timeManagerEventChannel.Invoke(TimeManagerEvents.EndTimerEvent.Initializer());
            turnManagerEventChannel.Invoke(TurnManagerEvents.DrawItemCardsStartEvent.Initializer());
            turnManagerEventChannel.Invoke(TurnManagerEvents.BreakTimeEndEvent.Initializer());
        }

        private void HandleItemDrawCardsEnds(DrawItemCardsEndEvent obj)
        {
            if (isGameOver)
                return;
            // 대기 시간 시작
            Initialize();

            //uiEventChannel.Invoke(UIEvents.AddCard.Initializer(watering));
            //uiEventChannel.Invoke(UIEvents.AddCard.Initializer(watering));

            isWaitingTime = true;
            timeManagerEventChannel.Invoke(TimeManagerEvents.SetTimerEvent.Initializer(waitingTime));
            turnManagerEventChannel.Invoke(TurnManagerEvents.WaitingTimeStartEvent.Initializer());
        }

        private void HandleTimeSkipEvent(TimeSkipEvent evt)
        {
            if (isGameOver)
                return;
            if (isWaitingTime)
            {
                // 바로 웨이브 시작
                StartWave();
            }
            if (isBreakTime)
            {
                // 바로 카드 뽑기 시작
                DrawCards();
            }
        }

        private void StartWave()
        {
            if (isGameOver)
                return;
            Initialize();
            timeManagerEventChannel.Invoke(TimeManagerEvents.EndTimerEvent.Initializer());
            timeManagerEventChannel.Invoke(TimeManagerEvents.SetTimerEvent.Initializer(waveTime));
            turnManagerEventChannel.Invoke(TurnManagerEvents.WaveStartEvent.Initializer());
            turnManagerEventChannel.Invoke(TurnManagerEvents.WaitingTimeEndEvent.Initializer());
        }

        public void DrawCards()
        {
            if (isGameOver)
                return;
            Initialize();
            timeManagerEventChannel.Invoke(TimeManagerEvents.EndTimerEvent.Initializer());
            turnManagerEventChannel.Invoke(TurnManagerEvents.DrawUnitCardsStartEvent.Initializer());
            turnManagerEventChannel.Invoke(TurnManagerEvents.BreakTimeEndEvent.Initializer());
        }

        private void Initialize()
        {
            if (isGameOver)
                return;
            isBreakTime = false;
            isWaitingTime = false;
        }
    }
}
