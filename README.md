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

## Лаб 2 — локальный запуск

### Клонирование и сборка

```bash
git clone https://github.com/NikitaOrela/OOPLABS_SPRING.git
cd OOPLABS_SPRING

# текущая итерация:
git checkout feature/lab2-business-rules-api

dotnet restore lab2-car-rental/CarRental.sln
dotnet build   lab2-car-rental/CarRental.sln --nologo
dotnet test    lab2-car-rental/CarRental.sln --nologo
```

### Запуск API

```bash
dotnet run --project lab2-car-rental/src/CarRental.Presentation
```

По умолчанию API стартует на `http://localhost:5000` (см. вывод `dotnet run`). Дальше `BASE=http://localhost:5000`.

> Состояние API хранится **в памяти процесса** — после рестарта пользователи, машины и заявки исчезают.

Numeric‑коды enum’ов: `UserRole` — `1=Client`, `2=Manager`, `3=Administrator`. `CarStatus` — `1=Available`, `2=Rented`, `3=UnderMaintenance`. `RentalRequestStatus` — `1=Pending`, `2=Approved`, `3=Rejected`, `4=Completed`.

### Smoke‑сценарий (happy path)

```bash
BASE=http://localhost:5000

# 1. Создаём менеджера (id=1) и клиента (id=2).
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"mgr","fullName":"Manager","age":40,"drivingExperienceYears":10,"roles":[2]}'
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"alice","fullName":"Alice","age":30,"drivingExperienceYears":5,"roles":[1]}'

# 2. Менеджер регистрирует авто (id=1), 120 HP, 50 у.е./день.
curl -s -X POST $BASE/api/cars -H 'Content-Type: application/json' \
  -d '{"managerId":1,"vin":"VIN001","make":"Toyota","model":"Corolla","powerHp":120,"dailyTariff":50}'

# 3. Клиент подаёт заявку на 2026-07-01 .. 2026-07-04.
curl -s -X POST $BASE/api/rentals -H 'Content-Type: application/json' \
  -d '{"clientId":2,"carId":1,"startDate":"2026-07-01","endDate":"2026-07-04"}'

# 4. Менеджер одобряет заявку.
curl -s -X POST $BASE/api/rentals/1/approve -H 'Content-Type: application/json' \
  -d '{"managerId":1}'

# 5. Авто стало Rented, price = 50 * 3 = 150.
curl -s $BASE/api/cars/1
curl -s $BASE/api/rentals/1

# 6. Менеджер закрывает заявку — вернули 2026-07-04, без повреждений.
curl -s -X POST $BASE/api/rentals/1/complete -H 'Content-Type: application/json' \
  -d '{"managerId":1,"actualReturnDate":"2026-07-04","damaged":false}'

# 7. Авто снова Available, заявка Completed, Penalty = 0.
curl -s $BASE/api/cars/1
curl -s $BASE/api/rentals/1
```

### Сценарии с отказом

