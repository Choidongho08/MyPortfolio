namespace Assets.Work.CDH.Code.Maps.Rooms
{
    public interface IRoomDropObj
    {
        /// <summary>
        ///  Bus<RoomObjGenerateEvent> 이벤트 발행
        /// </summary>
        protected void Generate();

        /// <summary>
        ///  Bus<RoomObjRemoveEvent> 이벤트 발행
        /// </summary>
        protected void Remove();
    }
}
