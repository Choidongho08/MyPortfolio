using Assets.Work.CDH.Code.Maps;
using Assets.Work.CDH.Code.UIs.Maps;
using UnityEngine;

public class MinimapUIInstaller : MonoBehaviour
{
    [SerializeField] private MinimapView view = null!;

    /// <summary>
    /// From MapManager
    /// </summary>
    public MinimapPresenter Initializer(IMapDataProvider mapModel)
    {
        var presenter = new MinimapPresenter(mapModel, view);
        return presenter;
    }
}
