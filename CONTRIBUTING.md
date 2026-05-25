# 기여 가이드 (CONTRIBUTING)

> Caretaker 레포에 기여하기 전 반드시 이 문서를 읽어주세요.

---

## 목차
1. [브랜치 전략](#1-브랜치-전략)
2. [커밋 메시지 컨벤션](#2-커밋-메시지-컨벤션)
3. [Pull Request 규칙](#3-pull-request-규칙)
4. [코드 리뷰 기준](#4-코드-리뷰-기준)
5. [Linear 연동](#5-linear-연동)
6. [Unity 협업 주의사항](#6-unity-협업-주의사항)

---

## 1. 브랜치 전략

### 구조
```
main        ← 안정 버전 (직접 push 금지, PR + 리뷰 필수)
develop     ← 통합 브랜치 (스프린트 머지 대상)
└── feat/{kebab-case-description}
└── fix/{kebab-case-description}
└── art/{kebab-case-description}
└── docs/{kebab-case-description}
└── chore/{kebab-case-description}
└── refactor/{kebab-case-description}
└── test/{kebab-case-description}
```

### 브랜치 명명 규칙
```
{타입}/{설명-케밥케이스}
```

**예시:**
```
feat/player-movement
fix/collision-desync
art/player-character-rig
docs/gdd-level-design-section
```

> Linear 이슈 연결은 브랜치명이 아닌 **PR에서** 처리합니다. (→ [5. Linear 연동](#5-linear-연동) 참조)

### 규칙
- `main` 직접 push **절대 금지**
- 브랜치는 반드시 `develop`에서 분기
- 작업 완료 후 `develop`으로 PR 생성
- 스프린트 종료 시 PM이 `develop → main` 머지

---

## 2. 커밋 메시지 컨벤션

[Conventional Commits](https://www.conventionalcommits.org/) 기반:

```
{type}({scope}): DEV-{N} {한국어 또는 영어 설명}
```

> 이슈 번호(`DEV-{N}`)는 Linear 이슈가 있는 경우 필수. 이슈 없는 소규모 작업(타이포, 포맷팅)은 생략 가능.

### 타입
| 타입 | 설명 |
|------|------|
| `feat` | 새 기능 추가 |
| `fix` | 버그 수정 |
| `art` | 에셋(아트/오디오) 추가 및 수정 |
| `docs` | 문서 수정 |
| `refactor` | 리팩토링 (동작 변경 없음) |
| `test` | 테스트 코드 추가/수정 |
| `chore` | 빌드, CI, 설정 파일 변경 |
| `perf` | 성능 개선 |

### Scope
| scope | 대상 |
|-------|------|
| `player` | 플레이어 제어, 이동, 상태 |
| `network` | 네트워크, 동기화, 세션 |
| `causality` | 시간 인과 시스템, 트리거 |
| `ai` | 감시/추격 AI |
| `ui` | UI/HUD |
| `audio` | BGM, SFX |
| `level` | 레벨 디자인, 퍼즐 배치 |
| `character` | 캐릭터 스프라이트, 애니메이션 |
| `gdd` | GDD 문서 |
| `drd` | DRD 문서 |

> scope는 프로젝트 진행에 따라 추가/변경 가능.

### 예시
```
feat(player): DEV-42 플레이어 이동 컨트롤러 구현
fix(network): DEV-61 충돌 감지 오류 수정
art(character): DEV-38 플레이어 캐릭터 리깅 에셋 추가
docs(drd): DEV-19 Check-Off List 작성
chore: .editorconfig 수정
```

---

## 3. Pull Request 규칙

### PR 생성 전 체크리스트
- [ ] `develop` 브랜치를 base로 설정
- [ ] PR 제목 또는 본문에 Linear 이슈 ID 포함
- [ ] 셀프 리뷰 완료
- [ ] Unity 씬/프리팹 변경 시 스크린샷 첨부
- [ ] 충돌(Conflict) 해결 완료

### 머지 조건
- 최소 **1명** 이상 Approve
- Squash and Merge 사용 → `develop` 히스토리 클린 유지

---

## 4. 코드 리뷰 기준

| 기준 | 체크 항목 |
|------|-----------|
| **정확성** | 로직이 요구 사항대로 동작하는가 |
| **가독성** | 변수/함수명이 명확한가, 주석이 적절한가 |
| **성능** | Update()에서 불필요한 연산이 없는가 |
| **일관성** | 코딩 컨벤션 준수 여부 → [`coding-standards.md`](docs/technical/coding-standards.md) |

---

## 5. Linear 연동

### 작업 흐름
1. 작업 시작 전 반드시 **Linear 이슈 생성**
2. Linear에서 브랜치 생성하거나 직접 분기
3. **아래 중 하나 선택 가능**
    - **PR 제목**에 이슈 ID 포함: `DEV-42 플레이어 이동 구현`
    - **PR 본문** 첫 줄에 Magic Word 작성: `Closes DEV-42`

### Magic Word

Magic Word는 **PR 본문**에 작성해야 합니다. PR 댓글에서는 동작하지 않습니다.

#### Closing Magic Words — 이슈를 **Done**으로 자동 이동

PR이 default branch(`develop` 또는 `main`)에 머지될 때 연결된 이슈를 자동으로 완료 처리합니다.  
PR이 열리는 시점에 이슈를 **In Progress**로, 머지 시 **Done**으로 이동합니다.

| 권장 단어 | 전체 변형 | 권장 상황 |
|----------|---------|---------|
| **`Closes`** | close, closes, closed, closing | 기능/작업이 완전히 완료되는 경우 ✅ **기본 사용** |
| **`Fixes`** | fix, fixes, fixed, fixing | 버그 수정 PR |
| **`Resolves`** | resolve, resolves, resolved, resolving | 쟁점/논의 사항 해결 |
| `Completes` | complete, completes, completed, completing | 특정 작업 완료 |
| `Implements` | implements, implemented, implementing | 기능 구현 완료 |

```
Closes DEV-42
Fixes DEV-61
```

#### Non-closing Magic Words — 이슈 **연결만** (Done 이동 없음)

PR이 이슈 작업의 일부이거나, 아직 완료가 아닌 경우 사용합니다.

| 권장 단어 | 전체 변형 | 권장 상황 |
|----------|---------|---------|
| **`Part of`** | part of | 이슈가 여러 PR로 나뉘어 진행될 때 ✅ |
| **`Ref`** | ref, refs, references | 이슈를 참조만 할 때 |
| `Related to` | related to, contributes to | 간접 관련 작업 |
| `Toward` | toward, towards | 이슈 해결에 기여하지만 완료는 아닐 때 |

```
Part of DEV-42
Ref DEV-55
```

#### 여러 이슈 동시 연결

```
Closes DEV-42, DEV-43 and DEV-56
```

#### 연결 무시 (브랜치명에 이슈 ID가 있어도 연결 차단)

```
skip DEV-42
ignore DEV-42
```

**예시 PR 본문:**
```
Closes DEV-42

플레이어 기본 이동 및 점프 구현.
- Rigidbody2D 기반 이동
- 지면 감지 레이캐스트
```

### 이슈 생성 규칙
- 예상 공수(Story Point) 기입
- 담당자 지정 필수
- Cycle(스프린트) 연결

---

## 6. Unity 협업 주의사항

### 씬(Scene) 충돌 방지
- 씬 파일은 **한 사람만** 편집. 편집 전 팀 채널에 공지
- 씬 편집 완료 즉시 PR 생성 → 대기 최소화
- 프로토타입용 씬은 별도 폴더에서 관리하며 `develop` 머지 불필요

### 프리팹(Prefab) 규칙
- 프리팹 오버라이드는 최소화 → 가능하면 Nested Prefab 사용
- 프리팹 변경 시 PR에 Inspector 전후 스크린샷 필수

### 주요 코드 규칙 (요약)
자세한 코딩 컨벤션 → [`docs/technical/coding-standards.md`](docs/technical/coding-standards.md)

- `[SerializeField] private` 사용, `public` 필드 금지
- `GetComponent<T>()`는 `Awake()`에서 캐싱
- `OnEnable()`에서 이벤트 구독 시 `OnDisable()`에서 반드시 해제

---

*질문은 팀 채널 또는 PM에게 연락하세요.*
