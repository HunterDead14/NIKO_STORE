# План виконання — N!KO Store REST API

**Практичний супутник до [ТЗ](TZ_NIKO-STORE.md).** ТЗ відповідає на питання «що будуємо»; цей файл — «що робити руками зараз, у якому порядку і за якими конвенціями».

- Один крок = одна feature-гілка = один Pull Request. Не переходь до наступного кроку, поки не закритий блок «Готово, коли».
- Розділ ТЗ у дужках (напр. `розд. 4.5`) — куди піти по деталь. Пункт `2.3` — куди подітись усередині цього плану.
- Порядок кроків важливий: Domain → Infrastructure → Application → Api → тести.

---

## Зафіксовані рішення

| Питання | Рішення | Наслідок |
|---|---|---|
| MediatR | **12.5.0** (MIT) | Лог без попереджень про ліцензію. Версії 13+ вимагають ліцензійного ключа — не оновлюємось |
| UI документації | **Swagger UI (Swashbuckle 10.2.3)** на `/swagger` | `AddOpenApi()`/`MapOpenApi()` прибираємо; Scalar не додаємо |
| Конвенції коду | Єдині на весь проєкт (таблиця нижче) | Кроки 1–5 задають каркас, кроки 6–9 його перевикористовують, не вигадуючи нового |

> **ТЗ синхронізовано** (версія 3.3, 17.10.2026): розд. 8 описує помилки як класи-нащадки з абстрактною базою, розд. 9.2 — Swagger UI, розд. 10 — конвенції шарів (DI-файли, `ApplicationDbContext`, `EnableDynamicJson()`, `ConfigureWarnings`, `ConnectionStrings:DefaultConnection`, автоміграція через `DbModule`). Якщо в тексті ТЗ знайдеться щось, що розходиться з цим планом, — правити треба ТЗ, а не код.

## Конвенції коду

| Елемент | Як робимо | Де зафіксовано |
|---|---|---|
| DI-файли | `Application/ConfigureApplicationServices.cs`, `Infrastructure/ConfigureInfrastructureServices.cs`, `Infrastructure/Persistence/ConfigurePersistenceServices.cs` + приватний `AddRepositories` | розд. 9.1, 10 |
| Шлях фічі | `Application/<Entities>/{Commands,Exceptions}` | розд. 9.1 |
| DbContext | `ApplicationDbContext`, `DbSet<...> { get; init; }`, `ApplyConfigurationsFromAssembly` | розд. 10 |
| Конфігурація сутності | `<Entity>Configuration : IEntityTypeConfiguration<T>`, явні типи колонок, `HasIndex(...).IsUnique()` | розд. 10 |
| Час у UTC | `DateTimeUtcConverter : ValueConverter<DateTime, DateTime>` — приводить до UTC в обидва боки | розд. 10 |
| Автоміграція | `ApplicationDbContextInitialiser` + `Api/Modules/DbModule.cs` → `await app.InitialiseDatabaseAsync()` | розд. 10 |
| Ініціалізація Api | `Api/Modules/SetupModule.cs` (контролери + CORS) | розд. 10 |
| Валідація | `ValidationBehaviour<TRequest, TResponse>` кидає `ValidationException` до обробника | розд. 6, 10 |
| Відповідь 400 | `Api/Modules/Errors/ValidationExceptionFilter` → `ValidationProblemDetails` | розд. 6, 10 |
| Помилки як значення | абстрактна база на сутність + нащадки; **не** `throw`, а повертаються; фабрика `ToObjectResult` зі `switch` | розд. 8 |
| Типізований id | `record ProductId(Guid Value)` з `New()`/`Empty()`/`ToString()` | розд. 4.1 |
| Конвертер id | `builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ProductId(x));` | розд. 10 |
| Контролер | `ISender` + `IQueries`, `result.Match<ActionResult<T>>(...)`, без `try/catch` і перевірок на `null` | розд. 8, 10 |

---

## Прогрес

| # | Крок | Хто (розд. 13 ТЗ) | Статус |
|---|---|---|---|
| 0 | Середовище | разом | ☑ |
| 1 | Каркас (Етап 0 ТЗ) | разом | ☑ |
| 2 | Domain з нуля | разом | ☑ |
| 3 | Infrastructure / EF | Микола | ☑ |
| 4 | Application: CQRS + каркас помилок | Микола | ☐ |
| 5 | Api: `Category` наскрізь | Микола | ☑ |
| 6 | `Product` (+ транзакції) | Олександр | ☑ |
| 7 | Images | Олександр | ☑ |
| 8 | Тести | разом | ☐ |
| 9 | Стабілізація і приймання | разом | ☐ |

---

## Де ми зараз (стан коду)

