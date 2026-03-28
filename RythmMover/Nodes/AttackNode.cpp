#include "pch.h"
#include "AttackNode.h"
#include "PlayableManager.h"
#include "Entity.h"
#include "Collider.h"
#include "AttackNodeObj.h"
#include "SceneManager.h"
#include "CollisionManager.h"
#include "CameraManager.h"
AttackNode::AttackNode() :
	m_attackObj(new AttackNodeObj),
	m_finishedAttack(false),
	m_startAttack(false),
	m_targetPos({50.f, -5.f}),
	m_timer(0.f),
	m_attackTime(0.5f)
{
	GET_SINGLE(CollisionManager)->CheckLayer(Layer::ATTACK, Layer::BOX);
	GET_SINGLE(SceneManager)->GetCurScene()->AddObject(m_attackObj, Layer::ATTACK);
}

AttackNode::~AttackNode()
{
}

void AttackNode::Init()
{
	m_finishedAttack = false;
	m_timer = 0.f;
}

void AttackNode::Excute()
{
	Entity* owner = GET_SINGLE(PlayableManager)->GetCurPlayer();
	m_attackObj->Init();
	Collider* col = m_attackObj->GetComponent<Collider>();

	m_attackObj->SetSize({ 50.f, 50.f });
	m_attackObj->SetEntity(owner);
	col->SetSize({ 50.f, 50.f });
	const Vec2& pos = owner->GetPos();

	//GET_SINGLE(CameraManager)->StartShake(20.f,0.3f);

	m_startAttack = true;
	m_finishedAttack = false;
	m_timer = 0.f;
	m_attackObj->SetVisul(true);
}

void AttackNode::Update()
{
	if (!m_startAttack)
		return;

	Entity* owner = GET_SINGLE(PlayableManager)->GetCurPlayer();
	m_timer += fDT;
	if (m_finishedAttack)
	{
		m_attackObj->RequestRemoveCompo<Collider>();
		m_attackObj->SetVisul(false);
	}
	else
	{
		const Vec2& pos = owner->GetPos();
		const float ratio = m_timer / m_attackTime;
		if (ratio >= 1.f)
		{
			m_finishedAttack = true;
			return;
		}
		const Vec2 result = { pos.x + (owner->GetFaceDirection() * (m_targetPos.x * (ratio))) , pos.y + (m_targetPos.y * (ratio)) };

		cout << "x : " << result.x << ", y : " << result.y << '\n';
		m_attackObj->SetPos({ result.x, result.y});
	}
	m_attackObj->Update();
}

void AttackNode::Render(HDC hdc)
{
	m_attackObj->Render(hdc);
}