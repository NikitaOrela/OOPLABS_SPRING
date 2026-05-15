# Заметки к защите лабораторных работ

Документ — конспект к устной защите ЛР1 и ЛР2. Соответствует текущему состоянию решения.

## Технологический стек

- Язык: **C#** (только английские идентификаторы; код соответствует styleguide курса).
- Платформа: **.NET 8**.
- Web: **ASP.NET Core** Web API.
- ORM: **Entity Framework Core** (запланирована; в скелете — in‑memory репозитории).
- CQRS/Mediator: **MediatR** (запланирована).
- Тесты: **xUnit**.

## Архитектура

Используется Clean Architecture с разделением на 4 слоя: `Domain`, `Application`, `Infrastructure`, `Presentation`. Каждый слой — отдельный `.csproj`, зависимости направлены внутрь к `Domain`. Тесты выделены в проект `*.Tests`.

Контракты репозиториев живут в `Domain.Interfaces`, реализация — в `Infrastructure.Persistence`. Это позволяет тестировать `Application` без подключения базы.

## ЛР1: библиотека — ключевые моменты

- Роли (`Librarian`, `Writer`, `Reader`) — enum, начинается с `1` (требование styleguide).
- Сущность `User` поддерживает несколько ролей (коллекция `Roles`, проверка через `HasRole`).
- В `Book` явно отделены три величины: `Circulation` (тираж, объявленный лимит), `SuppliedCopies` (сколько уже завёз писатель — монотонно растёт), `AvailableCopies` (сколько сейчас на полке).
- `BookRequest` — единственный «лог событий»: создание, одобрение, отклонение всех потоков (`Receive`, `Supply`, `Return`) сохраняется как `BookRequest` со своим `Status` и `ResolverId`.

### Реализованные бизнес‑правила в `BookRequestService`

1. Уникальность `UserName` (`InMemoryUserRepository`).
2. Писатель поставляет только свои книги (`book.WriterId == applicant.Id`).
3. Писатель не превышает `Circulation` — проверяется через `RemainingCirculation = Circulation − SuppliedCopies` и на создании, и на одобрении.
4. Читатель не может взять книгу, которую уже брал ранее — `ReaderHasEverBorrowedAsync` смотрит все одобренные `Receive` для пары читатель‑книга.
5. Читатель может вернуть только активно занятую книгу — `ReaderCurrentlyHoldsAsync` сверяет одобренные `Receive` минус одобренные `Return`.
6. Мультироль: `User.Roles` — коллекция, все проверки идут через `HasRole`.
7. Если заявитель имеет роль `Librarian`, заявка создаётся сразу со `Status = Approved`, побочные эффекты применяются немедленно.
8. `AvailableCopies` меняется **только** при `Approved` (создание `Pending` ничего не трогает; `RejectAsync` тоже ничего не меняет).
9. Резолвить заявку (`ApproveAsync` / `RejectAsync`) может только пользователь с ролью `Librarian`; повторное резолвинг даёт `RequestAlreadyResolvedException`.

### Что отвечать на типичные вопросы

- *«Почему отдельного `Loan` нет?»* Все события «брал/вернул» уже сохраняются как `BookRequest`. Правило «не брал раньше» проверяется агрегированием по `BookRequest`. Отдельный `Loan` имеет смысл вводить, когда появятся сроки/штрафы — это следующая итерация.
- *«Почему `SuppliedCopies` отдельно от `AvailableCopies`?»* `AvailableCopies` уменьшается при выдаче и растёт при возврате, поэтому по нему нельзя проверить «не превысил ли писатель тираж». Нужна монотонная величина — `SuppliedCopies`. `RemainingCirculation` — производная (`Circulation − SuppliedCopies`).
- *«Что если применяющий и Reader, и Librarian?»* Заявка авто‑одобряется. В тесте `CreateReceive_ByLibrarianReader_AutoApprovesAndDecrementsCopies` показано, что `AvailableCopies` уменьшается прямо при создании, статус = `Approved`, `ResolverId` равен самому заявителю.
- *«Почему `Receive` и `Return` только на 1 копию?»* Так бизнес‑правило «не брал раньше» однозначно работает по паре (читатель, книга), и тривиально проверять активный займ — счётчики не разъезжаются. Для писательской `Supply` `Quantity` может быть любым положительным до `RemainingCirculation`.

## ЛР2: аренда — ключевые моменты

- Роли (`Client`, `Manager`, `Administrator`) — enum, начинается с `1`. Мультироль поддерживается (`User.Roles` — коллекция, проверка через `HasRole`).
- Статусы: `CarStatus` (`Available`, `Rented`, `UnderMaintenance`), `RentalRequestStatus` (`Pending`, `Approved`, `Rejected`, `Completed`).
- Сущности: `User`, `Car`, `RentalRequest` (см. `docs/lab2-architecture.md`). `RentalRequest.DurationDays = EndDate − StartDate` (полуинтервал, `EndDate > StartDate`).

### Реализованные бизнес‑правила в `RentalRequestService`

