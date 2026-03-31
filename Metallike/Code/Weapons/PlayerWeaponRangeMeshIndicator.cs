using UnityEngine;

namespace Assets.Work.CDH.Code.Weapons
{
    public class PlayerWeaponRangeMeshIndicator : WeaponRangeMeshIndicator
    {
        private Player player;

        public override void Initialize(Entity _entity)
        {
            base.Initialize(_entity);
            player = _entity as Player;
        }

        private void HandleRightButton(bool active)
        {
            SetActive(active);
            Clear();
        }

        private void Update()
        {
            if (!isActive)
                return;

            Vector3 dir = player.PlayerInput.GetWorldPosition() - originTrm.position;
            SetDir(new Vector2(dir.x, dir.z));
        }
    }
}
