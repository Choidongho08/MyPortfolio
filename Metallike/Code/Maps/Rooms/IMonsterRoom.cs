using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public interface IMonsterRoom
    {
        Transform[] SpawnTrms { get; }
        void SpawnMonsters();
    }
}
