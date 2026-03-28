#include "pch.h"
#include "DashNode.h"

#include "PlayableManager.h"
#include "Object.h"
#include "Player.h"
#include "Rigidbody.h"
#include "Animator.h"
#include "Animation.h"
#include "Texture.h"
#include "Core.h"

// ---------------------------------------------
//  헬퍼 함수: BMP + 마젠타 컬러키 -> 32bit ARGB 생성
// ---------------------------------------------

// bmp(마젠타 컬러키) -> 32bit ARGB DIB 로 변환해서 TrailTextureData 에 저장
static bool CreateARGBFromColorKey(
    HDC hRefDC,             // 기준 DC (렌더링 대상 DC)
    HDC hSrcDC,             // 시트 텍스쳐 DC
    int sx, int sy,         // 시트에서 잘라올 시작 좌표
    int sw, int sh,         // 잘라올 크기
    COLORREF colorKey,      // 예: RGB(255,0,255)
    TrailTextureData& t)    // 결과를 채워 넣을 Trail 데이터
{
    if (sw <= 0 || sh <= 0)
        return false;

    // 1) 먼저 소스 프레임을 임시 비트맵(hTmpBmp)에 복사
    HDC hTmpDC = ::CreateCompatibleDC(hRefDC);
    if (!hTmpDC)
        return false;

    HBITMAP hTmpBmp = ::CreateCompatibleBitmap(hRefDC, sw, sh);
    if (!hTmpBmp)
    {
        ::DeleteDC(hTmpDC);
        return false;
    }

    HBITMAP hOldTmp = (HBITMAP)::SelectObject(hTmpDC, hTmpBmp);

    // 시트 텍스쳐에서 현재 프레임만 복사
    ::BitBlt(
        hTmpDC,
        0, 0, sw, sh,
        hSrcDC,
        sx, sy,
        SRCCOPY
    );

    // 2) 32bit ARGB DIBSection 생성 (top-down)
    BITMAPINFO bmi{};
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = sw;
    bmi.bmiHeader.biHeight = -sh;          // 음수 => top-down
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;

    void* pBits = nullptr;
    HBITMAP hARGBBmp = ::CreateDIBSection(
        hRefDC,
        &bmi,
        DIB_RGB_COLORS,
        &pBits,
        nullptr,
        0
    );

    if (!hARGBBmp || !pBits)
    {
        ::SelectObject(hTmpDC, hOldTmp);
        ::DeleteObject(hTmpBmp);
        ::DeleteDC(hTmpDC);
        return false;
    }

    // 3) hTmpBmp 의 내용을 32bit 형식으로 pBits 에 복사
    int scanLines = ::GetDIBits(
        hTmpDC,
        hTmpBmp,
        0,
        sh,
        pBits,
        &bmi,
        DIB_RGB_COLORS
    );

    ::SelectObject(hTmpDC, hOldTmp);
    ::DeleteObject(hTmpBmp);
    ::DeleteDC(hTmpDC);

    if (scanLines == 0)
    {
        ::DeleteObject(hARGBBmp);
        return false;
    }

    // 4) 마젠타 컬러키 => alpha 0, 나머지 => alpha 255 로 세팅
    BYTE keyR = GetRValue(colorKey);
    BYTE keyG = GetGValue(colorKey);
    BYTE keyB = GetBValue(colorKey);

    DWORD* pixels = static_cast<DWORD*>(pBits);
    int total = sw * sh;

    for (int i = 0; i < total; ++i)
    {
        BYTE* px = reinterpret_cast<BYTE*>(&pixels[i]); // [B,G,R,A]
        BYTE B = px[0];
        BYTE G = px[1];
        BYTE R = px[2];

        if (R == keyR && G == keyG && B == keyB)
        {
            px[3] = 0;    // alpha 0 (완전 투명)
        }
        else
        {
            px[3] = 255;  // alpha 255 (불투명)
        }
    }

    // 5) 이 ARGB 비트맵을 사용할 DC 생성
    HDC hARGBDC = ::CreateCompatibleDC(hRefDC);
    if (!hARGBDC)
    {
        ::DeleteObject(hARGBBmp);
        return false;
    }

    HBITMAP hOld = (HBITMAP)::SelectObject(hARGBDC, hARGBBmp);

    // Trail 데이터에 세팅
    t.hARGBDC = hARGBDC;
    t.hARGBBmp = hARGBBmp;
    t.hOldBmp = hOld;
    t.ARGBWidth = sw;
    t.ARGBHeight = sh;
    t.bARGBReady = true;

    return true;
}

// TrailTextureData 안의 GDI 리소스 정리용
static void DestroyTrailBitmap(TrailTextureData& t)
{
    if (t.hARGBDC)
    {
        ::SelectObject(t.hARGBDC, t.hOldBmp);
        ::DeleteObject(t.hARGBBmp);
        ::DeleteDC(t.hARGBDC);

        t.hARGBDC = nullptr;
        t.hARGBBmp = nullptr;
        t.hOldBmp = nullptr;
        t.ARGBWidth = 0;
        t.ARGBHeight = 0;
        t.bARGBReady = false;
    }
}

// ---------------------------------------------
//               DashNode 구현
// ---------------------------------------------

DashNode::DashNode()
    : m_MaxTrailCount(4)
    , m_TrailSpawnInterval(0.05f)  // 0.05초마다 하나씩
    , m_TrailLifeTime(0.75f)       // 0.75초 동안 유지
    , m_TrailActive(false)
    , m_SpawnedCount(0)
    , m_SpawnTimer(m_TrailSpawnInterval)
{
}

DashNode::~DashNode()
{
    for (TrailTextureData& t : m_Trails)
        DestroyTrailBitmap(t);
}

void DashNode::Init()
{
    // 필요하면 여기서 값 조절 가능
}

