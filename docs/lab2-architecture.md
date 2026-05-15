# Лабораторная работа 2 — Аренда автомобилей

## Цель

Учебное приложение «Сервис аренды автомобилей» на C# / .NET 8 / ASP.NET Core / Entity Framework / MediatR. Содержит роли **Client**, **Manager**, **Administrator**, доменные сущности и бизнес‑потоки бронирования.

## Слои (Clean Architecture)

- `CarRental.Domain` — сущности (`User`, `Car`, `RentalRequest`), перечисления (`UserRole`, `CarStatus`, `RentalRequestStatus`), доменные исключения, контракты репозиториев.
- `CarRental.Application` — варианты использования: создание/одобрение/отклонение/закрытие заявок, расчёт стоимости и штрафов.
- `CarRental.Infrastructure` — `DbContext` (EF Core), репозитории, миграции. Сейчас — in‑memory заглушки.
- `CarRental.Presentation` — ASP.NET Core Web API.
- `CarRental.Tests` — модульные и интеграционные тесты.

## Доменная модель

- `User`: `Id`, `UserName` (уникальный), `FullName`, `Age`, `DrivingExperienceYears`, `Roles`.
- `Car`: `Id`, `Vin` (уникальный), `Make`, `Model`, `PowerHp`, `DailyTariff`, `Status` (`Available | Rented | UnderMaintenance`).
- `RentalRequest`: `Id`, `ClientId`, `CarId`, `StartDate`, `EndDate`, `Status` (`Pending | Approved | Rejected | Completed`), `Price`, `Penalty`, `CreatedAt`, `ResolvedAt`, `ResolverId`.

## Бизнес‑правила

1. **Уникальность VIN и UserName** — проверяется при создании.
2. **Доступность авто**: нельзя арендовать машину со статусом `Rented` или `UnderMaintenance` на указанные даты; пересечение интервалов запрещено.
3. **Клиент**: возраст > 21, стаж ≥ 2 лет.
4. **Мощные авто** (`PowerHp` выше порога) — дополнительные требования (например, стаж ≥ 5 лет). Порог задаётся конфигурацией.
5. **Авто‑одобрение**: если клиент одновременно `Manager`, заявка переходит в `Approved` автоматически.
6. **Расчёт стоимости**: `price = dailyTariff * days`.
7. **Штрафы**: за повреждения или возврат с опозданием — `Penalty` фиксируется в `RentalRequest` при `CompleteAsync`.
8. Все нарушения — доменные исключения (`CarRentalDomainException` и наследники).

## Сервисы (планируемые)

- `UserService` — регистрация, валидация возраста/стажа.
- `CarService` — CRUD, смена статуса, обслуживание.
- `RentalRequestService` — создание/одобрение/отклонение/завершение заявок, расчёт стоимости/штрафов.

## Тестирование (планируемое)

- Smoke‑тесты enum’ов, доменных сущностей и ценообразования (есть в skeleton).
- Юнит‑тесты `RentalRequestService` на каждое бизнес‑правило (возраст, стаж, мощность, перекрытие дат, авто‑одобрение).
- Интеграционные тесты с `InMemoryCarRepository`, далее — с EF Core.

## TODO для полной реализации

- Подключить EF Core, описать `CarRentalDbContext` и миграции.
- Подключить MediatR, выделить команды/запросы.
- Описать контроллеры `UsersController`, `CarsController`, `RentalsController`.
- Настроить DI в `Program.cs`.
