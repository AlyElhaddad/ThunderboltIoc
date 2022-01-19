One of the very first IoC frameworks for .Net that has no reflection. An IoC that casts its services before thunder casts its bolts :smile:

The goal is to create a code-generation based IoC container for .Net. Almost all containers today rely on reflection to provide a functioning IoC, which is a costly operation. Regardless of the valued attempts to improve the performance of these containers over the years, these containers would not match the performance of a code-generation based IoC, if that could be achieved. It is quite irritating to witness the cost we pay for trying to clean our code by -among other things- using a reflection-based IoC framework and being able to do nothing about it. The idea is to let the user (the developer) write their code as they please, and let the IoC take care of code generation during compilation to provide a working and performant solution in the target assembly.

I have finally managed to find the time to start working on the solution. Fortunately for me and for the community, I read about source generators in .Net before starting. Having watched the power of source-generators combined with the power of Roslyn, I decided to pick that path over other approaches such as T4 templates and CodeDom for code generation, and I&apos;m glad I made that choice. Mine might not be the first solution to follow this approach, but I like to think of it as the most powerful and flexible one to-date.

Below is a documentation as well as features overview and a quick-start guide to walk you through the framework. If you like my work and are looking forward to supporting me and helping me to continue to improve it, you may do that by:
- [Sponsoring me on Patreon.com](https://www.patreon.com/alyelhaddad "Sponsoring me on Patreon.com").
- [One-time donations via Paypal](https://paypal.me/alyelhaddad "One-time donations via Paypal").

I&apos;m also open to your suggestions. Feel free to contact me on twitter [@aly_elhaddad](https://twitter.com/aly_elhaddad "@aly_elhaddad")

*p.s. the phrase &quot;if that could be achieved&quot; did exist in my original pretext in March 2021. Even though I managed to make this true today, I decided to keep it here as further motivation for the reader.*

#### Document outline
- [1. Installation](#1-installation)
  - [1.1. Make sure that you're using C# version 9.0 or later](#11-make-sure-that-youre-using-c-version-90-or-later)
  - [1.2. Implement ThunderboltRegistration as a partial class](#12-implement-thunderboltregistration-as-a-partial-class)
- [2. Quick start](#2-quick-start)
  - [2.1. Anywhere in your assembly FooAssembly](#21-anywhere-in-your-assembly-fooassembly)
  - [2.2. At your startup code](#22-at-your-startup-code)
  - [2.3. Using your services](#23-using-your-services)
- [3. Features overview](#3-features-overview)
- [4. Benchmarks](#4-benchmarks)
- [5. Service lifetimes](#5-service-lifetimes)
  - [5.1. Singleton](#51-singleton)
  - [5.2. Scoped](#52-scoped)
  - [5.3. Transient](#53-transient)
- [6. Service registration](#6-service-registration)
  - [6.1. Services registered for you by default](#61-services-registered-for-you-by-default)
  - [6.2. Explicit registration](#62-explicit-registration)
    - [6.2.1. Same service and implementation type](#621-same-service-and-implementation-type)
    - [6.2.2. Different service and implementation types](#622-different-service-and-implementation-types)
    - [6.2.3. The service has a factory to determine the resulting instance](#623-the-service-has-a-factory-to-determine-the-resulting-instance)
    - [6.2.4. The service has runtime logic to determine its implementation](#624-the-service-has-runtime-logic-to-determine-its-implementation)
  - [6.3. Attribute registration](#63-attribute-registration)
    - [6.3.1. ThunderboltIncludeAttribute](#631-thunderboltincludeattribute)
    - [6.3.2. ThunderboltExcludeAttribute](#632-thunderboltexcludeattribute)
  - [6.4. Regex-based attribute (convention) registration](#64-regex-based-attribute-convention-registration)
    - [6.4.1. ThunderboltRegexIncludeAttribute](#641-thunderboltregexincludeattribute)
      - [6.4.1.1. Registering services that they themselves represent their own implementation](#6411-registering-services-that-they-themselves-represent-their-own-implementation)
      - [6.4.1.2. Registering services that have different type for their implementation](#6412-registering-services-that-have-different-type-for-their-implementation)
        - [6.4.1.2.1 IFooService: FooService: For the common scenario where we may want to match IFooService with FooService](#64121-ifooservice-fooservice-for-the-common-scenario-where-we-may-want-to-match-ifooservice-with-fooservice-we-may-use-the-following)
    - [6.4.2. ThunderboltRegexExcludeAttribute](#642-thunderboltregexexcludeattribute)
- [7. How to resolve/get your instances](#7-how-to-resolveget-your-instances)
  - [7.1. Obtaining an IThunderboltContainer](#71-obtaining-an-ithunderboltcontainer)
  - [7.2. Obtaining an IThunderboltScope](#72-obtaining-an-ithunderboltscope)
- [8. Supported project types](#8-supported-project-types)
- [9. Known limitations](#9-known-limitations)
- [10. Planned for future versions](#10-planned-for-future-versions)

# 1. Installation
ThunderboltIoc&apos;s installation is a simple as installing the nuget to the target assemblies. No further configuration is needed. For the sake of registering your services, however, you&apos;re going to need to implement (i.e create a class that inherits) `ThunderboltRegistration` as a **partial** class in each project where you may want to register services.
You may find the package at [Nuget.org](https://www.nuget.org/packages/ThunderboltIoc "Nuget.org"): 
using dotnet:
```
dotnet add package ThunderboltIoc
```
or using Nuget Package Manager Console:
```
Install-Package ThunderboltIoc
```
### 1.1. Make sure that you&apos;re using C# version 9.0 or later
In each of your projects where ThunderboltIoc is referenced, make sure that the C# version used is `9.0` or later. In your `*.csproj` add:
```csharp
<PropertyGroup>
    <LangVersion>9.0</LangVersion>
</PropertyGroup>
```
Please note that C# `9.0` is supported only in Visual Studio 2022, and Visual Studio 2019 starting from version `16.7`.
### 1.2. Implement `ThunderboltRegistration` as a **partial** class
This step is not needed if you don&apos;t have any types/services that you would like to register in this assembly. However, if you do (which is likely), you should create a **partial** class that implements the `ThunderboltRegistration` abstract class. More on the registration can be found in the relevant section of this document.

# 2. Quick start
After installation, here&apos;s a minimal working example:
## 2.1. Anywhere in your assembly *FooAssembly*
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
# 3. Features overview
- Achieving dependency injection in .Net without reflection, based on roslyn source generators, with a simple and intuitive API.
- Being able to register your services with three different lifetimes: Singleton, Scoped and Transient.
- Explicit registration where you instruct the framework to register a particular service.
- The ability to register services while specifying user-defined factories for their creation while maintaining their lifetimes.
- The ability to register services while specifying user-defined logic to determine the type of the service implementation.
- Attribute-based service registration where you can use attributes to register or exclude types.
- Registration by convention where we can register services by naming convention using regular expressions.

# 4. Benchmarks
The [src/benchmarks](https://github.com/AlyElhaddad/ThunderboltIoc/tree/master/src/benchmarks) project uses BenchmarkDotNet to conduct measured performance comparison between ThunderboltIoc and the following dependency injection frameworks:
- [Microsoft.Extensions.DepdendencyInjection](https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.DependencyInjection) (on [nuget.org](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/)) *- benchmarks legend: MicrosoftDI*
The industry-standard dependency injection framework for .Net provided by Microsoft that offers basic set of features at a high performance. It relies on runtime expression compilation for services creation.

- [Grace](https://github.com/ipjohnson/Grace) (on [nuget.org](https://www.nuget.org/packages/Grace/))
Known for its wide range of features offered at a superior performance. It depends on `System.Reflection.Emit` runtime code generation for creating services.

	*It is worth mentioning that iOS [does not support](https://docs.microsoft.com/en-us/xamarin/ios/internals/limitations#systemreflectionemit) runtime code generation and therefore Grace&apos;s options are limited when it comes to Xamarin apps or client-side apps in general.*

- [Autofac](https://github.com/autofac/Autofac) (on [nuget.org](https://www.nuget.org/packages/Autofac))
Famous and known for its rich features. It uses raw reflection to instantiate services, but that comes at a grievous cost as the benchmarks show.

Despite being new, ThunderboltIoc attempts to combine the best of each of these frameworks and avoid the worst, with an optimum memory usage. The benchmarks run multiple times with different run strategies and measures the performance of each framework in terms of startup and runtime, where startup is what it takes to fully create and configure the container, and runtime is what it takes to get/resolve/locate few services.

 I will first share an example run of the benchmarks on my machine, and then few insights on what the numbers mean. I will first share an example run of the benchmarks on my machine, and then few insights on what the numbers mean.
```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1466 (21H1/May2021Update)
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-OEAUYL : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-LTYVCA : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-LSBWLP : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
InvocationCount=1  UnrollFactor=1
|                 Method |        Job | RunStrategy |       Mean |       Error |     StdDev |     Median | Ratio | RatioSD | Allocated |
|----------------------- |----------- |------------ |-----------:|------------:|-----------:|-----------:|------:|--------:|----------:|
|    MicrosoftDI_Startup | Job-OEAUYL |   ColdStart |  22.968 us |  10.9253 us |  32.214 us |  13.000 us |  2.54 |    2.14 |   5,328 B |
|          Grace_Startup | Job-OEAUYL |   ColdStart |  37.339 us |  13.3667 us |  39.412 us |  26.250 us |  4.25 |    2.72 |   7,720 B |
| ThunderboltIoc_Startup | Job-OEAUYL |   ColdStart |  90.660 us | 270.3756 us | 797.209 us |   6.900 us |  1.00 |    0.00 |     856 B |
|        Autofac_Startup | Job-OEAUYL |   ColdStart | 105.869 us |  21.0066 us |  61.938 us |  84.100 us | 12.77 |    7.67 |  26,056 B |
|                        |            |             |            |             |            |            |       |         |           |
| ThunderboltIoc_Startup | Job-LTYVCA |  Monitoring |  12.690 us |  10.4828 us |   6.934 us |  10.200 us |  1.00 |    0.00 |     856 B |
|    MicrosoftDI_Startup | Job-LTYVCA |  Monitoring |  38.230 us |  31.6916 us |  20.962 us |  38.600 us |  3.27 |    1.54 |   5,328 B |
|          Grace_Startup | Job-LTYVCA |  Monitoring |  52.810 us |  27.9963 us |  18.518 us |  52.700 us |  5.14 |    2.41 |   7,720 B |
|        Autofac_Startup | Job-LTYVCA |  Monitoring | 139.300 us |  28.6205 us |  18.931 us | 136.400 us | 13.64 |    6.00 |  26,056 B |
|                        |            |             |            |             |            |            |       |         |           |
| ThunderboltIoc_Startup | Job-LSBWLP |  Throughput |   6.144 us |   0.4747 us |   1.275 us |   5.600 us |  1.00 |    0.00 |     856 B |
|    MicrosoftDI_Startup | Job-LSBWLP |  Throughput |  16.027 us |   2.6902 us |   7.848 us |  12.350 us |  2.78 |    1.48 |   5,328 B |
|          Grace_Startup | Job-LSBWLP |  Throughput |  24.843 us |   1.4343 us |   3.804 us |  23.400 us |  4.17 |    0.94 |   7,720 B |
|        Autofac_Startup | Job-LSBWLP |  Throughput |  82.163 us |   6.7114 us |  19.039 us |  74.600 us | 14.03 |    4.51 |  26,056 B |
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1466 (21H1/May2021Update)
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-CUSHNI : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-FPJGPJ : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-AVRZIC : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
|                 Method |        Job | RunStrategy | UnrollFactor |         Mean |         Error |          StdDev |      Median | Ratio | RatioSD |  Gen 0 | Allocated |
|----------------------- |----------- |------------ |------------- |-------------:|--------------:|----------------:|------------:|------:|--------:|-------:|----------:|
| ThunderboltIoc_Runtime | Job-CUSHNI |   ColdStart |            1 |  21,729.0 ns |  46,261.22 ns |   136,402.24 ns |  6,000.0 ns |  1.00 |    0.00 |      - |   1,080 B |
|    MicrosoftDI_Runtime | Job-CUSHNI |   ColdStart |            1 | 159,045.0 ns | 438,290.24 ns | 1,292,308.67 ns |  4,500.0 ns |  4.08 |   23.49 |      - |   1,368 B |
|        Autofac_Runtime | Job-CUSHNI |   ColdStart |            1 | 162,129.0 ns | 429,238.06 ns | 1,265,618.12 ns | 26,550.0 ns |  5.54 |    3.18 |      - |   9,600 B |
|          Grace_Runtime | Job-CUSHNI |   ColdStart |            1 | 274,814.0 ns | 908,008.17 ns | 2,677,282.58 ns |  3,850.0 ns |  1.29 |    2.27 |      - |   1,080 B |
|                        |            |             |              |              |               |                 |             |       |         |        |           |
|          Grace_Runtime | Job-FPJGPJ |  Monitoring |            1 |   8,270.0 ns |  11,520.37 ns |     7,620.01 ns |  4,400.0 ns |  1.04 |    1.20 |      - |   1,056 B |
| ThunderboltIoc_Runtime | Job-FPJGPJ |  Monitoring |            1 |  11,170.0 ns |  12,569.96 ns |     8,314.25 ns |  6,200.0 ns |  1.00 |    0.00 |      - |   1,056 B |
|        Autofac_Runtime | Job-FPJGPJ |  Monitoring |            1 |  42,750.0 ns |  23,380.49 ns |    15,464.74 ns | 44,950.0 ns |  5.69 |    3.88 |      - |   9,576 B |
|    MicrosoftDI_Runtime | Job-FPJGPJ |  Monitoring |            1 | 212,850.0 ns | 886,209.43 ns |   586,172.67 ns | 32,450.0 ns | 23.35 |   62.90 |      - |   1,832 B |
|                        |            |             |              |              |               |                 |             |       |         |        |           |
|          Grace_Runtime | Job-AVRZIC |  Throughput |           16 |     469.1 ns |       8.36 ns |         7.82 ns |    472.3 ns |  0.52 |    0.02 | 0.1988 |     624 B |
|    MicrosoftDI_Runtime | Job-AVRZIC |  Throughput |           16 |     552.3 ns |      10.97 ns |        11.27 ns |    557.9 ns |  0.62 |    0.02 | 0.1984 |     624 B |
| ThunderboltIoc_Runtime | Job-AVRZIC |  Throughput |           16 |     891.9 ns |      17.16 ns |        22.31 ns |    892.7 ns |  1.00 |    0.00 | 0.1984 |     624 B |
|        Autofac_Runtime | Job-AVRZIC |  Throughput |           16 |   5,913.1 ns |     116.74 ns |       109.20 ns |  5,856.3 ns |  6.60 |    0.24 | 2.9144 |   9,144 B |
```
The data above is sorted by the mean column from the fastest to the slowest. The mean however can be (and is) affected by few outliers, which is why I have included baseline ratio and median columns. Some of the medians in the data above provide evidence on the fact their corresponding means are affected by outliers.

However, in order to have a more accurate insight as to which framework performs better in which scenario, we should look at the ratio column. ThunderboltIoc will always have one and in each scenario, the other frameworks will have proportional values where a smaller value means better performance in this scenario and a bigger value means worse performance in that scenario.

*For those who might not be familiar with BenchmarksDotNet, the ratio is often confused with final means proportional to each other. That is not true. Instead, in each run, the ratio is calculated and stored for this particular operation, and in the end, the ratio displayed will be the mean of all the ratios calculated. This provides better immunity against outliers.*

It is also worth mentioning that the allocated memory for ThunderboltIoc prevails (or equals the minimum) in each scenario.

# 5. Service lifetimes
Thunderbolt&apos;s service lifetimes are very similar (or in fact, almost identical) to those of `Microsoft.Extensions.DependencyInjection` that was first introduced with .Net Core.
## 5.1. Singleton
Specifies that a single instance of the service will be created throughout your program’s lifecycle.
## 5.2. Scoped
Specifies that a new instance of the service will be created for each scope. If no scope was available at the time of resolving the service (i.e the `IThundernoltResolver` used was not an `IThunderboltScope`), a singleton service will be returned.

If a service gets resolved using an `IThunderboltScope` as the `IThunderboltResolver`, each and every scoped dependency of that service will be resolved using the same `IThunderboltScope` (unless it was a singleton service whose scope dependencies had been resolved earlier).
## 5.3. Transient
Specifies that a new instance of the service will be created every time it is requested.
# 6. Service registration
As well as services registered for you by default, here are three ways in which you may register your assemblies:
- Explicit registration
- Attribute registration
- Regex-based attribute registration

All the three of them are valid to use either alone or in conjunction with any other. In fact, the source generator generates explicit registrations for attribute registrations.
## 6.1. Services registered for you by default
- IThunderboltContainer
The container itself is registered as a singleton service. In fact, even ThunderboltActivator.Container that you may use to get the container returns the same singleton instance.

- IThunderboltScope
This is registered as a transient service, meaning, every time you try to resolve/get an `IThunderboltScope`, a new `IThunderboltScope` will be created. This is the same as `IThunderboltContainer.CreateScope()`.

	It is worth mentioning that an `IThunderboltScope` is an `IDisposable`, however, that doesn&apos;t mean that disposing an `IThunderboltScope` would dispose scoped instances (at least in the current release).

- IThunderboltResolver
This is registered as a transient service and returns  `IThunderboltResolver` that was used to get this instance. This will only ever return `IThunderboltContainer` or `IThunderboltScope`.

## 6.2. Explicit registration
After you have created a **partial** class that inherits `ThunderboltRegistration`, you will have to override the abstract `void Register`. This method gives you a single argument of type `IThunderboltRegistrar` (`reg`), which you can use to register your services using the `Add{serviceLifetime}` methods.

For explicit registration, code generation is going to happen for every `Add{serviceLifetime}` call that happens inside (except for factory registrations), regardless of whether or not you implement a logic in your Register method (i.e even if you implement a logic where a particular call to an Add method is not reachable, code generation would still consider it anyway).

Explicit registration supports four different scenarios for registering your services.
### 6.2.1. Same service and implementation type
For this scenario, you may register your services like:
```csharp
reg.AddSingleton<FooService>();
reg.AddScoped<BarService>();
reg.AddTransient<BazService>();
```
### 6.2.2. Different service and implementation types
For this scenario, you may register your services like:
```csharp
reg.AddSingleton<IFooService, FooService>();
reg.AddScoped<IBarService, BarService>();
reg.AddTransient<IBazService, BazService>();
```
### 6.2.3. The service has a factory to determine the resulting instance
For this scenario, you may register your services like:
```csharp
reg.AddSingletonFactory<FooService>();
reg.AddScopedFactory<BarService>();
reg.AddTransientFactory<BazService>();
```
In this scenario, no code generation happens at all as it is assumed that you are going to provide all the necessary details to get an instance of this service. Service lifetimes however would still apply to service registered using any of the factory signatures.
### 6.2.4. The service has runtime logic to determine its implementation
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
## 6.3. Attribute registration
This is where you may register your services using attributes. It is important to know that even if you don&apos;t have any explicit registrations, you still need to create a **partial** class that inherits `ThunderboltRegistration` for attribute registration to work. Attribute registrations are managed via two attributes (+ the regex ones): `ThunderboltIncludeAttribute` and` ThunderboltExcludeAttribute`.
### 6.3.1. ThunderboltIncludeAttribute
Define this attribute at the top of the types you want to register. There are few signatures for this attribute but they all come down to three simple arguments.
- serviceLifetime (required)
Specifies the service lifetime you want to register for this service.
- implementation (optional)
If this service has another type as its implementation, this is how you may specify that.
- applyToDerviedTypes (optional, default: false)
Determines whether derived types should also be registered just like this service.
It is important to note that captured derived types are limited only to the types that are accessible in the assembly that defines the attribute; meaning: if you have an assembly `FooAssembly` where you use `ThunderboltIncludeAttribute` (with applyToDerivedTypes: true), and another assembly `BarAssembly` that references `FooAssembly` but also defines some derived types, types in `BarAssembly` will not be registered (unless another `ThunderboltIncludeAttribute` is defined in `BarAssembly` as well).

### 6.3.2. ThunderboltExcludeAttribute
Specifies that this type will not be registered unless it gets registered explicitly via `ThunderboltRegistration.Register` (even if `ThunderboltIncludeAttribute` or `ThunderboltRegexIncludeAttribute` is present).

This attribute also has the optional parameter (default: false) applyToDerivedTypes which specifies that derived types accessible in this assembly should not be registered via attribute-registration attributes.
## 6.4. Regex-based attribute (convention) registration
This is how you may register several services at once, using their naming convention. Just like non-regex attribute registration, it is important to create a **partial** class that inherits `ThunderboltRegistration` for this to work, even if you don&apos;t have any explicit registrations. Regex attribute registrations are managed via two attributes: (`ThunderboltRegexIncludeAttribute` and `ThunderboltRegexExcludeAttribute`).

Attributes used for regex-based registration are assembly-level attributes (they should be defined on the target assembly `[assembly: ThunderboltRegexIncludeAttribute(...)]`).

### 6.4.1. ThunderboltRegexIncludeAttribute
This is where you define your conventions for naming-convention-based registration. The first argument passed to this attribute is the service lifetime for all the services matched by this attribute.

The regex argument passed to the attribute should match all the types that you wish to register. Your pattern is tested against the full names of the types (`global::Type.Full.Namespace.TypeName`).

Beware that your pattern gets tested against all of the accessible types, meaning that you may want to write a pattern that does not include types defined under the System namespace for instance.

You may have several `ThunderboltRegexIncludeAttribute` on the same assembly to define different patterns and lifetimes.
#### 6.4.1.1. Registering services that they themselves represent their own implementation
Using only the two arguments discussed above is going to be sufficient for this scenario.
#### 6.4.1.2. Registering services that have different type for their implementation
For this scenario, two more arguments are going to be needed in addition to the lifetime and the regex.
- implRegex
This should be a regular expression that matches all of your implementation types (and only your implementation types).
- joinKeyRegex
By now, your regex argument should be matching a number of services, and your implRegex argument should be matching a number of service implementations. This parameter is used to select a particular join key from the results matched by both of your other arguments to determine which implementations correspond to which services. This is also a regular expression.

##### 6.4.1.2.1 IFooService: FooService: For the common scenario where we may want to match IFooService with FooService, we may use the following:
```csharp
[assembly: ThunderboltRegexInclude(
	ThunderboltServiceLifetime.Scoped,
	regex: @"(global::)?(MyBaseNamespace)\.I[A-Z][A-z_]+Service",
	implRegex: @"(global::)?(MyBaseNamespace)\.[A-Z][a-z][A-z_]+Service",
	joinKeyRegex: @"(?<=(global::)?(MyBaseNamespace)\.I?)[A-Z][a-z][A-z_]+Service")]
```
### 6.4.2. ThunderboltRegexExcludeAttribute
This is where you may specify a regular expression that when matched with any type, does not get registered via attribute registration (and/or regex-based attribute registration).
# 7. How to resolve/get your instances
Getting service instances with respect to their registered lifetimes is as simple as using `Get<TService>()` on an `IThunderboltResolver`. An `IThunderboltResolver` may either be an `IThunderboltContainer` or an `IThunderboltScope`.
## 7.1. Obtaining an IThunderboltContainer
Provided you have attached at least one **partial** class that inherits `ThunderboltRegistration` via `ThunderboltActivator.Attach`, it is safe to use `ThunderboltActivator.Container` property to get the singleton `IThunderboltContainer` instance. If you already have an `IThunderboltResolver`, you may also get the same container using `resolver.Get<IThunderboltContainer>()`.
## 7.2. Obtaining an IThunderboltScope
Using an `IThunderboltContainer`, you may create a new scope using `container.CreateScope()`. If you already have an `IThunderboltResolver`, you may create a new scope using `resolver.Get<IThunderboltScope>()`.
# 8. Supported project types
All project types are inherently supported and your project wouldn&apos;t complain about working with ThunderboltIoc. However, up until the moment of writing this, there is no explicit integration with `Microsoft.Extensions.DependencyInjection`, which to some extent limits our options when working with .Net Core projects if we wanted to utilize Microsoft&apos;s DI.

It is intended for ThunderboltIoc to integrate with `Microsoft.Extensions.DependencyInjection` in the future, but it the meantime, there would be no harm in using both frameworks together side-by-sidy; that however wouldn&apos;t let us fully benefit from ThunderboltIoc&apos;s superior performance all the time.

It is perfectly safe to use ThunderboltIoc for any .Net C# project as a standalone IoC.

# 9. Known limitations
The only services for which code generation can work are services that have exactly one constructor that is public. It is planned to lift this restriction in future versions.

# 10. Planned for future versions
## 10.1. Lift the single public constructor restriction.
As mentioned in the section above, it is feasible and desired to remove this limitation.
## 10.2. Better source generation exception handling
Currently, in the best-case scenario and if you follow the documentation, we shouldn&apos;t worry about source generation exceptions. However, upon failing to adhere to the documentation, it is possible that an unhandled exception might arise. In such a case, the source generator might (or might not) fail at the whole process. When that happens, it is possible that no code gets generated at all (you would get a notification in the build output but you might not notice it).

It is planned to provide better exception handling so that failure to generate code for a particular service wouldn&apos;t cause the whole process to fail. It would also be nice to generate relevant warnings or errors.

## 10.3. Verify no cyclic dependencies exist
Currently, it is possible to fall into an infinite resolve operation where one (or more) service&apos;s dependencies may directly or indirectly depend on the same service.

## 10.4. Analyzers
It would be beneficial to have static code analyzers that show you warnings about failing to adhere to the best practices discussed in the documentation. For instance, generate a warning that tells you that a class you created that inherits `ThunderboltRegistration` does not have the **partial** modifier.

## 10.5. Property Injection
As is the case with any feature-rich IoC framework, property injection is a dependency injection style that is not currently supported by this framework.

## 10.6. Automatic object disposal
Disposing an `IThunderboltScope` should in turn dispose every `IDisposable` scoped service saved in the corresponding `IThunderboltScope`.

## 10.7. Integration with Microsoft.Extensions.DependencyInjection
The goal is to provide an intuitive API to effectively replace the default `IServiceProvider` of Microsoft&apos;s DI. Currently, `IThunderboltResolver` already implements `System.IServiceProvider` but no present elegant integration exists.

