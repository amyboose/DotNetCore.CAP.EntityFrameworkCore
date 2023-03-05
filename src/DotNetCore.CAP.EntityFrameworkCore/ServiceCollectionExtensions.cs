// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotNetCore.CAP;
using DotNetCore.CAP.EntityFrameworkCore;
using DotNetCore.CAP.EntityFrameworkCore.Interceptors;
using DotNetCore.CAP.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to <see cref="IServiceCollection" /> for configuring consistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures the consistence services for the consistency.
    /// </summary>
    /// <param name="services">The services available in the application.</param>
    /// <param name="setupAction">An action to configure the <see cref="CapOptions" />.</param>
    /// <returns>An <see cref="CapBuilder" /> for application services.</returns>
    public static CapBuilder AddEntityFrameworkCap(this IServiceCollection services, Action<CapOptions> setupAction)
    {
        services
            .AddCap(setupAction);

        var descriptor = new ServiceDescriptor(
            typeof(ICapPublisher),
            typeof(EntityFrameworkCapPublisher),
            ServiceLifetime.Singleton);

        services.Replace(descriptor);
        services.AddSingleton<IEntityFrameworkCapPublisher, EntityFrameworkCapPublisher>();

        services.AddScoped<CapTransactionIntercepter>();

        return new CapBuilder(services);
    }
}