using Assets.Work.CDH.Code.Maps;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public class MapUIInstaller : MonoBehaviour
    {
        [SerializeField] private MapViewport viewport;
        [SerializeField] private RoomMapView roomView;
        [SerializeField] private RegionMapView regionMap;
        [SerializeField] private LevelUICameraController levelUICameraController;

        public MapPresenter Initializer(IMapDataProvider mapModel)
        {
            MapPresenterCreateData createData = new()
            {
                Viewport = viewport,
                RoomsView = roomView,
                RegionView = regionMap,
                Model = mapModel,
                LevelUICameraController = levelUICameraController
            };
            return new(createData);
        }
    }
}
