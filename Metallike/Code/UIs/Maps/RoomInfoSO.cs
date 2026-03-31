using Assets.Work.CDH.Code.Maps;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomInfo
{
    public string roomName;
    public RoomType roomType;
    public string descript;
}

[CreateAssetMenu(fileName = "RoomInfoSO", menuName = "SO/RoomInfoSO")]
public class RoomInfoSO : ScriptableObject
{
    
    public RoomInfo[] infos;

    private Dictionary<RoomType, RoomInfo> infoDict;

    private void BuildDict()
    {
        if (infoDict != null) return;

        infoDict = new Dictionary<RoomType, RoomInfo>();

        foreach (var info in infos)
        {
            if (infoDict.ContainsKey(info.roomType))
            {
                Debug.LogWarning(
                    $"RoomInfoSO 중복 RoomType 발견: {info.roomType}", this);
                continue;
            }

            infoDict.Add(info.roomType, info);
        }
    }
    public string GetNameInfo(RoomType room)
    {
        BuildDict();
        return infoDict.TryGetValue(room, out var info)
            ? info.roomName
            : string.Empty;
    }

    public string GetDescInfo(RoomType room)
    {
        BuildDict();
        return infoDict.TryGetValue(room, out var info)
            ? info.descript
            : string.Empty;
    }

}
