#include "pch.h"
#include "RythmNode.h"
#include "INode.h"
#include "CameraManager.h"
#include "ResourceManager.h"

#include <array>
#include <iostream>
using std::cout;

// ----------------------------------------------------------
// 생성자
// ----------------------------------------------------------
RythmNode::RythmNode() :
    m_curIndex(0),
    m_timer(1.f),
    m_oneNodeTimer(0.7f),
    m_rythmNodeSlotMaxDistance(100.f),
    Object()
{
    m_tex = GET_SINGLE(ResourceManager)->GetTexture(L"MainUI");

    // 슬롯 초기화
    m_nodes.fill(nullptr);

    SetName("RythmNode");

    static Vec2 size = { WINDOW_WIDTH, WINDOW_HEIGHT / 5 };
    static Vec2 pos = { WINDOW_WIDTH / 2.f, WINDOW_HEIGHT - (size.y / 2.f) };
    SetSize(size);
    SetPos(pos);

    // 사용 안 하는 임시 변수 (원래 코드대로 남겨둠)
    static float oneSlotX = size.x / 4;
    static float curX = size.x / 8;
    static float y = size.y / 2;

    // ====== 네모 효과 초기화 ======
    m_slotBlinkTimers.fill(0.f);
    m_slotBlinkDuration = 0.3f;               // 0.3초 동안 보이기

    m_blinkRectSize = { size.x / 3.7f, size.y * 0.95f };
    m_blinkRectAlpha = static_cast<BYTE>(0.3f * 255.f);
}

// ----------------------------------------------------------
// 소멸자
// ----------------------------------------------------------
RythmNode::~RythmNode()
{
    for (auto* item : m_nodes)
    {
        item = nullptr;
    }
}

// ----------------------------------------------------------
// Update
// ----------------------------------------------------------
void RythmNode::Update()
{
    m_timer += fDT;
    if (m_timer >= m_oneNodeTimer)
    {
        // 현재 슬롯 노드 실행
        if (m_nodes[m_curIndex] != nullptr)
        {
            m_nodes[m_curIndex]->Excute();
        }

        cout << "뿅";
        GET_SINGLE(CameraManager)->StartPulse(38, 0.8f);

        // 이 타이밍에 현재 슬롯 네모 깜빡이기 시작
        m_slotBlinkTimers[m_curIndex] = m_slotBlinkDuration;

        // 타이머/인덱스 갱신
        m_timer = 0.f;
        m_curIndex++;
        if (m_curIndex > 3)
        {
            m_curIndex = 0;
        }
    }

    // 각 슬롯의 남은 표시 시간 감소
    for (int i = 0; i < (int)m_slotBlinkTimers.size(); ++i)
    {
        if (m_slotBlinkTimers[i] > 0.f)
        {
            m_slotBlinkTimers[i] -= fDT;
            if (m_slotBlinkTimers[i] < 0.f)
                m_slotBlinkTimers[i] = 0.f;
        }
    }

    // 노드 업데이트
    for (auto* node : m_nodes)
    {
        if (!node)
            continue;

        node->Update();
    }
}

// ----------------------------------------------------------
// Render
// ----------------------------------------------------------
void RythmNode::Render(HDC _hdc)
{
    Vec2 pos = GetRenderPos();
    Vec2 size = GetRenderSize();

    LONG width = m_tex->GetWidth();
    LONG height = m_tex->GetHeight();

    // 리듬 바 배경
    ::TransparentBlt(_hdc
        , (int)(pos.x - size.x / 2)
        , (int)(pos.y - size.y / 2)
        , (int)size.x
        , (int)size.y
        , m_tex->GetTextureDC()
        , 0, 0, width, height,
        RGB(255, 0, 255));

    ComponentRender(_hdc);

    // 슬롯에 붙어 있는 노드 렌더링
    for (int i = 0; i < (int)m_nodes.size(); ++i)
    {
        if (!m_nodes[i])
            continue;
        m_nodes[i]->Render(_hdc);
    }

    // ====== 각 슬롯의 네모(반투명) 렌더링 ======
    for (int i = 0; i < (int)m_slotBlinkTimers.size(); ++i)
    {
        if (m_slotBlinkTimers[i] <= 0.f)
            continue;

        Vec2 center = GetSlotCenter(i);
        DrawAlphaRect(_hdc, center);
    }
}

// ----------------------------------------------------------
// 슬롯에 INode 설정
// ----------------------------------------------------------
bool RythmNode::SetRythmNode(INode* node, int idx)
{
    if (idx < 0 || idx >= (int)m_nodes.size())
        return false;

    if (!node)
    {
        cout << "[SetRythmNode]의 매개변수인 node가 nullptr입니다\n";
        return false;
    }
    if (m_nodes[idx])
    {
        cout << "이미 노드가 들어가 있습니다.\n";
        return false;
    }

    m_nodes[idx] = node;
    node->Init();
    return true;
}

