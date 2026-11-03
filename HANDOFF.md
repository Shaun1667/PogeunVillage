# HANDOFF.md — 작업 환경 옮기기 · 이어서 작업하기

> 처음 작성: 2026-10-27. **전면 정리: 2026-10-30**(작업 환경 이전 — 사용자 요청).
> 이 저장소만 받으면 작업을 이어갈 수 있도록, 저장소 밖에 있던 정보(외부 도구 · Varco3D 플로우 · Mixamo · Notion · 작업 방식 · Claude 설정 · 요령)를 한곳에 모은 문서.
> 다른 문서: 규칙 [CLAUDE.md](CLAUDE.md) · 기획 [GDD.md](GDD.md) · 작업 순서 · 일지 [TASKS.md](TASKS.md) · 수치 [BALANCE.md](BALANCE.md) · 진행 현황 [PROGRESS.md](PROGRESS.md) · 모델 규격 [MODELING_SPEC.md](MODELING_SPEC.md) · 재미 검증 점검표 [PLAYTEST.md](PLAYTEST.md) · 방법론 [claude_all_type_genre.md](claude_all_type_genre.md)

---

## 0. 옮기기 전에 (기존 PC에서)
1. **푸시하기** — 2026-10-30 기준 `main`이 `origin/main`보다 **커밋 13개 앞서 있음**(6-1d 이후 P-3 · P-4 · P-5 전부). Claude는 푸시하지 않으므로 사용자가 직접:
   ```
   git push origin main
   ```
   - FBX 원본(68개)은 Git LFS라 푸시 때 LFS 업로드가 같이 됨(수백 MB, GitHub LFS 무료 용량 1GB 확인).
2. `git status`가 깨끗한지 확인(아래 7-3 "되돌릴 잡음" 참고).
3. Claude 메모리(저장소 밖, 4번)는 새 PC에서 다시 만들어야 함 — 내용은 4번에 적어 둠.

## 1. 새 PC 세팅 순서
1. **Git + Git LFS**
   ```
   git lfs install
   git clone https://github.com/Shaun1667/PogeunVillage.git
   ```
   - 커밋 작성자: `git config user.name "Shaun1667"`, `git config user.email "namulletta@gmail.com"`
   - LFS 없이 받으면 `Art/Raw/**/*.fbx`가 작은 텍스트 포인터라 모델이 안 보임 → `git lfs pull`
2. **Unity Hub**: Unity **6000.6.0f1** 설치 → 저장소 안의 **`PogeunVillage/` 하위 폴더**를 프로젝트로 열기(저장소 루트가 아님). 첫 실행은 Library 생성으로 오래 걸림(정상).
3. **씬 열기**: `Assets/PogeunVillage/Scenes/VillagePrototype.unity` → Play.
   - 씬 · 프리팹이 깨져 보이면 메뉴 **Pogeun Village > Build Map Prototype**(여러 번 실행해도 같은 결과, 모델 가져오기도 먼저 부름).
   - 저장이 없으면 인트로가 나옴(ESC로 건너뛰기). 저장이 있으면 시작 화면(F 이어하기 / 새로 시작).
4. **Claude Code**: 저장소 **루트** 폴더를 작업 폴더로 → CLAUDE.md가 자동으로 읽힘.
5. **외부 도구 연결**(2번): Unity MCP, Varco3D, Claude in Chrome(Mixamo), Notion.
6. **확인**: 메뉴 `Pogeun Village > Run EditMode Tests` · `Run PlayMode Tests` → **EditMode 268 · PlayMode 192 통과**(2026-10-30 기준), 콘솔 경고 0.

> 주의: pull 받은 뒤 에디터에 이미 열려 있던 씬은 이전 상태로 남을 수 있음 → 씬을 디스크에서 다시 연다.

