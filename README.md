<div align="center">

<img src="https://img.shields.io/badge/ResumeAI-AI%20Resume%20Builder-4f46e5?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xNCAySDZhMiAyIDAgMCAwLTIgMnYxNmEyIDIgMCAwIDAgMiAyaDEyYTIgMiAwIDAgMCAyLTJWOTh6bTQgMThINlY0aDd2NWg1djEzeiIvPjwvc3ZnPg==" />

# ResumeAI

### A Production-Grade AI-Powered Resume Builder Built on Microservices

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031?style=flat-square&logo=angular)](https://angular.dev/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Core-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/en-us/sql-server)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-MassTransit-FF6600?style=flat-square&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)

<p>ResumeAI is a modern, scalable resume-building platform engineered with a full microservices architecture, event-driven design patterns, and a distributed backend — enabling users to craft ATS-friendly resumes, generate content via AI, match with jobs, and export to PDF/DOCX at scale.</p>

[Architecture](#-system-architecture) · [Microservices](#-microservices-overview) · [Setup](#-getting-started)

---

</div>

## 📌 Table of Contents

- [Tech Stack](#-tech-stack)
- [Architecture Overview](#-system-architecture)
- [UML Diagrams](#-uml-diagrams)
  - [Use Case Diagram](#1-use-case-diagram)
  - [System Architecture](#2-system-architecture-diagram)
  - [Entity Class Diagram](#3-entity-class-diagram)
  - [Export Flow Sequence](#4-export-flow-sequence)
  - [Angular Component Diagram](#5-angular-frontend-component-diagram)
  - [Inter-Service Communication](#6-inter-service-communication-map)
- [Microservices Overview](#-microservices-overview)
- [Core Features](#-core-features)
- [Infrastructure](#-infrastructure)
- [Design Patterns](#-key-design-patterns)
- [Getting Started](#-getting-started)

---

## 🛠 Tech Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| **Backend** | ASP.NET Core 8 Web API | 8 independent microservices |
| **Frontend** | Angular 21 | Single-page application (Standalone Components) |
| **Database** | SQL Server / PostgreSQL | Per-service isolated databases (DB-per-service) |
| **ORM** | Entity Framework Core | Code-first migrations & data access |
| **Message Broker** | RabbitMQ + MassTransit | Async event-driven communication |
| **Real-Time** | SignalR | Websockets for instant UI notifications |
| **Auth** | JWT (HS256) | Stateless authentication |
| **Gateway** | API Gateway | Single entry point, routing & JWT validation |
| **Containerization** | Docker & Docker Compose | Full local orchestration |

---

## 🏗 System Architecture

ResumeAI follows a **Microservices Architecture** with these core principles:

| Pattern | Applied Where |
|---------|--------------|
| ✅ **Microservices** | 8 independently deployable services |
| ✅ **Event-Driven Architecture** | Async messaging via RabbitMQ + MassTransit |
| ✅ **Background Processing** | PDF/DOCX export and AI generation happen asynchronously |
| ✅ **API Gateway** | Single entry point for all Angular client requests |
| ✅ **DB-Per-Service** | Strict data isolation across all services |
| ✅ **DTO Pattern** | Separate request/response models from entities |

---

## 📐 UML Diagrams

### 1. Use Case Diagram

> All actors and use cases across every module of ResumeAI

```mermaid
graph LR
    GUEST(["👤 Guest"])
    USER(["👤 Registered User"])
    SYSTEM(["⚙️ System\nBackground"])

    subgraph UC["«system» ResumeAI — Microservices Platform"]

        subgraph AUTH["🔐 Authentication"]
            UC1["Register Account"]
            UC2["Login / Get JWT"]
            UC3["Manage Subscription"]
        end

        subgraph RESUME["📝 Resume Builder"]
            UC7["Create Resume"]
            UC8["Edit Sections\nExp, Edu, Skills"]
            UC9["Select Template"]
        end

        subgraph AI["🤖 AI Integration"]
            UC11["Generate Content\nvia OpenAI"]
            UC12["Enhance Summary"]
        end

        subgraph EXPORT["📄 Exporting"]
            UC17["Export to PDF"]
            UC18["Export to DOCX"]
        end

        subgraph JOBS["💼 Job Match"]
            UC21["Upload JD"]
            UC22["Score Resume vs JD"]
        end

        subgraph SYS_AUTO["⚙️ System Automation"]
            UC41["Render PDF (Background)"]
            UC42["Send SignalR Notification"]
            UC43["Process Mock Payment"]
        end

    end

    %% Guest
    GUEST --> UC1 & UC2

    %% Registered User
    USER --> UC3
    USER --> UC7 & UC8 & UC9
    USER --> UC11 & UC12
    USER --> UC17 & UC18
    USER --> UC21 & UC22

    %% System triggers
    SYSTEM --> UC41 & UC42 & UC43

    %% Styling
    style GUEST fill:#1565C0,color:#fff,stroke:#1565C0
    style USER fill:#2E7D32,color:#fff,stroke:#2E7D32
    style SYSTEM fill:#6A1B9A,color:#fff,stroke:#6A1B9A
    
    style UC1 fill:#1976D2,color:#fff,stroke:#1976D2
    style UC7 fill:#4f46e5,color:#fff,stroke:#4f46e5
    style UC11 fill:#d97706,color:#fff,stroke:#d97706
    style UC17 fill:#dc2626,color:#fff,stroke:#dc2626
    style UC21 fill:#0f766e,color:#fff,stroke:#0f766e
```

---

### 2. System Architecture Diagram

> Full deployment view — Client → API Gateway → Microservices → Infrastructure

```mermaid
graph TB
    subgraph CLIENT["🌐 Client Layer"]
        WEB["Angular 21\nResumeAI Frontend\n:4200"]
    end

    subgraph GATEWAY["🔀 API Gateway"]
        GW["Routes:\n/api/auth | /api/resumes | /api/ai\n/api/templates | /api/export | /api/billing"]
    end

    subgraph SERVICES["⚙️ Microservices Layer"]
        AUTH["Auth.API\nJWT · Users"]
        RESUME["Resume.API\nCore Builder"]
        AI["AISection.API\nOpenAI Gens"]
        TEMPLATE["Template.API\nUI Themes"]
        EXPORT["Export.API\nPDF/DOCX"]
        JOB["JobMatch.API\nScoring"]
        PAYMENT["Payment.API\nMock Razorpay"]
        NOTIF["Notification.API\nSignalR WebSockets"]
    end

    subgraph INFRA["🗄️ Infrastructure"]
        DB[("Database\nDB per Service")]
        RMQ["RabbitMQ\nMassTransit"]
    end

    WEB -->|"HTTP REST + JWT"| GW
    WEB -- "SignalR (WSS)" --> NOTIF
    
    GW --> AUTH & RESUME & AI & TEMPLATE & EXPORT & JOB & PAYMENT
    
    EXPORT -->|"ExportFinishedEvent"| RMQ
    PAYMENT -->|"PaymentSuccessEvent"| RMQ
    AI -->|"AIGenerationDoneEvent"| RMQ
    
    RMQ -->|"Consumes"| NOTIF
    
    AUTH & RESUME & AI & TEMPLATE & EXPORT & JOB & PAYMENT -->|"R/W"| DB

    style WEB fill:#DD0031,color:#fff,stroke:#DD0031
    style GW fill:#6c63ff,color:#fff,stroke:#6c63ff
    style AUTH fill:#059669,color:#fff,stroke:#059669
    style RESUME fill:#4f46e5,color:#fff,stroke:#4f46e5
    style AI fill:#d97706,color:#fff,stroke:#d97706
    style EXPORT fill:#dc2626,color:#fff,stroke:#dc2626
    style DB fill:#336791,color:#fff,stroke:#336791
    style RMQ fill:#FF6600,color:#fff,stroke:#FF6600
```

---

### 3. Entity Class Diagram

> Domain model for the Core Resume Service

```mermaid
classDiagram
    direction TB

    class User {
        +Guid Id
        +String Email
        +String PasswordHash
        +String Role
        +String SubscriptionPlan
    }

    class Resume {
        +Guid Id
        +Guid UserId
        +String Title
        +Guid TemplateId
        +DateTime CreatedAt
        +DateTime UpdatedAt
    }

    class Section {
        +Guid Id
        +Guid ResumeId
        +String SectionType
        +String ContentJson
        +Int OrderIndex
    }

    class ResumeTemplate {
        +Guid Id
        +String Name
        +String HtmlStructure
        +String CssStyles
    }

    class ExportJob {
        +Guid Id
        +Guid ResumeId
        +String Format
        +String Status
        +String FileUrl
    }

    User "1" --> "0..*" Resume : owns
    Resume "1" --> "0..*" Section : contains
    Resume "1" --> "1" ResumeTemplate : uses
    Resume "1" --> "0..*" ExportJob : exports
```

---

### 4. Export Flow Sequence

> Sequence diagram — Event-driven, asynchronous background PDF generation

```mermaid
sequenceDiagram
    autonumber
    actor User as Angular Client
    participant GW as API Gateway
    participant ExportService as Export.API
    participant ResumeService as Resume.API
    participant RMQ as RabbitMQ
    participant NotifService as Notification.API

    User->>GW: POST /api/export (ResumeId, Format=PDF)
    GW->>ExportService: Forward Request
    
    ExportService->>ResumeService: GET /api/resumes/{Id}
    ResumeService-->>ExportService: Return Resume Data
    
    ExportService-->>GW: 202 Accepted (Job Started)
    GW-->>User: 202 Accepted (Show UI Loader)
    
    Note over ExportService: Background Render PDF
    
    ExportService->>RMQ: Publish [ExportCompletedEvent]
    
    RMQ->>NotifService: Consume [ExportCompletedEvent]
    
    NotifService->>User: SignalR Message: "ExportReady" (URL)
    
    Note over User: UI reveals "Download" Button
    User->>GW: GET File
    GW-->>User: Return PDF Document
```

---

### 5. Angular Frontend Component Diagram

> Standalone components, lazy-loaded routes, and HTTP interceptors

```mermaid
graph LR
    subgraph GUARDS["🛡️ Guards"]
        AG["authGuard"]
        GG["guestGuard"]
    end

    subgraph INTERCEPTORS["⚡ Interceptors"]
        AI["AuthInterceptor"]
    end

    subgraph COMPONENTS["📄 Features (Lazy Loaded)"]
        LOGIN["Login/Register\n/auth"]
        DASH["Dashboard\n/dashboard"]
        RESUME["Resume Builder\n/resumes/:id/builder"]
        AI_TOOLS["AI Tools\n/ai-tools"]
        TEMP["Templates\n/templates"]
        JOB["Job Match\n/job-match"]
        EXPORT["Export\n/export"]
        BILL["Billing\n/billing"]
    end

    subgraph SERVICES["🔌 Core Services"]
        AS["AuthService"]
        RS["ResumeService"]
        NS["NotificationService\n(SignalR)"]
    end

    GG --> LOGIN
    AG --> DASH & RESUME & AI_TOOLS & TEMP & JOB & EXPORT & BILL
    
    AI -.->|Attaches JWT| AS & RS
    
    RESUME --> RS & AS
    EXPORT --> NS
    
    style AG fill:#DD0031,color:#fff,stroke:#DD0031
    style GG fill:#DD0031,color:#fff,stroke:#DD0031
    style DASH fill:#1976D2,color:#fff,stroke:#1976D2
    style RESUME fill:#1976D2,color:#fff,stroke:#1976D2
    style RS fill:#2E7D32,color:#fff,stroke:#2E7D32
    style NS fill:#2E7D32,color:#fff,stroke:#2E7D32
```

---

### 6. Inter-Service Communication Map

> Synchronous calls and asynchronous events between microservices

```mermaid
graph LR
    RESUME["Resume.API"]
    AUTH["Auth.API"]
    EXPORT["Export.API"]
    AI["AISection.API"]
    JOB["JobMatch.API"]
    NOTIF["Notification.API"]
    PAYMENT["Payment.API"]

    EXPORT -->|"HTTP GET\nFetch Resume Data"| RESUME
    JOB -->|"HTTP GET\nFetch Resume Text"| RESUME
    AI -->|"HTTP PUT\nUpdate Resume Section"| RESUME
    
    PAYMENT -->|"RabbitMQ\nPaymentSuccessEvent"| NOTIF
    EXPORT -->|"RabbitMQ\nExportCompletedEvent"| NOTIF
    AI -->|"RabbitMQ\nAIGeneratedEvent"| NOTIF

    style RESUME fill:#4f46e5,color:#fff,stroke:#4f46e5
    style AUTH fill:#059669,color:#fff,stroke:#059669
    style NOTIF fill:#db2777,color:#fff,stroke:#db2777
    style EXPORT fill:#dc2626,color:#fff,stroke:#dc2626
```

---

## 📦 Microservices Overview

| Service | Responsibility |
|---------|----------------|
| **Auth.API** | User registration, login, JWT generation |
| **ResumeAI (Resume.API)** | Core CRUD operations for resumes and sections |
| **AISection.API** | Integration with AI to generate bullet points and summaries |
| **ExportService** | Generates PDF/DOCX files asynchronously |
| **JobMatchService** | Analyzes resume against job descriptions |
| **PaymentService** | Mock Razorpay integration for premium features |
| **NotificationService** | SignalR hub for pushing real-time alerts to the client |
| **TemplateService** | Provides HTML/CSS templates for resumes |
| **ApiGateway** | Single routing endpoint for the Angular app |

---

## 🎯 Core Features

<details>
<summary><strong>👤 User & Auth</strong></summary>

- Register and login with secure JWT (JSON Web Tokens)
- Premium subscription management mock integration

</details>

<details>
<summary><strong>📝 Resume Builder</strong></summary>

- Interactive UI to add, edit, and reorder resume sections (Experience, Education, Skills, etc.)
- Dynamic template selection to change resume aesthetics instantly

</details>

<details>
<summary><strong>🤖 AI-Powered Content</strong></summary>

- Generate professional experience bullets using AI
- Auto-write impactful professional summaries
- Eliminate manual typing with AI suggestions

</details>

<details>
<summary><strong>📄 Background Exports</strong></summary>

- Generate pixel-perfect PDFs or editable DOCX files
- Event-driven background processing to keep the UI responsive
- Real-time download links delivered via SignalR

</details>

---

## 🔧 Infrastructure

### RabbitMQ — Event Queue
- **Abstraction:** MassTransit over RabbitMQ
- **Publishers:** ExportService, PaymentService, AISection
- **Consumer:** NotificationService
- **Pattern:** Prevents long-running HTTP timeouts by offloading work to background workers.

### SignalR — WebSockets
- **Purpose:** Pushes notifications directly to the Angular client.
- **Trigger:** When a RabbitMQ event is consumed (e.g., Export Finished), SignalR broadcasts the result to the specific user's connection ID.

---

## 📊 Key Design Patterns

| Pattern | Applied Where |
|---------|--------------|
| Repository Layer | Decoupling EF Core logic across microservices |
| Dependency Injection | standard ASP.NET Core DI |
| Event-Driven Messaging | `ExportCompletedEvent` via MassTransit |
| Database-per-Service | Independent schemas for scalability |
| API Gateway | Routing frontend requests to backend services |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+ & npm](https://nodejs.org/) (for Angular 21)
- [Docker Desktop](https://www.docker.com/) (for RabbitMQ and Databases)

### Running Locally

1. **Start Infrastructure**: Spin up RabbitMQ and required databases using Docker Compose.
   ```bash
   docker-compose up -d
   ```
2. **Start Backend**: Open `Sprint_Project_Resume.sln` in Visual Studio and run the multiple startup projects, or run them individually via the .NET CLI.
3. **Start Frontend**: 
   ```bash
   cd FrontEnd/resumeai-frontend
   npm install
   npm start
   ```
4. Access the application at `http://localhost:4200`.

---

<div align="center">

Made with ❤️ · ResumeAI — Microservices Platform

</div>
