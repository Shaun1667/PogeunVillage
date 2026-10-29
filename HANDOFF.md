# HANDOFF.md — 다른 PC에서 이어서 작업하기

> 작성: 2026-10-27. 이 저장소만 받으면 작업을 이어갈 수 있도록, 저장소 밖에 있던 정보(외부 도구 · Varco3D 플로우 · Notion · 작업 방식)를 모아 둔 문서.
> 진행 현황은 [PROGRESS.md](PROGRESS.md), 규칙은 [CLAUDE.md](CLAUDE.md), 방법론은 [claude_all_type_genre.md](claude_all_type_genre.md).

## 1. 새 PC 세팅 순서
1. **Git**: `git clone https://github.com/Shaun1667/PogeunVillage.git`
   - 커밋 작성자 이메일을 기존과 같게: `git config user.name "Shaun1667"`, `git config user.email "namulletta@gmail.com"`
2. **Unity Hub**: Unity **6000.6.0f1** 설치 → 저장소 안의 **`PogeunVillage/` 하위 폴더**를 프로젝트로 열기(저장소 루트가 아님).
   - 첫 실행 때 Library가 새로 만들어져 시간이 걸린다(정상).
3. **씬 열기**: `Assets/PogeunVillage/Scenes/VillagePrototype.unity` → Play.
   - 씬이 깨져 보이면 메뉴 **Pogeun Village > Build Map Prototype** 으로 다시 생성(여러 번 실행해도 같은 결과).
4. **Claude Code**: 저장소 **루트** 폴더를 작업 폴더로 연다 → CLAUDE.md가 자동으로 읽힌다.
5. **외부 도구 연결** (2번 참고): Unity MCP, Varco3D, Notion.
6. 확인: `git status`가 깨끗한지, 콘솔 에러 0인지.

> 주의 (가이드 14번): pull 받은 뒤 에디터에 이미 열려 있던 씬은 이전 상태로 남을 수 있다 → pull 후 씬을 디스크에서 다시 연다.
> 설정 파일이 **내용 변화 없이 줄바꿈만** 바뀌어 보이면 커밋하지 말고 되돌린다(`git checkout -- 파일`). 예: `ProjectSettings/ProjectAuditorSettings.asset`.

## 2. 외부 도구
| 도구 | 용도 | 연결 방법 / 메모 |
|---|---|---|
| **Unity MCP** (Unity AI Assistant) | Claude가 에디터에서 C# 실행 · 콘솔 확인 · 카메라 캡처 | 패키지 `com.unity.ai.assistant`가 manifest에 포함됨 → 에디터를 켜 두면 연결. 스크립트를 고친 직후엔 도메인 리로드 동안 잠시 "Unity not detected"가 정상 |
| **Varco3D** | 모든 3D 에셋 생성 (컨셉 이미지 → 3D) | Claude 커넥터(MCP). **브라우저에서 플로우 편집기를 열어 둬야** 작동. 아래 3번 플로우 |
| **Notion** | 기획서 정리 | Claude 커넥터. 페이지 "🏡 포근 마을 - 게임 기획서" (개인 페이지 최상위) — https://app.notion.com/p/3fe74e10a764819b97afe202c13d411e |
| Blender MCP | (선택) 모델 검수 · 수정 | 아직 사용 안 함 |

## 3. Varco3D 플로우 구조
- 플로우 ID: `9457cf6c-aefb-476a-aee0-8cb949d7fb78` (이름 "Untitled") — 주민 · 소품 · 주민별 집이 모두 이 플로우에 있음
- 다른 플로우 `f679a6c8-…`는 **다른 프로젝트**(양 캐릭터 · 토끼 몬스터) — 건드리지 않음
- 공통 설정: 이미지 모델 `nano-banana-pro`, 3D 삼각형 · 텍스처는 아래 표, 라이선스 PRIVATE, 일반 텍스처(PBR 아님)

| 구역 | 캔버스 위치 | 구성 | 3D 설정 |
|---|---|---|---|
| 주민 20명 | y 0 ~ 8550 | 텍스트(x 0, 세로로 450 간격) → 이미지(x 400~1760, 5열) → 3D(x 2200~3560) | 3만 tri, 2048, **T-pose** |
| 맵 소품 21종 | 메모 y 9050, 그룹 y 9300 ~ 13800 | 4열 그룹(x = 열 × 1100): 텍스트(+0) → 이미지(+340) → 3D(+680), 행 간격 900 | 나무 1만 · 꽃 4천 · 바위·우체통 3천 · 분수대 1만 · 게시판 5천 · 가로등 4천 · 다리 8천 · 일반 집 1.5만 |
| 주민별 집 20채 | 메모 y 14700, 공통 규칙 텍스트 (0, 14950), 그룹 y 15300 ~ 18900 | 4열 그룹: AI 어시스턴트(+0) → 이미지(+340) → 3D(+680) | 1.5만 tri, 2048 |

