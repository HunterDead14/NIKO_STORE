# Технічне завдання — N!KO Store REST API

**Версія 3.1** · 16.10.2026 · Микола, Олександр

---

## 1. Про проєкт

**N!KO Store** — платформа для музикантів, яка поєднує магазин інструментів, барахолку та форум. Спеціалізація на музичному обладнанні та довіра спільноти відрізняють її від звичайних інтернет-магазинів і дошок оголошень.

Це технічне завдання описує **перший етап** — REST API модуля «Магазин / каталог»: товари, категорії, зображення та характеристики. Решта модулів (барахолка, форум, адмін-панель) свідомо винесені за межі, але архітектура проєктується так, щоб додати їх пізніше без переробки ядра.

| Параметр | Значення |
|---|---|
| Проєкт | N!KO Store |
| Мета етапу | REST API каталогу: товари, категорії, зображення, характеристики |
| Платформа | ASP.NET Core (.NET 10), Clean Architecture, PostgreSQL |
| Ролі (в майбутньому) | Admin, Customer |
| Репозиторій | Порожній на старті, інкрементна історія комітів |

> Документ одночасно є й специфікацією, і робочим планом: розділи 1–12 описують «що будуємо», розділи 13–14 — «як і в якому порядку».

---

## 2. Межі роботи

**Входить у перший етап:**

- CRUD-ядро каталогу — категорії та товари.
- Зв'язок «товар ↔ категорія» (many-to-many).
- Характеристики товару як окремий value object (`specs`, jsonb).
- Завантаження та зберігання зображень товару.
- Читання каталогу: список товарів із категоріями та деталі товару.

**Поза межами першого етапу:**

- Автентифікація та авторизація, реальне застосування ролей Admin/Customer.
- Модуль барахолки (оголошення від користувачів).
- Форум (`Topic` / `Post`).
- Адмін-панель.

> **Про безпеку.** Автентифікації на цьому етапі немає, тому всі ендпоінти публічні — зокрема ті, що змінюють дані. Це свідоме й тимчасове обмеження; архітектура дозволяє додати авторизацію пізніше без змін у ядрі.

---

## 3. Сутності та мапінг ролей

Вимоги описують нейтральні ролі (Main, Group, Link, Attachment, Attributes). Нижче — як вони лягають на предметну область N!KO Store.

