using Microsoft.Extensions.DependencyInjection;

namespace Booster.Core.Testing;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void 层次低的容器优先()
    {
        var collection = new ServiceCollection();

        collection.AddSingleton(new Animal { Name = "Animal" });
        collection.AddScoped(p => new Dog { Name = "dog" });

        var provider = collection.BuildServiceProvider();
        var singleton = new SingletonServiceFactoryContainer(collection, provider);
        var scoped = new ScopedServiceFactoryContainer(collection, provider);

        var container = new ServiceContainer(scoped, singleton);

        var runner = container.CreateDelegate<Action>((Animal animal, Dog dog) =>
        {
            Console.WriteLine($"animal name = {animal.Name} and dog name = {dog.Name}");
        });

        runner();
    }

    [TestMethod]
    public void 同层次更精确的类型优先()
    {
        var builder = new ServiceContainerBuilder();
        builder.AddService<Animal>(new Animal { Name = "Animal" });
        builder.AddService<Dog>(new Dog { Name = "Dog" });

        var container = new ServiceContainer(builder);

        var runner = container.CreateDelegate<Action>((Animal animal, Dog dog) =>
        {
            Console.WriteLine($"animal name = {animal.Name} and dog name = {dog.Name}");
        });

        runner();
    }
}


public class Animal { public required string Name { get; set; } }

public class Human : Animal;

public class Dog : Animal;

public class Book { public required string Title { get; set; } }