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
| 0 | Середовище | разом | ☐ |
| 1 | Каркас (Етап 0 ТЗ) | разом | ☐ |
| 2 | Domain з нуля | разом | ☐ |
| 3 | Infrastructure / EF | Микола | ☐ |
| 4 | Application: CQRS + каркас помилок | Микола | ☐ |
| 5 | Api: `Category` наскрізь | Микола | ☐ |
| 6 | `Product` (+ транзакції) | Олександр | ☐ |
| 7 | Images | Олександр | ☐ |
| 8 | Тести | разом | ☐ |
| 9 | Стабілізація і приймання | разом | ☐ |

---

## Де ми зараз (стан коду)

```
src/Domain/Products/          (порожньо, лише .gitkeep)
src/Domain/Categories/        (порожньо, лише .gitkeep)
src/Domain/Domain.csproj
src/Application/{Categories,Products,ProductImages}/{Commands,Exceptions}/
src/Application/Common/{Behaviours,Interfaces/{Queries,Repositories,Services}}/
src/Infrastructure/{Persistence/{Configurations,Converters,Migrations,Repositories},Services}/
src/Api/{Controllers,Dtos,Filters,Modules/Errors,Settings}/
tests/Tests.Common/Services/, tests/Tests.Data/{Categories,Products}/
tests/Api.Tests.Integration/{Categories,Products,ProductImages}/
```

- **Domain порожній навмисно** (17.10.2026): попередній код видалено, щоб розд. 4 пройти по порядку. Код є в коміті (`git show HEAD:src/Domain/Products/Product.cs`) і в бекапі `%TEMP%\niko-domain-before-reset-20261017-003948\Domain`.
- **`Api` уже містить `Swashbuckle.AspNetCore 10.2.3`** — це те, що потрібно за рішенням вище. Додати треба лише `Microsoft.EntityFrameworkCore.Design`.
- Скелет папок уже задає фічову структуру: `Application/Common/{Behaviours,Interfaces/{Queries,Repositories}}` і `Api/Modules/Errors` — переробляти нічого не треба.
- Папки `Interfaces/Services/`, `ProductImages/`, `Api/Filters/`, `Api/Settings/`, `Infrastructure/Services/` — під зовнішні сервіси й зображення (розд. 7, 10).
- Чого немає: пакетів, DI, `DbContext`, міграцій, конфігурацій EF, будь-якого коду в Application/Api/Infrastructure, тестів, `docker-compose.yml`.

> Практична порада: коли папка перестає бути порожньою — видаляй з неї `.gitkeep`.

---

## Крок 0 — Середовище (≈30 хв, разом)

- [ ] `.NET SDK 10`: `dotnet --version` → `10.0.x`.
- [ ] Docker Desktop запущено: `docker info` → без помилок.
- [ ] Інструмент міграцій: `dotnet tool install --global dotnet-ef` (або `dotnet tool update --global dotnet-ef`).
- [ ] `git config user.name` / `user.email` задані.
- [ ] Каркас збирається: `dotnet build NikoGuitar.slnx --no-incremental` → `0 Warning(s), 0 Error(s)`.

**Готово, коли:** усі команди зелені. Далі будь-яка поломка — це вже твоя зміна.

> **Порт 5432.** Якщо стоїть локальна служба PostgreSQL, вона займе 5432 і контейнер туди не сяде. Або зупини службу, або зміни зовнішній порт у `docker-compose.yml` — див. Додаток B.

---

## Крок 1 — Каркас (= Етап 0 ТЗ, ≈1 день, разом)

Мета: порожній, але **працюючий** застосунок — піднімається, застосовує порожню міграцію, віддає Swagger.

### 1.1 Пакети (розд. 9.2)

- [ ] `Application`: MediatR **12.5.0**, FluentValidation + `.DependencyInjectionExtensions` **12.2.0**, LanguageExt.Core **4.4.9**, `DependencyInjection.Abstractions`.
- [ ] `Infrastructure`: EF Core, EF Core `.Relational`, Npgsql.EntityFrameworkCore.PostgreSQL **10.0.3**, EFCore.NamingConventions **10.0.1**, `Configuration.Abstractions`, `DependencyInjection.Abstractions`.
- [ ] `Api`: **залишити** `Swashbuckle.AspNetCore 10.2.3`; додати `Microsoft.EntityFrameworkCore.Design` (з `PrivateAssets="all"`).
- [ ] Тестові проєкти: `Mvc.Testing` і `Testcontainers.PostgreSql` (Tests.Common), `FluentAssertions` + `xunit.runner.visualstudio` (Api.Tests.Integration).