## 2. 외부 도구
| 도구 | 용도 | 연결 방법 / 메모 |
|---|---|---|
| **Unity MCP** (Unity AI Assistant) | Claude가 에디터에서 C# 실행 · 콘솔 · 카메라 캡처 | 패키지 `com.unity.ai.assistant`가 manifest에 있음 → 에디터를 켜 두면 연결. 스크립트 수정 직후 도메인 리로드 동안 "Unity not detected"는 정상 |
| **Varco3D** | 모든 3D 에셋 생성(컨셉 이미지 → 3D) | Claude 커넥터(MCP). **브라우저에서 플로우 편집기를 열어 둬야** 작동. 등급 CREATOR. 비용: 이미지 생성 약 20점, 3D 생성 약 200점, Rig · Animate 0점 |
| **Claude in Chrome** | Mixamo 리깅 · 동작 | 앱 내장 브라우저는 파일 올리기가 안 됨 → 사용자의 Chrome 확장 사용. Mixamo는 사용자 Adobe 계정으로 로그인돼 있어야 함 |
| **Notion** | 기획서 | 페이지 "🏡 포근 마을 - 게임 기획서"(개인 페이지 최상위) — https://app.notion.com/p/3fe74e10a764819b97afe202c13d411e |
| Blender MCP | (선택) 모델 검수 · 수정 | 사용 안 함 |

## 3. Varco3D 플로우 · Mixamo
### 3-1. 플로우 구조
- 플로우 ID: `9457cf6c-aefb-476a-aee0-8cb949d7fb78`(이름 "Untitled") — 이 프로젝트의 모든 생성물이 여기
- 다른 플로우 `f679a6c8-…`는 **다른 프로젝트**(양 캐릭터 · 토끼 몬스터) — 건드리지 않음
- 공통: 이미지 모델 `nano-banana-pro`, 라이선스 PRIVATE, 일반 텍스처(PBR 아님)

| 구역 | 캔버스 위치(y) | 구성 | 3D 설정 | 상태 |
|---|---|---|---|---|
| 주민 20명 | 0 ~ 8550 | 텍스트(x 0) → 이미지(x 400~1760, 5열) → 3D(x 2200~3560) | 3만 tri, 2048, T-pose | 적용 완료(P-3c) |
| 맵 소품 21종 | 메모 9050, 그룹 9300 ~ 13800 | 4열 그룹(x = 열 × 1100): 텍스트 +0 → 이미지 +340 → 3D +680, 행 간격 900 | 나무 1만 · 꽃 4천 · 바위·우체통 3천 · 분수대 1만 · 게시판 5천 · 가로등 4천 · 다리 8천 · 일반 집 1.5만 | 적용 완료(P-3a, 일반 집 8채는 안 씀) |
| 주민별 집 20채 | 메모 14700, 공통 규칙 텍스트 (0, 14950), 그룹 15300 ~ 18900 | 4열: AI 어시스턴트 +0 → 이미지 +340 → 3D +680 | 1.5만 tri, 2048 | 적용 완료(P-3b) |
| P-4 메모 | 20000 | 남은 모델 목록 메모 | — | — |
| 플레이어 | 20300 | 텍스트 → 이미지 → 3D → (안 쓰는) Rig · Animate | 3만 tri, T-pose | 생성 · Mixamo 완료, 적용 전 |
| 열매 2 | 21300 · 21800 | 4열 그룹 | 2천 tri, 1024 | 생성 완료, 적용 전 |
| 기본 가구 8 | 22500 · 23400 | 4열 그룹 | 4천 tri, 1024 | 생성 완료, 적용 전 |
| 인트로 방 소품 7 | 24300(메모) · 24600 · 25500 | 4열 그룹 | 2천 ~ 5천 tri, 1024 | 적용 완료(P-5d) |
| **다음 묶음** | **26400부터** | 같은 4열 그룹 방식 | — | 크레딧 충전 대기 |

