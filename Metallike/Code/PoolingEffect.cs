using GondrLib.ObjectPool.RunTime;
using UnityEngine;

public class PoolingEffect : MonoBehaviour, IPoolable
{
    [field: SerializeField] public PoolItemSO PoolItem { get; private set; }
    public GameObject GameObject => gameObject;

    [SerializeField] private GameObject effectObject;

    private IPlayableVFX playableVFX;
    private Pool myPool;

    public void SetUpPool(Pool pool)
    {
        myPool = pool;
        playableVFX = effectObject.GetComponent<IPlayableVFX>();
    }

    public void ResetItem()
    {

    }

    private void OnValidate()
    {
        if (effectObject == null) return;
        playableVFX = effectObject.GetComponent<IPlayableVFX>();
        if (playableVFX == null)
        {
            Debug.LogError($"The effect object {effectObject.name} does not implement IPlayableVFX.");
            effectObject = null;
        }
    }

    public void PlayVFX(Vector3 hitPoint, Quaternion rotation)
    {
        playableVFX.PlayVfx(hitPoint, rotation);
    }
}
