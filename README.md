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
    Library.Presentation/    # ASP.NET Core Web API, контроллеры, DI
  tests/
    Library.Tests/           # unit + API integration тесты (xUnit, WebApplicationFactory)
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
- `curl` (опционально, для ручных проверок API).

## Лаб 1 — локальный запуск

### Клонирование и сборка

```bash
git clone https://github.com/NikitaOrela/OOPLABS_SPRING.git
cd OOPLABS_SPRING

# для иллюстрации текущей итерации (ещё не смерженной):
git checkout feature/lab1-business-rules

dotnet restore lab1-library/Library.sln
dotnet build   lab1-library/Library.sln
dotnet test    lab1-library/Library.sln --nologo
```

### Запуск API

```bash
dotnet run --project lab1-library/src/Library.Presentation
```

По умолчанию API стартует на `http://localhost:5000` или `https://localhost:5001` (см. вывод `dotnet run`). Дальше — далее предполагается `BASE=http://localhost:5000`.

> Состояние API хранится **в памяти процесса** — после рестарта пользователи, книги и заявки исчезают. EF Core добавим в следующей итерации.

### Smoke‑сценарий (happy path)

```bash
BASE=http://localhost:5000

# 1. Создаём библиотекаря, писателя и читателя.
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"lib","fullName":"Lib Larian","roles":[1]}'        # Librarian
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"writer","fullName":"W Riter","roles":[2]}'        # Writer
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"reader","fullName":"R Eader","roles":[3]}'        # Reader

# 2. Писатель регистрирует книгу (тираж 5).
curl -s -X POST $BASE/api/books -H 'Content-Type: application/json' \
  -d '{"writerId":2,"title":"Dune","circulation":5}'

# 3. Писатель подаёт заявку на поставку 3 экземпляров.
curl -s -X POST $BASE/api/requests -H 'Content-Type: application/json' \
  -d '{"applicantId":2,"bookId":1,"type":2,"quantity":3}'

# 4. Библиотекарь одобряет заявку #1.
curl -s -X POST $BASE/api/requests/1/approve -H 'Content-Type: application/json' \
  -d '{"librarianId":1}'

# 5. Читатель просит выдать ему книгу (Receive, quantity=1).
curl -s -X POST $BASE/api/requests -H 'Content-Type: application/json' \
  -d '{"applicantId":3,"bookId":1,"type":1,"quantity":1}'

# 6. Библиотекарь одобряет заявку #2.
curl -s -X POST $BASE/api/requests/2/approve -H 'Content-Type: application/json' \
  -d '{"librarianId":1}'

# 7. Состояние книги: 3 supplied, 2 available, 2 remaining circulation.
curl -s $BASE/api/books/1
```

Numeric‑коды enum’ов:
`UserRole` — `1=Librarian`, `2=Writer`, `3=Reader`. `RequestType` — `1=Receive`, `2=Supply`, `3=Return`. `RequestStatus` — `1=Pending`, `2=Approved`, `3=Rejected`, `4=Completed`.

### Сценарии с отказом

```bash
# Дубликат имени пользователя → 409 Conflict
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/users \
  -H 'Content-Type: application/json' \
  -d '{"userName":"lib","fullName":"Other","roles":[3]}'

# Писатель пытается поставить больше тиража (5 при оставшихся 2) → 422
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/requests \
  -H 'Content-Type: application/json' \
  -d '{"applicantId":2,"bookId":1,"type":2,"quantity":5}'

# Читатель пытается взять одну и ту же книгу второй раз → 422
curl -s -X POST $BASE/api/requests -H 'Content-Type: application/json' \
  -d '{"applicantId":3,"bookId":1,"type":3,"quantity":1}'   # Return
curl -s -X POST $BASE/api/requests/3/approve -H 'Content-Type: application/json' \
  -d '{"librarianId":1}'
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/requests \
  -H 'Content-Type: application/json' \
  -d '{"applicantId":3,"bookId":1,"type":1,"quantity":1}'   # 422 — уже брал

# Резолв не библиотекарем → 403
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/requests/1/approve \
  -H 'Content-Type: application/json' \
  -d '{"librarianId":3}'                                    # reader id
```

## Лаб 2 — статус

Скелет (`lab2-car-rental/`) собирается, но бизнес‑логика будет реализована в отдельных PR после слияния Лаб 1.

## Сводные команды

```bash
dotnet restore lab1-library/Library.sln
dotnet build   lab1-library/Library.sln --nologo
dotnet test    lab1-library/Library.sln --nologo

dotnet build   lab2-car-rental/CarRental.sln --nologo
dotnet test    lab2-car-rental/CarRental.sln --nologo

# опционально
dotnet format
```

## Workflow

1. Работайте в feature‑ветке (`feat/lab1-*`, `feat/lab2-*`), не пушьте напрямую в `main`.
2. Каждый PR должен зеленеть `dotnet build` и `dotnet test`.
3. Перед коммитом запускать `dotnet format` (или `dotnet format --verify-no-changes` в CI).
4. При появлении в курсе шаблона `pin-oop-y28/template` или ветки/интерфейсов под конкретную ЛР — мигрировать структуру под него: совпадение имён проектов и слоёв уже подобрано так, чтобы миграция свелась к переименованию папок и копированию шаблонных файлов (CI, EditorConfig, базовых интерфейсов).

## Что дальше

- Подключить EF Core (`LibraryDbContext`, миграции) и заменить in‑memory репозитории.
- Перевести сервисы на MediatR (команды/запросы).
- Реализовать аналогичный API в `lab2-car-rental/`.