Оновлено **06.11.2026**: Кроки 0–3, 5, 6 і 7 закрито (6–7 перевірено вручну на PostgreSQL у Docker); у Кроці 4 лишився `UpdateCategory` (конфлікт із ТЗ). Наступний — Крок 8 (тести, разом).

```
src/Domain/Products/          Product.cs, ProductId.cs, ProductImage.cs, ProductImageId.cs, ProductSpecs.cs
src/Domain/Categories/        Category.cs, CategoryId.cs, ProductCategory.cs
src/Domain/Domain.csproj

src/Application/              ConfigureApplicationServices.cs
src/Application/Common/       Interfaces/…, Behaviours/ValidationBehaviour.cs
src/Application/Categories/   Commands/CreateCategoryCommand(+Validator).cs, Exceptions/CategoryException.cs
src/Application/ProductImages/Exceptions/ProductImageException.cs
src/Infrastructure/           ConfigureInfrastructureServices.cs
src/Infrastructure/Persistence/{ApplicationDbContext,ApplicationDbContextInitialiser,ConfigurePersistenceServices}.cs
src/Infrastructure/Persistence/Configurations/   {Category,Product,ProductCategory,ProductImage}Configuration.cs
src/Infrastructure/Persistence/Converters/       DateTimeUtcConverter + {Product,Category,ProductImage}IdConverter
src/Infrastructure/Persistence/Queries/          CategoryQueries.cs
src/Infrastructure/Persistence/Repositories/     UnitOfWork.cs, CategoryRepository.cs
src/Infrastructure/Persistence/Migrations/       …InitialSchema (4 таблиці, jsonb, індекси, FK)
src/Api/                      Program.cs, Modules/{SetupModule,DbModule}.cs
src/Api/Controllers/          CategoriesController.cs
src/Api/Dtos/                 CategoryDto.cs (+ CreateCategoryRequest)
src/Api/Modules/Errors/       ValidationExceptionFilter.cs, CategoryErrorFactory.cs
src/Api/Settings/            ApplicationSettings.cs (+ валідатор, SettingsModule)
src/Api/Filters/             (порожньо — Крок 9)

tests/Tests.Common/Services/, tests/Tests.Data/{Categories,Products}/
tests/Api.Tests.Integration/{Categories,Products,ProductImages}/
docker-compose.yml, Dockerfile, .dockerignore
```

- **Кроки 0–1 закрито.** Каркас збирається (0/0), стартує, віддає Swagger UI на `/swagger`.
- **Крок 2 закрито.** Доменний код на місці; Domain не посилається ні на EF Core, ні на ASP.NET.
- **Крок 3 закрито.** EF-модель на 4 сутності, `specs` — `jsonb` через `EnableDynamicJson()`, міграція `InitialSchema` застосована. `has-pending-model-changes` — чисто; дубль `title` відхиляє сама БД.
- **Крок 4 — майже.** Контракти, ієрархії помилок, `ValidationBehaviour` і `CreateCategory` готові; `UpdateCategory` не реалізовано (ТЗ розд. 4.3/5 не передбачають редагування категорій).
- **Крок 5 закрито.** `POST /categories` → 201 + `Location`, дубль → 409, назва з 2 символів → 400 `ValidationProblemDetails`; `GET /categories` (сорт. за назвою) і `GET /categories/{id}` → 200/404 `ProblemDetails`. Перевірено руками через Swagger.
- **PostgreSQL у Docker:** контейнер `NIKO_STORE` (compose-проєкт `niko-store`), порт назовні **5467**, пароль **6767**, БД `niko_store`, користувач `postgres`. Локальна служба PostgreSQL займає 5432, тому порт зсунуто.
- **Відхилення від Кроку 1:** окремого `appsettings.Development.json` немає — уся конфігурація в `appsettings.json`; у README поки немає розділу «домовленості по Git».
- Чого немає: Product-команд і контролера (Крок 6), зображень (Крок 7), тестів (Крок 8).

> Практична порада: коли папка перестає бути порожньою — видаляй з неї `.gitkeep`.

---

## Крок 0 — Середовище (≈30 хв, разом)

- [x] `.NET SDK 10`: `dotnet --version` → `10.0.x`.
- [x] Docker Desktop запущено: `docker info` → без помилок.
- [x] Інструмент міграцій: `dotnet tool install --global dotnet-ef` (або `dotnet tool update --global dotnet-ef`).
- [x] `git config user.name` / `user.email` задані.
- [x] Каркас збирається: `dotnet build NikoGuitar.slnx --no-incremental` → `0 Warning(s), 0 Error(s)`.

**Готово, коли:** усі команди зелені. Далі будь-яка поломка — це вже твоя зміна.

> **Порт 5432.** Якщо стоїть локальна служба PostgreSQL, вона займе 5432 і контейнер туди не сяде. Або зупини службу, або зміни зовнішній порт у `docker-compose.yml` — див. Додаток B.

---

## Крок 1 — Каркас (= Етап 0 ТЗ, ≈1 день, разом)

