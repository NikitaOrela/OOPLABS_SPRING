# Лабораторная работа 2 — Аренда автомобилей

## Цель

Учебное приложение «Сервис аренды автомобилей» на C# / .NET 8 / ASP.NET Core. Реализует роли **Client**, **Manager**, **Administrator**, доменные сущности и бизнес‑потоки бронирования: создание заявок, проверка прав клиента, одобрение/отклонение, завершение с расчётом штрафов.

## Слои (Clean Architecture)

- `CarRental.Domain` — сущности (`User`, `Car`, `RentalRequest`), перечисления (`UserRole`, `CarStatus`, `RentalRequestStatus`), доменные исключения, контракты репозиториев, политика допуска клиента (`ClientEligibilityPolicy`).
- `CarRental.Application` — варианты использования: `UserService`, `CarService`, `RentalRequestService`, ценообразование (`RentalPricing`).
- `CarRental.Infrastructure` — in‑memory репозитории (`InMemory*Repository`). EF Core оставлен на будущую итерацию, как и в ЛР1.
- `CarRental.Presentation` — ASP.NET Core Web API: контроллеры `UsersController`, `CarsController`, `RentalRequestsController`, фильтр доменных исключений, DTO и mapping.
- `CarRental.Tests` — xUnit: unit‑тесты сервисов, тесты политики допуска, API‑интеграция через `WebApplicationFactory<Program>`.

## Доменная модель

- `User`: `Id`, `UserName` (уникальный), `FullName`, `Age`, `DrivingExperienceYears`, `Roles`. Поддерживается несколько ролей через коллекцию `Roles` и `HasRole`.
- `Car`: `Id`, `Vin` (уникальный), `Make`, `Model`, `PowerHp`, `DailyTariff`, `Status` (`Available | Rented | UnderMaintenance`).
- `RentalRequest`: `Id`, `ClientId`, `CarId`, `StartDate`, `EndDate` (полуинтервал, `EndDate > StartDate`), `Status` (`Pending | Approved | Rejected | Completed`), `Price`, `Penalty`, `CreatedAt`, `ResolvedAt`, `ResolverId`, `RejectionReason`, `ActualReturnDate`, `Damaged`. Производное поле `DurationDays = EndDate − StartDate` в днях.

`enum`‑ы начинаются с `1` (требование styleguide курса).

## Бизнес‑правила

Все правила сосредоточены в `RentalRequestService` (создание/одобрение/отклонение/завершение) и в `ClientEligibilityPolicy` (допуск клиента). Любое нарушение — типизированное доменное исключение, наследник `CarRentalDomainException`.

1. **Уникальность** `Vin` и `UserName` проверяется при создании (репозитории + сервисы).
2. **Регистрация автомобиля** — только пользователь с ролью `Manager` (`CarService.CreateAsync`). Иначе — `UnauthorizedRoleException` (403).
3. **Смена статуса автомобиля** — только `Manager` (`CarService.UpdateStatusAsync`).
4. **Управление ролями** — только `Administrator` (`UserService.UpdateRolesAsync`); это явный эндпойнт `PUT /api/users/{id}/roles`.
5. **Создание заявки** — только пользователь с ролью `Client`, `EndDate > StartDate`.
6. **Допуск клиента** (`ClientEligibilityPolicy`):
   - базовое: `Age > 21` **и** `DrivingExperienceYears ≥ 2`;
   - повышенное для «мощных» авто (`PowerHp ≥ 250`): `Age ≥ 25` **и** `DrivingExperienceYears ≥ 5`.
   Все пороги — `public const` в `ClientEligibilityPolicy`, чтобы тесты и доки ссылались на единый источник истины.
