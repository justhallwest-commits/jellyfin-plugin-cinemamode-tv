using System;
using System.Collections.Generic;
using Jellyfin.Plugin.CinemaModeTV.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller;

namespace Jellyfin.Plugin.CinemaModeTV
{
    /// <summary>
    /// Cinema Mode TV Plugin - enables pre-rolls and trailers for Movies and TV Shows.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <inheritdoc />
        public override string Name => "Cinema Mode TV";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <summary>
        /// Gets the plugin configuration.
        /// </summary>
        public PluginConfiguration PluginConfiguration => Configuration;

        /// <summary>
        /// Gets the server application paths.
        /// </summary>
        public static IServerApplicationPaths? ServerApplicationPaths { get; private set; }

        /// <summary>
        /// Gets the library manager.
        /// </summary>
        public static ILibraryManager? LibraryManager { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        public Plugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer,
            ILibraryManager libraryManager,
            IServerApplicationPaths serverApplicationPaths)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            LibraryManager = libraryManager;
            ServerApplicationPaths = serverApplicationPaths;
        }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            };
        }
    }
}