> **`Design` — у стартовий проєкт.** Пакет потрібен лише для команд `dotnet ef` і ставиться в `Api`, інакше `dotnet ef` не бачить контексту й падає з `Your startup project 'Api' doesn't reference Microsoft.EntityFrameworkCore.Design`. Дублювати в `Infrastructure` не треба.
>
> **Пінінг `Relational`.** `Npgsql 10.0.3` тягне `Relational 10.0.4`; без явного посилання збірка дає `MSB3277`.
>
> **LanguageExt — саме 4.x.** У версії 5 API суттєво змінено, тому лишаємось на 4.4.9.

### 1.2 DI-методи (розд. 9.1, 10)

- [ ] `src/Application/ConfigureApplicationServices.cs` — `AddApplicationServices(this IServiceCollection)`.
- [ ] `src/Infrastructure/ConfigureInfrastructureServices.cs` — `AddInfrastructureServices(this IServiceCollection, IConfiguration)`.
- [ ] `src/Infrastructure/Persistence/ConfigurePersistenceServices.cs` — `AddPersistenceServices(services, configuration)` + приватний `AddRepositories`.
- [ ] `src/Api/Modules/SetupModule.cs` — `SetupServices(this IServiceCollection)`: контролери + CORS.
- [ ] `Program.cs` викликає `SetupServices()`, `AddApplicationServices()`, `AddInfrastructureServices(builder.Configuration)`.

### 1.3 DbContext, конфігурація, міграція

- [ ] `src/Infrastructure/Persistence/ApplicationDbContext.cs` — поки **без** `DbSet` (щоб порожня міграція на 1.4 була справді порожня).
- [ ] `src/Api/appsettings.json` + `appsettings.Development.json`: `ConnectionStrings:DefaultConnection` і `Storage:RootPath`.
- [ ] `.gitignore`: додати `storage/`.
- [ ] `dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Api --output-dir Persistence/Migrations` → `Up`/`Down` **порожні**.
- [ ] `ApplicationDbContextInitialiser` + `Api/Modules/DbModule.cs` (розд. 10), у `Program.cs` — `await app.InitialiseDatabaseAsync();`.
- [ ] `public partial class Program { }` (потрібно тестовому проєкту).
- [ ] `Program.cs`: **прибрати** `AddOpenApi()`/`MapOpenApi()`, замість них `AddEndpointsApiExplorer()` + `AddSwaggerGen()` і `UseSwagger()`/`UseSwaggerUI()` у Development. `UseHttpsRedirection()` прибрати — TLS завершується не застосунком.
- [ ] `src/Api/Properties/launchSettings.json`, профіль `http`: `"launchBrowser": true`, `"launchUrl": "swagger"` — щоб Swagger відкривався сам.

### 1.4 PostgreSQL у Docker

- [ ] `docker-compose.yml` з сервісом `postgres` (+ том, щоб дані виживали перезапуск).
- [ ] `docker compose up -d postgres` → `docker compose ps` показує `running`.

### 1.5 README-скелет

- [ ] Тема й мапінг ролей (розд. 3), запуск API, запуск тестів, примітка про зафіксовані версії, домовленості по Git.

**Готово, коли:**
- [ ] `dotnet build` → 0 warnings, 0 errors.
- [ ] `dotnet run --project src/Api` → у лозі `Applied migration '...InitialCreate'` і `Now listening on: http://localhost:5050`.
- [ ] `/swagger` → 200 і рендериться; `/swagger/v1/swagger.json` → 200.
- [ ] У БД є `__EFMigrationsHistory` (`docker compose exec postgres psql -U postgres -d niko_store -c '\dt'`).

---

## Крок 2 — Domain з нуля (≈1 день, разом)

Форма сутності фіксована розд. 4 ТЗ: приватний конструктор і фабрика `New(...)`, приватні сетери, усі часові мітки — UTC. Папки `src/Domain/{Products,Categories}/` зараз порожні.

### 2.1 Фундамент — strongly-typed ids (розд. 4.1)

- [ ] `src/Domain/Products/ProductId.cs`
- [ ] `src/Domain/Products/ProductImageId.cs`
- [ ] `src/Domain/Categories/CategoryId.cs`

