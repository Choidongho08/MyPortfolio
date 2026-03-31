using Assets.Work.CDH.Code.Weapons;
using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using static Assets.Work.CDH.Code.Weapons.WeaponComponent;

public enum RangeShape
{
    None,
    Rectangle,
    Trapezoid,
    Sector,
}

[Serializable]
public struct RangeMarkData
{
    [Header("Common Params")]
    public float startOffset;
    public float length;

    [Header("Rectangle/Trapezoid")]
    public float width;        // Rectangle 폭(전체)
    public float farWidth;     // Trapezoid 원거리 폭(전체) (near=width, far=farWidth)

    [Header("Sector")]
    [Range(1f, 360f)]
    public float sectorAngle;
    [Range(3, 180)]
    public int sectorSegments;

    [Header("State")]
    public RangeShape shape;
}

/// <summary>
/// Decal 대신 "바닥에 얇은 메쉬"로 범위 표시.
/// - 오브젝트는 originTrm 위치(groundY)에 붙고, dir 방향으로 회전
/// - 메쉬 로컬 좌표에서 +Z가 "앞" 입니다.
/// - startOffset은 "origin에서부터 범위 시작까지의 거리"로 로컬 Z에 그대로 반영됩니다.
/// </summary>
public class WeaponRangeMeshIndicator : MonoBehaviour, IEntityComponent
{
    [SerializeField] private MeshFilter meshFilter;

    [Header("Follow")]
    [SerializeField] private float groundY = 0f;
    [SerializeField] private float yOffset = 0.02f; // Z-fighting 방지용(원치 않으면 0)

    private Mesh mesh;
    protected Transform originTrm;
    protected bool isActive;
    protected RangeMarkData curWeaponRangeMarkData;

    // 재사용 버퍼(할당 줄이기)
    private readonly List<Vector3> verts = new(512);
    private readonly List<int> tris = new(1024);
    private readonly List<Vector2> uvs = new(512);

    public virtual void Initialize(Entity _entity)
    {
        originTrm = _entity.transform;
        mesh = new Mesh { name = "WeaponRangeMesh" };
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;
        isActive = false;
        curWeaponRangeMarkData = default;
    }

    public void HandleChangeWeaponEvent(ChangeWeaponEvent evt)
    {
        IWeapon weapon = evt.newWeapon;
        curWeaponRangeMarkData = weapon.WeaponData.rangeMarkData;
    }

    private void LateUpdate()
    {
        // 예시: 항상 현재 shape로 갱신하고 싶으면
        // 원치 않으면 외부에서 SetXXX 호출할 때만 Build 하도록 바꿔도 됩니다.
        if (!isActive || curWeaponRangeMarkData.shape == RangeShape.None || originTrm == null) return;

        // 위치/회전 갱신
        UpdateTransform(lastDir);

        // 메쉬는 파라미터 바뀔 때만 다시 빌드하는 게 최적이지만,
        // 지금은 단순하게 매 프레임 갱신 형태로 둡니다.
        Build(curWeaponRangeMarkData.shape);
    }

    private Vector2 lastDir = new Vector2(0, 1);

    /// <summary>외부에서 마우스 방향 등으로 호출</summary>
    public void SetDir(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        lastDir = dir.normalized;
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }

    public void SetShape(RangeShape newShape) => curWeaponRangeMarkData.shape = newShape;

    public void SetCommon(float newStartOffset, float newLength)
    {
        curWeaponRangeMarkData.startOffset = newStartOffset;
        curWeaponRangeMarkData.length = newLength;
    }

    public void SetRectParams(float newWidth) => curWeaponRangeMarkData.width = newWidth;

    public void SetTrapParams(float nearWidth, float newFarWidth)
    {
        curWeaponRangeMarkData.width = nearWidth;
        curWeaponRangeMarkData.farWidth = newFarWidth;
    }

    public void SetSectorParams(float angleDeg, int segments)
    {
        curWeaponRangeMarkData.sectorAngle = Mathf.Clamp(angleDeg, 1f, 360f);
        curWeaponRangeMarkData.sectorSegments = Mathf.Clamp(segments, 3, 180);
    }

