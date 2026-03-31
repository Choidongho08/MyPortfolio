using UnityEngine;

public class SetRenderQueue : MonoBehaviour
{
    [SerializeField]
    private int renderQueue = 2020; // 2000(기본)보다 높은 값

    [SerializeField]
    private int sortingOrder = 10;

    void Start()
    {
        Renderer ren = GetComponent<Renderer>();
        if (ren != null)
        {
            // 1. 큐 설정 (기존과 동일)
            if (ren.material != null)
            {
                ren.material.renderQueue = renderQueue;
            }

            // 2. 정렬 순서(Sorting Order) 강제 지정
            // 마스크는 0(기본값)일 테니, 문을 10으로 두면
            // 무조건 "마스크(0) -> 문(10)" 순서로 그려집니다.
            ren.sortingOrder = sortingOrder;
        }
    }
}