- **주민별 집 연결**: [공통 집 규칙 텍스트] → AI 어시스턴트 `prompt` / [주민 텍스트] → `referenceText` / [주민 컨셉 이미지] → `reference` → 이미지 생성 `prompt`(+ 주민 이미지를 `reference`로 한 번 더) → 3D. **테마 가구 80(P-4c)도 이 방식 추천**.
- 사용 크레딧(대략): 소품 4,600 · 주민별 집 4,500 · P-4 지금까지 2,420

### 3-2. 프롬프트 꼬리말
- 소품: `Cute cozy stylized 3D game prop for a healing village life-sim game, soft pastel colors, rounded chunky shapes, simple clean forms, clean stylized 3D-render look, single isolated object, entire object visible, 3/4 front view from slightly above, plain solid white background, no shadow, no ground plane, no text, centered.`
- 주민별 집 공통 규칙(AI 어시스턴트 지시문 끝): `Compact cozy one-story cottage with a simple sturdy body and a charming roof, front door centered on the front facade, cute cozy stylized 3D game building for a healing village life-sim game, soft pastel colors, rounded chunky shapes, clean stylized 3D-render look, single isolated building, entire building visible, 3/4 front view from slightly above, plain solid white background, no shadow, no ground plane, no fence, no characters, no animals, no text, centered.`

### 3-3. 받아서 넣기
1. MCP `get_output_downloads`로 3D 노드마다 받기 — exportType **fbx**, `pivotToBottom: true`
2. `Assets/PogeunVillage/Art/Raw/{Props|Houses|Villagers|Furniture|Characters}/이름.fbx`에 그대로 보관(수정 금지)
3. 메뉴 `Pogeun Village/Import Varco Models` → 색 텍스처 꺼내기(`Textures/Models/id`) · 노멀 텍스처 지움 · FBX 재질 · 애니메이션 끔 · CurvedToon 재질(`Materials/Models`) · `Data/ModelLibrary.asset` 항목(없을 때만; id · 모델 · 재질 · 높이 · 방향)
4. 규격(단위 · 피벗 · 정면 +Z · 높이)은 [MODELING_SPEC.md](MODELING_SPEC.md)

### 3-4. Mixamo (리깅 · 동작은 모두 Mixamo — 사용자 결정 2026-10-30)
1. Varco3D에서 **OBJ zip**으로 받고 **노멀 텍스처를 빼서 10MB 아래로**(Chrome 올리기 제한)
2. Claude in Chrome으로 mixamo.com → Upload Character → 자동 리깅(Standard Skeleton 65)
3. 받기: 캐릭터 = **FBX for Unity · With Skin · T-pose**, 동작 = **Without Skin · 30fps**, 걷기 · 달리기는 **In Place**
4. 받은 파일(`%USERPROFILE%/Downloads`)을 `Art/Raw/Characters/`로 옮기고 `이름@동작.fbx`로
- 지금 있는 것: `Player_Varco.fbx`(원본) · `Player.fbx`(리깅, 뼈 46) · `Player@Idle` · `Player@Walk` · `Player@Jog`

## 4. Claude 설정 · 사용자와 합의한 작업 방식
### 4-1. Claude 메모리 (저장소 밖 — 새 PC에서 다시 만들기)
- 위치: `~/.claude/projects/<프로젝트 경로>/memory/`
- 내용: **사용자에게 보이는 모든 글은 한국어로**(대답 · 요약 · 질문 · 생각 정리 모두). 새 PC 첫 대화에서 "한국어로 답해, 기억해 둬"라고 하면 됨.