1. **Уникальность** `Vin` (InMemoryCarRepository) и `UserName` (InMemoryUserRepository) — `DuplicateVinException` / `DuplicateUserNameException` → 409.
2. **Авторизация ролей**:
   - регистрация авто и смена статуса — только `Manager` (`CarService`);
   - управление ролями (`PUT /api/users/{id}/roles`) — только `Administrator` (`UserService.UpdateRolesAsync`);
   - создание заявки — только `Client`;
   - одобрение/отклонение/завершение — только `Manager`.
3. **Допуск клиента** — `ClientEligibilityPolicy` (Domain). Базовое: `Age > 21` ∧ `DrivingExperienceYears ≥ 2`. Для авто с `PowerHp ≥ 250`: `Age ≥ 25` ∧ `DrivingExperienceYears ≥ 5`. Все пороги — `public const` в одной точке, тесты ссылаются на те же константы.
4. **Доступность авто** проверяется и на создании, и на одобрении: `Car.Status == Available`, иначе `CarNotAvailableException` (422).
5. **Пересечение дат**: полуинтервалы `[s1,e1)` и `[s2,e2)` пересекаются ⇔ `s1 < e2 ∧ s2 < e1`. Блокируют только `Approved` заявки — `Pending`/`Rejected`/`Completed` не держат календарь. Реализовано в `InMemoryRentalRequestRepository.HasOverlapAsync`.
6. **Авто‑одобрение для Client+Manager**: при создании заявки сразу `Status = Approved`, `Price = DailyTariff * DurationDays`, авто → `Rented`, `ResolverId = ClientId`. Тест `CreateRequest_ByClientManager_IsAutoApproved`.
7. **Расчёт цены и штрафов** — `RentalPricing`:
   - `base = DailyTariff * DurationDays`,
   - `damageFee = base * 0.5` при повреждении,
   - `lateFee = DailyTariff * 1.5 * lateDays`.
   Штрафы записываются в `RentalRequest.Penalty` при `CompleteAsync`. Цена считается в момент одобрения (или авто‑одобрения).
8. **Завершение** возвращает авто в `Available`, только если оно сейчас `Rented` — менеджер мог перевести его в `UnderMaintenance` (например, после серьёзной аварии), и мы не должны это сбрасывать.
9. **Повторное резолвинг**: одобрённую/отклонённую/завершённую заявку нельзя одобрить заново — `RentalRequestAlreadyResolvedException` (409).
10. **Завершить можно только `Approved`** — `RentalRequestNotApprovedException` (422).

### Что отвечать на типичные вопросы

- *«Почему мощные авто = 250 HP и 25/5 лет?»* Это deterministic учебное правило. Пороги вынесены в `ClientEligibilityPolicy` как `public const`, чтобы их легко менять и чтобы тесты, документация и сервис ссылались на один источник. Они не претендуют на реальную модель страховых тарифов.
- *«Почему перекрытие дат смотрится только по Approved?»* `Pending` ещё не контракт — две `Pending` заявки на один автомобиль могут уживаться до момента одобрения; кто первый получит `Approve`, того менеджер и оформляет. `Completed`/`Rejected` уже не держат авто.
- *«Что если авто свободно (`Available`), но есть Approved заявка с пересекающимися датами?»* Бывает: завершённый владелец вернул машину раньше срока, но интервал до `EndDate` всё ещё «занят» под другого клиента, чью заявку менеджер уже одобрил. Сервис продолжает блокировать новые заявки на этот промежуток — `HasOverlapAsync` смотрит даты, а не текущий статус машины. Это покрыто тестом `CreateRequest_OverlapsExistingApprovedRental_Throws`.
- *«Зачем `Client + Manager` авто‑одобрение?»* Прямое требование задачи: если запрос подаёт сотрудник в роли клиента, контракт оформляется сразу. Реализовано симметрично паттерну ЛР1 (`Librarian + Reader`).
- *«Где валидация дат?»* `EndDate > StartDate` — `InvalidRentalRequestException` (422). `ActualReturnDate` не может быть раньше `StartDate` — `InvalidRentalRequestException` (422). Прочая валидация (положительные `PowerHp`, `DailyTariff`, `Age`) — `ArgumentOutOfRangeException` (400).
- *«Как состояние сохраняется?»* В памяти процесса. `Singleton`‑регистрация репозиториев в `Program.cs`. EF Core добавим в следующей итерации.

## Соответствие styleguide курса

- Только английские идентификаторы.
- Enum’ы начинаются с `1`.
- Нет `dynamic`, `goto`, LINQ query syntax, кортежей в публичных сигнатурах методов и публичных полях.
- Аргументы валидируются (`ArgumentNullException`, `ArgumentOutOfRangeException`, доменные исключения).
- Кастомные исключения для бизнес‑ошибок (`LibraryDomainException` и наследники).
- Namespace соответствует пути папок.
- Простой читаемый код, без лишней магии.

## План доработки до зачёта

1. Добавить EF Core и DbContext в обе работы, описать миграции.
2. Перевести сервисы на MediatR (команды/запросы).
3. Описать контроллеры и сценарии REST API.
4. Расширить тесты: интеграционные на EF Core, e2e через WebApplicationFactory.
5. Реализовать `BookRequestService`‑аналог в ЛР2 (`RentalRequestService`).
