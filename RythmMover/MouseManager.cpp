#include "pch.h"
#include "MouseManager.h"
#include "DragRythmNodeObject.h"
#include "InputManager.h"

void MouseManager::Init()
{
    isDetecting = false;
    m_vecObj = nullptr;
    m_rythmNode = nullptr;
    m_dragTarget = nullptr;
}

void MouseManager::Update()
{
    // 아직 세팅 안 됐으면 리턴
    if (!isDetecting || m_vecObj == nullptr || m_rythmNode == nullptr)
        return;

    const POINT& pt = GET_MOUSEPOS;
    m_curMousePos = { static_cast<float>(pt.x), static_cast<float>(pt.y) };

    // 드래그 시작
    if (GET_KEYDOWN(KEY_TYPE::LBUTTON))
    {
        m_dragTarget = nullptr;
        bool found = false;

        for (int i = (int)Layer::END - 1; i >= 0 && !found; --i)
        {
            // m_vecObj 는 포인터지만, m_vecObj[i] 로 바로 접근 가능
            auto& vec = m_vecObj[i];
            for (auto it = vec.rbegin(); it != vec.rend(); ++it)
            {
                Object* obj = *it;
                if (!obj)
                    continue;

                auto* dragable = dynamic_cast<DragRythmNodeObject*>(obj);
                if (!dragable)
                    continue;

                if (dragable->HitTest(m_curMousePos))
                {
                    if (dragable->GetSlotIndex() != -1)
                    {
                        m_rythmNode->RemoveRythmNode(dragable->GetSlotIndex());
                    }
                    m_dragTarget = dragable;
                    m_dragTarget->BeginDrag(m_curMousePos);
                    found = true;
                    break;
                }
            }
        }
    }

    static Vec2 centerPos;

    // 드래그 중
    if (m_dragTarget)
    {
        if (GET_KEY(KEY_TYPE::LBUTTON))
        {
            const POINT& pt = GET_MOUSEPOS;
            m_curMousePos = { static_cast<float>(pt.x), static_cast<float>(pt.y) };

            int index;
            const bool& isNearSlot = m_rythmNode->FindNearestSlot(m_curMousePos, index, centerPos);
            if (isNearSlot)
            {
                m_dragTarget->SetTargetPos(centerPos);
                m_dragTarget->Drag(m_curMousePos, true);
            }
            else
            {
                m_dragTarget->Drag(m_curMousePos);
            }
        }

        // 드래그 끝
        if (GET_KEYUP(KEY_TYPE::LBUTTON))
        {
            int index;
            const bool& isNearSlot = m_rythmNode->FindNearestSlot(m_dragTarget->GetPos(), index, centerPos);
            m_dragTarget->EndDrag(m_curMousePos, isNearSlot);
            if (isNearSlot)
            {
                if (m_rythmNode->SetRythmNode(m_dragTarget->GetNode(), index))
                {
                    m_dragTarget->OnSeted(centerPos, index);
                }
            }
            m_dragTarget = nullptr;
        }
    }
}

void MouseManager::StartSearchRythmNode(vector<Object*> vecObj[(UINT)Layer::END], RythmNode* rythmNode)
{
    // vecObj 는 매개변수에서 자동으로 "vector<Object*>*"(첫 원소 포인터) 로 decay 됨
    m_vecObj = vecObj;
    m_rythmNode = rythmNode;
    isDetecting = (m_vecObj != nullptr && m_rythmNode != nullptr);
}

void MouseManager::Release()
{
    isDetecting = false;

    // 외부에서 관리하는 배열을 비우고 싶으면 그대로 사용
    if (m_vecObj)
    {
        for (UINT i = 0; i < (UINT)Layer::END; ++i)
        {
            m_vecObj[i].clear();
        }
    }

    m_rythmNode = nullptr;
    m_dragTarget = nullptr;
    m_vecObj = nullptr;
}