### 4-2. 진행 방식
- `claude_all_type_genre.md` 방법론: 문서 먼저 → 한 번에 기능 하나 → 테스트 → 확인 방법 안내 → 플레이 확인.
- 설계 질문은 **선택지 + 추천안**으로 한 번에 묶어 묻고, 답은 날짜 · 이유와 함께 GDD 9번 / TASKS / BALANCE에. Notion 기획서에도 같이.
- 수치는 BALANCE.md 표 + **검산 케이스**(그대로 단위 테스트).
- **플레이 확인은 Claude에게 맡김**(1-1부터): 테스트 전체 통과 + 콘솔 경고 0 + 실제 화면 캡처를 근거로 "플레이 확인 완료" 처리, 캡처는 사용자에게 보냄.
- 사용자는 Remote Control(휴대폰)로도 봄 → 결과 · 실패를 끝에 한국어로 분명히, 스크린샷은 파일로 보냄.
- 커밋: 한 항목 = 한 커밋, `MM.DD/Phase-항목/PascalCase요약`(수정은 `…Fix`), 끝에 `Co-Authored-By` 줄. **푸시는 사용자가 직접**. 작업 단위가 끝날 때마다 PROGRESS.md 갱신.
- Varco3D 생성은 **묶음마다 크레딧 확인** 후 실행.
- 그래픽 작업은 원래 재미 검증 이후지만, P-3 · P-4 · P-5는 사용자 요청으로 **순서 예외**.

## 5. 지금까지 한 일 (2026-10-30)
| 구분 | 내용 | 상태 |
|---|---|---|
| Phase 0 | 문서 · asmdef · 테스트 메뉴 · 템플릿 정리 · WorldSettings | 완료 |
| Phase 1 | 이동 · 카메라 · 상호작용 · 채집 · 인벤토리 · 도구 5종(도끼 · 삽 · 물뿌리개 · 잠자리채 · 낚싯대) · 땅 칸 · 묘목 · 꽃 | 완료 |
| Phase 2 | 게임 시간(하루 20분) · 상점 · 모아 · HUD · 주민 · 대화 · 선물 · 친밀도 · 캠핑 | 완료 |
| Phase 3 | 캠핑 이주 · 괴롭히기 · 떠남 · 부탁 · 마을 평가 · 가구(놓기 · 제작 · 상점 · 테마 · 평가) · 낚시(도감 · 수조) · 곤충(도감 · 채집통) | 완료 |
| Phase 4 | 4-1 재미 검증 | **보류**(사용자 요청, PLAYTEST.md 준비됨) |
| Phase 6 | 6-1 세이브(a 틀 · b 마을 · c 난수 · d 시작 화면) | 완료 |
| P-1 · P-2 | 둥근 행성 맵 · Varco3D 주민 · 소품 · 집 생성 | 완료 |
| P-3 | Varco3D 모델 적용(소품 · 주민별 집 · 주민) | 완료 |
| P-4 | 남은 모델 생성 — 플레이어(+Mixamo) · 열매 2 · 기본 가구 8 생성 | **일시 중지**(크레딧) |
| P-5 | 인트로(마을 → 아이 방 모니터 → 클릭 → 빨려 들기 → 낙하 · 착지), ESC로만 건너뛰기 | 그레이박스 완료 |

자세한 일지는 TASKS.md, 항목별 표는 PROGRESS.md 1번.

## 6. 앞으로 할 일 (순서대로 추천)
1. **P-4 모델 생성 이어가기**(크레딧 충전 뒤, 묶음마다 확인). 다음 묶음부터:
   1. ~~시설 6 + 상점 주인~~ → 완료(2026-11-02, 1,750점, 게임 적용까지 — TASKS P-4g · P-4f). Varco3D 플로우 다음 묶음은 y 38700부터
   2. ~~도구 5~~ → 완료(2026-11-02, 1,250점, 노점 매대 물건까지 — TASKS P-4h). 손에 든 모습은 플레이어 모델 적용 때
   3. ~~땅 · 줍는 물건~~ → 완료(2026-11-02, 1,250점 — TASKS P-4i). 꽃 포기는 보일 곳이 없어 생성 안 함
   4. ~~물고기 8 + 수조~~ → 생성 완료(2026-11-02, 2,250점, 가져오기까지). **게임 적용은 나중에**(사용자 요청) — 적용 요령은 TASKS P-4d
   5. ~~곤충 8 + 채집통~~ → 생성 완료(2026-11-02, 2,250점, **원본 FBX만 — Unity에서 가져오기 메뉴 아직**). 게임 적용은 나중에
   6. 테마 가구 80 — 주민 20 × 화분 · 벤치 · 탁자 · 가로등, **색만 다르게가 아니라 주민마다 특색**(주민별 집 방식). → **80개 모두 생성 완료**(2026-11-03, 주민 01 시험 4개 + 02 ~ 20 76개, 20,000점, 원본 FBX만 — Unity 가져오기 · 검수 · 적용은 나중에). 플로우 다음 묶음 y 57000부터. **P-4 생성 목록 전부 끝**