| Роль у вимогах | Сутність N!KO Store | Таблиця | Маршрут |
|---|---|---|---|
| Main (основна) | `Product` — товар | `products` | `/products` |
| Group (група) | `Category` — категорія | `categories` | `/categories` |
| Link (зв'язок) | `ProductCategory` | `product_categories` | — |
| Attachment (вкладення) | `ProductImage` — зображення | `product_images` | `/products/{id}/images` |
| Attributes (атрибути) | `ProductSpecs` (value object) | колонка `specs` (jsonb) | — |

Отже, проєкт має **4 persisted-сутності** (`Product`, `Category`, `ProductCategory`, `ProductImage`) і **1 owned value object** (`ProductSpecs`) — це відповідає мінімальному складу моделі.

---

## 4. Доменна модель

### 4.1 Strongly-typed ids

`ProductId`, `CategoryId`, `ProductImageId` — це `record`, що обгортає `Guid`, з фабричними методами `New()` та `Empty()`. Порожній id (`Guid.Empty`) вважається невалідним.

### 4.2 Product (Main)

| Поле | Тип | Обмеження |
|---|---|---|
| `Id` | `ProductId` | PK |
| `Title` | `string` | обов'язкове, 3–255, унікальне |
| `Description` | `string` | обов'язкове, 3–500 |
| `CreatedAt` | `DateTime` (UTC) | встановлюється при створенні |
| `UpdatedAt` | `DateTime?` (UTC) | заповнюється в `UpdateDetails(...)` |
| `Specs` | `ProductSpecs` | owned, jsonb |
| `Categories` | `ICollection<ProductCategory>` | many-to-many із `Category` |
| `Images` | `ICollection<ProductImage>` | one-to-many |

### 4.3 Category (Group)

| Поле | Тип | Обмеження |
|---|---|---|
| `Id` | `CategoryId` | PK |
| `Name` | `string` | обов'язкове, ≤ 255, унікальне |
| `CreatedAt` | `DateTime` (UTC) | — |

Категорії на цьому етапі не редагуються й не видаляються — лише створення та читання.

### 4.4 ProductCategory (Link)

Складений первинний ключ `(CategoryId, ProductId)`, без сурогатного ключа.

### 4.5 ProductImage (Attachment)

| Поле | Тип | Примітка |
|---|---|---|
| `Id` | `ProductImageId` | PK |
| `ProductId` | `ProductId` | FK → `products` |
| `OriginalName` | `string` | ім'я файлу від клієнта (тільки для показу) |
| `StoredExtension` | `string` | напр. `.jpg` — для формування шляху |
| `ContentType` | `string` | напр. `image/jpeg` |
| `SizeBytes` | `long` | розмір файлу |
| `SortOrder` | `int` | порядок показу в галереї |
| `CreatedAt` | `DateTime` (UTC) | — |

> Вимога фіксує `Id`, `OriginalName`, `MainId`. Ми додаємо `StoredExtension`, `ContentType`, `SizeBytes`, `SortOrder` і `CreatedAt`, бо без них неможливо ні побудувати шлях `{productId}/{imageId}{extension}`, ні коректно віддати файл клієнту.

### 4.6 ProductSpecs (value object)

Незмінний `record`; усі властивості nullable; фабрика `ProductSpecs.Empty()`. Зберігається як один jsonb-стовпець.

| Поле | Тип | Приклад |
|---|---|---|
| `Brand` | `string?` | Fender |
| `Model` | `string?` | Stratocaster Player |
| `Color` | `string?` | Sunburst |
| `Condition` | `string?` | New / Used |
| `ManufactureYear` | `int?` | 2022 |

### 4.7 Правила домену

- Приватні конструктори; створення — лише через фабрику `New(...)`.
- Приватні сетери; зміна стану — через методи поведінки (`UpdateDetails(...)`), які оновлюють `UpdatedAt`.
- Усі часові мітки — в UTC.
- Фабрики та методи поведінки не мають побічних ефектів поза межами сутності.

---

## 5. API — ендпоінти

Обов'язкові ендпоінти з вимог позначено як **Основа**; додані нами — як **Розширення**.

### Категорії

| # | Метод і маршрут | Тип | Поведінка | Коди |
|---|---|---|---|---|
| 1 | `POST /categories` | Основа | Створити категорію | 201, 400, 409 (назва зайнята) |
| 2 | `GET /categories/{id}` | Основа | Категорія за id | 200, 404 |
| 3 | `GET /categories` | Розширення | Список категорій | 200 |

### Товари

| # | Метод і маршрут | Тип | Поведінка | Коди |
|---|---|---|---|---|
| 4 | `GET /products` | Основа | Список товарів разом із категоріями | 200 |
| 5 | `POST /products` | Основа | Створити товар зі списком `categoryIds` і необов'язковими `specs`; після успіху надіслати сповіщення через `INotificationService` | 201, 400, 404 (перелік відсутніх id категорій), 409 (назва зайнята) |
| 6 | `PUT /products` | Основа | Оновити `title`, `description`, `specs` і пересинхронізувати категорії (додати нові, прибрати зняті). Уся операція — в одній транзакції | 200, 400, 404 (товар або категорію не знайдено), 409 (назва зайнята іншим товаром) |
| 7 | `DELETE /products/{id}` | Основа | Видалити товар; зв'язки та зображення — каскадно | 204, 404 |
| 8 | `GET /products/{id}` | Розширення | Деталі товару (категорії, specs, зображення) | 200, 404 |

> **Про `PUT /products`.** За вимогами маршрут оновлення — `PUT /{items}`, тобто без id у шляху; ідентифікатор товару передається в тілі запиту. Саме в такому вигляді ендпоінт і реалізується.

### Зображення

| # | Метод і маршрут | Тип | Поведінка | Коди |
|---|---|---|---|---|
| 9 | `POST /products/{id}/images` | Основа | Завантажити файл (`multipart/form-data`): спершу запис `ProductImage`, потім збереження через `IFileStorage` за шляхом `{productId}/{imageId}{extension}` | 201, 400, 404 |
| 10 | `GET /products/{id}/images` | Розширення | Список зображень товару | 200, 404 |
| 11 | `DELETE /products/{id}/images/{imageId}` | Розширення | Видалити зображення (БД + файл) | 204, 404 |
| 12 | `GET /products/{id}/images/{imageId}` | Розширення | Віддати байти зображення з правильним `Content-Type` | 200, 404 |

### Приклади запитів і відповідей

`POST /products`
```json
{
  "title": "Fender Stratocaster Player",
  "description": "Електрогітара, вудкорт-корпус, 22 лади.",
  "categoryIds": ["11111111-1111-1111-1111-111111111111"],
  "specs": {
    "brand": "Fender",
    "model": "Stratocaster Player",
    "color": "Sunburst",
    "condition": "New",
    "manufactureYear": 2022
  }
}
```
Відповідь `201 Created`, заголовок `Location: /products/{id}`:
```json
{
  "id": "22222222-2222-2222-2222-222222222222",
  "title": "Fender Stratocaster Player",
  "description": "Електрогітара, вудкорт-корпус, 22 лади.",
  "createdAt": "2026-10-16T10:00:00Z",
  "updatedAt": null,
  "specs": { "brand": "Fender", "model": "Stratocaster Player", "color": "Sunburst", "condition": "New", "manufactureYear": 2022 },
  "categories": [ { "id": "11111111-1111-1111-1111-111111111111", "name": "Електрогітари" } ],
  "images": []
}
```

Приклад відповіді `404`, коли вказано неіснуючі категорії:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "One or more categories were not found",
  "status": 404,
  "missingCategoryIds": ["33333333-3333-3333-3333-333333333333"]
}
```

---

## 6. Правила валідації

| Поле | Правила |
|---|---|
| `Category.Name` | обов'язкове, ≤ 255 символів, унікальне |
| `Product.Title` | обов'язкове, 3–255 символів, унікальне |
| `Product.Description` | обов'язкове, 3–500 символів |
| `Product.CategoryIds` | обов'язкове, не порожнє; усі id існують |
| Будь-який id у маршруті чи тілі | не `Guid.Empty` |
| Файл зображення | обов'язковий, не порожній; ≤ 5 МБ; тип `image/jpeg`, `image/png`, `image/webp` |

- Унікальність перевіряється в command handler і повертається як `AlreadyExists` → `409`.
- Будь-яка помилка валідації дає `400` з тілом `ValidationProblemDetails` — і з DTO-фільтра, і з MediatR-пайплайну. Валідація **ніколи** не дає `500`.
- **Рекомендовано** (не обов'язково за вимогами): додатковий унікальний індекс у БД на `title`/`name` та обробка помилки PostgreSQL `23505`, щоб захиститись від гонки.

---

## 7. Зображення товару

### 7.1 Де фізично лежать файли

Корінь сховища береться з конфігурації (`Storage:RootPath`), лежить **поза репозиторієм** і доданий у `.gitignore`. Шлях файлу формується як `{productId}/{imageId}{extension}`:

```
<Storage:RootPath>\
  products\
    {productId}\
      {imageId}.jpg
      {imageId}.png
