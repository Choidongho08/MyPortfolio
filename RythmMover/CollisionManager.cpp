#include "pch.h"
#include "CollisionManager.h"
#include "SceneManager.h"
#include "Scene.h"
#include "Object.h"
#include "Collider.h"
#include "Rigidbody.h"

void CollisionManager::Release()
{
	
}

void CollisionManager::Update()
{
	for (UINT Row = 0; Row < (UINT)Layer::END; ++Row)
	{
		for (UINT Col = Row; Col < (UINT)Layer::END; ++Col)
		{
			if (m_objectLayer[Row] & (1 << Col))
			{
				//int a = 0;
				CollisionLayerUpdate((Layer)Row, (Layer)Col);
			}
		}
	}
}

void CollisionManager::CheckLayer(Layer _left, Layer _right)
{
	// 작은쪽을 행으로 씁시다.
	// 작은 값을 LAYER의 행으로, 큰 값을 열
	UINT Row = (UINT)_left;
	UINT Col = (UINT)_right;
	//Row = min(Row, Col);
	if (Row > Col)
		std::swap(Row, Col);
	//m_arrLayer[Row];
	//Col;

	//// 비트 연산
	// 체크가 되어있다면
	if (m_objectLayer[Row] & (1 << Col))
	{
		// 체크 풀기
		m_objectLayer[Row] &= ~(1 << Col);
	}
	// 체크가 안되어있다면r
	else
	{
		m_objectLayer[Row] |= (1 << Col);
	}
	int a = 0;
}

