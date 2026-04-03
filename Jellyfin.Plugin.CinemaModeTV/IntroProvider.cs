using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaModeTV
{
    /// <summary>
    /// Intro provider that supports both Movies and TV Episodes.
    /// </summary>
    public class IntroProvider : IIntroProvider
    {
        /// <inheritdoc />
        public string Name { get; } = "CinemaModeTV";

        private readonly ILogger<IntroProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntroProvider"/> class.
        /// </summary>
        public IntroProvider(ILogger<IntroProvider> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            var config = Plugin.Instance?.PluginConfiguration;
            if (config == null)
            {
                _logger.LogWarning("CinemaModeTV: Plugin instance or configuration is null.");
                return Task.FromResult(Enumerable.Empty<IntroInfo>());
            }

            // Check item type AND whether that type is enabled in config
            bool isMovie = item is Movie;
            bool isEpisode = item is Episode;

            if (isMovie && !config.EnableForMovies)
            {
                _logger.LogDebug("CinemaModeTV: Cinema Mode disabled for movies.");
                return Task.FromResult(Enumerable.Empty<IntroInfo>());
            }

            if (isEpisode && !config.EnableForTVShows)
            {
                _logger.LogDebug("CinemaModeTV: Cinema Mode disabled for TV shows.");
                return Task.FromResult(Enumerable.Empty<IntroInfo>());
            }

            if (!isMovie && !isEpisode)
            {
                return Task.FromResult(Enumerable.Empty<IntroInfo>());
            }

            _logger.LogInformation(
                "CinemaModeTV: Getting intros for {Type} '{Name}'",
                isMovie ? "Movie" : "Episode",
                item.Name);

            var introManager = new IntroManager(_logger);
            return Task.FromResult(introManager.Get(item, user, isEpisode));
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllIntroFiles()
        {
            return Enumerable.Empty<string>();
        }
    }
}