Мета: порожній, але **працюючий** застосунок — піднімається, застосовує порожню міграцію, віддає Swagger.

### 1.1 Пакети (розд. 9.2)

- [x] `Application`: MediatR **12.5.0**, FluentValidation + `.DependencyInjectionExtensions` **12.2.0**, LanguageExt.Core **4.4.9**, `DependencyInjection.Abstractions`.
- [x] `Infrastructure`: EF Core, EF Core `.Relational`, Npgsql.EntityFrameworkCore.PostgreSQL **10.0.3**, EFCore.NamingConventions **10.0.1**, `Configuration.Abstractions`, `DependencyInjection.Abstractions`.
- [x] `Api`: **залишити** `Swashbuckle.AspNetCore 10.2.3`; додати `Microsoft.EntityFrameworkCore.Design` (з `PrivateAssets="all"`).
- [x] Тестові проєкти: `Mvc.Testing` і `Testcontainers.PostgreSql` (Tests.Common), `FluentAssertions` + `xunit.runner.visualstudio` (Api.Tests.Integration).

> **`Design` — у стартовий проєкт.** Пакет потрібен лише для команд `dotnet ef` і ставиться в `Api`, інакше `dotnet ef` не бачить контексту й падає з `Your startup project 'Api' doesn't reference Microsoft.EntityFrameworkCore.Design`. Дублювати в `Infrastructure` не треба.
>
> **Пінінг `Relational`.** `Npgsql 10.0.3` тягне `Relational 10.0.4`; без явного посилання збірка дає `MSB3277`.
>
> **LanguageExt — саме 4.x.** У версії 5 API суттєво змінено, тому лишаємось на 4.4.9.

### 1.2 DI-методи (розд. 9.1, 10)

- [x] `src/Application/ConfigureApplicationServices.cs` — `AddApplicationServices(this IServiceCollection)`.
- [x] `src/Infrastructure/ConfigureInfrastructureServices.cs` — `AddInfrastructureServices(this IServiceCollection, IConfiguration)`.
- [x] `src/Infrastructure/Persistence/ConfigurePersistenceServices.cs` — `AddPersistenceServices(services, configuration)` + приватний `AddRepositories`.
- [x] `src/Api/Modules/SetupModule.cs` — `SetupServices(this IServiceCollection)`: контролери + CORS.
- [x] `Program.cs` викликає `SetupServices()`, `AddApplicationServices()`, `AddInfrastructureServices(builder.Configuration)`.

### 1.3 DbContext, конфігурація, міграція

- [x] `src/Infrastructure/Persistence/ApplicationDbContext.cs` — поки **без** `DbSet` (щоб порожня міграція на 1.4 була справді порожня).
- [x] `src/Api/appsettings.json` + `appsettings.Development.json`: `ConnectionStrings:DefaultConnection` і `Storage:RootPath`.
- [x] `.gitignore`: додати `storage/`.
- [x] `dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Api --output-dir Persistence/Migrations` → `Up`/`Down` **порожні**.
- [x] `ApplicationDbContextInitialiser` + `Api/Modules/DbModule.cs` (розд. 10), у `Program.cs` — `await app.InitialiseDatabaseAsync();`.
- [x] `public partial class Program { }` (потрібно тестовому проєкту).
- [x] `Program.cs`: **прибрати** `AddOpenApi()`/`MapOpenApi()`, замість них `AddEndpointsApiExplorer()` + `AddSwaggerGen()` і `UseSwagger()`/`UseSwaggerUI()` у Development. `UseHttpsRedirection()` прибрати — TLS завершується не застосунком.
- [x] `src/Api/Properties/launchSettings.json`, профіль `http`: `"launchBrowser": true`, `"launchUrl": "swagger"` — щоб Swagger відкривався сам.

### 1.4 PostgreSQL у Docker

- [x] `docker-compose.yml` з сервісом `postgres` (+ том, щоб дані виживали перезапуск).
- [x] `docker compose up -d postgres` → `docker compose ps` показує `running`.

### 1.5 README-скелет

- [x] Тема й мапінг ролей (розд. 3), запуск API, запуск тестів, примітка про зафіксовані версії, домовленості по Git.

**Готово, коли:**
- [x] `dotnet build` → 0 warnings, 0 errors.
- [x] `dotnet run --project src/Api` → у лозі `Applied migration '...InitialCreate'` і `Now listening on: http://localhost:5050`.
- [x] `/swagger` → 200 і рендериться; `/swagger/v1/swagger.json` → 200.
- [x] У БД є `__EFMigrationsHistory` (`docker compose exec postgres psql -U postgres -d niko_store -c '\dt'`).

---

## Крок 2 — Domain з нуля (≈1 день, разом)

