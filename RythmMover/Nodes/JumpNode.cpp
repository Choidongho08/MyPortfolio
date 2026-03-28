#include "pch.h"
#include "JumpNode.h"
#include "Entity.h"
#include "Rigidbody.h"
#include "Animator.h"
#include "PlayableManager.h"

JumpNode::JumpNode()
{
}

JumpNode::~JumpNode()
{
}

void JumpNode::Init()
{
}

void JumpNode::Excute()
{
	Entity* owner = GET_SINGLE(PlayableManager)->GetCurPlayer();
	Rigidbody* rigid;
	Animator* animator;

	if (owner)
	{
		rigid = owner->GetComponent<Rigidbody>();
		animator = owner->GetComponent<Animator>();
	}
	else
	{
		cout << "***\nPlayableManager의 currrentPlayable변수가 nullptr입니다.\n***\n";
		return;
	}
	if (rigid)
	{
		rigid->SetGrounded(false); // 점프 순간에는 땅에 안 닿았다고 처리
		rigid->SetVelocity({ rigid->GetVelocity().x, 0.f });
		rigid->AddForce(Vec2(0.f, -400.f), ForceMode::Impulse);
		animator->Play(L"PlayerJump");
	}
	else
	{
		cout << "Jump실패\n";
		return;
	}
}

void JumpNode::Update()
{
}

void JumpNode::Render(HDC hdc)
{
}