Player* DashNode::GetPlayer() const
{
    Object* owner = GET_SINGLE(PlayableManager)->GetCurPlayer();
    if (!owner)
        return nullptr;

    return dynamic_cast<Player*>(owner);
}

void DashNode::Excute()
{
    Player* player = GetPlayer();
    if (!player)
    {
        std::cout << "***\nPlayableManager의 currentPlayable 변수가 nullptr입니다.\n***\n";
        return;
    }

    // 1) 대시 물리 처리
    Rigidbody* rigid = player->GetComponent<Rigidbody>();
    if (!rigid)
    {
        std::cout << "Dash 실패 : Rigidbody 없음\n";
        return;
    }

    Vec2 force = player->GetFaceDirection() > 0.f ? Vec2(720.f, -10.f) : Vec2(-720.f, -10.f);
    Vec2 result = force + rigid->GetVelocity();
    std::cout << "Result x : " << result.x << ", y : " << result.y << '\n';
    rigid->AddForce(result, ForceMode::Impulse);
    player->SetIsDash(true);

    // 2) 꼬리 관련 상태 초기화
    for (TrailTextureData& t : m_Trails)
        DestroyTrailBitmap(t);
    m_Trails.clear();

    m_TrailActive = true;
    m_SpawnedCount = 0;
    m_SpawnTimer = m_TrailSpawnInterval;
}

// Trail 하나 생성
void DashNode::SpawnTrail()
{
    Player* player = GetPlayer();
    if (!player)
    {
        m_TrailActive = false;
        return;
    }

    Animator* animComp = player->GetComponent<Animator>();
    if (!animComp)
        return;

    Animation* anim = animComp->GetCurrent();
    if (!anim)
        return;

    Texture* sheetTex = anim->GetCurFrame();         // 시트 텍스쳐
    const tAnimFrame* fr = anim->GetCurFrameInfo();  // 현재 프레임 정보 (vLT, vSlice)
    if (!sheetTex || !fr)
        return;

    TrailTextureData t;
    t.Texture = sheetTex;
    t.Pos = player->GetPos();
    t.Size = player->GetSize();
    t.CurLifeTime = 0.f;
    t.SrcLT = fr->vLT;
    t.SrcSlice = fr->vSlice;
    t.bARGBReady = false;  // 아직 ARGB 비트맵은 안 만들었음

    m_Trails.push_back(t);
    ++m_SpawnedCount;
}

void DashNode::Update()
{
    if (!m_TrailActive)
        return;

    // 1) Trail 생성 타이머 갱신
    m_SpawnTimer += fDT;

    // 일정 간격마다 Trail 생성 (최대 m_MaxTrailCount 개)
    while (m_SpawnedCount < m_MaxTrailCount && m_SpawnTimer >= m_TrailSpawnInterval)
    {
        m_SpawnTimer -= m_TrailSpawnInterval;
        SpawnTrail();
    }

    // 2) 이미 생성된 Trail들의 수명 증가 + 제거
    for (size_t i = 0; i < m_Trails.size(); )
    {
        TrailTextureData& t = m_Trails[i];
        t.CurLifeTime += fDT;

        if (t.CurLifeTime >= m_TrailLifeTime)
        {
            // 수명 끝난 꼬리 제거 (GDI 리소스도 같이 정리)
            DestroyTrailBitmap(t);
            m_Trails.erase(m_Trails.begin() + i);
            continue; // i 증가 X
        }

        ++i;
    }

    if (m_SpawnedCount >= m_MaxTrailCount && GetPlayer()->GetIsDash())
    {
        GetPlayer()->SetIsDash(false);
    }

    // 모든 Trail 생성 끝 + 다 사라졌으면 비활성화
    if (m_SpawnedCount >= m_MaxTrailCount && m_Trails.empty())
    {
        m_TrailActive = false;
    }
}

void DashNode::Render(HDC _dc)
{
    if (!m_TrailActive || m_Trails.empty())
        return;

    for (TrailTextureData& t : m_Trails)
    {
        float lifeRatio = t.CurLifeTime / m_TrailLifeTime; // 0 ~ 1
        if (lifeRatio >= 1.f)
            continue;

        BYTE alpha = static_cast<BYTE>(255.f * (1.f - lifeRatio)); // 점점 0으로

        int sx = static_cast<int>(t.SrcLT.x);
        int sy = static_cast<int>(t.SrcLT.y);
        int sw = static_cast<int>(t.SrcSlice.x);
        int sh = static_cast<int>(t.SrcSlice.y);

        int dx = static_cast<int>(t.Pos.x - t.Size.x / 2);
        int dy = static_cast<int>(t.Pos.y - t.Size.y / 2);
        int dw = static_cast<int>(t.Size.x);
        int dh = static_cast<int>(t.Size.y);

        if (!t.Texture)
            continue;

        // 아직 이 Trail에 대한 ARGB 비트맵이 없다면, 지금 만든다
        if (!t.bARGBReady)
        {
            if (!CreateARGBFromColorKey(
                _dc,
                t.Texture->GetTextureDC(),
                sx, sy, sw, sh,
                RGB(255, 0, 255),   // 컬러키
                t))
            {
                continue;
            }
        }

        BLENDFUNCTION bf{};
        bf.BlendOp = AC_SRC_OVER;
        bf.BlendFlags = 0;
        bf.SourceConstantAlpha = alpha;        // 전체 투명도 (꼬리 페이드)
        bf.AlphaFormat = AC_SRC_ALPHA; // ARGB 알파 사용

        ::AlphaBlend(
            _dc,
            dx, dy, dw, dh,
            t.hARGBDC,
            0, 0, t.ARGBWidth, t.ARGBHeight,
            bf
        );
    }
}
