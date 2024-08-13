using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

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
            Logger.LogMessage($"animal name = {animal.Name} and dog name = {dog.Name}");
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
            Logger.LogMessage($"animal name = {animal.Name} and dog name = {dog.Name}");
        });

        runner();
    }

    [TestMethod]
    public void 支持返回值()
    {
        var builder = new ServiceContainerBuilder();
        builder.AddService<Animal>(new Animal { Name = "Animal" });
        builder.AddService<Dog>(new Dog { Name = "Dog" });

        var container = new ServiceContainer(builder);

        var runner = container.CreateDelegate<Func<string>>((Animal animal, Dog dog) =>
        {
            return $"animal name = {animal.Name} and dog name = {dog.Name}";
        });

        Logger.LogMessage(runner());
    }
}


public class Animal { public required string Name { get; set; } }

public class Human : Animal;

public class Dog : Animal;

public class Book { public required string Title { get; set; } }