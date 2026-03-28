#include "pch.h"
#include "BulletNode.h"
#include "Projectile.h"
#include "Entity.h"
#include "PlayableManager.h"
#include "InputManager.h"
#include "SceneManager.h"
#include "CameraManager.h"
#include "PlayerProjectile.h"

BulletNode::BulletNode()
{
}

BulletNode::~BulletNode()
{
}

void BulletNode::Init()
{
}

void BulletNode::Excute()
{
	targetPlayable = GET_SINGLE(PlayableManager)->GetCurPlayer();

	Projectile* proj = new PlayerProjectile(2.5f);
	Vec2 pos = targetPlayable->GetPos();
	pos.y -= targetPlayable->GetSize().y / 2.f;
	proj->SetPos(pos);
	proj->SetSize({ 30.f,30.f });

	POINT pt = GET_MOUSEPOS;

	Vec2 mousePos{ static_cast<float>(pt.x),  static_cast<float>(pt.y) };

	Vec2 dir;
	dir.x = mousePos.x - pos.x;
	dir.y = mousePos.y - pos.y;

	float speed = 15.f; // ¿ø·¡ ¾²´ø -15.f Å©±â¶û ¸ÂÃçÁÜ
	dir.x *= speed;
	dir.y *= speed;

	GET_SINGLE(CameraManager)->StartShake(0.7f, 0.3f);

	proj->SetDir(dir);
	GET_SINGLE(SceneManager)->GetCurScene()->AddObject(proj, Layer::PROJECTILE);
}

void BulletNode::Update()
{
}

void BulletNode::Render(HDC hdc)
{
}
