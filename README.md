# OOPLABS_SPRING

Учебный репозиторий для двух лабораторных работ курса ООП 2026 на C# / .NET 8.

- **Лаб 1 — Электронная библиотека** (`lab1-library/`): роли Librarian / Writer / Reader, заявки на приём/выдачу/возврат книг.
- **Лаб 2 — Аренда автомобилей** (`lab2-car-rental/`): роли Client / Manager / Administrator, заявки на аренду с проверкой стажа, возраста, доступности авто.

Документация архитектуры и заметки к защите — в каталоге [`docs/`](docs/).

## Структура

```
lab1-library/
  src/
    Library.Domain/          # сущности, enum'ы, исключения, интерфейсы репозиториев
    Library.Application/     # сервисы / use-cases
    Library.Infrastructure/  # in-memory репозитории (далее EF Core)
    Library.Presentation/    # ASP.NET Core Web API
  tests/
    Library.Tests/           # smoke-тесты на xUnit
  Library.sln

lab2-car-rental/
  src/
    CarRental.Domain/
    CarRental.Application/
    CarRental.Infrastructure/
    CarRental.Presentation/
  tests/
    CarRental.Tests/
  CarRental.sln

docs/
  lab1-architecture.md
  lab2-architecture.md
  defense-notes.md
```

## Требования

- .NET SDK 8.x (`dotnet --version` → `8.x`).

## Команды

Запускать в корне соответствующего лабораторного каталога (`lab1-library/` или `lab2-car-rental/`).

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Запуск Web API:

```bash
dotnet run --project src/Library.Presentation         # ЛР1
dotnet run --project src/CarRental.Presentation       # ЛР2
```

## Текущий статус

Это **скелет**, а не финальная реализация. Он:

- компилируется на .NET 8;
- содержит доменные типы, enum'ы, кастомные исключения, контракты репозиториев;
- содержит in-memory заглушки репозиториев, достаточные для smoke‑тестов;
- содержит smoke‑тесты, проверяющие базовое поведение enum'ов, сущностей и заглушек.

Бизнес‑логика, EF Core, MediatR и контроллеры будут добавлены инкрементально в отдельных PR.

## Workflow

1. Работайте в feature‑ветке (`feat/lab1-*`, `feat/lab2-*`), не пушьте напрямую в `main`.
2. Каждый PR должен зеленеть `dotnet build` и `dotnet test`.
3. Перед коммитом запускать `dotnet format` (или `dotnet format --verify-no-changes` в CI).
4. При появлении в курсе шаблона `pin-oop-y28/template` или ветки/интерфейсов под конкретную ЛР — мигрировать структуру под него: совпадение имён проектов и слоёв уже подобрано так, чтобы миграция свелась к переименованию папок и копированию шаблонных файлов (CI, EditorConfig, базовых интерфейсов).

## Что дальше

- Добавить `Microsoft.EntityFrameworkCore` + `DbContext` в `*.Infrastructure`, описать миграции.
- Подключить `MediatR`, перевести `*.Application/Services` на команды/запросы.
- Реализовать бизнес‑правила и валидаторы (см. [`docs/lab1-architecture.md`](docs/lab1-architecture.md), [`docs/lab2-architecture.md`](docs/lab2-architecture.md)).
- Добавить контроллеры, DI и интеграционные тесты.
