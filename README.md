# AIcruiter

## 0. 팀
- 광운대학교 응용소프트웨어실습 2조
- 한종서, 문정은, 이석호, 편선아

## 1. 프로젝트 개요

> **"AI 면접관과 함께하는 실전 대비"**

`AIcruiter`는 면접 준비에 있어 가장 어려운 부분 중 하나인 **객관적인 피드백 부족** 문제를 해결하기 위해 기획된 **AI 면접 시뮬레이터** 입니다.<br>
GPT-4-Turbo 기반 AI가 답변을 채점하고 피드백을 제공하며, 사용자는 실제 면접처럼 실전 감각을 기를 수 있습니다.

- **CS 전공지식(자료구조, 운영체제, 데이터베이스, 컴퓨터네트워크)** 과 **인성 질문** 을 포함한 총 5개 카테고리의 질문을 기반으로 진행됩니다.

## 2. 주요 기능

<img width="503" height="307" alt="Image" src="https://github.com/user-attachments/assets/48607b2c-2053-42f7-ad73-a4b2538117da" />

> **클라이언트**

<br> <img width="540" height="374" alt="Image" src="https://github.com/user-attachments/assets/18b7eb8e-1a98-42f1-8c8b-c581c4bbe2ea" />

> **서버**

### 2.1 랜덤 질문

<img width="497" height="313" alt="Image" src="https://github.com/user-attachments/assets/09f42c77-d58a-433c-825b-1dda42eb554a" />

- 셔플 기반 랜덤 질문 시스템
- 카테고리별 질문 제공

### 2.2 질문 응답 및 채점

<img width="389" height="488" alt="Image" src="https://github.com/user-attachments/assets/d4dc76dc-d0ff-474d-aa26-4827aabb109e" />

- GPT-4 API를 통해 면접자의 답변을 실시간으로 채점
- 점수화(정확성/논리성/표현력) 및 시각적 피드백 제공

### 2.3 내 답변 확인

<img width="434" height="793" alt="Image" src="https://github.com/user-attachments/assets/d9aa3294-f8a3-4408-a0f2-d21b9b05bc7a" />

- 내가 답변한 질문 리스트 조회
- 검색 기능으로 키워드 포함 질문별 필터링 가능
- 답변 확인 및 수정 가능

### 2.4 모범 답안 확인

<img width="434" height="654" alt="Image" src="https://github.com/user-attachments/assets/943c3c2d-894f-4e07-a323-500f41dce82d" />

- 인성 질문을 제외한 CS 질문의 정답 제공
- ListBox 기반 인터페이스

### 2.5 공유 답변 확인 (커뮤니티)

<img width="491" height="388" alt="Image" src="https://github.com/user-attachments/assets/ba905ad2-74c2-426f-9912-10dea9e0dd09" />

- 여러 사용자의 답변을 동일 질문 기준으로 확인 가능

<img width="498" height="466" alt="Image" src="https://github.com/user-attachments/assets/2d662c3e-4bcd-4856-a558-b837145bb269" />

- 서버에서 커뮤니티 관리 가능

## 3. 프로젝트 구조

```plaintext
AIcruiter/
├── AIcruiter/
│   ├── bin/Debug/
│   │   ├── DataStructure.txt
│   │   ├── OS.txt
│   │   ├── DataBase.txt
│   │   ├── Network.txt
│   │   └── Character.txt
│   ├── Models/
│   │   ├── Question.cs
│   │   ├── SharedAnswer.cs
│   │   └── UserAnswer.cs
│   ├── Migrations/
│   ├── App.config
│   ├── AppDbContext.cs
│   ├── Form1.cs         // 클라이언트 메인 UI
│   ├── Program.cs
│   └── packages.config
│
├── AICruiter_Server/
│   ├── Models/
│   │   └── SharedAnswer.cs
│   ├── Migrations/
│   ├── App.config
│   ├── AppDbContext.cs
│   ├── Form1.cs         // 서버 UI 및 관리 도구
│   ├── Program.cs
│   └── packages.config
```

### 텍스트 파일 형식

```plaintext
1/해시 테이블을 설명하시오./해시 테이블은 무한에 가까운 데이터들을 유한한 개수의 해시 값으로 매핑한 테이블입니다. 이를 통해 작은 크기의 캐시 메모리로도 프로세스를 관리하도록 할 수 있습니다.
...
12/스택을 스레드마다 독립적으로 할당하는 이유는 무엇인가요?/스택은 함수 호출 시 지역 변수 저장, 매개변수 전달, 리턴 주소 저장 등에 사용되며, 실행 흐름마다 독립적인 호출 기록이 필요하기 때문에 스레드마다 별도로 할당됩니다. 만약 스택을 공유하게 되면 여러 스레드가 동시에 지역 변수나 함수 호출 정보를 덮어쓰게 되어 프로그램의 동작이 예측 불가능해지고, 심각한 오류나 충돌이 발생할 수 있습니다.
...
23/희망하지 않는 직무에 배치된다면 어떻게 하실 건가요?
...
```

### 서버-클라이언트 구조

<img width="738" height="445" alt="Image" src="https://github.com/user-attachments/assets/f4a45a88-864f-43bc-8e40-52eaabd66b0e" />

#### 목적
- GPT API Key 보안 문제 해결
- 커뮤니티 기능 확장 대비 구조 분리

#### 구현 방식
- 소켓 프로그래밍 기반 비동기 서버
- 클라이언트는 질문/답변 UI만 담당
- 서버에서 GPT 호출, 채점 결과 전달

### 프로젝트 ERD

<img width="675" height="400" alt="Image" src="https://github.com/user-attachments/assets/eaa441fa-ed1b-4c32-ad38-8a8454b2624f" />

## 4. 사용 기술 및 도구

- `C#`, `Windows Forms`, `.NET Framework 4.7.2`
- `Entity Framework Core 3.1.32`, `SQLite`
- `OpenAI GPT-4 API` , `TCP Socket`
- `System.Windows.Forms.DataVisualization.Charting`, `Newtonsoft.Json`
- `Git`, `GitHub`, `Notion`, `Figma`, `Visual Studio 2022`

## 5. 역할

| 이름     | 주요 역할 |
|----------|-----------|
| 한종서   | - 전체 DB 구조 설계<br>- SQLite 기반 질문/답변 저장 기능 구현<br>- 인성 질문 카테고리 추가 및 채점, 키워드 확인 로직 분기 처리 |
| 문정은   | - AI 프롬프트 구성 및 채점 로직 작성<br>- 채점 결과 시각화 및 UI 구성<br>- 공유 답변 기능 구현 및 관련 테스트 로직 작성 |
| 이석호   | - 서버-클라이언트 통신 구조 설계 및 구현<br>- GPT 응답 처리 서버 개발<br>- 자료구조, 운영체제 질문 카테고리 추가 |
| 편선아   | - 전체 UI 구성 및 디자인 설계<br>- 내 답변 확인, 검색 기능 구현<br>- 데이터베이스, 네트워크 질문 카테고리 추가 |
