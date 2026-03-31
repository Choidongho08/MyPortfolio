using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class NormalRoom : AbstractModuleRoom, IMonsterRoom
    {
        [field: SerializeField] public Transform[] SpawnTrms { get; private set; }

        public void ChangeToBossRoom()
        {
            gameObject.AddComponent<BossRoom>();
            Destroy(gameObject);
        }

        public void SpawnMonsters()
        {
            EnemySpawn(this);
        }
    }
}
