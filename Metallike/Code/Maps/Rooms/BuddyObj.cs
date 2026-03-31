using Code.Interface;
using Core.EventBus;
using GondrLib.ObjectPool.RunTime;
using UnityEngine;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class BuddyObj : MonoBehaviour, IInteractable, IPoolable
    {
        [field: SerializeField] public CharacterSO character { get; private set; }

        [field: SerializeField] public PoolItemSO PoolItem { get; private set; }

        public GameObject GameObject => gameObject;

        private Pool myPoool;
        private bool canInteract;

        public void EnterInteractionRange()
        {
        }

        public void ExitInteractionRange()
        {
        }

        public void OnInteract(Entity interacter)
        {
            if (!canInteract)
                return;

            Bus<FindFriendEvent>.Raise(new(character));
            PushItem();
        }

        public void ResetItem()
        {
            canInteract = false;
        }

        public void EnableInteract()
        {
            canInteract = true;
        }

        public void SetUpPool(Pool pool)
        {
            myPoool = pool;
        }

        public void PushItem()
        {
            myPoool.Push(this);
        }
    }
}
