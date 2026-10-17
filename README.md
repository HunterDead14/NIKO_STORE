# N!KO Store

REST API модуля «Магазин / каталог» платформи для музикантів **N!KO Store**
(магазин інструментів, барахолка, форум).

> **Про назви.** Продукт зветься **N!KO Store**. У репозиторії лишилися спадкові назви —
> solution `NikoGuitar.slnx`, папка `GuitarHub`, цей заголовок з `NIKO_STORE`.
> Перейменування в перший етап не входить; у документації, конфігурації та новому коді
> вживаємо «N!KO Store».

## Мапінг ролей на сутності

| Роль у вимогах | Сутність N!KO Store | Таблиця |
|---|---|---|
| Main (основна) | `Product` — товар | `products` |
| Group (група) | `Category` — категорія | `categories` |
| Link (зв'язок) | `ProductCategory` | `product_categories` |
| Attachment (вкладення) | `ProductImage` — зображення | `product_images` |
| Attributes (атрибути) | `ProductSpecs` (value object, jsonb) | колонка `specs` |

## Технології

ASP.NET Core (.NET 10) · Clean Architecture · MediatR 12.5.0 · FluentValidation 12.2.0 ·
LanguageExt.Core 4.4.9 · EF Core + Npgsql 10.0.3 · PostgreSQL · xUnit + Testcontainers.

> Версії пакетів зафіксовані точно у `.csproj`. Не оновлювати MediatR до 13+ та
> FluentAssertions до 8+ без свідомого рішення — ці версії комерційні.

## Запуск

### 1. База даних (Docker)

```bash
docker compose up -d postgres
```

> Порт назовні — **5433** (локальна служба PostgreSQL займає 5432). Усередині
> Docker-мережі сервіс `api` підключається до `postgres:5432` — це не змінюється.

### 2. API

```bash
dotnet run --project src/Api
# http://localhost:5050/swagger
```

Міграції застосовуються автоматично при старті застосунку.

### Усе разом (API + PostgreSQL у Docker)

```bash
docker compose up -d --build
# http://localhost:5050/swagger
```

## Запуск тестів

Потрібні лише .NET SDK і запущений Docker (Testcontainers сам підіймає PostgreSQL):

```bash
dotnet test NikoGuitar.slnx
```

## Структура

```
src/
  Domain/          сутності, strongly-typed ids, value objects
  Application/     команди, handlers, validators, pipeline behaviours, інтерфейси, помилки
  Infrastructure/  DbContext, конфігурації, міграції, репозиторії, зовнішні сервіси
  Api/             контролери, DTO, фільтри, мапінг помилок, composition root
tests/
  Api.Tests.Integration/
  Tests.Common/    WebApplicationFactory, базовий клас, test doubles
  Tests.Data/      фабрики тестових даних
```

## Документація

- `TZ_NIKO-STORE.md` — технічне завдання.
- `PLAN_NIKO-STORE.md` — покроковий план виконання.
