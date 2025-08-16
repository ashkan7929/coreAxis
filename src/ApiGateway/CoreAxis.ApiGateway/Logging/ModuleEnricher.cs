using CoreAxis.BuildingBlocks;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace CoreAxis.ApiGateway.Logging
{
    /// <summary>
    /// Enriches log events with information about the current module.
    /// </summary>
    public class ModuleEnricher : ILogEventEnricher
    {
        private readonly IModuleRegistrar _moduleRegistrar;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleEnricher"/> class.
        /// </summary>
        /// <param name="moduleRegistrar">The module registrar.</param>
        /// <exception cref="ArgumentNullException">Thrown when moduleRegistrar is null.</exception>
        public ModuleEnricher(IModuleRegistrar moduleRegistrar)
        {
            _moduleRegistrar = moduleRegistrar ?? throw new ArgumentNullException(nameof(moduleRegistrar));
        }

        /// <summary>
        /// Enriches the log event with module information.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">The property factory.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null || propertyFactory == null)
                return;

            var modules = _moduleRegistrar?.GetRegisteredModules() ?? new List<IModule>();
            var moduleNames = new List<string>();

            foreach (var module in modules)
            {
                if (module?.Name != null)
                {
                    moduleNames.Add(module.Name);
                }
            }

            var modulesProperty = propertyFactory.CreateProperty("Modules", moduleNames);
            logEvent.AddPropertyIfAbsent(modulesProperty);

            var moduleCountProperty = propertyFactory.CreateProperty("ModuleCount", modules.Count);
            logEvent.AddPropertyIfAbsent(moduleCountProperty);
        }
    }
}