- **주민별 집 노드 연결**: [공통 집 규칙 텍스트] → AI 어시스턴트 `prompt` / [주민 텍스트] → `referenceText` / [주민 컨셉 이미지] → `reference` → 이미지 생성 `prompt` (+ 주민 이미지를 `reference`로 한 번 더) → 3D
- 소품 구역의 **일반 집 8채**는 주민별 집과 중복 — 정리 여부 열린 항목(GDD 10번)
- 사용 크레딧(대략): 소품 약 4,600, 주민별 집 약 4,500
- 결과 모델은 **아직 검수 · Unity 가져오기 전**. 가져오기는 재미 검증(Phase 4) 이후, MODELING_SPEC.md 작성 후.

### 프롬프트 공통 꼬리말
- 소품: `Cute cozy stylized 3D game prop for a healing village life-sim game, soft pastel colors, rounded chunky shapes, simple clean forms, clean stylized 3D-render look, single isolated object, entire object visible, 3/4 front view from slightly above, plain solid white background, no shadow, no ground plane, no text, centered.`
- 주민별 집 공통 규칙(AI 어시스턴트 지시문 끝 문장): `Compact cozy one-story cottage with a simple sturdy body and a charming roof, front door centered on the front facade, cute cozy stylized 3D game building for a healing village life-sim game, soft pastel colors, rounded chunky shapes, clean stylized 3D-render look, single isolated building, entire building visible, 3/4 front view from slightly above, plain solid white background, no shadow, no ground plane, no fence, no characters, no animals, no text, centered.`

## 4. 작업 방식 (사용자와 합의한 것)
- `claude_all_type_genre.md` 방법론을 따른다: 문서 먼저 → 한 번에 기능 하나 → 테스트 → 확인 방법 안내 → 사용자 플레이 확인.
- 설계 질문은 **선택지 + 추천안**으로 한 번에 묶어서 묻고, 답은 날짜 · 이유와 함께 GDD 9번 / TASKS / BALANCE에 남긴다. Notion 기획서에도 같이 반영한다.
- 수치는 BALANCE.md에 표 + **검산 케이스**(나중에 그대로 단위 테스트)로.
- 커밋: 한 항목 = 한 커밋, `MM.DD/Phase-항목/PascalCase요약`, 끝에 `Co-Authored-By` 줄. 작업 단위가 끝날 때마다 PROGRESS.md 갱신.
- 그래픽 작업은 재미 검증 전까지 더 하지 않는다(맵 프로토타입과 Varco3D 에셋은 순서 예외로 먼저 한 "선행 작업").

## 5. 아직 사용자 확인이 필요한 해석
결정 문장을 해석해서 문서에 적은 부분. 해당 작업 착수 때 확인한다.
| 항목 | 현재 해석 | 위치 |
|---|---|---|
| ~~괴롭히기 "3분"~~ (확정 2026-10-28) | 실제 시간, 일시정지 중 제외 | GDD 4-3 · BALANCE 2 |
| 괴롭히기 연속 | 주민마다 따로 셈, 다른 주민을 때려도 끊기지 않음 | GDD 4-3 |
| 이사 취소 기준 | -20 **초과**(-19 이상)로 회복하면 취소 | GDD 4-3 |
| ~~도끼 열매~~ (확정 2026-10-27) | 베어지는 순간(5번째)에만 떨어짐 | GDD 4-7 |
| ~~묘목 가속~~ (확정 2026-10-27) | 물 준 날은 그날 하루 더 성장(하루 +2) → 매일 물이면 3일 | GDD 4-8 · BALANCE 8 |
| ~~꽃 번식 칸~~ (확정 2026-10-27) | 상하좌우 빈 칸 중 무작위 1칸 | GDD 4-8 |
| ~~구멍 사라짐~~ (확정 2026-10-27) | 주민이 빠졌다 나올 때만(플레이어는 아님) | GDD 4-8-1 |
| 구멍 -2 | 하루 상한 · 괴롭히기 연속과 별개, 하한 -20 적용 | GDD 10번 |
| 다 자란 나무 | 옮길 수 없음 | GDD 10번 |

