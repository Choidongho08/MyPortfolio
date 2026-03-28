	#include "pch.h"
	#include "Rigidbody.h"
	#include "Object.h"

	Rigidbody::Rigidbody()
		: m_mass(1.f)                // 질량 (1~5)
		, m_friction(2.5f)           // 지면 마찰
		, m_force(0.f, 0.f)          // 누적 힘
		, m_airDrag(0.1f)            // 공중 감속
		, m_useGravity(true)         // 중력 사용 여부
		, m_isKinematic(false)       // 키네마틱 여부
		, m_velocity(0.f, 0.f)       // 현재 속도
		, m_maxVelocity(100000.f, 1000.f) // 속도 제한 (x, y)
		, m_gravity(0.f, 600.f)      // 중력 가속도
		, m_accelation(0.f, 0.f)     // 가속도
		, m_grounded(false)          // 접지 여부
		, m_useMass(true)
	{
	}

	Rigidbody::~Rigidbody()
	{
	}

	void Rigidbody::Init()
	{
	}

	void Rigidbody::Update()
	{
	}
	void Rigidbody::FixedUpdate(float _fixedDT)
	{
		if (m_isKinematic)
		{
			m_force = { 0.f,0.f };
			return;
		}
		// 힘 -> 가속도
		m_accelation = m_force / m_mass;
		m_velocity += m_accelation * _fixedDT;

		if (m_jumpGraceTime > 0.f)
		{
			m_jumpGraceTime -= _fixedDT;
		}
		else if (m_useGravity)
		{
			if (!m_grounded)
				m_velocity += m_gravity * _fixedDT;
			else
				m_velocity.y = 0.f;
		}

		// 감속 처리
		if (m_velocity.Length() > 0.f && m_useMass)
		{
			if (m_grounded)
			{
				// 바닥 마찰
				float frictionAmount = m_friction * _fixedDT;
				m_velocity.x = std::lerp(m_velocity.x, 0.f, frictionAmount);
			}
			else
			{
				// 공중 감속
				float dragAmount = m_airDrag * _fixedDT;
				m_velocity.x = std::lerp(m_velocity.x, 0.f, dragAmount);
			}
		}

		// 속도 제한(축 기준 단순화)
		m_velocity.x = std::clamp(m_velocity.x, -m_maxVelocity.x, m_maxVelocity.x);
		m_velocity.y = std::clamp(m_velocity.y, -m_maxVelocity.y, m_maxVelocity.y);


		Object* owner = GetOwner();
		Vec2 pos = owner->GetPos();
		// 거속시
		pos += m_velocity * _fixedDT;

		Vec2 size = owner->GetSize();
		float halfW = size.x * 0.5f;
		float halfH = size.y * 0.5f;

		// 화면 기준: (0,0) ~ (WINDOW_WIDTH, WINDOW_HEIGHT)
		pos.x = std::clamp(pos.x, halfW, WINDOW_WIDTH - halfW);
		// pos.y = std::clamp(pos.y, halfH, WINDOW_HEIGHT - halfH);

		owner->SetPos(pos);

		// 힘은 한번 처리하고 초기화, 1프레임 단위로 유효
		m_force = Vec2(0.f, 0.f);
	}

	void Rigidbody::Render(HDC hDC)
	{
	}