// ----------------------------------------------------------
// 슬롯에서 노드 제거
// ----------------------------------------------------------
void RythmNode::RemoveRythmNode(int index)
{
    if (index < 0 || index >= (int)m_nodes.size())
        return;

    m_nodes[index] = nullptr;
    m_slotBlinkTimers[index] = 0.f; // 혹시 남아있던 효과도 제거
}

// ----------------------------------------------------------
// 플레이어 등록 (필요 시 구현)
// ----------------------------------------------------------
void RythmNode::ResisterPlayer()
{
    for (int i = 0; i < 4; ++i)
    {
        if (!m_nodes[i])
            continue;
        // TODO: 플레이어 관련 로직이 있으면 여기서 처리
    }
}

// =================== 슬롯 관련 구현 ========================

// 슬롯 idx 의 중앙 위치
Vec2 RythmNode::GetSlotCenter(int idx) const
{
    Vec2 pos = GetPos();
    Vec2 size = GetSize();

    float slotCount = (float)m_nodes.size();
    float slotW = size.x / slotCount;
    float left = pos.x - size.x * 0.5f;

    Vec2 center;
    center.x = left + slotW * (idx + 0.5f);
    center.y = pos.y;
    return center;
}

// 슬롯 반폭 (스냅 범위 기준으로 사용)
float RythmNode::GetSlotHalfWidth() const
{
    Vec2 size = GetSize();
    float slotW = size.x / (float)m_nodes.size();
    return slotW * 0.5f;
}

// worldPos 근처에 있는 가장 가까운 슬롯 찾기
bool RythmNode::FindNearestSlot(const Vec2& worldPos,
    int& outIndex,
    Vec2& outCenter) const
{
    outIndex = -1;

    float maxDistSq = m_rythmNodeSlotMaxDistance * m_rythmNodeSlotMaxDistance;
    float bestDistSq = maxDistSq;

    for (int i = 0; i < (int)m_nodes.size(); ++i)
    {
        Vec2 c = GetSlotCenter(i);
        float dx = worldPos.x - c.x;
        float dy = worldPos.y - c.y;
        float distSq = dx * dx + dy * dy;

        if (distSq <= bestDistSq)
        {
            bestDistSq = distSq;
            outIndex = i;
            outCenter = c;
        }
    }

    return (outIndex != -1);
}

// =================== 네모(반투명) 관련 ========================

// 네모 크기 설정
void RythmNode::SetBlinkRectSize(const Vec2& size)
{
    m_blinkRectSize = size;
}

// 투명도 설정 (0~1)
void RythmNode::SetBlinkRectAlpha(float alpha01)
{
    if (alpha01 < 0.f) alpha01 = 0.f;
    if (alpha01 > 1.f) alpha01 = 1.f;
    m_blinkRectAlpha = static_cast<BYTE>(alpha01 * 255.f);
}

// 반투명 네모를 그리는 헬퍼
void RythmNode::DrawAlphaRect(HDC hdc, const Vec2& center)
{
    int w = (int)m_blinkRectSize.x;
    int h = (int)m_blinkRectSize.y;
    if (w <= 0 || h <= 0)
        return;

    // 1. 메모리 DC + 비트맵 준비
    HDC memDC = CreateCompatibleDC(hdc);
    if (!memDC)
        return;

    HBITMAP bmp = CreateCompatibleBitmap(hdc, w, h);
    if (!bmp)
    {
        DeleteDC(memDC);
        return;
    }

    HBITMAP oldBmp = (HBITMAP)SelectObject(memDC, bmp);

    // 2. 흰 네모를 그린다 (완전 불투명 이미지)
    HBRUSH brush = CreateSolidBrush(RGB(255, 255, 255));
    HPEN   pen = CreatePen(PS_SOLID, 0, RGB(255, 255, 255));
    HBRUSH oldBrush = (HBRUSH)SelectObject(memDC, brush);
    HPEN   oldPen = (HPEN)SelectObject(memDC, pen);

    Rectangle(memDC, 0, 0, w, h);

    // GDI 원복
    SelectObject(memDC, oldBrush);
    SelectObject(memDC, oldPen);
    DeleteObject(brush);
    DeleteObject(pen);

    // 3. AlphaBlend로 반투명하게 붙이기
    BLENDFUNCTION bf = {};
    bf.BlendOp = AC_SRC_OVER;
    bf.BlendFlags = 0;
    bf.SourceConstantAlpha = m_blinkRectAlpha; // 0~255 (투명도)
    bf.AlphaFormat = 0;                // 소스 알파는 무시, 상수 알파만 사용

    int x = (int)(center.x - w * 0.5f);
    int y = (int)(center.y - h * 0.5f);

    AlphaBlend(hdc,
        x, y, w, h,
        memDC,
        0, 0, w, h,
        bf);

    // 4. 정리
    SelectObject(memDC, oldBmp);
    DeleteObject(bmp);
    DeleteDC(memDC);
}
