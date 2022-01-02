# 1. Installation
ThunderboltIoc&apos;s installation is a simple as installing the nuget to the target assemblies. No further configuration is needed. For the sake of registering your services, however, you&apos;re going to need to implement (i.e create a class that inherits) ThunderboltRegistration as a partial class in each project where you may want to register services.
You may find the package at [Nuget.org](https://www.nuget.org/packages/ThunderboltIoc "Nuget.org"): 
```
dotnet add package ThunderboltIoc
```
or
```
Install-Package ThunderboltIoc
```
## 1.1. Implement `ThunderboltRegistration` as a **partial** class
This step is not needed if you don&apos;t have any types/services that you would like to register in this assembly. However, if you do, you should create a **partial** class that implements the `ThunderboltRegistration` abstract class. More on the registration can be found in the relevant section of this document.

# 2. Quick start
After installation, here&apos;s a minimal working example:
## 2.1. Anywhere in your assembly FooAssembly
```csharp
public partial class FooThunderboltRegistration : ThunderboltRegistration
{
	protected override void Register(IThunderboltRegistrar reg)
	{
		reg.AddSingleton<BazService>();
		reg.AddScoped<IBarService, BarService>();
		reg.AddTransientFactory<Qux>(() => new Qux());
	}
}
```
## 2.2. At your startup code
Where this code may execute before any attempt to resolve/get your services.
```csharp
ThunderboltActivator.Attach<FooThunderboltRegistration>();
```
## 2.3. Using your services
The simplemost way to get your services would be as follows:
```csharp
BazService bazService = ThunderboltActivator.Container.Get<BazService>();
```

# 3. Service lifetimes
Thunderbolt&apos;s service lifetimes are very similar (or in fact, almost identical) to those of `Microsoft.Extensions.DependencyInjection` that was first introduced with .Net Core.
## 3.1. Singleton
Specifies that a single instance of the service will be created throughout your program’s lifecycle.
## 3.2. Scoped
Specifies that a new instance of the service will be created for each scope. If no scope was available at the time of resolving the service (i.e the `IThundernoltResolver` used was not an `IThunderboltScope`), a singleton service will be returned.

If a service gets resolved using an `IThunderboltScope` as the `IThunderboltResolver`, each and every dependency of that service will be resolved using the same `IThunderboltScope`.
## 3.3. Transient
Specifies that a new instance of the service will be created every time it is requested.
# 4. Service registration
As well as services registered for you by default, here are three ways in which you may register your assemblies:
- Explicit registration
- Attribute registration
- Regex-based attribute registration

All the three of them are valid to use either alone or in conjunction with any other. In fact, the source generator generates explicit registrations for attribute registrations.
## 4.1. Services registered for you by default
- IThunderboltContainer
The container itself is registered as a singleton service. In fact, even ThunderboltActivator.Container that you may use to get the container returns the same singleton instance.
- IThunderboltResolver
This is also registered as a singleton instance and returns an `IThunderboltContainer` (the same as above).
- IThunderboltScope
This is registered as a transient service, meaning, every time you try to resolve/get an `IThunderboltScope`, a new `IThunderboltScope` will be created. This is the same as `IThunderboltContainer.CreateScope()`.

	It is worth mentioning that an `IThunderboltScope` is an `IDisposable`, however, that doesn&apos;t mean that disposing an `IThunderboltScope` would dispose scoped instances.

## 4.2. Explicit registration
After you have created a **partial** class that inherits `ThunderboltRegistration`, you will have to override the abstract `void Register`. This method gives you a single argument of type `IThunderboltRegistrar` (`reg`), which you can use to register your services using the `Add{serviceLifetime}` methods.

For explicit registration, code generation is going to happen for every `Add{serviceLifetime}` call that happens inside (except for factory registrations), regardless of whether or not you implement a logic in your Register method (i.e even if you implement a logic where a particular call to an Add method is not reachable, code generation would still consider it anyway).

Explicit registration supports four different scenarios for registering your services.
### 4.2.1. Same service and implementation type
For this scenario, you may register your services like:
```csharp
reg.AddSingleton<FooService>();
reg.AddScoped<BarService>();
reg.AddTransient<BazService>();
```
### 4.2.2. Different service and implementation types
For this scenario, you may register your services like:
```csharp
reg.AddSingleton<IFooService, FooService>();
reg.AddScoped<IBarService, BarService>();
reg.AddTransient<IBazService, BazService>();
```
### 4.2.3. The service has a factory to determine the resulting instance
For this scenario, you may register your services like:
```csharp
reg.AddSingletonFactory<FooService>();
reg.AddScopedFactory<BarService>();
reg.AddTransientFactory<BazService>();
```
In this scenario, no code generation happens at all as it is assumed that you are going to provide all the necessary details to get an instance of this service. Service lifetimes however would still apply to service registered using any of the factory signatures.
### 4.2.4. The service has runtime logic to determine its implementation
For this scenario, you may register your services like:
```csharp
reg.AddSingleton<IYearHalfService>(() =>
{
	if (DateTime.UtcNow.Month <= 6)
		return typeof(YearFirstHalfService);
	else
		return typeof(YearSecondHalfService);
});
```
In this scenario, it is important to know that code generation happens for every capture of the statement `return typeof(TService)` inside the scope. This means that your implementations must be known at the compile time. It is also important to notice that either this:
```csharp
Type fooType = typeof(FooType);
return fooType;
```
or that:
```csharp
return someFooVariable.GetType();
```
will not work.

Also, you cannot pass a function pointer to this signature. It will compile but it won&apos;t work. The only accepted syntaxes are lambda expressions (e.g `() => typeof(TService)` or  `() => { return typeof(TService); }`) and anonymous method expressions (e.g `delegate { return typeof(TService); }`).
## 4.3. Attribute registration
This is where you may register your services using attributes. It is important to know that even if you don&apos;t have any explicit registrations, you still need to create a **partial** class that inherits `ThunderboltRegistration` for attribute registration to work. Attribute registrations are managed via two attributes (+ the regex ones): `ThunderboltIncludeAttribute` and` ThunderboltExcludeAttribute`.
### 4.3.1. ThunderboltIncludeAttribute
Define this attribute at the top of the types you want to register. There are few signatures for this attribute but they all come down to three simple arguments.
- serviceLifetime (required)
Specifies the service lifetime you want to register for this service.
- implementation (optional)
If this service has another type as its implementation, this is how you may specify that.
- applyToDerviedTypes (optional, default: false)
Determines whether derived types should also be registered just like this service.
It is important to note that captured derived types are limited only to the types that are accessible in the assembly that defines the attribute; meaning: if you have an assembly `FooAssembly` where you use `ThunderboltIncludeAttribute` (with applyToDerivedTypes: true), and another assembly `BarAssembly` that references `FooAssembly` but also defines some derived types, types in `BarAssembly` will not be registered (unless another `ThunderboltIncludeAttribute` is defined in `BarAssembly` as well).

### 4.3.2. ThunderboltExcludeAttribute
Specifies that this type will not be registered unless it gets registered explicitly via `ThunderboltRegistration.Register` (even if `ThunderboltIncludeAttribute` or `ThunderboltRegexIncludeAttribute` is present).

This attribute also has the optional parameter (default: false) applyToDerivedTypes which specifies that derived types accessible in this assembly should not be registered via attribute-registration attributes.
## 4.4. Regex-based attribute (convention) registration
This is how you may register several services at once, using their naming convention. Just like non-regex attribute registration, it is important to create a **partial** class that inherits `ThunderboltRegistration` for this to work, even if you don&apos;t have any explicit registrations. Regex attribute registrations are managed via two attributes: (`ThunderboltRegexIncludeAttribute` and `ThunderboltRegexExcludeAttribute`).

Attributes used for regex-based registration are assembly-level attributes (they should be defined on the target assembly `[assembly: ThunderboltRegexIncludeAttribute(...)]`).

### 4.4.1. ThunderboltRegexIncludeAttribute
This is where you define your conventions for naming-convention-based registration. The first argument passed to this attribute is the service lifetime for all the services matched by this attribute.

The regex argument passed to the attribute should match all the types that you wish to register. Your pattern is tested against the full names of the types (`global::Type.Full.Namespace.TypeName`).

Beware that your pattern gets tested against all of the accessible types, meaning that you may want to write a pattern that does not include types defined under the System namespace for instance.

You may have several `ThunderboltRegexIncludeAttribute` on the same assembly to define different patterns and lifetimes.
#### 4.4.1.1. Registering services that they themselves represent their own implementation
Using only the two arguments discussed above is going to be sufficient for this scenario.
#### 4.4.1.2. Registering services that have different type for their implementation
For this scenario, two more arguments are going to be needed in addition to the lifetime and the regex.
- implRegex
This should be a regular expression that matches all of your implementation types (and only your implementation types).
- joinKeyRegex
By now, your regex argument should be matching a number of services, and your implRegex argument should be matching a number of service implementations. This parameter is used to select a particular join key from the results matched by both of your other arguments to determine which implementations correspond to which services. This is also a regular expression.

##### 4.4.1.2.1 IFooService: FooService: For the common scenario where we may want to match IFooService with FooService, we may use the following:
```csharp
[assembly: ThunderboltRegexInclude(
	ThunderboltServiceLifetime.Scoped,
	regex: @"(global::)?(MyBaseNamespace)\.I[A-Z][A-z_]+Service",
	implRegex: @"(global::)?(MyBaseNamespace)\.[A-Z][a-z][A-z_]+Service",
	joinKeyRegex: @"(?<=(global::)?(MyBaseNamespace)\.I?)[A-Z][a-z][A-z_]+Service")]
```
### 4.4.2. ThunderboltRegexExcludeAttribute
This is where you may specify a regular expression that when matched with any type, does not get registered via attribute registration (and/or regex-based attribute registration).
# 5. How to resolve/get your instances
Getting service instances with respect to their registered lifetimes is as simple as using `Get<TService>()` on an `IThunderboltResolver`. An `IThunderboltResolver` may either be an `IThunderboltContainer` or an `IThunderboltScope`.
## 5.1. Obtaining an IThunderboltContainer
Provided you have attached at least one **partial** class that inherits `ThunderboltRegistration` via `ThunderboltActivator.Attach`, it is safe to use `ThunderboltActivator.Container` property to get the singleton `IThunderboltContainer` instance. If you already have an `IThunderboltResolver`, you may also get the same container using `resolver.Get<IThunderboltContainer>()`.
## 5.2. Obtaining an IThunderboltScope
Using an `IThunderboltContainer`, you may create a new scope using `container.CreateScope()`. If you already have an `IThunderboltResolver`, you may create a new scope using `resolver.Get<IThunderboltScope>()`.
# 6. Supported project types
All project types are inherently supported and your project wouldn&apos;t complain about working with ThunderboltIoc. However, up until the moment of writing this, there is no explicit integration with `Microsoft.Extensions.DependencyInjection`, which to some extent limits our options when working with .Net Core projects if we wanted to utilize Microsoft&apos;s DI.

It is intended for ThunderboltIoc to integrate with `Microsoft.Extensions.DependencyInjection` in the future, but it the meantime, there would be no harm in using both frameworks together side-by-sidy; that however wouldn&apos;t let us fully benefit from ThunderboltIoc&apos;s superior performance all the time.

It is perfectly safe to use ThunderboltIoc for any .Net C# project as a standalone IoC.

# 7. Known limitations
The only services for which code generation can work are services that have exactly one constructor that is public. It is planned to lift this restriction in future versions.

# 8. Planned for future versions
## 8.1. Lift the single public constructor restriction.
As mentioned in the section above, it is feasible and desired to remove this limitation.
## 8.2. Better source generation exceptional handling
Currently, in the best-case scenario and if you follow the documentation, we shouldn&apos;t worry about source generation exceptions. However, upon failing to adhere to the documentation, it is possible that an unhandled exception might arise. In such a case, the source generator might (or might not) fail at the whole process. When that happens, it is possible that no code gets generated at all (you would get a notification in the build output but you might not notice it).

It is planned to provide better exception handling so that failure to generate code for a particular service wouldn&apos;t cause the whole process to fail. It would also be nice to generate relevant warnings or errors.

## 8.3. Verify no cyclic dependencies exist
Currently, it is possible to fall into an infinite resolve operation where one (or more) service&apos;s dependencies may directly or indirectly depend on the same service.

## 8.4. Analyzers
It would be beneficial to have static code analyzers that show you warnings about failing to adhere to the best practices discussed in the documentation. For instance, generate a warning that tells you that a class you created that inherits `ThunderboltRegistration` does not have the **partial** modifier.

## 8.5. Property Injection
As is the case with any feature-rich IoC framework, property injection is a dependency injection style that is not currently supported by this framework.

## 8.6. Automatic object disposal
Disposing an `IThunderboltScope` should in turn dispose every `IDisposable` scoped service saved in the corresponding `IThunderboltScope`.

## 8.7. Integration with Microsoft.Extensions.DependencyInjection
The goal is to provide an intuitive API to effectively replace the default `IServiceProvider` of Microsoft&apos;s DI. Currently, `IThunderboltResolver` already implements `System.IServiceProvider` but no present elegant integration exists.

