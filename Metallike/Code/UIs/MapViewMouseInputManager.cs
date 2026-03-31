using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Work.CDH.Code.UIs
{
    public class MapViewMouseInputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerMoveHandler
    {
        // private HashSet<RaycastResult> prevUIs; <- RaycastResult가 구조체라 매 프레임 마우스 위치나 거리 등에 의해서 달라지기 때문에 Enter후 바로 Exit가 발생했었음.
        private HashSet<GameObject> prevUIs;

        private void Awake()
        {
            prevUIs = new();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SendInputEvent(eventData, ExecuteEvents.pointerClickHandler);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SendInputEvent(eventData, ExecuteEvents.pointerDownHandler);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            SendInputEvent(eventData, ExecuteEvents.pointerMoveHandler);
            UpdatePointerEnterExit(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SendInputEvent(eventData, ExecuteEvents.pointerUpHandler);
        }

        private void SendInputEvent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> functor) where T : IEventSystemHandler
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            HashSet<GameObject> uis = new(); // 현재 UI들의 GameObject를 가져오기
            foreach (var result in results)
            {
                uis.Add(result.gameObject);
            }

            EventExecuter(uis, eventData, functor);
        }

        private void EventExecuter<T>(IEnumerable<GameObject> uis, PointerEventData eventData, ExecuteEvents.EventFunction<T> functor) where T : IEventSystemHandler
        {
            foreach (GameObject ui in uis)
            {
                if (ui == gameObject)
                {
                    continue;
                }
                ExecuteEvents.Execute(ui, eventData, functor);
            }
        }

        private void UpdatePointerEnterExit(PointerEventData eventData)
        {
            List<RaycastResult> raycastResults = new();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            HashSet<GameObject> curUIs = new(); // 현재 UI들의 GameObject를 가져오기
            foreach (var result in raycastResults)
            {
                curUIs.Add(result.gameObject);
            }

            // 2. Enter 이벤트 처리: [새로 들어온 UI] = [현재 UI] - [이전 UI]
            HashSet<GameObject> enteredUIs = new(curUIs);
            enteredUIs.ExceptWith(prevUIs);
            EventExecuter(enteredUIs, eventData, ExecuteEvents.pointerEnterHandler);

            // 3. Exit 이벤트 처리: [빠져나간 UI] = [이전 UI] - [현재 UI]
            HashSet<GameObject> exitedUIs = new(prevUIs);
            exitedUIs.ExceptWith(curUIs);
            EventExecuter(exitedUIs, eventData, ExecuteEvents.pointerExitHandler);

            prevUIs = curUIs;
        }
    }
}