# Caretaker

| Contributor | 이름 | 학번 |
|------|------|------|
| winteryu21 | 유민서 | 22311884 |
| DoKyeongKim22012139 | 김도경 | 22012139 |
| gyu5577 | 김병규 | 22012140 |
| somsoko | 백승헌 | 22112089 |
| PLMQ2785 | 배원일 | 22313530 |
| certa1014 | 권순표 | 22313552 |



---

## 프로젝트 개요

**Caretaker**는 시간 인과율 기반 2인 비대칭 협동 퍼즐 어드벤처 PC 게임입니다.

| 항목 | 내용 |
|------|------|
| 장르 | 2인 비대칭 협동 퍼즐 어드벤처 |
| 플랫폼 | PC (Windows) |
| 엔진 | Unity 6.4 |
| 네트워크 | Netcode for GameObjects (Host-Client) |
| 팀 규모 | 6인 |
| 개발 기간 | 2026.03 – 2026.11 |

---

## 빠른 시작 (팀원 온보딩)

### 1. 사전 요구사항
- [Unity 6.4](https://unity.com/releases/lts) 설치 (정확한 버전: `ProjectSettings/ProjectVersion.txt` 참고)
- [Git LFS](https://git-lfs.github.com/) 설치 및 활성화 (`git lfs --version`으로 확인)
- [JetBrains Rider](https://www.jetbrains.com/rider/) 또는 Visual Studio Code

### 2. 레포 클론
```bash
# Git LFS 초기화 (최초 1회)
git lfs install

# 레포 클론 & 에셋 다운로드
git clone https://github.com/winteryu21/Caretaker.git
cd Caretaker
git lfs pull
```

### 3. Unity에서 열기
Unity Hub → **Open** → 레포 안의 `Caretaker/` Unity 프로젝트 폴더 선택

### 4. Unity 에디터 설정 확인
- Edit → Project Settings → Editor:
  - **Version Control Mode**: Visible Meta Files
  - **Asset Serialization Mode**: Force Text

> ⚠️ **씬을 강제로 변경하지 마세요.** 씬 편집 전 반드시 이슈 브랜치를 생성하고 팀에 공유하세요.

---

## 디렉터리 구조

```
Caretaker/                  # Git 저장소 루트
├── Caretaker/              # Unity 프로젝트 루트
│   ├── Assets/
│   │   ├── _Project/       # 팀이 직접 관리하는 게임 자산
│   │   │   ├── Scenes/
│   │   │   ├── Scripts/
│   │   │   │   ├── Core/
│   │   │   │   ├── World/
│   │   │   │   ├── Gameplay/
│   │   │   │   ├── Presentation/
│   │   │   │   └── Shared/
│   │   │   ├── Data/
│   │   │   ├── Prefabs/
│   │   │   ├── Art/
│   │   │   ├── Audio/
│   │   │   └── Tests/
│   │   ├── Settings/      # Unity/URP 설정 자산
│   │   └── ...            # Unity 기본 생성 자산
│   ├── Packages/
│   └── ProjectSettings/
├── docs/
│   ├── design/            # DRD, DSD, 게임 설계서
│   ├── technical/         # 프로젝트 구조, 코딩 컨벤션
│   ├── sprints/           # 스프린트 계획, 이슈, 회고
│   └── archive/           # 아카이브된 원본 회의록
├── .gitignore
├── .gitattributes         # Git LFS 설정
├── .editorconfig
└── CONTRIBUTING.md        # 브랜치·커밋·PR 컨벤션
```

> Unity Hub에서는 저장소 루트가 아니라 `Caretaker/` Unity 프로젝트 폴더를 엽니다. 팀 제작 자산은 가능한 한 `Assets/_Project` 아래에 둡니다.

---

## 브랜치 & 워크플로우

| 브랜치 | 역할 |
|--------|------|
| `main` | 항상 빌드 가능한 안정 버전 (직접 Push 금지) |
| `develop` | 통합 브랜치 (스프린트 단위 머지) |
| `feat/{설명}` | 기능 개발 |
| `fix/{설명}` | 버그 수정 |
| `art/{설명}` | 에셋 작업 |
| `docs/{설명}` | 문서 작업 |

자세한 컨벤션 → [`CONTRIBUTING.md`](CONTRIBUTING.md)

---

## 기술 스택

| 영역 | 선택 |
|------|------|
| 엔진 | Unity 6.4 |
| 언어 | C# 12 |
| 네트워크 | Netcode for GameObjects |
| 렌더링 | URP (2D) |
| 이슈 트래킹 | Linear |
| 버전 관리 | Git + Git LFS |

---

## 주요 문서

| 문서 | 링크 |
|------|------|
| Design Requirements Document | [`docs/design/DRD.md`](docs/design/DRD.md) |
| Detailed System Design | [`docs/design/DSD.md`](docs/design/DSD.md) |
| 게임 설계서 (흐름 + 퍼즐 명세) | [`docs/design/game-design.md`](docs/design/game-design.md) |
| Unity 프로젝트 구조 | [`docs/technical/architecture.md`](docs/technical/architecture.md) |
| 코딩 컨벤션 | [`docs/technical/coding-standards.md`](docs/technical/coding-standards.md) |
| 기여 가이드 | [`CONTRIBUTING.md`](CONTRIBUTING.md) |

---

## 팀 구성

| 이름 | 주 담당 |
|------|--------|
| 유민서 | PM |
| 김도경 | 콘텐츠 팀 |
| 김병규 | 콘텐츠 팀 |
| 백승헌 | 시스템 팀 |
| 배원일 | 시스템 팀 |
| 권순표 | 콘텐츠 팀 |

---

*Caretaker © 2026 Team. Graduation Project.*
