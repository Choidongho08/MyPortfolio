using Assets.Work.CDH.Code.Maps;
using System;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    public readonly record struct RegionImageState(
        bool IsActive
        );

    public interface IRegionImage
    {
        event Action<RegionType> OnClick;

        RegionType RegionType { get; }

        void Initialize(RegionImageInitData data);
        void UpdateState(RegionImageState state);
    }
}
