# CLAUDE.md

## 프로젝트 개요
포근 마을 — 작은 둥근 행성 위 마을에서 동물 주민들과 교류하고 마을을 꾸미는 힐링 생활 게임 (레퍼런스: 동물의숲, 두근두근타운, 쁘띠플래닛).
기획은 GDD.md, 작업 순서와 작업 일지는 TASKS.md, 수치는 BALANCE.md, 진행 현황 요약은 PROGRESS.md(작업 단위가 끝날 때마다 갱신).
수치는 BALANCE.md와 데이터 에셋(ScriptableObject) 기준이며 코드에 임의로 넣지 않는다.
방법론: `claude_all_type_genre.md`(장르 무관 협업 가이드)를 따른다.

## 작업 원칙
- 한 번에 하나의 기능 단위만. TASKS.md의 현재 항목만 구현한다.
- 요청받지 않은 시스템(세이브, 낚시 등)은 미리 만들지 않는다.
- 구현 전에 문서를 읽고, 구현 후 문서를 고친다. 문서와 코드가 다르면 실제 기준으로 고치고 알린다.
- 불확실한 설계 결정은 선택지 + 추천안으로 질문하고, 답은 날짜·이유와 함께 GDD/TASKS에 남긴다.
- 그래픽 작업은 재미 검증(Phase 4) 이전에 하지 않는다. 그레이박스는 "색만 다른 기본 도형"이다.
- 완료 = 테스트 전체 통과 + 콘솔 경고 0 + 확인 방법 안내 + 사용자 플레이 확인. 확인 전에는 "플레이 확인 대기".
- 에디터 수작업은 코드로 우회하지 않고 목록으로 안내한다. 반복 생성 작업은 여러 번 실행해도 결과가 같은 에디터 메뉴로 만든다.
- 실패·생략·미확인은 그대로 보고한다.

## 기술 스택 (변경 금지)
- 플랫폼: PC (Windows)
- 엔진: Unity 6000.6.0f1
- 렌더: URP (`Assets/Settings/PC_RPAsset`, Forward+)
- 입력: Input System (새 입력 시스템만, Active Input Handling = Input System Package)
- UI: uGUI + TextMeshPro (결정 2026-10-19)
- 3D 에셋: Varco3D로 생성 (컨셉 이미지 → 3D). 플로우의 노드 연결 구조는 TASKS "선행 작업" 참고

## 폴더 구조
저장소 루트에 문서, Unity 프로젝트는 `PogeunVillage/` 하위 폴더.
```
PogeunVillage/                      ← 저장소 루트 (CLAUDE.md, GDD.md, TASKS.md)
└─ PogeunVillage/                   ← Unity 프로젝트
   └─ Assets/PogeunVillage/         ← 프로젝트 에셋 (결정 2026-10-19: 현재 구조 유지)
      ├─ Scripts/{Core, 기능별…, UI, Data}   ← 런타임 코드 (namespace PogeunVillage.*)
      ├─ Scripts/Editor/            ← 에디터 메뉴·생성 도구
      ├─ Tests/EditMode/            ← 단위 테스트 (첫 기능부터)
      ├─ Data/                      ← 수치·콘텐츠 데이터 에셋
      ├─ Prefabs/  Scenes/  Shaders/  Materials/  Meshes/  Textures/
      ├─ Art/Raw/                   ← Varco3D 원본(GLB 등) 보관, 수정하지 않음
      └─ Audio/
```

## 코딩 규칙
- 네임스페이스 `PogeunVillage`(런타임), `PogeunVillage.EditorTools`(에디터). 클래스·메서드 PascalCase, 필드 camelCase.
- 게임 규칙(시간·인벤토리·호감도·경제 계산)은 엔진 의존 없는 순수 C#으로 작성하고, MonoBehaviour는 얇은 어댑터(입력·표시·이벤트 구독)로 둔다.
- 수치·콘텐츠는 ScriptableObject 데이터 에셋으로. enum은 끝에만 추가한다.
- 난수는 주입한다(용도별 분리). 생성 스크립트는 고정 시드를 쓴다.
- Find/GetComponent는 캐시, 프레임당 무거운 연산 금지. 시간 진행은 게임 시간 틱으로.
- 결합도는 C# event/Action으로 낮추고, "바뀐 것만" 알린다.

## 핵심 설계 규칙 (변경 금지)
1. **둥근 행성 연출은 화면에서만.** 위치·물리·충돌·길찾기는 평평한 월드 기준이고, `PogeunVillage/CurvedToon` 셰이더가 그릴 때만 구면으로 휜다. 따라서 화면에 그려지는 모든 메시(가져온 Varco3D 모델 포함)는 CurvedToon 셰이더(또는 같은 `CurvedWorld.hlsl` 휨을 쓰는 셰이더)를 써야 한다.
2. 휨 중심은 플레이어(`CurvedWorld.focus`), 반경은 `CurvedWorld.radius`(현재 110). 휨 때문에 화면 위쪽에서 내려오는 물체가 사라지지 않도록 `CurvedWorld`가 컬링 시야각을 넓힌다(`cullingFovScale`).
3. 시간은 게임 시간(실제 시계와 무관). 하루 길이는 BALANCE 수치. (결정 2026-10-19)
4. 물(강·바다)은 지면 높이 `waterLevel` 아래로 판정하며 플레이어는 들어가지 않는다.

## 아트 · 그래픽 규칙
- 스타일: 파스텔 톤 툰 셰이딩(파란 기운 그림자), 둥글고 통통한 형태, 경계가 또렷한 지면 색.
- 재질은 CurvedToon 하나를 공유하고 색·텍스처만 다르게(배칭 유지). 상태 표현은 재질 교체 대신 프로퍼티 블록.
- Varco3D 원본은 `Art/Raw/`에 그대로 보관하고 검수(메시 찢어짐·표면·컨셉 불일치)한 뒤 가져온다.
- 모델 가져오기 규격(단위·피벗·정면 축·크기 기준)은 MODELING_SPEC.md를 만든 뒤 따른다(첫 모델 가져오기 전에 작성).
- 지면·물·언덕은 코드 생성 메시를 유지한다(휨·충돌이 지면 메시에 맞춰져 있음).

## Git
- 한 항목 = 한 커밋, 메시지 `MM.DD/Phase-항목/PascalCase요약` (예: `10.19/0-2/ProjectDocs`). 수정은 `…Fix`.
- 커밋 전 `git status`로 의도하지 않은 변경(줄바꿈만 바뀐 설정 파일 등)을 되돌린다.
