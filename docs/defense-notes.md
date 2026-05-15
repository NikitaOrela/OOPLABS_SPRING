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

- Роли (`Client`, `Manager`, `Administrator`) — enum, начинается с `1`.
- Статусы: `CarStatus` (`Available`, `Rented`, `UnderMaintenance`), `RentalRequestStatus` (`Pending`, `Approved`, `Rejected`, `Completed`).
- Бизнес‑правила (план):
  - уникальный `Vin` и `UserName`,
  - проверка возраста (> 21) и стажа (≥ 2 лет), повышенные требования для мощных авто,
  - запрет аренды для машин в `Rented` / `UnderMaintenance` и пересечения интервалов дат,
  - авто‑одобрение, если клиент одновременно `Manager`,
  - `price = dailyTariff * days`, штрафы добавляются при `Complete`,
  - нарушения — `CarRentalDomainException` и наследники.

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
