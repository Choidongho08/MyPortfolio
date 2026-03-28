#include "pch.h"
#include "DragableObject.h"
#include "TimeManager.h" 

DragableObject::DragableObject()
    : m_isDragging(false)
    , m_hasMomentum(false)
    , m_dragOffset{ 0.f, 0.f }
    , m_velocity{ 0.f, 0.f }
{
}

DragableObject::~DragableObject() = default;

bool DragableObject::HitTest(const Vec2& mouse) const
{
    float left = m_pos.x - m_size.x / 2;
    float top = m_pos.y - m_size.y / 2;
    float right = m_pos.x + m_size.x / 2;
    float bottom = m_pos.y + m_size.y / 2;

    return (mouse.x >= left && mouse.x <= right &&
        mouse.y >= top && mouse.y <= bottom);
}

void DragableObject::BeginDrag(const Vec2& mouse)
{
    if (m_isDragging)
        return;

    m_isDragging = true;
    m_hasMomentum = false;
    m_velocity = Vec2(0.f, 0.f);

    // 마우스와 오브젝트 중심 사이 거리 저장
    m_dragOffset.x = mouse.x - m_pos.x;
    m_dragOffset.y = mouse.y - m_pos.y;

    OnDragStart(mouse);
}

void DragableObject::Drag(const Vec2& mouse, bool useExternalTarget)
{
    if (!m_isDragging)
        return;

    // 슬롯에 붙일 때는 외부에서 넣은 m_targetPos를 그대로 사용
    // 평소에는 마우스를 기준으로 m_targetPos를 갱신
    if (!useExternalTarget)
    {
        m_targetPos.x = mouse.x - m_dragOffset.x;
        m_targetPos.y = mouse.y - m_dragOffset.y;
    }

    const float FOLLOW_STIFFNESS = 15.0f;
    float t = 1.0f - expf(-FOLLOW_STIFFNESS * fDT);
    if (t > 1.0f) t = 1.0f;

    Vec2 prevPos = m_pos;

    // 항상 m_targetPos 를 향해서만 LERP
    m_pos.x = std::lerp(m_pos.x, m_targetPos.x, t);
    m_pos.y = std::lerp(m_pos.y, m_targetPos.y, t);

    m_velocity = (m_pos - prevPos) / fDT;

    OnDragMove(mouse);
}

void DragableObject::EndDrag(const Vec2& mouse, const bool& isSlot)
{
    if (!m_isDragging)
        return;

    m_isDragging = false;

    // 속도가 거의 0이 아니면 관성 모드 ON
    if (m_velocity.Length() > 1.f && !isSlot)   //  isSlot 이면 관성 없애고 싶으면 이렇게도 가능
        m_hasMomentum = true;
    else
    {
        m_hasMomentum = false;
        m_velocity = Vec2(0.f, 0.f);
    }

    OnDragEnd(mouse, isSlot);
}

void DragableObject::Update()
{
    // 마우스를 떼고, 아직 관성이 남아 있을 때
    if (!m_isDragging && m_hasMomentum)
    {
        if (fDT <= 0.f)
            return;

        // 위치 = 위치 + 속도 * dt
        m_pos += m_velocity * fDT;

        // 마찰/감속 (per second 계수)
        const float DAMPING = 15.f; // 값 키우면 더 빨리 멈춤, 줄이면 더 오래 미끄러짐
        m_velocity -= m_velocity * (DAMPING * fDT);

        if (m_velocity.Length() < 1.0f)
        {
            m_velocity = Vec2(0.f, 0.f);
            m_hasMomentum = false;
        }
    }
}