```

Папки створює код (`Directory.CreateDirectory`) — вручну нічого не закладається. Оригінальна назва файлу на диску не використовується; вона зберігається лише в БД для показу користувачу.

### 7.2 Що зберігається в БД

`ProductImage` зберігає `Id`, `ProductId`, `OriginalName`, `StoredExtension`, `ContentType`, `SizeBytes`, `SortOrder`, `CreatedAt`.

**Повний URL у БД не зберігається.** Він обчислюється на льоту з `Id`, `ProductId` і базової адреси з конфігурації. Завдяки цьому зміна хоста, порту, домену чи перехід на CDN не вимагають міграцій БД.

### 7.3 Як формується посилання на зображення

- `imageUrl = {PublicBaseUrl}/products/{productId}/images/{imageId}`
- `PublicBaseUrl` береться зі strongly-typed settings-класу.
- DTO зображення повертає `imageUrl` разом із метаданими, тож клієнту не треба знати внутрішню структуру сховища.

### 7.4 Як це виглядає в запитах

**Завантаження** — клієнт пише лише id товару в URL і сам файл у тілі:

```
POST /products/22222222-2222-2222-2222-222222222222/images
Content-Type: multipart/form-data; boundary=----X

------X
Content-Disposition: form-data; name="file"; filename="strat.jpg"
Content-Type: image/jpeg