2. ~~**생성한 모델 게임 적용**~~ → 2026-11-03 전부 적용(물고기 · 수조, 곤충 · 채집통, 열매 · 기본 가구 8, 테마 가구 80, 플레이어 + 손에 든 도구). 자세한 내용은 TASKS P-4a ~ P-4f. 남은 그레이박스: 상점 매대의 묘목 공, 인트로 커서 · 먼지, 주민 집 없는 빈 집터 판
3. **인트로 다듬기**(모델 · 크레딧 뒤): 커서 모델, 마을 화면에서 손 흔드는 주민 — 방 소품 모델(P-5d) · 저녁 조명(P-5e)은 완료
4. **4-1 재미 검증**(보류) — 사용자가 PLAYTEST.md대로 플레이할 때. 기록은 에디터 플레이 때 `Temp/PlayLogs/*.csv`
5. 열린 항목(GDD 10번 · 아래 9번 표) — 해당 작업 때 결정

## 7. 만든 목록 · 위치
| 목록 | 위치 |
|---|---|
| 남은 모델 전체 목록(P-4a ~ i) · 결정 · 노드 위치 | TASKS.md **P-4** |
| 주민 20명(이름 · 종족 · 대사 · 색 · 테마 가구 · 집 · 모델) | `Assets/PogeunVillage/Data/Villagers/*.asset` |
| 모델 목록(id · 모델 · 재질 · 높이 · 방향) | `Data/ModelLibrary.asset` |
| 물건 · 도구 · 가구 · 물고기 · 곤충 데이터 | `Data/Items/` · 규칙 에셋 `Data/*Rules.asset` · 상점 `Data/ShopData.asset` · 인트로 `Data/IntroSettings.asset` |
| 모든 수치(표 + 검산) | BALANCE.md 1 ~ 20번 |
| 결정 기록(날짜 · 이유) | GDD.md 9번, TASKS 각 항목 "결정" |
| 재미 검증 점검표 | PLAYTEST.md |
| 커밋 기록 | PROGRESS.md 7번 |
| Varco3D 원본 | `Art/Raw/{Props, Houses, Villagers, Furniture, Characters}` |

## 8. 코드 지도 · 요령
### 8-1. 코드 지도 (`Assets/PogeunVillage/Scripts`)
- 폴더: Core · Data · Collection(도감) · Fishing · Furniture · Gathering · Ground · Insects · Interaction · Intro · Inventory · Rating · Save · Shop · Time · Tools · UI · Villagers · Editor
- 게임 규칙은 순수 C#(예: `IntroTimeline` · `StartMenu` · `ShopRules` · `CraftRules`), MonoBehaviour는 어댑터
- 맵 · 씬 · 프리팹은 전부 `Editor/VillageMapBuilder.cs`가 만듦 — 씬을 손으로 고치지 말고 빌더를 고침
- 에디터 메뉴(`Pogeun Village/…`): `Build Map Prototype` · `Import Varco Models` · `Run EditMode Tests` · `Run PlayMode Tests` · `Delete Save`
- 개발용: F8 = 다음 날 아침. 저장 위치 `Application.persistentDataPath/save.json`
- 주요 연결: 주민 모델 `VillagerDefinition.model` → `VillagerBehaviour.Setup`, 집 `VillagerDefinition.houseModel` → `HousePlot`, 맵 모델 `ModelVisual`(그릇 `Model_이름`) · `ColliderOnly`, 꽃 `FlowerLook`, 인트로 `IntroSequence`(방 (3000,0,0) · `FlatWorldCamera` · `CurvedScreen` 셰이더)