void CollisionManager::PhysicsResolve(Collider* _left, Collider* _right)
{
	if (_left->IsTrigger() || _right->IsTrigger())
		return;

	// 일정 수치를 넘으면 물리처리를 할 수 있게 상수값 정해주기. (안하면 지글발생)
	float const penetrationThreshold = 0.0001f;

	// 콜라이더를 가져왔으니 이제 얘네가 얼마만큼 깊이 침범했는지를 알아야겠지?
	// 충돌 깊이를 구해 일단.
	Vec2 leftPos = _left->GetUpdatedPos();
	Vec2 rightPos = _right->GetUpdatedPos();

	Vec2 leftSize = _left->GetSize();
	Vec2 rightSize = _right->GetSize();

	float overlapX = (leftSize.x / 2 + rightSize.x / 2) - std::abs(leftPos.x - rightPos.x);
	float overlapY = (leftSize.y / 2 + rightSize.y / 2) - std::abs(leftPos.y - rightPos.y);
	float percentX = overlapX * 0.8f;
	float percentY = overlapY * 0.8f;

	Rigidbody* leftRb = _left->GetOwner()->GetComponent<Rigidbody>();
	Rigidbody* rightRb = _right->GetOwner()->GetComponent<Rigidbody>();

	bool leftKinematic = leftRb == nullptr || leftRb->IsKinematic();
	bool rightKinematic = rightRb == nullptr || rightRb->IsKinematic();

	Vec2 leftVel = leftRb ? leftRb->GetVelocity() : Vec2{ 0, 0 };
	Vec2 rightVel = rightRb ? rightRb->GetVelocity() : Vec2{ 0, 0 };

	bool isHorizenCol = false;
	bool isVerticalCol = false;

	// 침범을 하지 않았으므로 무시
	if (overlapX <= 0 || overlapY <= 0)
		return;

	// 지정해둔 침범 임계치보다 작으면 무시
	if (overlapX < penetrationThreshold && overlapY < penetrationThreshold)
		return;

	// 둘 다 정적 오브젝트면 패스
	if (leftKinematic && rightKinematic)
		return;

	if (overlapX < overlapY)
		isHorizenCol = true;
	else
		isVerticalCol = true;

	//cout << overlapX << ' ' << overlapY << endl;

	// Y축으로 더 깊이 침범하면 X축 방향으로 밀기
	if (isHorizenCol)
	{
		// 왼쪽 물체만 정적일 때
		if ((leftKinematic || (leftVel.x == 0 && leftVel.y == 0)) && !rightKinematic)
		{
			// right를 기준으로 왼쪽이면 -로 오른쪽이면 +로 밀기
			int dir = (rightPos.x > leftPos.x) ? -1 : 1;
			Object* rightObj = _right->GetOwner();

			rightObj->SetPos({ rightPos.x - (percentX * dir), rightPos.y });
		}
		// 아니고 오른쪽 물체만 정적일 때
		else if (!leftKinematic && (rightKinematic || (rightVel.x == 0 && rightVel.y == 0)))
		{
			//left를 기준으로 왼쪽이면 -로 오른쪽이면 +로 밀기
			int dir = (leftPos.x > rightPos.x) ? -1 : 1;
			Object* leftObj = _left->GetOwner();

			leftObj->SetPos({ leftPos.x - (percentX * dir), leftPos.y });
		}
		// 둘 다 정적이 아닐 때
		else
		{
			//left를 기준으로 작용 반작용
			int dir = (leftPos.x > rightPos.x) ? -1 : 1;
			Object* leftObj = _left->GetOwner();
			Object* rightObj = _right->GetOwner();

			leftObj->SetPos({ leftPos.x - (percentX / 2.f * dir), leftPos.y });
			rightObj->SetPos({ rightPos.x + (percentX / 2.f * dir), rightPos.y });
		}
	}
	else if (isVerticalCol) // X축으로 더 깊이 침범하면 Y축 방향으로 밀기
	{
		// x와 다르게 y는 dir만으로는 못함.
		// 때문에 이전에 Rigidbody의 속도를 이용해 판단.
		float leftVy = leftVel.y;
		float rightVy = rightVel.y;

		// Left가 위의 있는지
		bool isLeft = (leftPos.y < rightPos.y);
		int dir = isLeft ? 1 : -1;

		// 왼쪽 물체만 정적일 때
		if (leftKinematic && !rightKinematic)
		{
			// 오른쪽 물체가 아래로 떨어지고 있을 때
			Object* rightObj = _right->GetOwner();

			if ((rightVy > 0 && !isLeft) || (rightRb->IsGrounded() && overlapY > 0.3f))
			{
				// 땅 충돌 처리
				rightObj->SetPos({ rightPos.x, rightPos.y + (percentY * dir) });

				rightRb->SetGrounded(true);
				rightRb->SetVelocity({ rightRb->GetVelocity().x, 0.f });
			}
			// 오른쪽 물체가 천장에 머리 박았을 때
			else if (rightVy < 0 && isLeft)
			{
				// 천장 처리
				rightObj->SetPos({ rightPos.x, rightPos.y + (percentY * dir) });

				rightRb->SetGrounded(false);
				rightRb->SetVelocity({ rightRb->GetVelocity().x, 0.f });
			}
		}
		// 아니고 오른쪽 물체만 정적일 때
		else if (!leftKinematic && rightKinematic)
		{
			// 왼쪽 물체가 아래로 떨어지고 있을 때
			Object* leftObj = _left->GetOwner();

			if (leftVy > 0 && isLeft || (leftRb->IsGrounded() && overlapY > 0.3f))
			{
				// 땅 충돌 처리
				leftObj->SetPos({ leftPos.x, leftPos.y + (percentY * dir) });

				leftRb->SetGrounded(true);
				leftRb->SetVelocity({ leftRb->GetVelocity().x, 0.f });
			}
			// 왼쪽 물체가 천장에 머리 박았을 때
			else if (leftVy < 0 && !isLeft)
			{
				// 천장 처리
				leftObj->SetPos({ leftPos.x, leftPos.y + (percentY * dir) });

				leftRb->SetGrounded(false);
				leftRb->SetVelocity({ leftRb->GetVelocity().x, 0.f });
			}
		}
		// 둘 다 정적이 아닐 때
		else
		{
			Object* leftObj = _left->GetOwner();
			Object* rightObj = _right->GetOwner();

			// 두 물체가 올라탈 때
			if (leftVy > 0 && isLeft)
			{
				// 왼쪽 물체가 오른쪽 물체 위에 올라탐
				leftObj->SetPos({ leftPos.x, (leftPos.y - percentY / 2.f) });
				rightObj->SetPos({ rightPos.x, (rightPos.y + percentY / 2.f) });

				leftRb->SetGrounded(true);
				leftRb->SetVelocity({ leftRb->GetVelocity().x, 0.f });
			}
			else if (rightVy > 0 && !isLeft)
			{
				// 오른쪽 물체가 왼쪽 물체 위에 올라탐
				leftObj->SetPos({ leftPos.x, (leftPos.y + percentY / 2.f) });
				rightObj->SetPos({ rightPos.x, (rightPos.y - percentY / 2.f) });

				rightRb->SetGrounded(true);
				rightRb->SetVelocity({ rightRb->GetVelocity().x, 0.f });
			}
		}
	}
}

