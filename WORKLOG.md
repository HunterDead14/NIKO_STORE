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
| 6 `Product` | Олександр | в роботі: репозиторій, queries, `CreateProduct` готові; далі `UpdateProduct`, `DeleteProduct`, контролер, DTO, `ProductErrorFactory` |
| 7 Images | Олександр | частково: `IFileStorage` + `LocalFileStorage`; помилки `ProductImageException` зробив Микола |
| 8–9 Тести, стабілізація | разом | не почато |

Останній коміт, на якому базується журнал: `123bd51` (локально, ще не на `origin/main`).

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

**Далі по зоні Олександра:** `UpdateProduct` (транзакція через `IUnitOfWork`), `DeleteProduct` (+ best-effort прибирання теки на Кроці 7), `ProductDto`/запити, `ProductErrorFactory` (з `missingCategoryIds` у тілі 404), `ProductsController`; потім Крок 7.