## 6. 다음 작업
TASKS의 Phase 6(6-1 세이브)까지 모두 완료 — 다음은 사용자와 정함(보류한 4-1 재미 검증 또는 새 확장 기획). 시작 화면(6-1d): `StartScreen`(어댑터) · `StartMenu`(규칙), 저장이 있으면 시작할 때 자동으로 불러오지 않고 `SaveManager.Continue` / `DeleteSave`, 테스트에서 다시 켠 뒤엔 F로 이어하기. 난수(6-1c): 모든 난수는 `SeededRandom`(상태 저장), 그날 것은 `SaveData.today`, 왕복 테스트 `SaveRoundTripPlayTests`. 마을 저장(6-1b): `Village.Capture / Restore`, `GroundSaver`(땅 · 나무 · 가구 · 떨어진 물건). 세이브(6-1a): `SaveManager`(자면 저장 · 시작 화면에서 불러오기) · `SaveCodec` · `SaveData`, 저장 위치 `Application.persistentDataPath/save.json`, 지우기는 메뉴 `Pogeun Village/Delete Save`, 테스트는 `SaveManager.PathOverride`(임시 폴더). 곤충 파트(3-7a ~ c)까지 완료 — Phase 3 기획 파트(가구 · 낚시 · 곤충) 모두 끝, 4-1 재미 검증은 보류. 채집통(3-7c): `InsectDefinition.cage`(가구 데이터, `cageInsect`), 수조와 같은 방식(`FurnitureDefinition.ContentColor`). 도감(3-6b · 3-7b): 공통 `Encyclopedia`(`CatchLog`) → `FishEncyclopedia` · `InsectEncyclopedia`. 곤충(3-7a): `Insects`(등장 · 도망 · 잡기) · `InsectBehaviour` · `InsectLogic`, 다른 PlayMode 테스트는 `WithInsects`가 false라 곤충을 숨김. 낚시 파트(3-6a ~ c)까지 완료, 4-1 재미 검증은 보류. 수조(3-6c): 물고기마다 `FishDefinition.aquarium`(가구 데이터, `aquariumFish`), 놓기는 가구 놓기 그대로. 도감(3-6b): `FishEncyclopedia`(`FishLog`), 게시판 고르기는 `VillageRater.ShowBoard`. 낚시(3-6a): 강 그림자 `FishShadows`, 낚싯대 `FishingRodUser`(Space를 `IToolPress`로 받음), 시간표 `BiteTimeline`, 물 칸 `GroundGridData.IsWater`. 4-1 재미 검증은 보류(사용자 요청) — 플레이할 때 `PLAYTEST.md`, 기록은 에디터 플레이 때 `Temp/PlayLogs/*.csv`(`PlayLogger`). Phase 3 가구 파트까지 완료. 가구 평가(3-4e): `VillageRating.FurniturePoints`, `RatingRules.furnitureKindCap` · `themeSetBonus`, 아침에만 계산. 테마 가구(3-4d): 주민 데이터 `themeFurniture` 4가지, 친밀도 보상은 대화 끝 `Village.GiveThemeReward`(`ThemeRewards`), 상점 `FurnitureCounter.TodayTheme`, 주민 색은 `PlacedFurniture.Tint`. 상점 가구(3-4c): 영업 중 상점 주인 F = 가구 사기 창(`FurnitureCounter`, 규칙 `ShopRules.BuyFurniture`). 제작(3-4b): 광장 동쪽 작업대 F = 제작 창(`Workbench`, 규칙 `CraftRules`). 두 창은 같은 목록 창(`ListWindow` + `IListSource`). 가구(3-4a): Tab 창에서 가구 칸 F = 놓기 모드(R 돌리기), 놓인 가구 F = 들기(`FurnitureKeeper` · `FurniturePlacer`). 마을 평가(3-5): 매일 아침 계산, HUD 별 · 광장 게시판 F(`VillageRater`). 부탁(3-3): 매일 아침 주민 1명(`Village.Request`). 괴롭히기 · 떠남(3-2): 잠자리채로 5번 → -2, -20이면 이사 준비 → 다음 아침 떠남(`Village.Departures`). 캠핑(2-6 · 3-1): 광장 서쪽 끝 텐트에 매일 아침 미거주 주민 1명(`Village.Camper`), 친밀도 5면 이주 확정(`Village.CampStay`) → 다음 아침 입주. 주민(2-4 · 2-5): 임시 20명 중 6명이 집터 12곳에 삶, 주민 앞 F = 대화창 → 끝에 선물하기 / 안녕, 친밀도는 이름 옆 숫자. 창이 플레이어를 멈출 땐 `PlayerLock`. 이름 · 대사는 `Data/Villagers/*.asset`에서 바꿈. HUD(2-3): Tab = 인벤토리 창, 안내는 화면 아래 알림(`GameNotice.Post`). 게임은 6시 시작, 상점은 8시에 열림 — 도구는 광장 서쪽 노점에서 삼. Phase 1 그레이박스 1-1 ~ 1-9 완료(곤충 · 낚시는 각 파트 기획 후). 개발용 F8 = 다음 날 아침. 테스트는 메뉴 `Pogeun Village > Run EditMode Tests` · `Run PlayMode Tests`로 실행(결과: `Temp/PogeunVillageTestResults_{EditMode|PlayMode}.txt`). PlayMode는 플레이 모드에 들어갔다 나오므로 씬에 저장 안 한 변경이 있으면 먼저 저장.
