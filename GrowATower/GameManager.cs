using Ami.BroAudio;
using Assets._01.Member.CDH.Code.Cores;
using Assets._01.Member.CDH.Code.EventBus;
using Assets._01.Member.CDH.Code.Events;
using System;
using UnityEngine;

namespace Assets._04.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO uiEventChannel;
        [SerializeField] private EventChannelSO turnManagerChannel;
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private UICardList unitList;
        [SerializeField] private UICardList itemList;
        [SerializeField] private InputReaderSO inputSO;

        [Header("BGMs")]
        [SerializeField] private SoundID waitWave;
        [SerializeField] private SoundID startWave;

        public bool IsFastGame { get; private set; }
        private bool isDrawingCard;

        private void Awake()
        {
            uiEventChannel.AddListener<SetTimeScale>(HandleSetSpeedGame);
            turnManagerChannel.AddListener<WaitingTimeStartEvent>(HandleBulidStart);
            turnManagerChannel.AddListener<WaveStartEvent>(HandleWaveStart);
            turnManagerChannel.AddListener<DrawUnitCardsStartEvent>(HandleDrawUnitCardStart);
            turnManagerChannel.AddListener<DrawItemCardsStartEvent>(HandleDrawItemCardStart);
            inputSO.OnFastGamePressed += HandleFastGame;
            Bus<GameEndEvent>.OnEvent += HandleGameEndEvent;
        }

        private void Start()
        {
            if (SceneData.Instance.UnitDataForPotManager != null)
                uiEventChannel?.Invoke(UIEvents.AddUintCard.Initializer(SceneData.Instance.UnitDataForPotManager));
        }

        private void HandleWaveStart(WaveStartEvent @event)
        {
            soundChannel.Invoke(SoundEvents.BGMEvent.Initialize(startWave));
        }

        private void HandleBulidStart(WaitingTimeStartEvent @event)
        {

            isDrawingCard = false;
        }

        private void HandleSetSpeedGame(SetTimeScale obj)
        {
            Time.timeScale = obj.value;
            soundChannel.Invoke(SoundEvents.SpeedEvent.Initialize(obj.value<1f ? 0.8f: obj.value));
        }

        public void HandleFastGame()
        {
            IsFastGame = !IsFastGame;
            if (isDrawingCard)
            {
                var failevt = GlitchEffectEvt.FailSpeedEvent.Initialize();
                uiEventChannel.Invoke(failevt);
                IsFastGame = false;
            }
            float value = IsFastGame ? 2f : 1f;
            var evt = SoundEvents.SpeedEvent.Initialize(value);
            soundChannel.Invoke(evt);
            var fadeEvt = GlitchEffectEvt.glichEvent.Initialize(IsFastGame);
            uiEventChannel.Invoke(fadeEvt);
            Time.timeScale = value;
        }

        private void OnDestroy()
        {
            turnManagerChannel.RemoveListener<DrawUnitCardsStartEvent>(HandleDrawUnitCardStart);
            turnManagerChannel.RemoveListener<DrawItemCardsStartEvent>(HandleDrawItemCardStart);
            turnManagerChannel.RemoveListener<WaitingTimeStartEvent>(HandleBulidStart);
            uiEventChannel.RemoveListener<SetTimeScale>(HandleSetSpeedGame);
            inputSO.OnFastGamePressed -= HandleFastGame;
            Bus<GameEndEvent>.OnEvent -= HandleGameEndEvent;
        }

        // From ShovelUI
        private void HandleGameEndEvent(GameEndEvent _evt)
        {
            // 메인 메뉴로
            const string mainMenuSceneName = "MainMenuScene";
            TransitionManager.Instance.NextSceneName = mainMenuSceneName;
            TransitionManager.Instance.LoadScene();
        }

        private void HandleDrawItemCardStart(DrawItemCardsStartEvent obj)
        {
            print("HandleDrawItemCardStart");
            var drawCardsEvt = UIEvents.RandomShuffle;
            drawCardsEvt.chooseTimer = 10f;
            drawCardsEvt.resultList = RandomShuffle.ShuffleRandomCards(itemList.unitStats, 6);
            drawCardsEvt.isItem = true;
            uiEventChannel.Invoke(drawCardsEvt);
        }

        private void HandleDrawUnitCardStart(DrawUnitCardsStartEvent evt)
        {
            print("HandleDrawUnitCardStart");
            var drawCardsEvt = UIEvents.RandomShuffle;
            soundChannel.Invoke(SoundEvents.BGMEvent.Initialize(waitWave));
            drawCardsEvt.chooseTimer = 10f;
            drawCardsEvt.resultList = RandomShuffle.ShuffleRandomCards(unitList.unitStats, 6);
            drawCardsEvt.isItem = false;
            uiEventChannel.Invoke(drawCardsEvt);
            IsFastGame = true;
            HandleFastGame();
            isDrawingCard = true;
        }
    }
}
