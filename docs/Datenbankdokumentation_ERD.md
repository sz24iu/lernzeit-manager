# Datenbankdokumentation mit ERD

## Zweck
Diese Dokumentation beschreibt das persistente Datenmodell der Anwendung, wie es in NoterDbContext und den Entities umgesetzt ist.

## DB-Tabellen und Bedeutung
- Users: Benutzerkonto mit Login-Informationen.
- StudyGoals: Lernziele eines Benutzers.
- Milestones: Teilziele zu einem Lernziel.
- StudySessions: Erfasste Lernzeit-Sessions.
- StudySessionPlans: Geplante Lerneinheiten pro Meilenstein.
- RefreshTokens: Tokens fuer JWT-Erneuerung.
- LearningTimers: Timerzustand (derzeit ohne API-Nutzung).
- Reminders: Erinnerungseintraege (derzeit ohne API-Nutzung).

## ERD (Mermaid)
```mermaid
erDiagram
    USERS {
        uuid Id PK
        string Email
        string HashPassword
    }

    STUDYGOALS {
        uuid Id PK
        uuid UserId FK
        string Title
        string Description
        int Type
        datetime StartDate
        datetime EndDate
        int Status
    }

    MILESTONES {
        uuid Id PK
        uuid StudyGoalId FK
        string Title
        datetime StartDateTime
        datetime EndDateTime
        datetime DueDateTime
        int Status
    }

    STUDYSESSIONS {
        uuid Id PK
        uuid UserId
        uuid GoalId
        datetime StartTime
        datetime EndTime
        int DurationMinutes
    }

    STUDYSESSIONPLANS {
        uuid Id PK
        uuid MilestoneId FK
        datetime PlannedStart
        int PlannedMinutes
        int Status
    }

    REFRESHTOKENS {
        uuid Id PK
        uuid UserId FK
        string Token
        string JwtId
        bool IsUsed
        bool IsRevoked
        datetime ExpiryDate
    }

    LEARNINGTIMERS {
        uuid Id PK
        datetime StartedAt
        datetime StoppedAt
        int State
    }

    REMINDERS {
        uuid Id PK
        uuid UserId
        string Message
        datetime ReminderTime
        bool IsSent
    }

    USERS ||--o{ STUDYGOALS : owns
    USERS ||--o{ REFRESHTOKENS : has
    STUDYGOALS ||--o{ MILESTONES : contains
    MILESTONES ||--o{ STUDYSESSIONPLANS : plans
    MILESTONES ||--o{ STUDYSESSIONS : tracked_via_GoalId
```

## Beziehungen im Code
- User 1:n StudyGoal ist explizit in OnModelCreating konfiguriert.
- StudyGoal 1:n Milestone ist explizit in OnModelCreating konfiguriert.
- Milestone 1:n StudySessionPlan ist explizit in OnModelCreating konfiguriert.
- RefreshToken besitzt ein ForeignKey-Attribut auf UserId.
- StudySession verwendet GoalId als Referenz auf Milestone.Id (technisch historisch benannt).

## Enum-Mapping
- GoalStatus: Planned, InProgress, Completed, Failed.
- GoalType: Module, Exam, Project, Assignment, Other.
- SessionStatus: Planned, Completed, Missed.
- TimerState: Running, Paused, Stopped.

## Integritaets- und Fachregeln
- StudyGoal darf nur mit nicht-leerem Title erstellt werden.
- API-seitig sind fuer StudyGoal sowohl Title als auch Description Pflicht.
- Milestone verlangt gueltiges Zeitintervall StartDateTime < EndDateTime.
- StudySession wird nur mit trackedMinutes > 0 angelegt.
- StudyGoal kann nur auf Completed gesetzt werden, wenn alle Milestones Completed sind.

## Bekannte Modellbesonderheiten
- Feldname StudySession.GoalId ist fachlich ein Milestone-Bezug.
- In StudySessionPlanRepository heisst die Lesemethode GetByStudyGoalIdAsync, filtert jedoch MilestoneId.
- StudySessionPlanController und UserController sind aktuell nicht mit Authorize abgesichert.