7. **Доступность авто**: на момент создания и на момент одобрения `Car.Status` должен быть `Available`. Иначе — `CarNotAvailableException` (422).
8. **Перекрытие дат**: запрос блокируется, если для того же `CarId` уже есть `Approved` заявка с пересекающимся полуинтервалом дат. Полуинтервалы `[s1,e1)` и `[s2,e2)` пересекаются ⇔ `s1 < e2 ∧ s2 < e1`. `Pending`, `Rejected`, `Completed` календарь не блокируют.
9. **Авто‑одобрение**: если клиент одновременно имеет роль `Manager`, заявка сразу переводится в `Approved`, рассчитывается `Price`, статус авто — `Rented`. `ResolverId` равен самому заявителю.
10. **Одобрение менеджером** (`ApproveAsync`): повторно проверяются `Car.Status` и перекрытие (состояние мира могло измениться между созданием и одобрением). На одобрении выставляется `Price = DailyTariff * DurationDays`, авто → `Rented`.
11. **Отклонение менеджером** (`RejectAsync`): требуется непустая `Reason`, состояние авто не меняется. Перерешать одобренную/отклонённую/завершённую заявку нельзя — `RentalRequestAlreadyResolvedException` (409).
12. **Завершение** (`CompleteAsync`): принимает `ActualReturnDate` и `Damaged`. Считает штрафы (`RentalPricing.CalculatePenalty`) и возвращает авто в `Available`, если оно было `Rented` (менеджер мог перевести его в `UnderMaintenance` — тогда не трогаем).
13. **Расчёт стоимости и штрафов** (`RentalPricing`):
    - `base = DailyTariff * planned days`;
    - `damageFee = base * 0.5` при `damaged = true`;
    - `lateFee = DailyTariff * 1.5 * lateDays` при `ActualReturnDate > EndDate`;
    - `Penalty = damageFee + lateFee` (записывается в `RentalRequest.Penalty`).
14. Резолвить (`Approve` / `Reject` / `Complete`) может только пользователь с ролью `Manager`.

## Слой Presentation (HTTP)

| Метод | Путь | Описание |
|------|------|---------|
| `POST` | `/api/users` | Создать пользователя (`Client`/`Manager`/`Administrator`, можно несколько ролей). |
| `GET` | `/api/users/{id}` | Получить пользователя. |
| `PUT` | `/api/users/{id}/roles` | Заменить роли пользователя — только `Administrator`. |
| `POST` | `/api/cars` | Зарегистрировать автомобиль — только `Manager`. |
| `GET` | `/api/cars/{id}` | Получить автомобиль. |
| `POST` | `/api/cars/{id}/status` | Сменить статус автомобиля — только `Manager`. |
| `POST` | `/api/rentals` | Создать заявку на аренду — только `Client`. Авто‑одобряется для `Client + Manager`. |
| `GET` | `/api/rentals/{id}` | Получить заявку. |
| `POST` | `/api/rentals/{id}/approve` | Одобрить заявку — только `Manager`. |
| `POST` | `/api/rentals/{id}/reject` | Отклонить заявку с причиной — только `Manager`. |
| `POST` | `/api/rentals/{id}/complete` | Завершить аренду (актуальная дата + флаг повреждения) — только `Manager`. |

`DomainExceptionFilter` переводит исключения в HTTP‑коды:

| Исключение | HTTP |
|-----------|------|
| `UserNotFoundException`, `CarNotFoundException`, `RentalRequestNotFoundException` | 404 |
| `DuplicateUserNameException`, `DuplicateVinException`, `RentalRequestAlreadyResolvedException` | 409 |
| `UnauthorizedRoleException` | 403 |
| `ClientNotEligibleException`, `CarNotAvailableException`, `InvalidRentalRequestException`, `RentalRequestNotApprovedException` | 422 |
| `CarRentalDomainException` (база) | 400 |
| `ArgumentException` / `ArgumentOutOfRangeException` | 400 |

## In‑memory хранилище — оговорки

Все три репозитория (`InMemoryUserRepository`, `InMemoryCarRepository`, `InMemoryRentalRequestRepository`) хранят данные в памяти процесса. `Program.cs` регистрирует их как `Singleton`, чтобы состояние выживало между запросами в рамках одного запуска API; после рестарта данные исчезают. EF Core добавим в следующей итерации.

## Тестирование

- `RentalRequestServiceTests` — 24 теста на все бизнес‑правила: возраст / стаж, мощные авто, статус авто, перекрытие дат, авто‑одобрение, корректные побочные эффекты на `Car`, штрафы за damage / late return.
- `CarServiceTests`, `UserServiceTests` — права (`Manager`, `Administrator`), уникальность.
- `CarRentalApiTests` — интеграционные тесты на `WebApplicationFactory<Program>`: full happy path, 409 на дубликаты, 403 на запрещённые действия, 422 на возраст / стаж / недоступное авто / перекрытие дат, авто‑одобрение для гибридной роли.
- `SmokeTests` — базовые проверки `enum`, доменных сущностей, пересчёта `DurationDays` и `RentalPricing.Calculate`. Сохранены без изменений.

Запуск:
```bash
dotnet build lab2-car-rental/CarRental.sln --nologo
dotnet test  lab2-car-rental/CarRental.sln --nologo
```