```bash
# Возраст ≤ 21 → 422 (ClientNotEligibleException)
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"kid","fullName":"Kid","age":19,"drivingExperienceYears":1,"roles":[1]}'        # id=3
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/rentals \
  -H 'Content-Type: application/json' \
  -d '{"clientId":3,"carId":1,"startDate":"2026-08-01","endDate":"2026-08-04"}'                    # 422

# Стаж < 2 лет → 422
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"fresh","fullName":"Fresh","age":25,"drivingExperienceYears":1,"roles":[1]}'    # id=4
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/rentals \
  -H 'Content-Type: application/json' \
  -d '{"clientId":4,"carId":1,"startDate":"2026-08-01","endDate":"2026-08-04"}'                    # 422

# Мощное авто (≥ 250 HP): нужен возраст ≥ 25 и стаж ≥ 5
curl -s -X POST $BASE/api/cars -H 'Content-Type: application/json' \
  -d '{"managerId":1,"vin":"VINMUSCLE","make":"Dodge","model":"Charger","powerHp":300,"dailyTariff":120}'   # car id=2
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"young","fullName":"Young","age":23,"drivingExperienceYears":3,"roles":[1]}'    # id=5
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/rentals \
  -H 'Content-Type: application/json' \
  -d '{"clientId":5,"carId":2,"startDate":"2026-08-01","endDate":"2026-08-04"}'                    # 422

# Авто в UnderMaintenance → 422
curl -s -X POST $BASE/api/cars/2/status -H 'Content-Type: application/json' \
  -d '{"managerId":1,"status":3}'                                                                  # UnderMaintenance
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/rentals \
  -H 'Content-Type: application/json' \
  -d '{"clientId":2,"carId":2,"startDate":"2026-09-01","endDate":"2026-09-04"}'                    # 422

# Перекрытие дат с уже Approved заявкой → 422
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"bob","fullName":"Bob","age":30,"drivingExperienceYears":5,"roles":[1]}'        # id=6
# alice уже арендовала VIN001 на 07-01..07-04 — Approved. Bob запрашивает 07-03..07-06:
curl -s -X POST $BASE/api/cars/1/status -H 'Content-Type: application/json' \
  -d '{"managerId":1,"status":1}'                                                                  # вручную в Available
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/rentals \
  -H 'Content-Type: application/json' \
  -d '{"clientId":6,"carId":1,"startDate":"2026-07-03","endDate":"2026-07-06"}'                    # 422

# Регистрация авто не менеджером → 403
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/cars \
  -H 'Content-Type: application/json' \
  -d '{"managerId":2,"vin":"VINNM","make":"X","model":"Y","powerHp":150,"dailyTariff":50}'        # 403

# Смена статуса авто не менеджером → 403
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/cars/1/status \
  -H 'Content-Type: application/json' \
  -d '{"managerId":2,"status":3}'                                                                  # 403

# Одобрение не менеджером → 403
curl -s -X POST $BASE/api/rentals -H 'Content-Type: application/json' \
  -d '{"clientId":2,"carId":1,"startDate":"2026-10-01","endDate":"2026-10-04"}'                    # id=2 (Pending)
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/rentals/2/approve \
  -H 'Content-Type: application/json' \
  -d '{"managerId":2}'                                                                              # 403

# Дубликат VIN → 409
curl -s -o /dev/null -w "%{http_code}\n" -X POST $BASE/api/cars \
  -H 'Content-Type: application/json' \
  -d '{"managerId":1,"vin":"VIN001","make":"Other","model":"Other","powerHp":100,"dailyTariff":30}'  # 409
```

### Авто‑одобрение для Client + Manager

```bash
# Пользователь со ролями [Client, Manager]:
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"ceo","fullName":"CEO","age":35,"drivingExperienceYears":10,"roles":[1,2]}'

# Заявка, поданная им самим, сразу Approved.
curl -s -X POST $BASE/api/rentals -H 'Content-Type: application/json' \
  -d '{"clientId":7,"carId":1,"startDate":"2026-11-01","endDate":"2026-11-03"}'
```

### Управление ролями (только Administrator)

```bash
# Создаём администратора:
curl -s -X POST $BASE/api/users -H 'Content-Type: application/json' \
  -d '{"userName":"root","fullName":"Root","age":40,"drivingExperienceYears":15,"roles":[3]}'

# Администратор перенастраивает роли alice на [Client, Manager]:
curl -s -X PUT $BASE/api/users/2/roles -H 'Content-Type: application/json' \
  -d '{"administratorId":8,"roles":[1,2]}'

# Не‑администратор не может менять роли → 403
curl -s -o /dev/null -w "%{http_code}\n" -X PUT $BASE/api/users/2/roles \
  -H 'Content-Type: application/json' \
  -d '{"administratorId":1,"roles":[1]}'
```

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
