# Example

```csharp
public class MyVerticle: Verticle{

    public override void UseBuilder(IHostApplicationBuilder builder){
        // add dependencies
    }

    public override void UseApplication(WebApplication app)
    {
        // do something after app created
    }
}
```