Форма сутності фіксована розд. 4 ТЗ: приватний конструктор і фабрика `New(...)`, приватні сетери, усі часові мітки — UTC. Папки `src/Domain/{Products,Categories}/` зараз порожні.

### 2.1 Фундамент — strongly-typed ids (розд. 4.1)

- [x] `src/Domain/Products/ProductId.cs`
- [x] `src/Domain/Products/ProductImageId.cs`
- [x] `src/Domain/Categories/CategoryId.cs`

Кожен — `record` довкола `Guid`, з `New()`, `Empty()` і перевизначеним `ToString()` (щоб у повідомленнях про помилки був сам id, а не `ProductId { Value = ... }`).

### 2.2 Value object (розд. 4.6)

- [x] `src/Domain/Products/ProductSpecs.cs` — незмінний `record`, усі властивості nullable (`Brand`, `Model`, `Color`, `Condition`, `ManufactureYear`), фабрика `Empty()`.

### 2.3 Group — `Category` (розд. 4.3)

- [x] `src/Domain/Categories/Category.cs` — `Id`, `Name`, `CreatedAt`, колекція `Products`; приватний конструктор, фабрика `New(id, name)`, `CreatedAt` — усередині.

### 2.4 Main — `Product` (розд. 4.2)

- [x] `src/Domain/Products/Product.cs` — `Id`, `Title`, `Description`, `CreatedAt`, `UpdatedAt?`, `Specs`, колекції `Categories` і `Images`.
- [x] `UpdateDetails(title, description, specs)` — оновлює `UpdatedAt`.
- [x] Метод пересинхронізації категорій (напр. `ReplaceCategories(...)`): приводить `Categories` рівно до переданого набору — потрібно для `PUT /products` (розд. 5, #6).

### 2.5 Link — `ProductCategory` (розд. 4.4)

- [x] `src/Domain/Categories/ProductCategory.cs` — `CategoryId` + `ProductId`, навігаційні властивості; **без сурогатного ключа** (складений PK зробить EF на Кроці 3).

### 2.6 Attachment — `ProductImage` (розд. 4.5)

- [x] `src/Domain/Products/ProductImage.cs` — усі поля: `Id`, `ProductId`, `OriginalName`, `StoredExtension`, `ContentType`, `SizeBytes` (`long`), `SortOrder` (`int`), `CreatedAt`.

### 2.7 Самоперевірка перед комітом (розд. 4.7)

- [x] Усі конструктори приватні, створення — лише через `New(...)`.
- [x] Жодної публічної властивості з `set` — стан міняють тільки доменні методи.
- [x] Усі часові мітки — `DateTime.UtcNow`.
- [x] Жодних атрибутів і посилань на ASP.NET чи EF Core в Domain.

> **Де межа перевірок.** Довжини (`3–255`, `3–500`), обов'язковість і унікальність — це валідація Application (FluentValidation, Крок 4). Домен лише не дає створити об'єкт у некоректний стан. Не дублюй перевірки довжин у Domain.

**Готово, коли:** `dotnet build src/Domain` → 0 warnings; у `ProductImage` усі 8 полів розд. 4.5; у `Product` є метод заміни набору категорій; жоден `set` не публічний.

---

## Крок 3 — Infrastructure: EF-конфігурації і перша справжня міграція (≈1 день, Микола)

Усе за розд. 10 ТЗ: набір сутностей наш, форма конфігурацій — з таблиці конвенцій вище.

- [x] `ApplicationDbContext`: `DbSet` на 4 сутності, `ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())`.
- [x] `Persistence/Converters/`: `DateTimeUtcConverter` + конвертери `ProductId`/`CategoryId`/`ProductImageId`.
- [x] `Persistence/Configurations/`: `CategoryConfiguration`, `ProductConfiguration`, `ProductCategoryConfiguration`, `ProductImageConfiguration`.
  - [x] Явні типи колонок і обов'язковість; довжина рядків **збігається** з правилами валідаторів (розд. 6).
  - [x] Унікальні індекси: `categories.name`, `products.title`.
  - [x] Складений PK `(category_id, product_id)` без сурогатного ключа.
  - [x] Поведінка видалення (розд. 10): `ProductCategory → Product` cascade, `ProductCategory → Category` restrict, `ProductImage → Product` cascade.
  - [x] `CreatedAt`: `HasConversion(new DateTimeUtcConverter())` + `HasDefaultValueSql("timezone('utc', now())")`.
  - [x] `ProductSpecs` → `OwnsOne(...).ToJson()`.
- [x] `ConfigurePersistenceServices`: `NpgsqlDataSourceBuilder` + **`EnableDynamicJson()`** (без цього jsonb не заведеться), `MigrationsAssembly`, `UseSnakeCaseNamingConvention()`, `ConfigureWarnings` з ігноруванням `ManyServiceProvidersCreatedWarning` і `PendingModelChangesWarning`.
- [x] Замінити порожню міграцію на справжню: `dotnet ef migrations add InitialSchema -p src/Infrastructure -s src/Api --output-dir Persistence/Migrations`; перевірити SQL через `dotnet ef migrations script`.
- [x] Перевірка «модель зайшла без залишків»: `dotnet ef migrations has-pending-model-changes -p src/Infrastructure -s src/Api` → `No changes have been made to the model since the last migration.`

**Готово, коли:** застосунок стартує, у БД є `products`, `categories`, `product_categories`, `product_images`; `specs` — `jsonb`; вставити дубль `title` напряму в БД не дає унікальний індекс (тобто унікальність тримає сама база, а не лише код застосунку).

---

## Крок 4 — Application: CQRS, валідація, помилки (≈2 дні, Микола)

Тут зосереджені ключові вимоги шару Application (розд. 6, 8, 10). Це найважливіший крок за обсягом логіки.

### 4.1 Контракти (розд. 10)

- [x] `Common/Interfaces/Queries/` — `ICategoryQueries`, `IProductQueries` (`Option<T>` замість `T?`).
- [x] `Common/Interfaces/Repositories/` — `ICategoryRepository`, `IProductRepository`; **без** `GetAll` — командам він не потрібен.
- [x] `Common/Interfaces/Services/` — `IFileStorage`, `INotificationService`.
- [x] `IUnitOfWork` + `ITransactionScope` (уже є; використовуються на Кроці 6).

### 4.2 Помилки як значення (розд. 8)

- [x] `Categories/Exceptions/CategoryException.cs`, `Products/Exceptions/ProductException.cs`, `ProductImages/Exceptions/ProductImageException.cs`. _(імена в однині — узгоджено з наявним `ProductException.cs`)_
- [x] Форма — абстрактна база на сутність + конкретні нащадки (`AlreadyExists`, `NotFound`, `Unhandled`). **Їх не кидають, а повертають як значення.**
- [x] Наші додаткові випадки з розд. 8: `ProductCategoriesNotFoundException` (несе список відсутніх id), `ProductImageOwnerNotFoundException`, `ProductImageNotFoundException`, `ProductImageInvalidFileException`.

### 4.3 Валідація (розд. 6, 10)

- [x] `Common/Behaviours/ValidationBehaviour.cs` — `IPipelineBehavior`, кидає `ValidationException`.
- [x] `ConfigureApplicationServices`: `AddValidatorsFromAssembly`, `AddMediatR(c => c.RegisterServicesFromAssembly(...))`, `AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>))`. Ручних реєстрацій обробників і валідаторів бути не повинно.
- [ ] Валідатори: `Name` 3–255, `Title` 3–255, `Description` 3–500, `CategoryIds` — непорожній список. _(`Name` зроблено; решта — разом із Product-командами, Крок 6)_

### 4.4 Зріз `Category`

- [x] `Categories/Commands/CreateCategoryCommand.cs` — команда + обробник в одному файлі, `IRequest<Either<CategoryException, Category>>`.
- [x] `Categories/Commands/CreateCategoryCommandValidator.cs`.
- [x] Обробник: `GetByName` → `MatchAsync(exists => new CategoryAlreadyExistsException(...), () => CreateEntity(...))`; у `CreateEntity` — `try/catch` навколо збереження → `CategoryUnhandledException`.

### 4.5 Ланцюжок оновлення

- [ ] `UpdateCategoryCommand`/handler: `MatchAsync(...)` → `CheckDuplicates(...).BindAsync(_ => UpdateEntity(...))`; `CheckDuplicates` повертає `Either<TError, Unit>`, тому потрібен псевдонім `using Unit = LanguageExt.Unit;` (інакше компілятор плутає його з `MediatR.Unit`).
  > **Відхилення:** ТЗ розд. 4.3 і 5 не передбачають редагування категорій (лише створення й читання), тому `UpdateCategory` не реалізовано. Пункт лишається відкритим до рішення: прибрати з плану чи додати `PUT /categories/{id}` як розширення.

**Готово, коли:** `dotnet build` зелений; в обробниках немає `throw` (крім `catch → Unhandled`), немає порівнянь з `null`, немає EF-типів; валідатор не пропускає `name` із 2 символів.

---

## Крок 5 — Api: `Category` наскрізь (≈1 день, Микола)

Форму відповідей і кодів задають розд. 5, 8, 10 ТЗ. Перший крок, де щось можна помацати руками.

- [x] `Api/Dtos/` — `CategoryDto` (`record` + `FromDomainModel`); **без** атрибутів валідації (вони живуть в Application).
- [x] У мапінгу id — `.Value`: `new(product.Id.Value, ...)`; типізовані id за межі застосунку не виходять.
- [x] `Api/Modules/Errors/ValidationExceptionFilter.cs` — `ValidationException` → `ValidationProblemDetails`; реєстрація в `SetupModule` через `options.Filters.Add<ValidationExceptionFilter>()`.
- [x] `Api/Modules/Errors/CategoryErrorFactory.cs` — `ToObjectResult(this CategoryException error)` зі `switch` на всі випадки і `_ => throw new NotImplementedException(...)`.
  - [x] Тіло — за розд. 8: `ProblemDetails`, `title` береться з класу помилки.
- [x] `Api/Controllers/CategoriesController` — `ISender` + `ICategoryQueries`; `POST` → `CreatedAtAction(...)`; жодного `try/catch` і жодної перевірки на `null` (усе через `Match<ActionResult<T>>`).

**Готово, коли** (руками через `/swagger`):
- [x] `POST /categories` з валідною назвою → `201` + `Location`; рядок є в БД.
- [x] Повторний `POST` → `409` з повідомленням із класу помилки.
- [x] `POST` з назвою з 2 символів → `400`, у тілі `errors` є повідомлення FluentValidation.
- [x] `GET /categories` → `200`, відсортовано за назвою; `GET /categories/{id}` → `200`, випадковий `Guid` → `404`.
- [x] Точка зупину в обробнику + невалідний запит: обробник **не** викликається (перевірено побічно — `400` і жодного запису в БД).

---

## Крок 6 — `Product` (≈2–3 дні, Олександр)

- [x] `IUnitOfWork` + реалізація в `Infrastructure/Persistence/Repositories/` поверх `IDbContextTransaction` (розд. 10).
- [x] `CreateProduct`: унікальність `title` → `ProductAlreadyExistsException`; перевірка всіх `categoryIds` → `ProductCategoriesNotFoundException(missingIds)`; збереження зв'язків і `specs`; виклик `INotificationService` + заглушка в `Infrastructure/Services/`.
- [x] `UpdateProduct`: уся операція в одній транзакції (`BeginTransactionAsync` → `CommitAsync` на `Right`, `RollbackAsync` на `Left`/винятку), повна заміна `title`/`description`/`categoryIds`/`specs`, пересинхронізація категорій (розд. 5, #6; розд. 8).
- [x] `Api/Modules/Errors/ProductErrorFactory.cs` — `ToObjectResult` зі `switch`; випадок `ProductCategoriesNotFoundException` додає `missingCategoryIds` (розд. 5, приклад відповіді `404`).
- [x] `DeleteProduct` — каскад у БД; прибирання теки сховища буде на Кроці 7.
- [x] Queries: список (сорт. `createdAt` desc) і деталі; `AsNoTracking()` на читанні — трекінг там не потрібен.
- [x] `ProductsController` — **`PUT /products` без id у маршруті**, id у тілі (розд. 5); `DELETE` → `NoContent()`.

**Готово, коли:** `POST /products` → `201` + `Location`, у БД товар, зв'язки й jsonb-`specs`; `PUT` з іншим набором категорій реально додає й прибирає зв'язки, `updatedAt` непорожній; `DELETE` → `204`, зв'язки зникли; при помилці всередині транзакції змін у БД немає.

---

## Крок 7 — Images (≈2 дні, Олександр)

- [x] `LocalFileStorage` у `Infrastructure/Services/`: корінь зі `Storage:RootPath`, лише відносні шляхи, `Directory.CreateDirectory`, **перевірка, що фінальний шлях усередині кореня** (path traversal).
- [x] Перевірка реального типу за **magic bytes** (JPEG/PNG/WebP), без сторонніх бібліотек; розмір ≤ 5 МБ.
- [x] `UploadProductImage`: спершу запис у БД і `SaveChanges`, потім файл; при помилці — компенсація. `SortOrder = max + 1` (перший → `0`), `StoredExtension` — з реального типу, не з імені файлу.
- [x] Роздача байтів із правильним `Content-Type`; пошук зображення **за парою** `{productId, imageId}` (чужий `imageId` → `404`).
- [x] `DELETE` зображення (БД + файл); `DELETE /products/{id}` додатково **best-effort** прибирає теку `products/{productId}` (невдача → лог + `204`).
- [x] Налаштування `Storage:RootPath` і необов'язковий `PublicBaseUrl`; `imageUrl` — **відносний** (розд. 7.3); довгий `Cache-Control` + `ETag`. _(`PublicBaseUrl` — у strongly-typed `Api/Settings/ApplicationSettings`, перевіряється при старті)_

**Готово, коли:** завантажений через Swagger справжній JPEG → `201` з `imageUrl`; `GET` за цим URL віддає байти, які відкриваються як картинка; `.png` з не-PNG вмістом → `400`; `imageId` іншого товару → `404`; видалення товару прибирає теку.

---

## Крок 8 — Тести (≈2–3 дні, разом)

Тести — вимога розд. 11 ТЗ; пишемо їх окремими PR-ами по сутностях, а не одним великим.

- [ ] `Tests.Common/Services/` — in-memory дублікат `INotificationService`.
- [ ] `Tests.Common/` — власний `WebApplicationFactory<Program>`: PostgreSQL у Testcontainers, переспрямований `DbContext`, підмінений `INotificationService`; базовий клас із `HttpClient` і `DbContext`.
- [ ] `Tests.Data/` — фабрики даних (окремо категорії, окремо товари).
- [ ] Категорії — 5 сценаріїв (розд. 11); товари — 10; зображення — 6.
- [ ] Додаткові: порожній каталог → `200 []`; сповіщення **не** надсилається при помилці; завеликий файл → `400`; видалення товару прибирає теку.

**Готово, коли:** `dotnet test` зелений на чистій машині з .NET SDK і запущеним Docker (локальна БД не потрібна); кожен клас сідить дані в `InitializeAsync` і прибирає в `DisposeAsync`.

---

## Крок 9 — Стабілізація і приймання (≈2 дні, разом)

- [ ] Єдиний контракт помилок: `ProblemDetails` для всіх кодів, зокрема `500`; деталі `Unhandled` — лише в Development.
- [ ] CORS, `SetupModule` доведений до підсумкового вигляду, Swagger лише в Development, фінальний README.
- [ ] `docker-compose.yml` з **API + PostgreSQL** (розд. 12, #6); папку сховища змонтувати як volume.
- [ ] Прогін критеріїв розд. 12 і фінальна перевірка архітектурних вимог: у контролерах немає `try/catch` і перевірок на `null`; в інтерфейсах немає `Guid` і `T?`; у проєкті Application немає теки `Services/`; `dotnet ef migrations has-pending-model-changes` чистий.
- [ ] (За наявності часу) CI на `dotnet test`.

---

## Додаток A — Шпаргалка команд

```bash
# збірка (свіжа, як у CI)
dotnet build NikoGuitar.slnx --no-incremental

# тести (потрібен запущений Docker — Testcontainers)
dotnet test NikoGuitar.slnx

# міграції: стартовий проєкт — Api, проєкт міграцій — Infrastructure
dotnet ef migrations add <Name> -p src/Infrastructure -s src/Api --output-dir Persistence/Migrations
dotnet ef migrations script -p src/Infrastructure -s src/Api     # подивитись SQL
dotnet ef migrations has-pending-model-changes -p src/Infrastructure -s src/Api
dotnet ef database update -p src/Infrastructure -s src/Api       # якщо треба вручну

# БД у Docker
docker compose up -d postgres
docker compose exec postgres psql -U postgres -d niko_store -c '\dt'
docker compose logs -f postgres

# запуск API
dotnet run --project src/Api
# → http://localhost:5050/swagger
```

## Додаток B — Часті граблі

| Симптом | Причина і що робити |
|---|---|
| `Your startup project 'Api' doesn't reference Microsoft.EntityFrameworkCore.Design` | Пакет `.Design` мусить бути в `Api` (стартовому проєкті `dotnet ef`), з `PrivateAssets="all"`. |
| `MSB3277` при збірці | `Npgsql 10.0.3` тягне `Relational 10.0.4`. Пінити `Relational` тієї ж версії, що EF Core. |
| jsonb не працює / помилка серіалізації `ProductSpecs` | У `ConfigurePersistenceServices` немає `dataSourceBuilder.EnableDynamicJson()`. |
| `The name 'Unit' ... ambiguous` | `Unit` є і в MediatR, і в LanguageExt. Додати `using Unit = LanguageExt.Unit;`. |
| Шум/попередження EF при старті | `ConfigureWarnings` має ігнорувати `ManyServiceProvidersCreatedWarning` і `PendingModelChangesWarning`. |
| `Failed to determine the https port for redirect` | Прибрати `UseHttpsRedirection()` — TLS завершується не застосунком. |
| Swagger не відкривається | У `Program.cs` лишились `AddOpenApi()`/`MapOpenApi()` замість `AddSwaggerGen()`/`UseSwagger()`. |
| `28P01` / контейнер не стартує | Порт 5432 зайнятий локальною службою PostgreSQL, або пароль у ній не `postgres`. |
| Дані в БД зникають після перезапуску контейнера | Забутий volume. Для `postgres:18-alpine` дефолтний `PGDATA` змінився, тому том монтують на `/var/lib/postgresql` (не на `/var/lib/postgresql/data`). |
| Під час першого запуску в логах `An error occurred using the connection to database ...` | **Нормально**: EF перевіряє, чи є база, не знаходить і одразу створює. |
| Тести «є», але `dotnet test` каже `0 tests` | Невідповідність версії `xunit.runner.visualstudio` до `xunit`; перевірити `Microsoft.NET.Test.Sdk` у тестовому проєкті. |
| `bin/` залочений, збірка падає | Працює `dotnet run`/`dotnet watch` у тому самому проєкті. Зупини процес перед свіжою збіркою. |

## Додаток C — Мапа «ТЗ → крок»

| Розділ ТЗ | Крок |
|---|---|
| 3 Сутності та мапінг ролей | 2, 5 (DTO/README) |
| 4 Доменна модель | 2 |
| 5 Ендпоінти, DTO | 5, 6, 7 |
| 6 Валідація | 4 (валідатори), 5 (фільтр), 7 (файл) |
| 7 Зображення | 7 |
| 8 Помилки → HTTP | 4 (класи), 5 (фабрика), 9 (`500`) |
| 9 Стек і структура | 1 |
| 10 Вимоги до шарів | 1 (DI), 3 (EF), 4 (Application), 5 (Api) |
| 11 Тестування | 8 |
| 12 Критерії приймання | 9 |
| 13 Етапи 0–4 | 1, 2, 4–7, 9 |
| 14 Відповідність вимогам курсу | контрольна перевірка на 9 |

## Додаток D — Що створюється в якому порядку

| Крок | Файли (у порядку появи) |
|---|---|
| 1 | `Application/ConfigureApplicationServices.cs` → `Infrastructure/ConfigureInfrastructureServices.cs` → `Infrastructure/Persistence/ConfigurePersistenceServices.cs` → `Infrastructure/Persistence/ApplicationDbContext.cs` → `ApplicationDbContextInitialiser.cs` → `Api/Modules/SetupModule.cs` → `Api/Modules/DbModule.cs` → `Program.cs` → `appsettings*.json` → міграція `InitialCreate` → `docker-compose.yml` → `README.md` |
| 2 | `ProductId.cs` → `ProductImageId.cs` → `CategoryId.cs` → `ProductSpecs.cs` → `Category.cs` → `Product.cs` → `ProductCategory.cs` → `ProductImage.cs` |
| 3 | `ApplicationDbContext` (DbSet-и) → `Converters/DateTimeUtcConverter.cs` → `Converters/*IdConverter.cs` (×3) → `Configurations/CategoryConfiguration.cs` → `ProductConfiguration.cs` → `ProductCategoryConfiguration.cs` → `ProductImageConfiguration.cs` → міграція `InitialSchema` |
| 4 | `Common/Interfaces/{Queries,Repositories,Services}/…` → `Common/IUnitOfWork.cs` → `Common/Behaviours/ValidationBehaviour.cs` → `Categories/Exceptions/CategoryExceptions.cs` → `Categories/Commands/CreateCategoryCommand.cs` → `CreateCategoryCommandValidator.cs` → `UpdateCategoryCommand.cs` + валідатор → реєстрації в `ConfigureApplicationServices` |
| 5 | `Api/Dtos/CategoryDto.cs` → `Api/Modules/Errors/ValidationExceptionFilter.cs` → `CategoryErrorFactory.cs` → `Api/Controllers/CategoriesController.cs` |
| 6 | `Repositories/UnitOfWork.cs` (+ `TransactionScope`) → `Products/Exceptions/ProductExceptions.cs` → `Products/Commands/{Create,Update,Delete}ProductCommand.cs` + валідатори → `Interfaces/Queries/IProductQueries.cs` → `Persistence/Repositories/ProductRepository.cs` → `Infrastructure/Services/NotificationServiceStub.cs` → `Api/Dtos/ProductDtos.cs` → `Api/Modules/Errors/ProductErrorFactory.cs` → `Api/Controllers/ProductsController.cs` |
| 7 | `Infrastructure/Services/LocalFileStorage.cs` → `ProductImages/Exceptions/ProductImageExceptions.cs` → `ProductImages/Commands/UploadProductImageCommand.cs` + валідатор → `Interfaces/Queries/IProductImageQueries.cs` → `Persistence/Repositories/ProductImageRepository.cs` → `Api/Dtos/ProductImageDto.cs` → `Api/Modules/Errors/ProductImageErrorFactory.cs` → `Api/Controllers/ProductImagesController.cs` |
| 8 | `Tests.Common/Services/InMemoryNotificationService.cs` → `Tests.Common/ApiFactory.cs` → `Tests.Common/IntegrationTestBase.cs` → `Tests.Data/{Categories,Products}/…` → `Api.Tests.Integration/{Categories,Products,ProductImages}/…` |

## Додаток E — Робочий цикл на один крок

1. `git switch -c feat/step-N-<що>`.
2. Пишеш код вузько за одним блоком плану — не більше.
3. Зелений `dotnet build` (і `dotnet test`, коли тести вже є).
4. Коміти атомарні й змістовні: «Add Product domain fields for image metadata», а не «fix»/«update».
5. Pull Request → рев'ю другого розробника → merge.
6. Позначаєш чек-лист кроку тут (і відповідний пункт у розд. 13 ТЗ).
