#include "pch.h"
#include "Collider.h"
#include "Object.h"
#include "ResourceManager.h"

UINT Collider::m_nextID = 0;

Collider::Collider()
	: m_size{30.f,30.f}
	, m_updatedPos{0.f,0.f}
	, m_offsetPos{0.f,0.f}
	, m_ID(m_nextID++)
	, m_showDebug(true)
{

}
void Collider::Update()
{
	// 오브젝트 따라가게
	Object* owner = GetOwner();
	Vec2 pos = owner->GetPos();
	m_updatedPos = pos + m_offsetPos;
}

void Collider::Render(HDC _hDC)
{ 
	//::TextOut(_hDC, 0, 0, L"안녕", 2);
	//GDISelector font(_hDC, FontType::TITLE);
	//::TextOut(_hDC, 10, 10, L"하세요", 3);

	//// 기즈모?
	//GET_SINGLE(ResourceManager)->GetPen();

	// RAII
	// PenType colorPen = PenType::GREEN;
	// if (m_showDebug)
	// 	colorPen = PenType::RED;
	// GDISelector pen(_hDC, colorPen);
	// GDISelector brush(_hDC, BrushType::HOLLOW);
	// RECT_RENDER(_hDC, m_updatedPos.x, m_updatedPos.y,
	// 	m_size.x, m_size.y);
}
void Collider::EnterCollision(Collider* _other)
{
	m_showDebug = true;
	GetOwner()->EnterCollision(_other);
}
void Collider::StayCollision(Collider* _other)
{
	GetOwner()->StayCollision(_other);

}
void Collider::ExitCollision(Collider* _other)
{
	m_showDebug = false;
	GetOwner()->ExitCollision(_other);

}
Collider::~Collider()
{

}

void Collider::Init()
{

}


