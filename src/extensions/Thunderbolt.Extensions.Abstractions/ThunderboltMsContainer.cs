using Microsoft.Extensions.DependencyInjection;

using ThunderboltIoc;

namespace Thunderbolt.Extensions.Abstractions;

internal sealed class ThunderboltMsContainer : ThunderboltContainer, ISupportRequiredService
{
    private readonly struct ServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IThunderboltContainer container;

        public ServiceScopeFactory(IThunderboltContainer container)
            => this.container = container;

        public IServiceScope CreateScope()
        {
            return new ServiceScope(container.CreateScope());
        }
    }
    private readonly struct ServiceScope : IServiceScope, IThunderboltScope, ISupportRequiredService
    {
        private readonly IThunderboltScope scope;

        public ServiceScope(IThunderboltScope scope)
        {
            ServiceProvider = this.scope = scope;
        }
        public IServiceProvider ServiceProvider { get; }

        public int Id => scope.Id;

        public T? Get<T>() where T : notnull
        {
            if (typeof(T).ToString().StartsWith("Swashbuckle"))
            {

            }
            return scope.Get<T>();
        }

        public object GetService(Type serviceType)
        {
            if (serviceType.ToString().StartsWith("Swashbuckle"))
            {

            }
            return scope.GetService(serviceType);
        }

        public void Dispose()
        {
            scope.Dispose();
        }

        public object GetRequiredService(Type serviceType)
        {
            try
            {
                return scope.GetService(serviceType) ?? throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");
            }
            catch (InvalidOperationException ex)
            {
                throw;
            }
        }
    }

    public ThunderboltMsContainer()
    {
        ThunderboltServiceRegistry<IServiceScopeFactory>.Dictate((_, _, userFactory) => userFactory!(null!), ThunderboltServiceLifetime.Singleton, null, _ => new ServiceScopeFactory(this));
        //ThunderboltServiceRegistry<IServiceScopeFactory>.Register(ThunderboltServiceLifetime.Singleton, _ => new ServiceScopeFactory(this));
    }

    public object GetRequiredService(Type serviceType)
    {
        if (serviceType.ToString().StartsWith("Swashbuckle"))
        {

        }
        try
        {
            return GetService(serviceType) ?? throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");
        }
        catch (InvalidOperationException ex)
        {
            throw;
        }
    }
}
