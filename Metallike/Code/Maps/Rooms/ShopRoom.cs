using Assets.Work.CDH.Code.Eventss;
using Core.EventBus;

namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public class ShopRoom : AbstractRoom
    {
        public override void FirstEnterRoom()
        {
            base.FirstEnterRoom();
            FastClear();
        }
    }
}
