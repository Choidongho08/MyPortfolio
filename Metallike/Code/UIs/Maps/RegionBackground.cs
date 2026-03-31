using Assets.Work.CDH.Code.Maps;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Work.CDH.Code.UIs.Maps
{
    // 구역 맵 배경(빈 공간)의 클릭을 감지하는 클래스
    public class RegionBackground : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public event Action<RegionType> OnClick;

        private Vector2 pointerDownPos;

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDownPos = eventData.position;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 1. 드래그 판정 시 클릭 무시 (RegionImage와 동일)
            float dragThreshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
            if (Vector2.Distance(pointerDownPos, eventData.position) > dragThreshold)
            {
                return;
            }

            // 2. 현재 마우스 위치 아래에 있는 모든 UI 객체 가져오기
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // 3. 클릭된 대상 중 'RegionImage(별)'가 있는지 검사
            foreach (var result in results)
            {
                // 별을 클릭했다면 배경 클릭은 무시하고 종료
                if (result.gameObject.GetComponent<RegionImage>() != null)
                {
                    return;
                }
            }

            // 4. 별이 아닌 진짜 맨땅(배경)을 클릭한 경우 None 발행
            OnClick?.Invoke(RegionType.None);
        }
    }
}