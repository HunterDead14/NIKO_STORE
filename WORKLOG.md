# Журнал робіт — N!KO Store

Над проєктом паралельно працюють кілька розробників. Щоб не робити ту саму роботу двічі й не ламати чуже, **кожна виконана задача записується сюди**.

**Правила:**
- Перед початком роботи: `git pull`, прочитати цей файл, [ТЗ](TZ_NIKO-STORE.md) і [план](PLAN_NIKO-STORE.md).
- Після кожної задачі: додати запис **у кінець** розділу «Записи» (дата, хто, що, коміти, файли, що лишилось).
- Зону іншого розробника (розд. 13 ТЗ) не чіпати без домовленості.

---

## Поточний стан (оновлюється з кожним записом)

| Крок плану | Хто | Стан |
|---|---|---|
| 0–2 Середовище, каркас, Domain | разом | готово |
| 3 Infrastructure / EF (конфігурації, конвертери, міграція) | Микола | готово (06.11) |
| 4 Application: CQRS, `ValidationBehaviour`, помилки | Микола | майже: відкрите питання `UpdateCategory` (див. план) |
| 5 Api: `Category` наскрізь | Микола | готово (06.11) |
| 6 `Product` | Олександр | готово (06.11), перевірено вручну на PostgreSQL |
| 7 Images | Олександр | готово (06.11), перевірено вручну; `PublicBaseUrl` — 11.11 |
| 8 Тести | разом | почато: дублікат сповіщень і фабрики даних (11.11); далі `WebApplicationFactory` + Testcontainers, базовий клас, 21 сценарій |
| 9 Стабілізація | разом | частково: strongly-typed settings (11.11) |

Останній коміт коду, на якому базується журнал: `8b9a7c6`.

---

## Записи

### 2026-10-15 · Олександр · Початкова структура й доменні моделі

- Коміт `d37e9c4` — структура рішення: тестові проєкти `Api.Tests.Integration`, `Tests.Common`, `Tests.Data` (додано в `NikoGuitar.slnx`, папка `/tests/`), порожні папки шарів із `.gitkeep`, доменні моделі за першою версією ТЗ.
- Пізніше Domain переписав Микола (`6426e24`, `9bb4535`) — актуальна версія його.

### 2026-10-17 · Олександр · Версія пакета

- Коміт `6cb7068` — `Microsoft.Extensions.DependencyInjection.Abstractions` в `src/Application/Application.csproj`: 10.0.11 → 10.0.13 (як в Infrastructure і ТЗ 9.2).

### 2026-10-17 · Олександр · `IUnitOfWork` (Крок 6)

- Коміт `16198c4`.
- `src/Application/Common/Interfaces/IUnitOfWork.cs`, `ITransactionScope.cs` — точно за ТЗ розд. 10.
- `src/Infrastructure/Persistence/Repositories/UnitOfWork.cs` — реалізація поверх `IDbContextTransaction`; EF-тип в Application не протікає; dispose без `CommitAsync` → rollback.
- Реєстрація: `services.AddScoped<IUnitOfWork, UnitOfWork>()` у `ConfigurePersistenceServices.AddPersistenceServices` (поруч з `ApplicationDbContextInitialiser`, **не** в `AddRepositories`).

### 2026-10-17 · Олександр · `INotificationService` (Крок 6)

- Коміт `4fcf308`.
- `src/Application/Common/Interfaces/Services/INotificationService.cs` — `Task SendProductCreatedAsync(Product product, CancellationToken ct)`.
- `src/Infrastructure/Services/LoggingNotificationService.cs` — заглушка, пише в лог.
- Реєстрація: `AddScoped` у `ConfigureInfrastructureServices`. У тестах підмінити in-memory дублікатом (ТЗ розд. 11).

### 2026-10-17 · Олександр · `IFileStorage` (Крок 7)

- Коміт `068df67`.
- `src/Application/Common/Interfaces/Services/IFileStorage.cs` — `SaveAsync`, `OpenReadAsync` (→ `Option<Stream>`), `DeleteAsync`, `DeleteDirectoryAsync`; усі шляхи відносні.
- `src/Infrastructure/Services/LocalFileStorage.cs` — корінь із `Storage:RootPath`, `Directory.CreateDirectory`, захист від path traversal (абсолютні шляхи й вихід за корінь → `ArgumentException`).
- Реєстрація: `AddSingleton<IFileStorage>(new LocalFileStorage(...))` у `ConfigureInfrastructureServices`; без `Storage:RootPath` застосунок не стартує.
- Перевірено окремою консольною програмою: запис/читання, відсутній файл → `None`, видалення теки, блокування `../`, `C:/...`, порожнього шляху.

### 2026-10-20 · Олександр · Помилки товару (Крок 6)

- Коміт `7f7b72d`.
- `src/Application/Products/Exceptions/ProductException.cs` — абстрактна база `ProductException(ProductId, message, inner?)` + `ProductAlreadyExistsException`, `ProductNotFoundException`, `ProductCategoriesNotFoundException` (`MissingCategoryIds`), `ProductUnhandledException` (ТЗ розд. 8). Не кидаються — повертаються як `Left`.

