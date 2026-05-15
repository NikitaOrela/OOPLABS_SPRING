# Лабораторная работа 1 — Библиотека

## Цель

Учебное приложение «Электронная библиотека» на C# / .NET 8 / ASP.NET Core / Entity Framework / MediatR. Содержит роли **Librarian**, **Writer**, **Reader**, доменные сущности и бизнес‑потоки выдачи/возврата книг.

## Слои (Clean Architecture)

- `Library.Domain` — сущности (`User`, `Book`, `BookRequest`), перечисления (`UserRole`, `RequestType`, `RequestStatus`), доменные исключения, контракты репозиториев.
- `Library.Application` — варианты использования (services / MediatR‑команды и запросы): создание/одобрение/отклонение заявок, бизнес‑правила.
- `Library.Infrastructure` — реализация `DbContext` (EF Core), репозиториев, миграции. Сейчас — in‑memory заглушки.
- `Library.Presentation` — ASP.NET Core Web API, контроллеры, DI‑регистрация.
- `Library.Tests` — модульные и интеграционные тесты на xUnit.

## Доменная модель

- `User`: `Id`, `UserName` (уникальный), `FullName`, `Roles` — мультироль (`Librarian | Writer | Reader`).
- `Book`: `Id`, `Title`, `WriterId`, `Circulation` (общий тираж), `AvailableCopies`.
- `BookRequest`: `Id`, `ApplicantId`, `BookId`, `Type` (`Receive | Supply | Return`), `Status`, `Quantity`, `CreatedAt`, `ResolvedAt`, `ResolverId`.

## Бизнес‑правила

1. **Уникальность имени пользователя** — проверяется при создании.
2. **Writer.Supply**: писатель может поставить только собственные книги и в количестве не больше `Circulation`.
3. **Reader.Receive**: читатель может запросить только книги, которых **ранее не брал**.
4. **Мультироль**: один пользователь может одновременно быть `Reader`, `Writer`, `Librarian`.
5. **Авто‑одобрение**: если заявитель сам — `Librarian`, заявка переходит в `Approved` без отдельного действия.
6. **Receive / Return / Supply** — изменяют `AvailableCopies` только при `Approved`.
7. Все нарушения правил выбрасывают доменные исключения (`LibraryDomainException` и наследники).

## Сервисы (планируемые)

- `UserService` — регистрация пользователей и валидация ролей.
- `BookService` — CRUD книг (для писателя — только собственные).
- `BookRequestService` — создание/одобрение/отклонение заявок, проверка правил, обновление `AvailableCopies`.

## Тестирование (планируемое)

- Smoke‑тесты enum’ов и базовых сущностей (есть в skeleton).
- Юнит‑тесты `BookRequestService` для каждого бизнес‑правила.
- Интеграционный тест с `InMemoryUserRepository` на запрет дубликата `UserName`.
- В дальнейшем — интеграционный тест с EF Core In‑Memory / SQLite.

## TODO для полной реализации

- Подключить `Microsoft.EntityFrameworkCore`, описать `LibraryDbContext` и миграции.
- Подключить `MediatR`, перевести сервисы на команды/запросы.
- Описать контроллеры `UsersController`, `BooksController`, `RequestsController`.
- Настроить DI в `Program.cs` (`AddDbContext`, `AddMediatR`, регистрация репозиториев).
