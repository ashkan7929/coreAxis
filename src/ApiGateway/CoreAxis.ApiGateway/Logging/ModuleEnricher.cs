using CoreAxis.BuildingBlocks;
using Serilog.Core;
using Serilog.Events;
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
        public ModuleEnricher(IModuleRegistrar moduleRegistrar)
        {
            _moduleRegistrar = moduleRegistrar;
        }

        /// <summary>
        /// Enriches the log event with module information.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">The property factory.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var modules = _moduleRegistrar.GetRegisteredModules();
            var moduleNames = new List<string>();

            foreach (var module in modules)
            {
                moduleNames.Add(module.Name);
            }

            var modulesProperty = propertyFactory.CreateProperty("Modules", moduleNames);
            logEvent.AddPropertyIfAbsent(modulesProperty);

            var moduleCountProperty = propertyFactory.CreateProperty("ModuleCount", modules.Count);
            logEvent.AddPropertyIfAbsent(moduleCountProperty);
        }
    }
}