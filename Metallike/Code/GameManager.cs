using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps.Rooms;
using Core.EventBus;
using DG.Tweening;
using ManagingSystem;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private StopInfoSO _stopInfoSO;

    public delegate void UnityEventListener();
    public event UnityEventListener OnStartEvent = null;

    public bool InPause { get; set; }

    protected override async void Awake()
    {
        DOTween.Init(true, true, LogBehaviour.Verbose).SetCapacity(2000, 100);

        var managers = GetComponentsInChildren<IManager>();
        foreach (var manager in managers)
        {
            manager.Init();
        }
    }

    private void Start()
    {
        OnStartEvent?.Invoke();
    }

    public void Pause()
    {
        InPause = true;
        StopManager.Instance.GenerateStop(_stopInfoSO);
        InputManager.Instance.SetEnableInputWithout(EInputCategory.Pause, true);
    }

    public void Resume(bool stateSettingSelf = true)
    {
        if (!InPause)
        {
            return;
        }

        if (stateSettingSelf)
        {
            InPause = false;
        }
        StopManager.Instance.ReleaseStop(StopChannel.UI);
        InputManager.Instance.SetEnableInputAll(true);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}