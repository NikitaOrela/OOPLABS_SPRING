# Лабораторная работа 1 — Библиотека

## Цель

Учебное приложение «Электронная библиотека» на C# / .NET 8 / ASP.NET Core / Entity Framework / MediatR. Содержит роли **Librarian**, **Writer**, **Reader**, доменные сущности и бизнес‑потоки выдачи/возврата книг.

## Слои (Clean Architecture)

- `Library.Domain` — сущности (`User`, `Book`, `BookRequest`), перечисления (`UserRole`, `RequestType`, `RequestStatus`), доменные исключения, контракты репозиториев.
- `Library.Application` — варианты использования (services / MediatR‑команды и запросы): создание/одобрение/отклонение заявок, бизнес‑правила.
- `Library.Infrastructure` — реализация `DbContext` (EF Core), репозиториев, миграции. Сейчас — in‑memory заглушки (`InMemoryUserRepository`, `InMemoryBookRepository`, `InMemoryBookRequestRepository`).
- `Library.Presentation` — ASP.NET Core Web API, контроллеры, DI‑регистрация.
- `Library.Tests` — модульные и интеграционные тесты на xUnit.

## Доменная модель

- `User`: `Id`, `UserName` (уникальный), `FullName`, `Roles` — мультироль (`Librarian | Writer | Reader`).
- `Book`: `Id`, `Title`, `WriterId`, `Circulation` (объявленный тираж), `SuppliedCopies` (сколько уже поставлено в библиотеку — растёт при одобрении `Supply`), `AvailableCopies` (доступно для выдачи); вычисляемое `RemainingCirculation = Circulation − SuppliedCopies`.
- `BookRequest`: `Id`, `ApplicantId`, `BookId`, `Type` (`Receive | Supply | Return`), `Status`, `Quantity`, `CreatedAt`, `ResolvedAt`, `ResolverId`.

## Бизнес‑правила и где они реализованы

Все правила сосредоточены в `BookRequestService` (`Library.Application/Services/BookRequestService.cs`). Каждое нарушение приводит к доменному исключению, наследнику `LibraryDomainException`.

1. **Уникальность `UserName`** — проверяется в `InMemoryUserRepository.AddAsync`, бросает `DuplicateUserNameException`.
2. **`Writer.Supply` только своих книг** — `BookRequestService.ValidateSupply` проверяет, что `book.WriterId == applicant.Id` и что заявитель имеет роль `Writer`. Иначе — `UnauthorizedRoleException`.
3. **`Writer.Supply` не более чем `Circulation`** — сумма всех одобренных поставок хранится в `Book.SuppliedCopies`. При создании и при одобрении проверяется `quantity ≤ RemainingCirculation`. Иначе — `WriterSupplyLimitException`.
4. **`Reader.Receive` только ранее не бравшихся книг** — `IBookRequestRepository.ReaderHasEverBorrowedAsync` возвращает `true`, если у читателя есть любой одобренный `Receive` по этой книге (даже если потом был `Return`). Это сохраняет историю выдач без отдельной сущности `Loan` — учебно прозрачно. Иначе — `ReaderAlreadyBorrowedException`.
5. **`Reader.Return`** — допустим только при активном займе; проверка через `IBookRequestRepository.ReaderCurrentlyHoldsAsync` (счётчик одобренных `Receive` минус одобренных `Return` для пары читатель–книга).
6. **Мультироль** — один `User` может иметь несколько `UserRole`. Все проверки используют `HasRole(role)`, что естественным образом покрывает мультироль.
7. **Авто‑одобрение для библиотекаря** — если применяющий имеет роль `Librarian`, при создании заявка сразу переводится в `Approved`, поля `ResolverId`/`ResolvedAt` заполняются им же, а побочные эффекты (изменение `AvailableCopies` / `SuppliedCopies`) применяются немедленно.
8. **Побочные эффекты только при `Approved`** — `BookRequestService.ApplySideEffects` вызывается только в ветке авто‑одобрения и в `ApproveAsync`. `RejectAsync` ничего не меняет на книге.
9. **Идемпотентность одобрения/отклонения** — `LoadForResolutionAsync` проверяет, что заявка ещё `Pending`. Иначе — `RequestAlreadyResolvedException`.
10. **Авторизация резолвера** — `ApproveAsync` и `RejectAsync` требуют, чтобы `librarianId` имел роль `Librarian`.

## Принятые решения этой итерации

- **Отслеживание заимствований без отдельной сущности `Loan`.** Все события «брал/вернул» уже хранятся как `BookRequest`, поэтому отдельный тип `Loan` пока не вводится: правило «не брать ту же книгу повторно» проверяется агрегированием по таблице заявок. Если в будущем потребуются сроки/штрафы — будет добавлена сущность `Loan` со ссылкой на исходный `Receive`‑request.
- **`Book.SuppliedCopies` как явное поле.** В скелете было только `Circulation` (объявленный лимит) и `AvailableCopies` (физически на полке). Это не позволяло корректно ограничить писателя: после возвратов `AvailableCopies` снова растёт, а лимит «не более тиража» — это про всё, что писатель когда‑либо завёз. Введено отдельное поле `SuppliedCopies`, которое монотонно растёт только при одобрении `Supply`.
- **`Quantity = 1` для `Receive` и `Return`.** Читатель берёт/возвращает по одной книге за заявку — это упрощает учёт «брал ранее или нет» и согласуется с реальной практикой выдачи.
- **`IClock` в `Application.Abstractions`.** Время заявки задаётся через интерфейс — это даёт детерминистичные тесты и готовое место для подмены источника времени, когда подключим EF Core.

## Сервисы

- `BookRequestService` — реализован: `CreateAsync`, `ApproveAsync`, `RejectAsync` со всей валидацией бизнес‑правил.
- `UserService` (план) — регистрация пользователей, проверка ролей.
- `BookService` (план) — CRUD книг, ограничение «писатель управляет только своими книгами».

## Тестирование

- `SmokeTests` — enum’ы, мультироль, уникальность `UserName`.
- `BookRequestServiceTests` — 19 сценариев на все бизнес‑правила (успех и провал) каждого из пяти потоков.

## TODO для полной реализации

- Подключить `Microsoft.EntityFrameworkCore`, описать `LibraryDbContext` и миграции; реализовать репозитории поверх EF.
- Подключить `MediatR`, перевести `BookRequestService` на команды/запросы.
- Описать контроллеры `UsersController`, `BooksController`, `RequestsController` и DI в `Program.cs`.
- Интеграционные тесты на EF Core In‑Memory / SQLite.
