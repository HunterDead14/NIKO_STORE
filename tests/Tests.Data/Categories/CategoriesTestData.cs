using Domain.Categories;

namespace Tests.Data.Categories;

public static class CategoriesTestData
{
    public static Category FirstTestCategory() => Category.New(
        CategoryId.New(),
        "Electric guitars");

    public static Category SecondTestCategory() => Category.New(
        CategoryId.New(),
        "Effect pedals");

    public static Category ThirdTestCategory() => Category.New(
        CategoryId.New(),
        "Acoustic guitars");
}
