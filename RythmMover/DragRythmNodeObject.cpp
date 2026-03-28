#include "pch.h"
#include "DragRythmNodeObject.h"
#include "ResourceManager.h"
#include "Texture.h"
#include "Tables.h"

DragRythmNodeObject::DragRythmNodeObject(Vec2 pos, const std::string& name) :
    m_slotIndex(-1)
{
    m_isDie = false;
    m_pos = pos;
    m_size = { 50.f, 50.f };
    m_name = name;
    m_node = nullptr;

    const RythmNodeData* data = FindRythmNodeData(name);

    m_dropTexture = (GET_SINGLE(ResourceManager)->GetTexture(data->DropTextureName));
    m_dragTexture = (GET_SINGLE(ResourceManager)->GetTexture(data->DragTextureName));
    m_rythmNodeTexture = (GET_SINGLE(ResourceManager)->GetTexture(data->RythmNodeTextureName));

    m_targetTexture = m_dropTexture;
}

DragRythmNodeObject::~DragRythmNodeObject()
{
    if(m_node)
        SAFE_DELETE(m_node);
}

void DragRythmNodeObject::Update()
{
    // 드래그 관련 로직은 DrageableObject 안에서 처리되니까 여기서는 딱히 할 거 없음.
    // 나중에 깜빡이게 한다든지, 텍스트 바꾸는 로직 추가 가능.
    DragableObject::Update(); // 혹시 Object::Update 안에 뭐가 생길 수도 있으니까 호출해둠
}

void DragRythmNodeObject::Render(HDC _hdc)
{
    Vec2 pos = GetRenderPos();
    Vec2 size = GetRenderSize();
    LONG width = m_targetTexture->GetWidth();
    LONG height = m_targetTexture->GetHeight();

    //// 3. StretchBlt - 확대, 축소, 반전
    ::StretchBlt(_hdc
        , static_cast<int>(pos.x - size.x / 2)
        , static_cast<int>(pos.y - size.y / 2)
        , size.x
        , size.y
        , m_targetTexture->GetTextureDC()
        , 0, 0, width, height, SRCCOPY);

    ComponentRender(_hdc);
}

void DragRythmNodeObject::OnSeted(Vec2 pos, int slotIndex)
{
    m_targetTexture = m_rythmNodeTexture;
    SetSize({ 100.f, 100.f });
    SetPos(pos);
    m_slotIndex = slotIndex;
    m_hasMomentum = false;
}

 void DragRythmNodeObject::OnDragStart(const Vec2&)
 {
     if (m_slotIndex != -1)
     {
         m_slotIndex = -1;
     }
     m_targetTexture = m_dragTexture;
     SetSize({ 100.f, 100.f });
 }
 
 void DragRythmNodeObject::OnDragMove(const Vec2&)
 {
     // 이동 중일 때 뭔가 하고 싶으면 여기서
 }
 
 void DragRythmNodeObject::OnDragEnd(const Vec2&, const bool&)
 {
     m_targetTexture = m_dropTexture;
     SetSize({ 50.f, 50.f });
 }