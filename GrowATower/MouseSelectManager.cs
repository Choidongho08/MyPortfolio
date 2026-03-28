using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

namespace Assets._01.Member.CDH.Code.Cores
{
    public class MouseSelectManager : MonoSingleton<MouseSelectManager>
    {
        [SerializeField] private InputReaderSO inputSO;
        private LayerMask targetLayer;
        private bool waitingForClick;

        private TaskCompletionSource<ISelect> selectionTcs;
        private ISelect currentSelected; // 현재 선택된 오브젝트

        protected override void Awake()
        {
            base.Awake();
            //inputSO.OnMouseLBPressed += HandleMouseLBClick;
        }

        private void OnDestroy()
        {
            //inputSO.OnMouseLBPressed -= HandleMouseLBClick;
        }

        private void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                HandleMouseLBClick();
        }

        private void HandleMouseLBClick()
        {
            if (!waitingForClick || selectionTcs == null)
                return;

            if (inputSO.GetMousePosition(out RaycastHit hit, targetLayer))
            {
                var selectable = hit.collider.GetComponent<ISelect>();
                if (selectable != null)
                {
                    selectionTcs.TrySetResult(selectable);
                }
            }
        }

        /// <summary>
        /// 🔹 일반 선택 모드: 같은 오브젝트 다시 클릭해도 유지됨.
        /// </summary>
        public async Task<T> SetMouseClick<T>(LayerMask unitLayer) where T : class, ISelect
        {
            Debug.Log("Waiting for mouse click (normal mode)...");

            waitingForClick = true;
            targetLayer = unitLayer;
            selectionTcs = new TaskCompletionSource<ISelect>();

            try
            {
                ISelect selected = await selectionTcs.Task;
                waitingForClick = false;
                selectionTcs = null;

                if (selected == currentSelected)
                {
                    Debug.Log("Clicked same object again (ignored).");
                    return selected as T;
                }

                // currentSelected?.DeSelect();

                T result = selected as T;
                if (result != null)
                {
                    currentSelected = selected;
                    // currentSelected.Select();
                    Debug.Log($"Selected new object: {result}");
                    return result;
                }

                Debug.LogWarning($"Selected object is not of type {typeof(T).Name}");
                return null;
            }
            catch (TaskCanceledException)
            {
                Debug.Log("Mouse click selection was canceled.");
                return null;
            }
            finally
            {
                waitingForClick = false;
                selectionTcs = null;
            }
        }

        /// <summary>
        /// 🔄 토글 선택 모드: 같은 오브젝트 다시 클릭하면 선택 해제됨.
        /// </summary>
        public async Task<T> SetMouseClickToggle<T>(LayerMask unitLayer) where T : class, ISelect
        {
            Debug.Log("Waiting for mouse click (toggle mode)...");

            waitingForClick = true;
            targetLayer = unitLayer;
            selectionTcs = new TaskCompletionSource<ISelect>();

            ISelect selected = await selectionTcs.Task;

            waitingForClick = false;
            selectionTcs = null;

            // 같은 오브젝트 클릭 → 선택 해제
            if (selected == currentSelected)
            {
                // currentSelected.DeSelect();
                currentSelected = null;
                Debug.Log("Deselected (clicked same object again).");
                return null;
            }

            // 기존 선택 해제
            if (currentSelected != null)
                // currentSelected.DeSelect();

            // 새 선택
            if (selected is T result)
            {
                currentSelected = result;
                // currentSelected.Select();
                Debug.Log($"Selected new object: {result}");
                return result;
            }

            Debug.LogWarning($"Selected object is not of type {typeof(T).Name}");
            return null;
        }

        public void StopMouseClick()
        {
            if (waitingForClick && selectionTcs != null)
            {
                selectionTcs.TrySetCanceled();
            }

            waitingForClick = false;
            selectionTcs = null;
        }

        public void ClearSelection()
        {
            if (currentSelected != null)
            {
                // currentSelected.DeSelect();
                currentSelected = null;
            }
        }

        public ISelect CurrentSelected => currentSelected;
    }
}
