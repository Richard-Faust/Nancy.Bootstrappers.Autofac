﻿using Autofac;
using Autofac.Core.Lifetime;
using Nancy.Configuration;

namespace Nancy.Bootstrappers.Autofac
{
    using System;
    using System.Collections.Generic;
    using Diagnostics;
    using Bootstrapper;

    public abstract class AutofacNancyBootstrapper : NancyBootstrapperWithRequestContainerBase<ILifetimeScope>
    {
        protected override IDiagnostics GetDiagnostics()
        {
            return this.ApplicationContainer.Resolve<IDiagnostics>();
        }

        /// <summary>
        /// Gets all registered application startup tasks
        /// </summary>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}"/> instance containing <see cref="IApplicationStartup"/> instances. </returns>
        protected override IEnumerable<IApplicationStartup> GetApplicationStartupTasks()
        {
            return this.ApplicationContainer.Resolve<IEnumerable<IApplicationStartup>>();
        }

        /// <summary>
        /// Gets all registered request startup tasks
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> instance containing <see cref="IRequestStartup"/> instances.</returns>
        protected override IEnumerable<IRequestStartup> RegisterAndGetRequestStartupTasks(ILifetimeScope container, Type[] requestStartupTypes)
        {
            container.Update(builder =>
            {
                foreach (var requestStartupType in requestStartupTypes)
                {
                    builder.RegisterType(requestStartupType).As<IRequestStartup>().PreserveExistingDefaults().InstancePerDependency();
                }
            });

            return container.Resolve<IEnumerable<IRequestStartup>>();
        }

        /// <summary>
        /// Gets all registered application registration tasks
        /// </summary>
        /// <returns>An <see cref="System.Collections.Generic.IEnumerable{T}"/> instance containing <see cref="IRegistrations"/> instances.</returns>
        protected override IEnumerable<IRegistrations> GetRegistrationTasks()
        {
            return this.ApplicationContainer.Resolve<IEnumerable<IRegistrations>>();
        }

        /// <summary>
        /// Get INancyEngine
        /// </summary>
        /// <returns>INancyEngine implementation</returns>
        protected override INancyEngine GetEngineInternal()
        {
            return this.ApplicationContainer.Resolve<INancyEngine>();
        }

        /// <summary>
        /// Gets the <see cref="INancyEnvironmentConfigurator"/> used by th.
        /// </summary>
        /// <returns>An <see cref="INancyEnvironmentConfigurator"/> instance.</returns>
        protected override INancyEnvironmentConfigurator GetEnvironmentConfigurator()
        {
            return this.ApplicationContainer.Resolve<INancyEnvironmentConfigurator>();
        }

        /// <summary>
        /// Registers an <see cref="INancyEnvironment"/> instance in the container.
        /// </summary>
        /// <param name="container">The container to register into.</param>
        /// <param name="environment">The <see cref="INancyEnvironment"/> instance to register.</param>
        protected override void RegisterNancyEnvironment(ILifetimeScope container, INancyEnvironment environment)
        {
            container.Update(builder => builder.RegisterInstance(environment));
        }

        /// <summary>
        /// Create a default, unconfigured, container
        /// </summary>
        /// <returns>Container instance</returns>
        protected override ILifetimeScope GetApplicationContainer()
        {
            return new ContainerBuilder().Build();
        }

        /// <summary>
        /// Bind the bootstrapper's implemented types into the container.
        /// This is necessary so a user can pass in a populated container but not have
        /// to take the responsibility of registering things like INancyModuleCatalog manually.
        /// </summary>
        /// <param name="applicationContainer">Application container to register into</param>
        protected override void RegisterBootstrapperTypes(ILifetimeScope applicationContainer)
        {
            applicationContainer.Update(builder => builder.RegisterInstance(this).As<INancyModuleCatalog>());
        }