<бінарні дані файлу>
------X--
```

Клієнт не знає ні шляху, ні `imageId`, ні розширення — усе генерує сервер. Відповідь `201 Created`:

```json
{
  "id": "44444444-4444-4444-4444-444444444444",
  "originalName": "strat.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 245120,
  "sortOrder": 0,
  "imageUrl": "/products/22222222-2222-2222-2222-222222222222/images/44444444-4444-4444-4444-444444444444"
}
```

**Читання** — клієнт іде за отриманим `imageUrl`:

```
GET /products/22222222-2222-2222-2222-222222222222/images/44444444-4444-4444-4444-444444444444
→ 200, Content-Type: image/jpeg, <байти файлу>
```

Повний ланцюжок:

```
клієнт: POST /products/{productId}/images + файл
   ↓
сервер: imageId = Guid.New() → запис у БД → файл на диск
   ↓
клієнт: GET /products/{productId} → отримує images[].imageUrl
   ↓
клієнт: GET imageUrl → отримує самі байти картинки
```

### 7.5 Правила та обмеження

- `IFileStorage`: `Save(path, stream)`, `OpenRead(path)`, `Delete(path)`.
- Порядок у handler: спершу створити запис `ProductImage` і зберегти зміни, потім записати файл; при помилці запису — прибрати запис (компенсація).
- Обмеження: розмір ≤ 5 МБ, білий список типів (jpeg/png/webp), перевірка реального типу, а не лише розширення.
- `OriginalName` санітизується і ніколи не використовується як шлях на диску.
- Захист від path traversal: шлях складається лише з `Guid`-ів, і перевіряється, що результат лежить усередині кореня сховища.
- Зображення незмінні (шлях містить `imageId`), тож віддаються з довгим `Cache-Control` та `ETag`.
- Docker: папку сховища монтуємо як volume, інакше файли зникають при перезапуску контейнера.
- Тести: `IFileStorage` підмінюється дублікатом або використовує тимчасову папку; обов'язковий сценарій перевіряє запис у БД, а не наявність файлу.

---

## 8. Помилки та відображення на HTTP

Функціональний стиль будується на **`LanguageExt.Core`** (власні `Result`/`Maybe` заборонені).

- Пошук однієї сутності повертає `Option<T>`; перевірки `== null` / `!= null` у handler'ах не використовуються.
- Command handler повертає `Either<TError, TResult>`: `Left` — бізнес-помилка, `Right` — успіх.
- Бізнес-помилки не кидаються; виняток з інфраструктури загортається в `Unhandled(exception)`.
- Потік handler'а будується комбінаторами `Match`/`MatchAsync`, `Bind`/`BindAsync`, `Map`/`MapAsync`.
- Оновлення товару виконується в транзакції: `commit` при `Right`, `rollback` при `Left` або винятку.

**Ієрархії помилок:**

- `CategoryError` — `AlreadyExists`, `NotFound`, `Unhandled`
- `ProductError` — `AlreadyExists`, `NotFound`, `CategoriesNotFound(missingIds)`, `Unhandled`
- `ProductImageError` — `ProductNotFound`, `InvalidFile`, `Unhandled`

**Мапінг у HTTP** відбувається в одному місці — extension-метод на кожному типі помилки з вичерпним pattern matching:

| Помилка | HTTP |
|---|---|
| `AlreadyExists` | 409 Conflict |
| `NotFound`, `CategoriesNotFound` | 404 Not Found |
| `InvalidFile` | 400 Bad Request |
| `Unhandled` (деталі — лише в Development) | 500 Internal Server Error |
| Помилка валідації | 400 Bad Request (`ValidationProblemDetails`) |

Усі відповіді з помилками — у форматі `ProblemDetails`, тож контракт помилок єдиний для всіх кодів.

---

## 9. Архітектура та стек

### 9.1 Структура рішення

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

**Правила залежностей:**

- `Domain` — ні від чого не залежить і не містить інфраструктурних пакетів.
- `Application` → `Domain`.
- `Infrastructure` → `Application`, `Domain`.
- `Api` → `Application`, `Infrastructure`.

Кожен шар має власний DI-метод (`AddApplicationServices()`, `AddInfrastructureServices(configuration)`), який викликає `Program.cs`.

### 9.2 Технології

| Область | Технологія | Версія |
|---|---|---|
| Runtime | .NET | 10 |
| Web | ASP.NET Core (controllers, не Minimal API) | — |
| CQRS / Mediator | MediatR | 12.5.0 |
| Валідація | FluentValidation | 12.x |
| Функціональні типи | LanguageExt.Core | 4.4.9 |
| ORM / БД | EF Core + Npgsql | 10.x |
| Іменування | EFCore.NamingConventions (snake_case) | 10.x |
| Документація | Microsoft.AspNetCore.OpenApi + Scalar (Development) | Scalar 2.x |
| Тести | xUnit, WebApplicationFactory, Testcontainers.PostgreSql | Testcontainers 4.x |
| Асерції | FluentAssertions | 7.2.2 |

**Про версії, важливо для відтворюваності:**

- MediatR — **12.5.0** (остання безкоштовна версія). Версія 13+ потребує ліцензійного ключа, тому поки не оновлюємо.
- FluentAssertions — **7.2.2** (безкоштовна). Версія 8+ комерційна; як альтернатива — Shouldly 4.x. Обираємо одну бібліотеку.
- LanguageExt.Core — **4.4.9** (остання стабільна). Лінійка 5.x досі в beta і може зламати API — не беремо.
- Усі пакети піняться точною версією у `.csproj`.

---

## 10. Вимоги до шарів

### Domain

- Приватні конструктори та фабричні методи; приватні сетери; поведінкові методи; UTC.
- `ProductSpecs` — незмінний record із nullable-властивостями та фабрикою `Empty()`.

### Application

- Кожна операція запису — MediatR-команда з власним handler'ом і валідатором.
- `ValidationBehaviour<TRequest, TResponse>` запускає валідатори автоматично через `IPipelineBehavior`.
- Репозиторії для запису (`IProductRepository`, `ICategoryRepository`) і окремі query-інтерфейси для читання (`IProductQueries`, `ICategoryQueries`).
- Зовнішні сервіси за інтерфейсами: `INotificationService` (після створення товару), `IFileStorage` (зображення). Заглушки в Infrastructure допустимі.
- Мапінг помилок на HTTP і DTO — не тут, а в Api.

### Infrastructure

- `IEntityTypeConfiguration<T>` на кожну сутність, `ApplyConfigurationsFromAssembly`.
- snake_case для таблиць і колонок.
- Value converters для всіх strongly-typed ids і конвертер, що гарантує зберігання/читання `DateTime` як UTC.
- `ProductSpecs` → `OwnsOne(...).ToJson(...)` у jsonb.
- Поведінка видалення: `ProductCategory → Product` cascade; `ProductCategory → Category` restrict; `ProductImage → Product` cascade.
- Міграції закомічені та застосовуються автоматично при старті застосунку.
- Connection string читається з конфігурації.

### Api

- Запис — через `ISender`; читання — через query-інтерфейси (напряму або через тонкий сервіс рівня API).
- DTO — `record` з явними `FromDomainModel` / `ToDomainModel`; доменні сутності напряму не серіалізуються.
- `201 Created` із заголовком `Location` і тілом створеного DTO.
- Налаштування прив'язані до strongly-typed settings-класу; налаштований CORS.
- `public partial class Program { }` для тестового проєкту.

---

## 11. Тестування

### Інфраструктура тестів

- Власний `WebApplicationFactory<Program>`: піднімає PostgreSQL у Testcontainers, спрямовує `DbContext` на нього та замінює `INotificationService` in-memory дублікатом.
- Базовий клас дає готові `HttpClient` і `DbContext` для підготовки даних та перевірок.
- Дані — з фабрик у `Tests.Data`; кожен клас сідить їх у `InitializeAsync` і прибирає в `DisposeAsync`, тож тести не залежать один від одного.
- `dotnet test` проходить на чистій машині лише з .NET SDK і Docker, без локальної БД.

### Обов'язкові сценарії

| Сутність | Сценарій | Очікування |
|---|---|---|
| Category | Створення | 201, рядок є в БД |
| Category | Дублікат назви | 409 |
| Category | Невалідні дані | 400 |
| Category | Отримання наявної | 200, коректні дані |
| Category | Отримання невідомої | 404 |
| Product | Список | 200, категорії включено |
| Product | Створення | 201, товар, зв'язки та specs у БД; сповіщення надіслано |
| Product | Невідома категорія | 404 |
| Product | Дублікат назви | 409 |
| Product | Невалідні дані | 400 |
| Product | Оновлення | 200, нові значення в БД, категорії пересинхронізовано |
| Product | Оновлення з невідомою категорією | 404 |
| Product | Оновлення невідомого товару | 404 |
| Product | Видалення | 204, товар, зв'язки й зображення видалено |
| Product | Видалення невідомого | 404 |
| ProductImage | Завантаження до наявного товару | 201, запис у БД |
| ProductImage | Завантаження до невідомого товару | 404 |

**Додатково (наші розширення):** порожній каталог → `200 []`; сповіщення не надсилається при помилці; завеликий файл → `400`; невірний тип файлу → `400`.

---

## 12. Результати та критерії приймання

1. Git-репозиторій зі змістовною інкрементною історією комітів — не один «initial commit».
2. `README.md`: тема й мапінг ролей (розділ 3), запуск API (Docker + `dotnet run`), запуск тестів, примітка про зафіксовані версії.
3. Закомічені міграції EF Core.
4. Жодних реальних секретів — лише локальні дефолти.
5. Усі обов'язкові ендпоінти (розділ 5) і тестові сценарії (розділ 11) працюють, `dotnet test` зелений.
6. **Додатково:** `docker-compose.yml` (API + PostgreSQL) підіймає застосунок однією командою; за наявності часу — CI на `dotnet test`.

---

## 13. Команда та план робіт

Над проєктом працюють двоє: **Микола** і **Олександр**. Базовий принцип — спільний каркас робимо разом, далі кожен веде свій вертикальний зріз (модуль від БД до HTTP), щоб мінімізувати конфлікти в Git.

**Процес (обидва):** feature-гілки, Pull Request із рев'ю другого розробника, атомарні змістовні коміти, зелений `dotnet test` перед мержем.

### Етап 0 — каркас (разом)
- [ ] Solution, 4 шари, DI-методи, правила залежностей.
- [ ] `docker-compose` з PostgreSQL, connection string, порожня міграція.
- [ ] Доменні примітиви: базовий тип помилок, strongly-typed ids.
- [ ] README-скелет, домовленості по Git.

### Етап 1 — Category (Микола)
- [ ] Домен `Category`, конфігурація EF, унікальність.
- [ ] `CreateCategory` — команда, handler, validator на `Either`.
- [ ] Ендпоінти `POST /categories`, `GET /categories`, `GET /categories/{id}`.
- [ ] Тести категорій.

### Етап 2 — Product (Олександр)
- [ ] Домен `Product`, `ProductSpecs`, `ProductCategory`, конфігурація EF.
- [ ] `CreateProduct`, `UpdateProduct` (транзакція), `DeleteProduct`.
- [ ] Ендпоінти списку, деталей і CRUD; `INotificationService` (заглушка).
- [ ] Тести товарів, зокрема «сповіщення надіслано».

### Етап 3 — Зображення (Олександр)
- [ ] `IFileStorage` і реалізація, корінь сховища з конфігурації.
- [ ] `UploadProductImage` з валідацією типу/розміру; роздача файлів.
- [ ] Тести зображень.

### Етап 4 — стабілізація (разом)
- [ ] Єдиний контракт помилок, `ProblemDetails` для `500`.
- [ ] CORS, Scalar, фінальний README.
- [ ] Прогін усього набору тестів і перевірка критеріїв приймання (розділ 12).

> Розподіл — пропозиція; його можна міняти між етапами. Незмінним лишається взаємний code review.

---

## 14. Відповідність вимогам курсу

| Вимога | Як виконано |
|---|---|
| ≥ 4 persisted-сутності + 1 value object | 4 сутності + `ProductSpecs` (розділ 3) |
| Main: Id, Title (unique), Description, CreatedAt, UpdatedAt?, Attributes | `Product` (п. 4.2) |
| Group: Id, Name (unique), CreatedAt | `Category` (п. 4.3) |
| Link: складений PK, без сурогатного ключа | `ProductCategory` (п. 4.4) |
| Attachment: Id, OriginalName, MainId | `ProductImage` (п. 4.5) |
| Attributes: ≥ 3 nullable-властивості, jsonb | `ProductSpecs` (п. 4.6) |
| ≥ 7 ендпоінтів | 7 обов'язкових + 5 розширень (розділ 5) |
| Обов'язковий стек | MediatR, FluentValidation, LanguageExt, EF Core + Npgsql, Scalar, xUnit + Testcontainers (розділ 9) |
| Структура шарів і правила залежностей | Розділ 9.1 |
| Доменні правила (ids, фабрики, UTC, VO) | Розділ 4 |
| Application (CQRS, Either, error hierarchy, Option, repository/query, services) | Розділи 8, 10 |
| Infrastructure (configurations, snake_case, converters, jsonb, delete behaviour, migrations, config) | Розділ 10 |
| Api (ISender, query interfaces, DTO mapping, error→HTTP, ValidationProblemDetails, settings, CORS, Program) | Розділи 8, 10 |
| Правила валідації | Розділ 6 |
| Тестування (WebApplicationFactory, Testcontainers, базовий клас, фабрики даних) | Розділ 11 |
| Обов'язкові тест-сценарії | Розділ 11 |
| Результати (git, README, міграції, без секретів) | Розділ 12 |

---

## 15. Перспектива (поза цим ТЗ)

- Автентифікація з ролями Admin/Customer.
- Модуль барахолки (`Listing` від користувача).
- Форум (`Topic` / `Post`).
- Адмін-панель модерації.
