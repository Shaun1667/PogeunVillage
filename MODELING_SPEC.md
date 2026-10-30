# MODELING_SPEC — Varco3D 모델 가져오기 규격

> 작성: 2026-10-30 (P-3a, 첫 모델 가져오기 전). CLAUDE.md "아트 · 그래픽 규칙"의 가져오기 규격.

## 1. 원본
- Varco3D 플로우 `9457cf6c…`(HANDOFF 3번)의 3D 결과를 **FBX**(바닥 피벗 `pivotToBottom`)로 받는다. Unity에 glTF 가져오기 패키지가 없어 FBX(텍스처 포함)를 씀.
- 원본은 `Assets/PogeunVillage/Art/Raw/{Props,Houses,Villagers}/이름.fbx`에 그대로 둔다(고치지 않음). Git LFS(`.gitattributes`: `*.fbx` · `*.glb`).
- 원본 규격(받은 그대로): 단위 m, 가장 긴 변이 약 1, 바닥 가운데 피벗, **정면 +Z**(Unity 기준 앞), 메시 1개 · 재질 1개, 색 텍스처 1024~2048 + 노멀 텍스처.

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

## 4. 놓는 규칙 (맵 생성 메뉴)
- 모델은 그릇 `Model_이름` 아래에 원본을 그대로 두고, 그릇에서 높이 · 회전을 맞춘다(원본 FBX의 축 변환 회전을 건드리지 않음).
- **충돌 · 상호작용은 기본 도형 그대로**(줄기 원기둥 · 바위 공 · 분수대 상자 · 다리 판 · 난간 기둥) — 렌더러만 지우고 모델이 모습을 맡는다. 위치 · 길찾기 · 칸 판정은 바뀌지 않음.
- 꽃은 그릇 이름이 `Petals`(시듦 색 · 번식 · 저장 모습이 이 아래 렌더러를 씀, `FlowerLook`). 모습 이름 = 재질 이름.
- 검수: 메시 찢어짐 · 표면 · 컨셉 불일치를 늘어놓은 캡처로 확인한 뒤 적용.
