# Unity 프로젝트 구조 결정

**프로젝트**: Caretaker

**상태**: 초기 Unity 개발 착수 시점의 디렉터리 구조 결정

---

## 1. 문서 목적

이 문서는 Caretaker의 **초기 Unity 프로젝트 디렉터리 구조**를 기록한다.

시스템 책임, 네트워크 동기화, 런타임 상태, 데이터 계약 같은 상세 소프트웨어 설계의 기준 문서는 [`../design/DSD.md`](../design/DSD.md)다. 이 문서는 그 설계를 실제 Unity `Assets` 안에서 너무 무겁지 않게 배치하기 위한 작업 규칙만 다룬다.

목표는 세 가지다.

1. 팀원이 Unity Project 창에서 길을 잃지 않을 것
2. Caretaker의 도메인 언어를 코드 구조에서 잃지 않을 것
3. 문서에 존재한다는 이유만으로 빈 폴더를 과하게 만들지 않을 것

---

## 2. 기본 판단

현재 프로젝트는 Unity 기본 템플릿에서 게임 구현을 시작하는 단계다. 따라서 지금 필요한 것은 완성형 아키텍처 트리가 아니라, **작게 시작하고 필요할 때만 자라는 구조**다.

다만 모든 팀 자산을 Unity `Assets` 루트에 흩어 두면 나중에 외부 패키지, 설정 자산, 팀 제작 자산이 섞일 수 있다. 그래서 팀 제작 자산은 `Assets/_Project` 아래에 모은다.

---

## 3. 최종 디렉터리 구조

```text
Assets/
├─ _Project/
│  ├─ Scenes/
│  ├─ Scripts/
│  │  ├─ Core/
│  │  ├─ World/
│  │  ├─ Gameplay/
│  │  ├─ Presentation/
│  │  └─ Shared/
│  ├─ Data/
│  ├─ Prefabs/
│  ├─ Art/
│  ├─ Audio/
│  └─ Tests/
├─ Settings/
└─ ... Unity 기본/패키지 생성 자산
```

### 폴더 책임

| 폴더 | 책임 |
| --- | --- |
| `_Project` | 팀이 직접 관리하는 게임 자산의 루트 |
| `Scenes` | 부트, 메뉴, 로비, 스테이지 씬 |
| `Scripts/Core` | 게임 흐름, 세션 역할, 체크포인트, 공통 부트스트랩 |
| `Scripts/World` | 룸, 시간 인과, 월드 오브젝트 상호작용 |
| `Scripts/Gameplay` | 플레이어, AI, 인벤토리, 무전기처럼 플레이 중 작동하는 규칙 |
| `Scripts/Presentation` | 카메라, HUD, 미니맵, 스플릿뷰 등 표시 계층 |
| `Scripts/Shared` | 여러 영역에서 재사용되는 작은 타입, ID, 유틸리티 |
| `Data` | ScriptableObject 기반 정적 규칙 데이터 |
| `Prefabs` | 재사용 가능한 GameObject 프리팹 |
| `Art` | 스프라이트, 타일, UI 이미지, VFX 등 시각 자산 |
| `Audio` | BGM, SFX, 무전기 관련 음향 자산 |
| `Tests` | EditMode / PlayMode 테스트 |

---

## 4. 폴더 생성 규칙

폴더는 설계를 보여주기 위한 장식이 아니라, **파일을 찾기 쉽게 만드는 도구**다. 그래서 다음 규칙을 따른다.

1. **예정된 시스템이라는 이유만으로 폴더를 만들지 않는다.** 실제 파일이 생길 때 만든다.
2. 한 폴더 안에 같은 성격의 파일이 **3개 이상** 생기면 하위 폴더를 검토한다.
3. 서로 다른 이유로 자주 수정되는 파일들이 섞이기 시작하면 분리한다.
4. `Past/Future`, `Host/Client`는 폴더 축으로 쓰지 않는다. 시간대 역할과 네트워크 역할은 코드/데이터에서 구분한다.
5. 네트워크 코드는 가능하면 해당 도메인 근처에 둔다. 공통 세션/전송 유틸리티만 별도 공용 코드로 뺀다.
6. `Shared`에는 정말 공용인 작은 타입만 둔다. 특정 시스템 지식이 들어가면 원래 시스템 폴더로 되돌린다.

예를 들어 `World`가 커지면 그때 다음처럼 나눈다.

```text
Scripts/World/
├─ Rooms/
├─ Causality/
└─ Interactions/
```

`Gameplay`도 실제 구현량이 늘면 다음처럼 분화할 수 있다.

```text
Scripts/Gameplay/
├─ Player/
├─ AI/
├─ Inventory/
└─ Radio/
```

---

## 5. 하지 않는 것

초기에는 다음을 하지 않는다.

- 모든 DSD 시스템을 폴더로 미리 펼치기
- `Managers`, `Controllers`, `Systems` 같은 기술 이름만으로 큰 폴더 만들기
- `Past`, `Future`, `Host`, `Client`를 최상위 코드 구조로 사용하기
- 빈 폴더를 많이 만들어 문서와 Project 창을 복잡하게 만들기

---

## 6. 기준 문서 관계

- 게임 설계 언어와 요구사항: `CONTEXT.md`, `docs/design/DRD.md`
- 시스템 책임과 데이터/네트워크 계약: `docs/design/DSD.md`
- Unity 자산 배치와 폴더 생성 규칙: 이 문서

이 문서는 `DSD`를 대체하지 않는다. 구현 중 구조가 흔들릴 때는 먼저 `DSD`의 시스템 경계를 확인하고, 그다음 이 문서의 폴더 생성 규칙에 맞춰 최소한으로 정리한다.