Кожен — `record` довкола `Guid`, з `New()`, `Empty()` і перевизначеним `ToString()` (щоб у повідомленнях про помилки був сам id, а не `ProductId { Value = ... }`).

### 2.2 Value object (розд. 4.6)

- [ ] `src/Domain/Products/ProductSpecs.cs` — незмінний `record`, усі властивості nullable (`Brand`, `Model`, `Color`, `Condition`, `ManufactureYear`), фабрика `Empty()`.

### 2.3 Group — `Category` (розд. 4.3)

- [ ] `src/Domain/Categories/Category.cs` — `Id`, `Name`, `CreatedAt`, колекція `Products`; приватний конструктор, фабрика `New(id, name)`, `CreatedAt` — усередині.

### 2.4 Main — `Product` (розд. 4.2)

- [ ] `src/Domain/Products/Product.cs` — `Id`, `Title`, `Description`, `CreatedAt`, `UpdatedAt?`, `Specs`, колекції `Categories` і `Images`.
- [ ] `UpdateDetails(title, description, specs)` — оновлює `UpdatedAt`.
- [ ] Метод пересинхронізації категорій (напр. `ReplaceCategories(...)`): приводить `Categories` рівно до переданого набору — потрібно для `PUT /products` (розд. 5, #6).

### 2.5 Link — `ProductCategory` (розд. 4.4)

- [ ] `src/Domain/Categories/ProductCategory.cs` — `CategoryId` + `ProductId`, навігаційні властивості; **без сурогатного ключа** (складений PK зробить EF на Кроці 3).

### 2.6 Attachment — `ProductImage` (розд. 4.5)

- [ ] `src/Domain/Products/ProductImage.cs` — усі поля: `Id`, `ProductId`, `OriginalName`, `StoredExtension`, `ContentType`, `SizeBytes` (`long`), `SortOrder` (`int`), `CreatedAt`.

### 2.7 Самоперевірка перед комітом (розд. 4.7)

- [ ] Усі конструктори приватні, створення — лише через `New(...)`.
- [ ] Жодної публічної властивості з `set` — стан міняють тільки доменні методи.
- [ ] Усі часові мітки — `DateTime.UtcNow`.
- [ ] Жодних атрибутів і посилань на ASP.NET чи EF Core в Domain.

> **Де межа перевірок.** Довжини (`3–255`, `3–500`), обов'язковість і унікальність — це валідація Application (FluentValidation, Крок 4). Домен лише не дає створити об'єкт у некоректний стан. Не дублюй перевірки довжин у Domain.

**Готово, коли:** `dotnet build src/Domain` → 0 warnings; у `ProductImage` усі 8 полів розд. 4.5; у `Product` є метод заміни набору категорій; жоден `set` не публічний.

---

## Крок 3 — Infrastructure: EF-конфігурації і перша справжня міграція (≈1 день, Микола)

Усе за розд. 10 ТЗ: набір сутностей наш, форма конфігурацій — з таблиці конвенцій вище.

- [ ] `ApplicationDbContext`: `DbSet` на 4 сутності, `ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())`.
- [ ] `Persistence/Converters/`: `DateTimeUtcConverter` + конвертери `ProductId`/`CategoryId`/`ProductImageId`.
- [ ] `Persistence/Configurations/`: `CategoryConfiguration`, `ProductConfiguration`, `ProductCategoryConfiguration`, `ProductImageConfiguration`.
  - [ ] Явні типи колонок і обов'язковість; довжина рядків **збігається** з правилами валідаторів (розд. 6).
  - [ ] Унікальні індекси: `categories.name`, `products.title`.
  - [ ] Складений PK `(category_id, product_id)` без сурогатного ключа.
  - [ ] Поведінка видалення (розд. 10): `ProductCategory → Product` cascade, `ProductCategory → Category` restrict, `ProductImage → Product` cascade.
  - [ ] `CreatedAt`: `HasConversion(new DateTimeUtcConverter())` + `HasDefaultValueSql("timezone('utc', now())")`.
  - [ ] `ProductSpecs` → `OwnsOne(...).ToJson()`.
- [ ] `ConfigurePersistenceServices`: `NpgsqlDataSourceBuilder` + **`EnableDynamicJson()`** (без цього jsonb не заведеться), `MigrationsAssembly`, `UseSnakeCaseNamingConvention()`, `ConfigureWarnings` з ігноруванням `ManyServiceProvidersCreatedWarning` і `PendingModelChangesWarning`.
- [ ] Замінити порожню міграцію на справжню: `dotnet ef migrations add InitialSchema -p src/Infrastructure -s src/Api --output-dir Persistence/Migrations`; перевірити SQL через `dotnet ef migrations script`.
- [ ] Перевірка «модель зайшла без залишків»: `dotnet ef migrations has-pending-model-changes -p src/Infrastructure -s src/Api` → `No changes have been made to the model since the last migration.`

**Готово, коли:** застосунок стартує, у БД є `products`, `categories`, `product_categories`, `product_images`; `specs` — `jsonb`; вставити дубль `title` напряму в БД не дає унікальний індекс (тобто унікальність тримає сама база, а не лише код застосунку).

---

## Крок 4 — Application: CQRS, валідація, помилки (≈2 дні, Микола)

Тут зосереджені ключові вимоги шару Application (розд. 6, 8, 10). Це найважливіший крок за обсягом логіки.

### 4.1 Контракти (розд. 10)

- [ ] `Common/Interfaces/Queries/` — `ICategoryQueries`, `IProductQueries` (`Option<T>` замість `T?`).
- [ ] `Common/Interfaces/Repositories/` — `ICategoryRepository`, `IProductRepository`; **без** `GetAll` — командам він не потрібен.
- [ ] `Common/Interfaces/Services/` — `IFileStorage`, `INotificationService`.
- [ ] `IUnitOfWork` + `ITransactionScope` (знадобляться на Кроці 6).

### 4.2 Помилки як значення (розд. 8)

- [ ] `Categories/Exceptions/CategoryExceptions.cs`, `Products/Exceptions/ProductExceptions.cs`, `ProductImages/Exceptions/ProductImageExceptions.cs`.
- [ ] Форма — абстрактна база на сутність + конкретні нащадки (`AlreadyExists`, `NotFound`, `Unhandled`). **Їх не кидають, а повертають як значення.**
- [ ] Наші додаткові випадки з розд. 8: `ProductCategoriesNotFoundException` (несе список відсутніх id), `ProductImageOwnerNotFoundException`, `ProductImageNotFoundException`, `ProductImageInvalidFileException`.

### 4.3 Валідація (розд. 6, 10)

- [ ] `Common/Behaviours/ValidationBehaviour.cs` — `IPipelineBehavior`, кидає `ValidationException`.
- [ ] `ConfigureApplicationServices`: `AddValidatorsFromAssembly`, `AddMediatR(c => c.RegisterServicesFromAssembly(...))`, `AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>))`. Ручних реєстрацій обробників і валідаторів бути не повинно.
- [ ] Валідатори: `Name` 3–255, `Title` 3–255, `Description` 3–500, `CategoryIds` — непорожній список.

### 4.4 Зріз `Category`

- [ ] `Categories/Commands/CreateCategoryCommand.cs` — команда + обробник в одному файлі, `IRequest<Either<CategoryException, Category>>`.
- [ ] `Categories/Commands/CreateCategoryCommandValidator.cs`.
- [ ] Обробник: `GetByName` → `MatchAsync(exists => new CategoryAlreadyExistsException(...), () => CreateEntity(...))`; у `CreateEntity` — `try/catch` навколо збереження → `CategoryUnhandledException`.

### 4.5 Ланцюжок оновлення

- [ ] `UpdateCategoryCommand`/handler: `MatchAsync(...)` → `CheckDuplicates(...).BindAsync(_ => UpdateEntity(...))`; `CheckDuplicates` повертає `Either<TError, Unit>`, тому потрібен псевдонім `using Unit = LanguageExt.Unit;` (інакше компілятор плутає його з `MediatR.Unit`).

**Готово, коли:** `dotnet build` зелений; в обробниках немає `throw` (крім `catch → Unhandled`), немає порівнянь з `null`, немає EF-типів; валідатор не пропускає `name` із 2 символів.

---

## Крок 5 — Api: `Category` наскрізь (≈1 день, Микола)

Форму відповідей і кодів задають розд. 5, 8, 10 ТЗ. Перший крок, де щось можна помацати руками.

- [ ] `Api/Dtos/` — `CategoryDto` (`record` + `FromDomainModel`); **без** атрибутів валідації (вони живуть в Application).
- [ ] У мапінгу id — `.Value`: `new(product.Id.Value, ...)`; типізовані id за межі застосунку не виходять.
- [ ] `Api/Modules/Errors/ValidationExceptionFilter.cs` — `ValidationException` → `ValidationProblemDetails`; реєстрація в `SetupModule` через `options.Filters.Add<ValidationExceptionFilter>()`.
- [ ] `Api/Modules/Errors/CategoryErrorFactory.cs` — `ToObjectResult(this CategoryException error)` зі `switch` на всі випадки і `_ => throw new NotImplementedException(...)`.
  - [ ] Тіло — за розд. 8: `ProblemDetails`, `title` береться з класу помилки.
- [ ] `Api/Controllers/CategoriesController` — `ISender` + `ICategoryQueries`; `POST` → `CreatedAtAction(...)`; жодного `try/catch` і жодної перевірки на `null` (усе через `Match<ActionResult<T>>`).

**Готово, коли** (руками через `/swagger`):
- [ ] `POST /categories` з валідною назвою → `201` + `Location`; рядок є в БД.
- [ ] Повторний `POST` → `409` з повідомленням із класу помилки.
- [ ] `POST` з назвою з 2 символів → `400`, у тілі `errors` є повідомлення FluentValidation.
- [ ] `GET /categories` → `200`, відсортовано за назвою; `GET /categories/{id}` → `200`, випадковий `Guid` → `404`.
- [ ] Точка зупину в обробнику + невалідний запит: обробник **не** викликається (валідація зупиняє запит до нього).

---

## Крок 6 — `Product` (≈2–3 дні, Олександр)

- [ ] `IUnitOfWork` + реалізація в `Infrastructure/Persistence/Repositories/` поверх `IDbContextTransaction` (розд. 10).
- [ ] `CreateProduct`: унікальність `title` → `ProductAlreadyExistsException`; перевірка всіх `categoryIds` → `ProductCategoriesNotFoundException(missingIds)`; збереження зв'язків і `specs`; виклик `INotificationService` + заглушка в `Infrastructure/Services/`.
- [ ] `UpdateProduct`: уся операція в одній транзакції (`BeginTransactionAsync` → `CommitAsync` на `Right`, `RollbackAsync` на `Left`/винятку), повна заміна `title`/`description`/`categoryIds`/`specs`, пересинхронізація категорій (розд. 5, #6; розд. 8).
- [ ] `Api/Modules/Errors/ProductErrorFactory.cs` — `ToObjectResult` зі `switch`; випадок `ProductCategoriesNotFoundException` додає `missingCategoryIds` (розд. 5, приклад відповіді `404`).
- [ ] `DeleteProduct` — каскад у БД; прибирання теки сховища буде на Кроці 7.
- [ ] Queries: список (сорт. `createdAt` desc) і деталі; `AsNoTracking()` на читанні — трекінг там не потрібен.
- [ ] `ProductsController` — **`PUT /products` без id у маршруті**, id у тілі (розд. 5); `DELETE` → `NoContent()`.

**Готово, коли:** `POST /products` → `201` + `Location`, у БД товар, зв'язки й jsonb-`specs`; `PUT` з іншим набором категорій реально додає й прибирає зв'язки, `updatedAt` непорожній; `DELETE` → `204`, зв'язки зникли; при помилці всередині транзакції змін у БД немає.

---

## Крок 7 — Images (≈2 дні, Олександр)

- [ ] `LocalFileStorage` у `Infrastructure/Services/`: корінь зі `Storage:RootPath`, лише відносні шляхи, `Directory.CreateDirectory`, **перевірка, що фінальний шлях усередині кореня** (path traversal).
- [ ] Перевірка реального типу за **magic bytes** (JPEG/PNG/WebP), без сторонніх бібліотек; розмір ≤ 5 МБ.
- [ ] `UploadProductImage`: спершу запис у БД і `SaveChanges`, потім файл; при помилці — компенсація. `SortOrder = max + 1` (перший → `0`), `StoredExtension` — з реального типу, не з імені файлу.
- [ ] Роздача байтів із правильним `Content-Type`; пошук зображення **за парою** `{productId, imageId}` (чужий `imageId` → `404`).
- [ ] `DELETE` зображення (БД + файл); `DELETE /products/{id}` додатково **best-effort** прибирає теку `products/{productId}` (невдача → лог + `204`).
- [ ] Налаштування `Storage:RootPath` і необов'язковий `PublicBaseUrl`; `imageUrl` — **відносний** (розд. 7.3); довгий `Cache-Control` + `ETag`.

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