### 8-2. 테스트
- 메뉴로 실행, 결과는 `PogeunVillage/Temp/PogeunVillageTestResults_{EditMode|PlayMode}.txt`(파일 시각이 바뀌면 끝)
- PlayMode는 플레이 모드에 들어갔다 나옴 → 씬에 저장 안 한 변경이 있으면 먼저 저장
- PlayMode 기반 `VillageSceneTest`: `WithIntro`(기본 false) · `WithInsects`(기본 false), 저장은 `SaveManager.PathOverride`(임시 폴더)

### 8-3. 커밋 전 되돌릴 잡음
테스트 · 맵 생성 뒤 내용 변화 없이 바뀌는 파일들. `PogeunVillage/` 폴더에서:
```
git checkout -- "Assets/PogeunVillage/Fonts/Cafe24Ssurround SDF.asset" Assets/PogeunVillage/Meshes Assets/PogeunVillage/Settings Assets/PogeunVillage/Textures/GroundColor.png.meta Assets/Settings/PC_RPAsset.asset ProjectSettings Assets/PogeunVillage/Prefabs
```
(단, 빌더를 고쳐 프리팹 · 메시가 실제로 바뀐 커밋이면 그 파일은 남김)

### 8-4. Unity MCP 요령
- 에디터 창이 포커스를 잃으면 플레이 모드가 2프레임에서 멈춤 → 캡처는 카메라를 RenderTexture에 직접 그려 PNG로 저장
- 가끔 플레이 모드 중 MCP가 응답 없음 → 사용자가 플레이 모드를 직접 멈춰야 함
- MCP로 실행하는 코드에서 `AssetDatabase.DeleteAsset` · `Directory.Delete`는 "User interactions not supported" 오류 → `File.Delete`나 셸 사용

## 9. 알려진 한계 · 사용자 확인이 필요한 해석
### 9-1. 알려진 한계
- 주민: T-pose 정적 모델(애니메이션 없음), 이름 · 대사 임시
- 다리 가운데에서 발이 판에 조금 묻힘 / 일부 집 모델에 우체통이 붙어 우체통이 둘로 보임(05 너구리 등)
- 플레이어 · 상점 주인은 아직 그레이박스(플레이어 모델은 생성됨, 적용 전)
- 인트로: 커서 · 먼지는 기본 도형, 스탠드 갓 · 창 유리는 빛을 받을 뿐 스스로 빛나지 않음, 섬광은 카메라 캡처에 안 찍힘
- 씬 삼각형 약 380만(나무 260그루 × 1만) — 에디터 약 50fps

### 9-2. 사용자 확인이 필요한 해석
| 항목 | 현재 해석 | 위치 |
|---|---|---|
| 괴롭히기 연속 | 주민마다 따로 셈, 다른 주민을 때려도 끊기지 않음 | GDD 4-3 |
| 이사 취소 기준 | -20 **초과**(-19 이상)로 회복하면 취소 | GDD 4-3 |
| 구멍 -2 | 하루 상한 · 괴롭히기 연속과 별개, 하한 -20 적용 | GDD 10번 |
| 다 자란 나무 | 옮길 수 없음 | GDD 10번 |
| 상점 주인 | 종족 = 나무늘보(2026-11-02), 이름 미정 | GDD 10번 |
| 인트로 패드 건너뛰기 | ESC와 함께 패드 Start도 건너뛰기 | TASKS P-5a |
