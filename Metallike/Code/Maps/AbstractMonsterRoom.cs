using Assets.Work.CDH.Code.Eventss;
using Assets.Work.CDH.Code.Maps.Rooms;
using Core.EventBus;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps
{
    public abstract class AbstractMonsterRoom : AbstractRoom, IMonsterRoom
    {
        [field: SerializeField] public Transform[] SpawnTrms { get; private set; }

        public void SpawnMonsters()
        {
            EnemySpawn(this);
        }
    }
}