        /// <summary>
        /// Bind the default implementations of internally used types into the container as singletons
        /// </summary>
        /// <param name="container">Container to register into</param>
        /// <param name="typeRegistrations">Type registrations to register</param>
        protected override void RegisterTypes(ILifetimeScope container, IEnumerable<TypeRegistration> typeRegistrations)
        {
            container.Update(builder =>
            {
                foreach (var typeRegistration in typeRegistrations)
                {
                    switch (typeRegistration.Lifetime)
                    {
                        case Lifetime.Transient:
                            builder.RegisterType(typeRegistration.ImplementationType).As(typeRegistration.RegistrationType).InstancePerDependency();
                            break;
                        case Lifetime.Singleton:
                            builder.RegisterType(typeRegistration.ImplementationType).As(typeRegistration.RegistrationType).SingleInstance();
                            break;
                        case Lifetime.PerRequest:
                            throw new InvalidOperationException("Unable to directly register a per request lifetime.");
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }

        /// <summary>
        /// Bind the various collections into the container as singletons to later be resolved
        /// by IEnumerable{Type} constructor dependencies.
        /// </summary>
        /// <param name="container">Container to register into</param>
        /// <param name="collectionTypeRegistrations">Collection type registrations to register</param>
        protected override void RegisterCollectionTypes(ILifetimeScope container, IEnumerable<CollectionTypeRegistration> collectionTypeRegistrations)
        {
            container.Update(builder =>
            {
                foreach (var collectionTypeRegistration in collectionTypeRegistrations)
                {
                    foreach (var implementationType in collectionTypeRegistration.ImplementationTypes)
                    {
                        switch (collectionTypeRegistration.Lifetime)
                        {
                            case Lifetime.Transient:
                                builder.RegisterType(implementationType).As(collectionTypeRegistration.RegistrationType).PreserveExistingDefaults().InstancePerDependency();
                                break;
                            case Lifetime.Singleton:
                                builder.RegisterType(implementationType).As(collectionTypeRegistration.RegistrationType).PreserveExistingDefaults().SingleInstance();
                                break;
                            case Lifetime.PerRequest:
                                throw new InvalidOperationException("Unable to directly register a per request lifetime.");
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Bind the given instances into the container
        /// </summary>
        /// <param name="container">Container to register into</param>
        /// <param name="instanceRegistrations">Instance registration types</param>
        protected override void RegisterInstances(ILifetimeScope container, IEnumerable<InstanceRegistration> instanceRegistrations)
        {
            container.Update(builder =>
            {
                foreach (var instanceRegistration in instanceRegistrations)
                {
                    builder.RegisterInstance(instanceRegistration.Implementation).As(instanceRegistration.RegistrationType);
                }
            });
        }

        /// <summary>
        /// Creates a per request child/nested container
        /// </summary>
        /// <param name="context">Current context</param>
        /// <returns>Request container instance</returns>
        protected override ILifetimeScope CreateRequestContainer(NancyContext context)
        {
            return ApplicationContainer.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag);
        }

        /// <summary>
        /// Bind the given module types into the container
        /// </summary>
        /// <param name="container">Container to register into</param>
        /// <param name="moduleRegistrationTypes"><see cref="INancyModule"/> types</param>
        protected override void RegisterRequestContainerModules(ILifetimeScope container, IEnumerable<ModuleRegistration> moduleRegistrationTypes)
        {
            container.Update(builder =>
            {
                foreach (var moduleRegistrationType in moduleRegistrationTypes)
                {
                    builder.RegisterType(moduleRegistrationType.ModuleType).As<INancyModule>();
                }
            });
        }

        /// <summary>
        /// Retrieve all module instances from the container
        /// </summary>
        /// <param name="container">Container to use</param>
        /// <returns>Collection of <see cref="INancyModule"/> instances</returns>
        protected override IEnumerable<INancyModule> GetAllModules(ILifetimeScope container)
        {
            return container.Resolve<IEnumerable<INancyModule>>();
        }

        /// <summary>
        /// Retreive a specific module instance from the container
        /// </summary>
        /// <param name="container">Container to use</param>
        /// <param name="moduleType">Type of the module</param>
        /// <returns>An <see cref="INancyModule"/> instance</returns>
        protected override INancyModule GetModule(ILifetimeScope container, Type moduleType)
        {
            return container.Update(builder => builder.RegisterType(moduleType).As<INancyModule>()).Resolve<INancyModule>();
        }
    }
}