### 2026-10-20 · Олександр · Контракти репозиторію й запитів товару (Крок 6)

- Коміт `9f738e1`.
- `src/Application/Common/Interfaces/Repositories/IProductRepository.cs` — `GetByIdAsync`, `GetByTitleAsync` (→ `Option<Product>`), `AddAsync`, `UpdateAsync`, `DeleteAsync`.
- `src/Application/Common/Interfaces/Queries/IProductQueries.cs` — `GetAllAsync` (з категоріями, `CreatedAt` desc), `GetByIdAsync` (→ `Option<Product>`, з категоріями й зображеннями).
- Реалізацій ще немає — чекають EF-конфігурацій (Крок 3, Микола).

**Далі по зоні Олександра:** див. останній запис.

### 2026-11-06 · Олександр · Реалізації репозиторію й запитів товару (Крок 6)

- Коміт `22565e4`.
- `src/Infrastructure/Persistence/Repositories/ProductRepository.cs`:
  - `GetByIdAsync` — **з трекінгом** і `Include(Categories)`: під `UpdateProduct`, щоб `ReplaceCategories` + `SaveChanges` самі додали/прибрали зв'язки;
  - `GetByTitleAsync` — `AsNoTracking`;
  - `UpdateAsync` лише викликає `SaveChanges` (товар уже відстежується після `GetByIdAsync`); `DeleteAsync` — `Remove` + `SaveChanges`, решту чистить каскад у БД.
- `src/Infrastructure/Persistence/Queries/ProductQueries.cs` — `AsNoTracking`; список з категоріями (`ThenInclude(Category)`), `CreatedAt` desc; деталі — категорії + зображення, `AsSplitQuery`.
- Реєстрація в `ConfigurePersistenceServices.AddRepositories`.
- На реальній БД не перевірено (Docker не був запущений) — перевірити разом з `ProductsController`.

### 2026-11-06 · Олександр · Команда `CreateProduct` (Крок 6)

