using Domain.Categories;

namespace Tests.Data.Categories;

// Фабрики тестових категорій. Назви унікальні (суфікс із Guid), бо в БД на name
// є унікальний індекс, а тестові класи сідять дані незалежно один від одного.
public static class CategoriesData
{
    public static Category MainCategory()
        => Category.New(CategoryId.New(), UniqueName("Electric guitars"));

    public static Category SecondCategory()
        => Category.New(CategoryId.New(), UniqueName("Effect pedals"));

    public static Category WithName(string name)
        => Category.New(CategoryId.New(), name);

    public static string UniqueName(string prefix)
        => $"{prefix} {Guid.NewGuid():N}"[..Math.Min(prefix.Length + 9, 255)];
}
