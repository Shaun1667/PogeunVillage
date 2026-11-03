# MODELING_SPEC — Varco3D 모델 가져오기 규격

> 작성: 2026-10-30 (P-3a, 첫 모델 가져오기 전). CLAUDE.md "아트 · 그래픽 규칙"의 가져오기 규격.

## 1. 원본
- Varco3D 플로우 `9457cf6c…`(HANDOFF 3번)의 3D 결과를 **FBX**(바닥 피벗 `pivotToBottom`)로 받는다. Unity에 glTF 가져오기 패키지가 없어 FBX(텍스처 포함)를 씀.
- 원본은 `Assets/PogeunVillage/Art/Raw/{Props,Houses,Villagers,Furniture,Room}/이름.fbx`에 그대로 둔다(고치지 않음). Git LFS(`.gitattributes`: `*.fbx` · `*.glb`).
- 원본 규격(받은 그대로): 단위 m, 가장 긴 변이 약 1, 바닥 가운데 피벗, **정면 +Z**(Unity 기준 앞), 메시 1개 · 재질 1개, 색 텍스처 1024~2048 + 노멀 텍스처.

## 1-1. 움직이는 캐릭터 (결정 2026-10-30)
- 플레이어처럼 걷는 캐릭터는 Varco3D에서 T-pose로 생성 → **Mixamo**에서 자동 리깅 · 동작. 받는 형식: FBX for Unity, 캐릭터는 T-pose + 몸 포함(`이름.fbx`), 동작은 몸 없음 30fps(`이름@동작.fbx`, 걷기 · 달리기는 In Place). 원본 Varco FBX는 `이름_Varco.fbx`로 함께 보관. 위치는 `Art/Raw/Characters/`(주민 리깅도 여기 `Villager번호.fbx` · `@Idle` · `@Walk`, Varco 원본은 `Art/Raw/Villagers/` 그대로 — P-4j)
- 주민(정적)은 지금처럼 Varco FBX 그대로

## 2. 가져오기 (메뉴 `Pogeun Village/Import Varco Models`, 여러 번 실행해도 같음)
1. FBX 안의 색 텍스처를 `Textures/Models/이름/base_color.png`로 꺼냄(처음 한 번). 노멀 텍스처는 쓰지 않아 지움(CurvedToon은 노멀 맵 없음).
2. FBX의 재질 가져오기 · 애니메이션 가져오기를 끔(URP Lit 재질이 생기지 않게).
3. 재질 `Materials/Models/이름.mat` = **CurvedToon** + `_BaseMap` 색 텍스처(모델마다 하나 — 같은 모델끼리 배칭). 핵심 설계 규칙 1(화면에서만 휨)을 지킴.
4. 목록 `Data/ModelLibrary.asset`에 항목(id · 모델 · 재질 · 게임 높이 · 정면 회전)을 넣음. **이미 있는 항목의 높이 · 회전은 건드리지 않음**(에셋에서 고침).
- 맵 생성 메뉴(`Build Map Prototype`)가 이 메뉴를 먼저 부른다.

## 3. 게임 안 크기 (ModelLibrary 처음 값, m)
| id | 높이 | 정면 회전 | 쓰는 곳 |
|---|---|---|---|
| TreeRound | 3.8 | 0 | 둥근 나무 · 키운 나무 |
| TreeApple / TreePeach | 3.8 | 0 | 사과 · 복숭아 나무(딸 수 있는 열매는 따로 공 3개) |
| TreePine | 4.2 | 0 | 침엽수 |
| Rock | 0.7 × 바위 크기(0.7~1.3) | 0 | 바위 |
| FlowerTulip | 0.5 | 0 | 꽃(무리마다 한 종류) · 심은 꽃 |
| FlowerDaisy / FlowerPansy | 0.42 | 0 | 꽃 |
| Fountain | 3.6 | 0 | 광장 분수대(폭 4.6 = 충돌 상자) |
| NoticeBoard | 2.3 | 0 | 광장 게시판 |
| StreetLamp | 2.6 | 0 | 광장 가로등 |
| Bridge | 판에 맞춤(길이 10.8 · 폭 3.0) | 90 | 다리 |
| Mailbox | 1.3 | 0 | 우체통(P-3b) |
| ShopStall | 맞춤(폭 5.6 · 높이 2.6 · 깊이 1.4) | 0 | 상점 노점 매대 + 차양(P-4g) |
| Shopkeeper | 1.6 | 0 | 상점 주인 나무늘보(정적 자세, P-4f) |
| SellBox | 폭 0.85에 맞춤 | 0 | 판매함 |
| Bed | 맞춤(폭 1.05 · 길이 1.95) | 180(그릇 안) | 광장 침대(모델 머리가 -z) |
| Workbench | 맞춤(폭 1.6 · 깊이 0.9) | 0 | 작업대 |
| CampTent | 2.3 | 0 | 캠핑 텐트 + 모닥불(텐트가 +x로 0.66 치우침) |
| LotSign | 1.25 | 0 | 빈 집터 표지판 |
| ToolNet / ToolFishingRod | 0.6 | 0 (잠자리채는 x 45°로 세움) | 노점 매대 위 도구(P-4h) |
| ToolAxe | 0.45 | 0 | 노점 매대 위 도끼 |
| ToolShovel | 0.55 | 0 (누워 생성 → x -90°로 세움) | 노점 매대 위 삽 |
| ToolWateringCan | 0.35 | 0 | 노점 매대 위 물뿌리개 |
| Stump | 폭 0.5에 맞춤(× 베어진 나무 배율) | 0 | 나무 밑동(P-4i) |
| SaplingStage0 / 1 / 2 | 0.5 / 0.9 / 1.4 | 0 | 심은 묘목 성장 3단계 |
| PickupWood | 0.3 | 0 | 떨어진 목재(`ItemDefinition.pickupModel`) |
| House01 ~ 20 | 3.6 | 0 | 주민 01~20의 집(정면 = 현관, 폭 약 4.8) |
| Villager01 ~ 20 | 1.5 | 0 | 주민 01~20(T-pose 정적, 뼈대 없음) |

## 4. 놓는 규칙 (맵 생성 메뉴)
- 모델은 그릇 `Model_이름` 아래에 원본을 그대로 두고, 그릇에서 높이 · 회전을 맞춘다(원본 FBX의 축 변환 회전을 건드리지 않음).
- **충돌 · 상호작용은 기본 도형 그대로**(줄기 원기둥 · 바위 공 · 분수대 상자 · 다리 판 · 난간 기둥) — 렌더러만 지우고 모델이 모습을 맡는다. 위치 · 길찾기 · 칸 판정은 바뀌지 않음.
- 꽃은 그릇 이름이 `Petals`(시듦 색 · 번식 · 저장 모습이 이 아래 렌더러를 씀, `FlowerLook`). 모습 이름 = 재질 이름.
- 검수: 메시 찢어짐 · 표면 · 컨셉 불일치를 늘어놓은 캡처로 확인한 뒤 적용.