- Коміт `123bd51`.
- `src/Application/Products/Commands/CreateProductCommand.cs` — `CreateProductCommand(Title, Description, IReadOnlyList<Guid> CategoryIds, ProductSpecs? Specs)` → `Either<ProductException, Product>`. Потік як у `CreateCategory`: `GetByTitle` → `MatchAsync` (`Some` → `ProductAlreadyExistsException`; `None` → перевірка категорій через `ICategoryQueries.GetExistingIdsAsync` → `BindAsync` → створення). Відсутні категорії → `ProductCategoriesNotFoundException(ProductId.Empty(), missingIds)`. Дублікати в `CategoryIds` прибираються; `Specs == null` → `ProductSpecs.Empty()`. Після збереження — `INotificationService.SendProductCreatedAsync`; виняток → `ProductUnhandledException`, сповіщення не надсилається.
- `CreateProductCommandValidator.cs` — `Title` 3–255, `Description` 3–500, `CategoryIds` непорожній, жоден id ≠ `Guid.Empty` (розд. 6).
- Перевірено на фейкових репозиторіях: успіх (зв'язки, specs, сповіщення), дубль назви, невідома категорія (правильний `missingIds`), падіння БД (без сповіщення), валідатор.

**Далі по зоні Олександра:** див. останній запис.

### 2026-11-06 · Олександр · Крок 6 завершено: `UpdateProduct`, `DeleteProduct`, Api товарів

- `6164485` — `UpdateProductCommand` (+ валідатор): уся операція в транзакції через `IUnitOfWork` — `CommitAsync` на `Right`, `RollbackAsync` на `Left` і на винятку. Назва конфліктує лише з **іншим** товаром (`Filter(other => other.Id != product.Id)`). Повна заміна полів, `Specs == null` → `Empty()`, `ReplaceCategories` пересинхронізує зв'язки. Перевірку категорій винесено в `Products/Commands/ProductCategoriesCheck.cs` (спільна для Create/Update).
- `2010480` — `DeleteProductCommand` (+ валідатор): каскад у БД, потім best-effort `DeleteDirectoryAsync("products/{id}")` — невдача лише `LogWarning`. Додано `Application/ProductImages/ProductImagePaths.cs` — єдине місце формування шляхів у сховищі. В Application додано пакет `Microsoft.Extensions.Logging.Abstractions 10.0.13`.
- `351889e` — `Api/Dtos/ProductDto.cs` (`ProductSpecsDto`, `ProductListItemDto`, `ProductDetailsDto`, `CreateProductRequest`, `UpdateProductRequest`), `Api/Dtos/ProductImageDto.cs` (відносний `imageUrl`), `Api/Modules/Errors/ProductErrorFactory.cs` (404 з `missingCategoryIds`).
- `5fedaef` — `ProductsController`: `GET /products`, `GET /products/{id}`, `POST`, `PUT /products` (id у тілі), `DELETE /products/{id}`. Після `POST`/`PUT` відповідь перечитується через `IProductQueries`, щоб `categories` мали назви.

### 2026-11-06 · Олександр · Крок 7 завершено: зображення

- `7f22e46` — `Application/ProductImages/ImageFileInspector.cs`: тип за magic bytes (JPEG `FF D8 FF`, PNG `89 50 4E 47 0D 0A 1A 0A`, WebP `RIFF`…`WEBP`@8), порожній / > 5 МБ / не з білого списку → `ProductImageInvalidFileException`. Читає не більше 5 МБ + 1 байт.
- `52509f5` — `IProductImageRepository` / `IProductImageQueries` + реалізації (пошук лише за парою `{productId, imageId}`, `GetNextSortOrderAsync` = max + 1, перше → 0); `IProductQueries.ExistsAsync`.
- `7c17160` — `UploadProductImageCommand` (спершу запис у БД, потім файл; помилка файлу → компенсація: видалення запису), санітизація `OriginalName`; `DeleteProductImageCommand` (БД, потім best-effort файл); валідатори; `ProductImageOwnerCheck`.
- `8b9a7c6` — `ProductImagesController`: `POST /products/{id}/images` (multipart, поле `file`), `GET .../images`, `GET .../images/{imageId}` (байти, `Content-Type`, `Cache-Control: immutable`, `ETag` → `304`), `DELETE .../images/{imageId}`; `ProductImageErrorFactory`.

**Ручна перевірка (PostgreSQL у Docker, `dotnet run`), усе пройшло:** порожній каталог `[]`; `POST` товару → 201 + `Location`, specs і категорії з назвами; дубль → 409; невідома категорія → 404 з `missingCategoryIds`; невалідні дані → 400 `ValidationProblemDetails`; `PUT` пересинхронізує категорії, `updatedAt` заповнений; `PUT` з невідомою категорією → 404 і **зміни відкочено**; `PUT` невідомого → 404; завантаження PNG → 201; JPEG з іменем `../../evil.png` і `type=image/png` → збережено як `image/jpeg`, `.jpg`, ім'я `evil.png`; підроблений PNG / порожній / 6 МБ → 400; невідомий товар → 404; без файлу → 400; байти збігаються з оригіналом; чужий `imageId` → 404 (GET і DELETE); видалення зображення → 204, файл зник; видалення товару → 204, тека й рядки в БД зникли; `/swagger/v1/swagger.json` → 200. Тестові дані з локальної БД прибрано.

**Нюанс:** при `dotnet run --project src/Api` відносний `Storage:RootPath = storage` резолвиться від теки `src/Api` → файли лягають у `src/Api/storage` (а не в корінь репозиторію, як пише ТЗ 7.1). Обидва варіанти в `.gitignore`; у Docker шлях абсолютний (`/app/storage`). Можна вирішити на Кроці 9.

**Далі:** Крок 8 — тести (разом): `ApiFactory` з Testcontainers, `InMemoryNotificationService`, фабрики даних, 21 обов'язковий сценарій із розд. 11 ТЗ. Варто узгодити з Миколою, хто бере інфраструктуру тестів, а хто — сценарії.

### 2026-11-11 · Олександр · Strongly-typed налаштування (розд. 10 ТЗ, закриває останній пункт Кроку 7)

- `src/Api/Settings/ApplicationSettings.cs` — `ApplicationSettings` (`ConnectionStrings.DefaultConnection`, `Storage.RootPath`, необов'язковий `PublicBaseUrl`), прив'язка до кореня конфігурації.
- `src/Api/Settings/ApplicationSettingsValidator.cs` — `IValidateOptions`: обов'язкові connection string і `Storage:RootPath`; `PublicBaseUrl` — порожній або абсолютний http(s) URL.
- `src/Api/Settings/SettingsModule.cs` — `AddApplicationSettings(configuration)` з `ValidateOnStart()`; виклик у `Program.cs` першим рядком реєстрацій.
- `appsettings.json` — `"PublicBaseUrl": ""`.
- Перевірено запуском: порожній і `https://…` → застосунок стартує; `PublicBaseUrl=not-a-url` → падає при старті з `OptionsValidationException` і зрозумілим текстом.

### 2026-11-11 · Олександр · Початок Кроку 8: тестові дублікати й фабрики даних

- `tests/Tests.Common/Services/InMemoryNotificationService.cs` — дублікат `INotificationService`: запам'ятовує `ProductId` надісланих сповіщень (`SentProductIds`, `WasSentFor`, `Clear`) — для сценаріїв «сповіщення надіслано» / «не надсилається при помилці».
- `tests/Tests.Data/Categories/CategoriesData.cs` — `MainCategory()`, `SecondCategory()`, `WithName()`; назви з унікальним суфіксом (у БД унікальний індекс).
- `tests/Tests.Data/Products/ProductsData.cs` — `MainProduct(categories)` (зі specs Fender), `SecondProduct(...)` (з порожніми specs), `New(...)`; категорії прив'язуються через `ReplaceCategories`, як у `CreateProduct`.

**Далі по Кроку 8** (узгодити з Миколою, хто що бере): власний `WebApplicationFactory<Program>` з `Testcontainers.PostgreSql` і підміною `INotificationService` на `InMemoryNotificationService` (+ `IFileStorage` на тимчасову теку), базовий клас з `HttpClient` і `DbContext`, потім тестові класи по сутностях.
