# N!KO Store

[![CI](https://github.com/HunterDead14/NIKO_STORE/actions/workflows/ci.yml/badge.svg)](https://github.com/HunterDead14/NIKO_STORE/actions/workflows/ci.yml)

REST API модуля «Магазин / каталог» платформи для музикантів **N!KO Store**
(магазин інструментів, барахолка, форум).

> **Про назви.** Продукт зветься **N!KO Store**. У репозиторії лишилися спадкові назви —
> папка `GuitarHub`, цей заголовок з `NIKO_STORE`.
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

> Порт назовні — **5467** (локальна служба PostgreSQL займає 5432), пароль — **6767**.
> Усередині Docker-мережі сервіс `api` підключається до `postgres:5432` — це не змінюється.

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
dotnet test NikoStore.sln
```

58 інтеграційних тестів: 21 обов'язковий сценарій із розд. 11 ТЗ, наші розширення
(порожній каталог, сповіщення при помилці, завеликий і підроблений файл, прибирання теки,
перейменування категорії), фільтрація товарів і контракт помилок.

Тестова інфраструктура:

- `IntegrationTestWebFactory` — `WebApplicationFactory<Program>`, піднімає PostgreSQL у Testcontainers,
  підключає `appsettings.Test.json`, підміняє `INotificationService` in-memory дублікатом
  і `IFileStorage` — тимчасовою текою;
- `BaseIntegrationTest` — готові `Client` і `Context` для підготовки даних і перевірок;
- кожен тестовий клас (`*ControllerTests`, `ProductsFilterTests`) сідить дані в `InitializeAsync`
  і прибирає їх у `DisposeAsync`; дані — з фабрик `*TestData` у `Tests.Data`.

Той самий прогін виконує CI (GitHub Actions) на кожен push і PR у `main`.

## Ендпоінти

| Метод і маршрут | Що робить | Коди |
|---|---|---|
| `GET /categories` | Список категорій (за назвою) | 200 |
| `GET /categories/{id}` | Категорія за id | 200, 404 |
| `POST /categories` | Створити категорію | 201, 400, 409 |
| `PUT /categories` | Перейменувати категорію (id у тілі) | 200, 400, 404, 409 |
| `GET /products` | Список товарів із категоріями (новіші першими) | 200 |
| `GET /products/{id}` | Деталі товару з категоріями, specs і зображеннями | 200, 404 |
| `POST /products/filter` | Фільтр за назвою, описом, specs, роком випуску й датою створення | 200 |
| `POST /products` | Створити товар | 201, 400, 404, 409 |
| `PUT /products` | Повністю замінити товар (id у тілі), у транзакції | 200, 400, 404, 409 |
| `DELETE /products/{id}` | Видалити товар разом зі зв'язками й файлами | 204, 404 |
| `POST /products/{id}/images` | Завантажити зображення (`multipart/form-data`, поле `file`; JPEG/PNG/WebP ≤ 5 МБ) | 201, 400, 404 |
| `GET /products/{id}/images` | Список зображень товару | 200, 404 |
| `GET /products/{id}/images/{imageId}` | Байти зображення (`ETag`, довгий `Cache-Control`) | 200, 304, 404 |
| `DELETE /products/{id}/images/{imageId}` | Видалити зображення | 204, 404 |

Повний опис запитів і відповідей — у Swagger UI (`/swagger`, лише в Development) і в розд. 5 ТЗ.

Зображення зберігаються за шляхом `{productId}/{imageId}{extension}` відносно `Storage:RootPath`.

### Помилки

Усі помилки віддаються у форматі `ProblemDetails` (`application/problem+json`):
бізнес-помилки (404/409) — через фабрики `ToObjectResult`, валідація — `ValidationProblemDetails` (400),
непередбачені винятки — `500` із `traceId`; текст винятку й стек — лише в Development.

## Домовленості по Git

- Коміти — [Conventional Commits](https://www.conventionalcommits.org/): `feat(scope): ...`, `fix`, `test`, `docs`, `ci`, `refactor`;
  scope — модуль (`products`, `categories`, `product-images`, `api`). Одна логічна зміна — один коміт.
- Перед пушем — `git pull --rebase` і зелений `dotnet test`.
- Кожна закрита задача завершується окремим змістовним комітом.
- Зона кожного розробника — розд. 13 ТЗ; чужу зону не чіпати без домовленості.

## Структура

```
NikoStore.sln
src/
  Domain/          сутності, strongly-typed ids, value objects
  Application/     команди, handlers, validators, pipeline behaviours, інтерфейси, помилки, фільтри
  Infrastructure/  DbContext, конфігурації, міграції, репозиторії (репозиторій + queries в одному класі),
                   зовнішні сервіси
  Api/             контролери, DTO, мапінг помилок, settings, composition root;
                   appsettings.json / appsettings.Development.json / appsettings.Test.json
tests/
  Api.Tests.Integration/  *ControllerTests, ProductsFilterTests, ErrorContractTests
  Tests.Common/           IntegrationTestWebFactory, BaseIntegrationTest, test doubles
  Tests.Data/             фабрики тестових даних (*TestData)
```

## Документація

- `TZ_NIKO-STORE.md` — технічне завдання та актуальний опис API.