void CollisionManager::CheckReset()
{
	// 메모리 초기화
	memset(m_objectLayer, 0, sizeof(UINT) * (UINT)Layer::END);
}

void CollisionManager::CollisionLayerUpdate(Layer _left, Layer _right)
{
	std::shared_ptr<Scene> pCurrentScene = GET_SINGLE(SceneManager)->GetCurScene();
	const vector<Object*>& vecLeftLayer = pCurrentScene->GetLayerObjects(_left);
	const vector<Object*>& vecRightLayer = pCurrentScene->GetLayerObjects(_right);
	std::unordered_map<ULONGLONG, bool>::iterator iter;
	for (size_t i = 0; i < vecLeftLayer.size(); ++i)
	{
		Collider* pLeftCollider = vecLeftLayer[i]->GetComponent<Collider>();
		// 충돌체 없는 경우
		if (nullptr == pLeftCollider)
			continue;
		for (size_t j = 0; j < vecRightLayer.size(); j++)
		{
			Collider* pRightCollider = vecRightLayer[j]->GetComponent<Collider>();
			// 충돌체가 없거나, 자기자신과의 충돌인 경우
			if (nullptr == pRightCollider || vecLeftLayer[i] == vecRightLayer[j])
				continue;

			// 두 충돌체로만 만들 수 있는 ID
			ULONGLONG colliderID = MakePairKey(pLeftCollider->GetID(), pRightCollider->GetID());

			iter = m_mapCollisionInfo.find(colliderID);
			// 이전 프레임 충돌한 적 없다.
			if (iter == m_mapCollisionInfo.end())
			{
				// 충돌 정보가 미등록된 상태인 경우 등록(충돌하지 않았다로)
				m_mapCollisionInfo.insert({ colliderID, false });
				//m_mapCollisionInfo[colliderID.ID] = false;
				iter = m_mapCollisionInfo.find(colliderID);
			}

			// 충돌여부 확인
			if (IsCollision(pLeftCollider, pRightCollider))
			{
				PhysicsResolve(pLeftCollider, pRightCollider);
				// 이전에도 충돌중
				if (iter->second)
				{
					if (vecLeftLayer[i]->GetIsDead() || vecRightLayer[j]->GetIsDead())
					{
						pLeftCollider->ExitCollision(pRightCollider);
						pRightCollider->ExitCollision(pLeftCollider);
						iter->second = false;
					}
					else
					{
						pLeftCollider->StayCollision(pRightCollider);
						pRightCollider->StayCollision(pLeftCollider);
					}
				}
				else // 이전에 충돌 x
				{
					if (!vecLeftLayer[i]->GetIsDead() && !vecRightLayer[j]->GetIsDead())
					{
						pLeftCollider->EnterCollision(pRightCollider);
						pRightCollider->EnterCollision(pLeftCollider);
						iter->second = true;
					}
				}
			}
			else // 충돌 안하네?
			{
				if (iter->second) // 근데 이전에 충돌중
				{
					pLeftCollider->ExitCollision(pRightCollider);
					pRightCollider->ExitCollision(pLeftCollider);
					iter->second = false;
				}
			}
		}
	}

}

bool CollisionManager::IsCollision(Collider* _left, Collider* _right)
{
	Vec2 leftPos = _left->GetUpdatedPos();
	Vec2 rightPos = _right->GetUpdatedPos();
	Vec2 leftSize = _left->GetSize();
	Vec2 rightSize = _right->GetSize();

	return (fabsf(rightPos.x - leftPos.x) < (leftSize.x + rightSize.x) / 2.f
		&& fabsf(rightPos.y - leftPos.y) < (leftSize.y + rightSize.y) / 2.f);
}

ULONGLONG CollisionManager::MakePairKey(UINT a, UINT b)
{
	// 좌우 순서를 무의미하게: 작은 값을 lo, 큰 값을 hi
	COLLIDER_ID id = {};
	id.lo_ID = (a < b) ? a : b;
	id.hi_ID = (a < b) ? b : a;
	return id.ID;
}