    private void UpdateTransform(Vector2 dir)
    {
        if (originTrm == null) return;

        Vector3 d = new Vector3(dir.x, 0f, dir.y);
        if (d.sqrMagnitude < 0.0001f) d = originTrm.forward;
        d.y = 0f;
        d.Normalize();

        transform.position = new Vector3(originTrm.position.x, groundY + yOffset, originTrm.position.z);
        transform.rotation = Quaternion.LookRotation(d, Vector3.up);
    }

    private void Build(RangeShape s)
    {
        switch (s)
        {
            case RangeShape.Rectangle:
                BuildRectangle(curWeaponRangeMarkData.width);
                break;
            case RangeShape.Trapezoid:
                BuildTrapezoid(curWeaponRangeMarkData.width, curWeaponRangeMarkData.farWidth);
                break;
            case RangeShape.Sector:
                BuildSector(curWeaponRangeMarkData.sectorAngle, curWeaponRangeMarkData.sectorSegments);
                break;
        }
    }

    // ------------------ Mesh Builders ------------------

    private void BuildRectangle(float w)
    {
        float halfW = w * 0.5f;
        float z0 = Mathf.Clamp(curWeaponRangeMarkData.startOffset, 0f, curWeaponRangeMarkData.length);
        float z1 = Mathf.Max(z0, curWeaponRangeMarkData.length); // 끝은 항상 length

        verts.Clear(); tris.Clear(); uvs.Clear();

        // 로컬 기준 +Z가 앞
        verts.Add(new Vector3(-halfW, 0f, z0)); // 0 near-left
        verts.Add(new Vector3(halfW, 0f, z0)); // 1 near-right
        verts.Add(new Vector3(-halfW, 0f, z1)); // 2 far-left
        verts.Add(new Vector3(halfW, 0f, z1)); // 3 far-right

        // 위(+Y) 노멀로 보이게
        tris.Add(0); tris.Add(2); tris.Add(1);
        tris.Add(2); tris.Add(3); tris.Add(1);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));

        Apply();
    }

    private void BuildTrapezoid(float nearW, float farW)
    {
        float hn = nearW * 0.5f;
        float hf = farW * 0.5f;
        float z0 = Mathf.Clamp(curWeaponRangeMarkData.startOffset, 0f, curWeaponRangeMarkData.length);
        float z1 = Mathf.Max(z0, curWeaponRangeMarkData.length);

        verts.Clear(); tris.Clear(); uvs.Clear();

        verts.Add(new Vector3(-hn, 0f, z0)); // 0 near-left
        verts.Add(new Vector3(hn, 0f, z0)); // 1 near-right
        verts.Add(new Vector3(-hf, 0f, z1)); // 2 far-left
        verts.Add(new Vector3(hf, 0f, z1)); // 3 far-right

        tris.Add(0); tris.Add(2); tris.Add(1);
        tris.Add(2); tris.Add(3); tris.Add(1);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));

        Apply();
    }

    private void BuildSector(float angleDeg, int segments)
    {
        segments = Mathf.Clamp(segments, 3, 180);

        float innerR = Mathf.Clamp(curWeaponRangeMarkData.startOffset, 0f, curWeaponRangeMarkData.length);
        float outerR = Mathf.Max(innerR, curWeaponRangeMarkData.length);

        float half = angleDeg * 0.5f;

        verts.Clear(); tris.Clear(); uvs.Clear();

        // 정점: (inner, outer) 쌍을 segments+1개 생성
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float a = Mathf.Lerp(-half, half, t) * Mathf.Deg2Rad;

            float sx = Mathf.Sin(a);
            float cz = Mathf.Cos(a);

            Vector3 inner = new Vector3(sx, 0f, cz) * innerR;
            Vector3 outer = new Vector3(sx, 0f, cz) * outerR;

            verts.Add(inner);
            verts.Add(outer);

            // UV: U는 각도 진행, V는 inner(0)~outer(1)
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));
        }

        // 삼각형: 띠(quad) 연결
        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;       // inner_i
            int o0 = i * 2 + 1;   // outer_i
            int i1 = (i + 1) * 2;     // inner_{i+1}
            int o1 = (i + 1) * 2 + 1; // outer_{i+1}

            // 두 삼각형으로 한 quad
            tris.Add(i0); tris.Add(o0); tris.Add(i1);
            tris.Add(i1); tris.Add(o0); tris.Add(o1);
        }

        Apply();
    }

    private void Apply()
    {
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    protected void Clear()
    {
        mesh.Clear();
        verts.Clear(); 
        tris.Clear();
        uvs.Clear();
    }
}
