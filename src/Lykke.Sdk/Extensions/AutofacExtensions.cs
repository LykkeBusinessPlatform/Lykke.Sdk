using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Lykke.SettingsReader;

namespace Lykke.Sdk
{
    internal static class AutofacExtensions
    {
        internal static void RegisterAssemblyModules<TAppSettings>(this ContainerBuilder builder,
            IReloadingManager<TAppSettings> settings,
            Action<IModuleRegistration> additionalModules,
            params Assembly[] assemblies)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            var moduleType = typeof(Autofac.Module);
            var internalBuilder = new ContainerBuilder();

            internalBuilder
                .RegisterAssemblyTypes(assemblies)
                .Where(t => moduleType.GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))
                .WithParameter(TypedParameter.From(settings))
                .As<IModule>();

            if (additionalModules != null)
            {
                var registration = new ModuleRegistration<TAppSettings>(settings, internalBuilder);

                additionalModules.Invoke(registration);
            }

            LoadModules(builder, internalBuilder);
        }

        private static void LoadModules(ContainerBuilder builder, ContainerBuilder internalBuilder)
        {
            using (var ctx = internalBuilder.Build())
            {
                foreach (var module in ctx.Resolve<IEnumerable<IModule>>())
                {
                    builder.RegisterModule(module);
                }
            }
        }

        private class ModuleRegistration<TAppSettings> : IModuleRegistration
        {
            private readonly IReloadingManager<TAppSettings> _settings;
            private readonly ContainerBuilder _internalBuilder;

            public ModuleRegistration(IReloadingManager<TAppSettings> settings, ContainerBuilder internalBuilder)
            {
                _settings = settings;
                _internalBuilder = internalBuilder;
            }

            public IModuleRegistration RegisterModule<TModule>()
                where TModule : IModule
            {
                _internalBuilder
                    .RegisterType<TModule>()
                    .WithParameter(TypedParameter.From(_settings))
                    .As<IModule>();

                return this;
            }
        }
    }
}