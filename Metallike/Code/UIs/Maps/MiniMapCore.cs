using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps;
using Core.EventBus;
using System;
using System.Collections.Generic;
using UnityEngine;
public interface IMinimapView
{
    void Initializer();
    void Show();
    void SetMinimap(List<IRoomUIData> datas, Vector2Int centerPos);
    void SetBattleSetting();
    void RollbackToDefaultSetting();
    void SetCurRoom(IRoomDef roomDef);
    void SetEnemies(List<Entity> enemies);
    void SetActive(bool active);
}

public class MinimapPresenter
{
    private readonly IMapDataProvider model;
    private readonly IMinimapView view;

    public MinimapPresenter(IMapDataProvider model, IMinimapView view)
    {
        this.model = model;
        this.view = view;
    }

    public void Initialize(Vector2Int targetPos)
    {
        // [핵심] 재시작 시 이벤트가 중복으로 등록되는 것을 방지하기 위해 먼저 해제
        Release();

        view.Initializer();
        view.SetMinimap(model.GetRoomIconListForMimimapView(), targetPos);

        Bus<EnterRoomEvent>.OnEvent += HandleEnterRoomEvent;
        Bus<EndAddRoomEvent>.OnEvent += HandleEndAddRoomEvent;
        Bus<BossRoomEvent>.OnEvent += HandleBossRoomEvent;
        Bus<EnemySpawnEvent>.OnEvent += HandleEnemySpawnEvent;
        Bus<RoomClearEvent>.OnEvent += HandleRoomClearEvent;
        Bus<AliveEnemiesEvent>.OnEvent += HandleAliveEnemiesEvent;
        Bus<ChoosePassiveEvents>.OnEvent += HandleChoosePassivleEvents;
        Bus<GetModuleEvents>.OnEvent += HandleGetModuleEvents;
    }

    public void Release()
    {
        Bus<EnterRoomEvent>.OnEvent -= HandleEnterRoomEvent;
        Bus<EndAddRoomEvent>.OnEvent -= HandleEndAddRoomEvent;
        Bus<BossRoomEvent>.OnEvent -= HandleBossRoomEvent;
        Bus<EnemySpawnEvent>.OnEvent -= HandleEnemySpawnEvent;
        Bus<RoomClearEvent>.OnEvent -= HandleRoomClearEvent;
        Bus<AliveEnemiesEvent>.OnEvent -= HandleAliveEnemiesEvent;
        Bus<ChoosePassiveEvents>.OnEvent -= HandleChoosePassivleEvents;
        Bus<GetModuleEvents>.OnEvent -= HandleGetModuleEvents;
    }

    private void HandleGetModuleEvents(GetModuleEvents evt)
    {
        view.SetActive(true);
    }

    private void HandleChoosePassivleEvents(ChoosePassiveEvents evt)
    {
        view.SetActive(false);
    }

    private void HandleAliveEnemiesEvent(AliveEnemiesEvent evt)
    {
        // if (evt.Enemies.Count > 0)
        view.SetEnemies(evt.Enemies);
    }

    private void HandleRoomClearEvent(RoomClearEvent evt)
    {
        view.RollbackToDefaultSetting();
        view.SetMinimap(model.GetRoomIconListForMimimapView(), model.PlayerRoomPos);
    }

    private void HandleEnemySpawnEvent(EnemySpawnEvent evt)
    {
        view.SetBattleSetting();
    }

    private void HandleBossRoomEvent(BossRoomEvent evt)
    {
        view.SetBattleSetting();
    }

    private void HandleEndAddRoomEvent(EndAddRoomEvent evt)
    {
        view.SetMinimap(model.GetRoomIconListForMimimapView(), model.PlayerRoomPos);
    }

    private void HandleEnterRoomEvent(EnterRoomEvent evt)
    {
        view.SetMinimap(model.GetRoomIconListForMimimapView(), evt.RoomDef.GridPos);
        view.SetCurRoom(evt.RoomDef);
    }
}