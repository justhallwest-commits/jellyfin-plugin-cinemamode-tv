using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.CinemaModeTV.Configuration;

#nullable enable
namespace Jellyfin.Plugin.CinemaModeTV
{
    internal enum PreRollType
    {
        TrailerPreRoll,
        FeaturePreRoll,
    }

    internal class PreRollSelector
    {
        private PreRollType Category { get; }
        private Random RNG { get; }
        private BaseItem Feature { get; }
        private User User { get; }
        private string PreRollLibrary { get; }
        private List<PreRollSelectionConfig> PreRollsSelections { get; }
        private bool IgnoreOutOfSeason { get; }
        private bool EnforceRatingLimit { get; }
        private List<SeasonalTagDefinition> SeasonalTagDefinitions { get; }
        private readonly ILogger Logger;

        public PreRollSelector(PreRollType category, BaseItem feature, User user, PluginConfiguration config, ILogger logger)
        {
            Category = category;
            RNG = new Random();
            Feature = feature;
            User = user;
            Logger = logger;

            if (category == PreRollType.TrailerPreRoll)
            {
                PreRollLibrary = config.TrailerPreRollsLibrary;
                PreRollsSelections = config.TrailerPreRollsSelections;
                EnforceRatingLimit = config.TrailerPreRollsRatingLimit;
                SeasonalTagDefinitions = config.SeasonalTagDefinitions;
                IgnoreOutOfSeason = config.TrailerPreRollsIgnoreOutOfSeason;
            }
            else
            {
                PreRollLibrary = config.FeaturePreRollsLibrary;
                PreRollsSelections = config.FeaturePreRollsSelections;
                EnforceRatingLimit = config.FeaturePreRollsRatingLimit;
                SeasonalTagDefinitions = config.SeasonalTagDefinitions;
                IgnoreOutOfSeason = config.FeaturePreRollsIgnoreOutOfSeason;
            }
        }

        private bool IsInSeason(string tag)
        {
            SeasonalTagDefinition? definition = SeasonalTagDefinitions
                .Find(x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase));

            if (definition == null)
            {
                return true;
            }

            DateTime today = DateTime.Now;
            var startParts = definition.Start.Split('/');
            var endParts = definition.End.Split('/');

            if (startParts.Length != 2 || endParts.Length != 2)
            {
                Logger.LogWarning("CinemaModeTV: Invalid seasonal date format for tag '{Tag}'", tag);
                return true;
            }

            if (!int.TryParse(startParts[0], out int startMonth) ||
                !int.TryParse(startParts[1], out int startDay) ||
                !int.TryParse(endParts[0], out int endMonth) ||
                !int.TryParse(endParts[1], out int endDay))
            {
                return true;
            }

            DateTime start = new DateTime(today.Year, startMonth, startDay);
            DateTime end = new DateTime(today.Year, endMonth, endDay);

            if (start > end)
            {
                end = end.AddYears(1);
                if (today < start)
                {
                    today = today.AddYears(1);
                }
            }

            return today >= start && today <= end;
        }

        private bool CheckSeasonalTags(List<string> tags)
        {
            foreach (string tag in tags)
            {
                if (SeasonalTagDefinitions.Any(x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!IsInSeason(tag))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool CheckAnyTagRule(List<string> featureTags, List<string> preRollTags)
        {
            return preRollTags.Any(tag => featureTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
        }

        private static bool CheckAllTagRule(List<string> featureTags, List<string> preRollTags)
        {
            return preRollTags.All(tag => featureTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
        }

        private bool CheckRating(BaseItem preRoll)
        {
            if (!EnforceRatingLimit)
            {
                return true;
            }

            string? featureRating = Feature.OfficialRating;
            string? preRollRating = preRoll.OfficialRating;

            if (string.IsNullOrEmpty(featureRating) || string.IsNullOrEmpty(preRollRating))
            {
                return true;
            }

            string[] ratings = { "G", "PG", "PG-13", "R", "NC-17" };
            int featureIndex = Array.IndexOf(ratings, featureRating);
            int preRollIndex = Array.IndexOf(ratings, preRollRating);

            if (featureIndex == -1 || preRollIndex == -1)
            {
                return true;
            }

            return preRollIndex <= featureIndex;
        }

        /// <summary>
        /// Find the library folder by name using the root folder's children.
        /// Libraries are CollectionFolder/Folder types, NOT Movie items.
        /// </summary>
        private Folder? FindLibraryFolder(string libraryName)
        {
            if (Plugin.LibraryManager == null)
            {
                return null;
            }

            // Libraries are children of the root folder
            var rootFolder = Plugin.LibraryManager.RootFolder;
            if (rootFolder == null)
            {
                return null;
            }

            return rootFolder.Children
                .OfType<Folder>()
                .FirstOrDefault(f => string.Equals(f.Name, libraryName, StringComparison.OrdinalIgnoreCase));
        }

        private List<BaseItem> GetPreRolls()
        {
            var preRolls = new List<BaseItem>();

            if (string.IsNullOrEmpty(PreRollLibrary) || PreRollLibrary == "-")
            {
                return preRolls;
            }

            if (Plugin.LibraryManager == null)
            {
                Logger.LogWarning("CinemaModeTV: LibraryManager is null.");
                return preRolls;
            }

            // Find the library folder properly (it's a Folder/CollectionFolder, not a Movie)
            Folder? library = FindLibraryFolder(PreRollLibrary);
            if (library == null)
            {
                Logger.LogWarning("CinemaModeTV: Pre-roll library '{Library}' not found.", PreRollLibrary);
                return preRolls;
            }

            Logger.LogDebug("CinemaModeTV: Found pre-roll library '{Library}' (Id: {Id})", PreRollLibrary, library.Id);

            // Query for video items within this library
            var query = new InternalItemsQuery(User)
            {
                MediaTypes = new[] { MediaType.Video },
                IsVirtualItem = false,
                Recursive = true,
                ParentId = library.Id,
            };

            BaseItem[] items = Plugin.LibraryManager.GetItemList(query);
            Logger.LogDebug("CinemaModeTV: Found {Count} video items in pre-roll library '{Library}'", items.Length, PreRollLibrary);

            foreach (BaseItem item in items)
            {
                if (IgnoreOutOfSeason && !CheckSeasonalTags(item.Tags.ToList()))
                {
                    continue;
                }

                if (!CheckRating(item))
                {
                    continue;
                }

                preRolls.Add(item);
            }

            return preRolls;
        }

        private BaseItem? SelectPreRollByTags(List<BaseItem> preRolls, List<string> featureTags, bool allTags)
        {
            var matchingPreRolls = new List<BaseItem>();

            foreach (BaseItem preRoll in preRolls)
            {
                List<string> preRollTags = preRoll.Tags.ToList();
                bool match = allTags
                    ? CheckAllTagRule(featureTags, preRollTags)
                    : CheckAnyTagRule(featureTags, preRollTags);

                if (match)
                {
                    matchingPreRolls.Add(preRoll);
                }
            }

            if (matchingPreRolls.Count == 0)
            {
                return null;
            }

            return matchingPreRolls[RNG.Next(matchingPreRolls.Count)];
        }

        private BaseItem? SelectPreRollByGenre(List<BaseItem> preRolls)
        {
            List<string> featureGenres = Feature.Genres.ToList();
            var matchingPreRolls = preRolls
                .Where(p => p.Genres.Any(g => featureGenres.Contains(g, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            if (matchingPreRolls.Count == 0)
            {
                return null;
            }

            return matchingPreRolls[RNG.Next(matchingPreRolls.Count)];
        }

        private BaseItem? SelectPreRollByStudios(List<BaseItem> preRolls)
        {
            List<string> featureStudios = Feature.Studios.ToList();
            var matchingPreRolls = preRolls
                .Where(p => p.Studios.Any(s => featureStudios.Contains(s, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            if (matchingPreRolls.Count == 0)
            {
                return null;
            }

            return matchingPreRolls[RNG.Next(matchingPreRolls.Count)];
        }

        public BaseItem? SelectPreRoll()
        {
            List<BaseItem> preRolls = GetPreRolls();

            if (preRolls.Count == 0)
            {
                return null;
            }

            foreach (PreRollSelectionConfig config in PreRollsSelections)
            {
                BaseItem? selected = null;

                if (config.Name)
                {
                    var tags = new List<string> { Feature.Name };
                    selected = SelectPreRollByTags(preRolls, tags, config.AllTags);
                }
                else if (config.Year)
                {
                    if (Feature.ProductionYear.HasValue)
                    {
                        var tags = new List<string> { Feature.ProductionYear.Value.ToString() };
                        selected = SelectPreRollByTags(preRolls, tags, config.AllTags);
                    }
                }
                else if (config.Decade)
                {
                    if (Feature.ProductionYear.HasValue)
                    {
                        int decade = (Feature.ProductionYear.Value / 10) * 10;
                        var tags = new List<string> { decade.ToString() + "s" };
                        selected = SelectPreRollByTags(preRolls, tags, config.AllTags);
                    }
                }
                else if (config.Seasonal)
                {
                    var tags = SeasonalTagDefinitions.Select(x => x.Tag).ToList();
                    selected = SelectPreRollByTags(preRolls, tags, config.AllTags);
                }
                else if (config.Genre)
                {
                    selected = SelectPreRollByGenre(preRolls);
                }
                else if (config.Studios)
                {
                    selected = SelectPreRollByStudios(preRolls);
                }

                if (selected != null)
                {
                    Logger.LogDebug("CinemaModeTV: Selected pre-roll '{Name}' for {Type}", selected.Name, Category);
                    return selected;
                }
            }

            // Fallback: random pre-roll
            var fallback = preRolls[RNG.Next(preRolls.Count)];
            Logger.LogDebug("CinemaModeTV: Falling back to random pre-roll '{Name}' for {Type}", fallback.Name, Category);
            return fallback;
        }
    }

    internal class TrailerSelector
    {
        private Random RNG { get; }
        private BaseItem Feature { get; }
        private User User { get; }
        private List<TrailerSelectionConfig> TrailerSelections { get; }
        private bool EnforceRatingLimit { get; }
        private bool ConsumeMode { get; }
        private readonly ILogger Logger;

        public TrailerSelector(BaseItem feature, User user, PluginConfiguration config, ILogger logger)
        {
            RNG = new Random();
            Feature = feature;
            User = user;
            Logger = logger;
            TrailerSelections = config.TrailerSelectionRules;
            EnforceRatingLimit = config.EnforceRatingLimitTrailers;
            ConsumeMode = config.TrailerConsumeMode;
        }

        private bool CheckRating(BaseItem trailer)
        {
            if (!EnforceRatingLimit)
            {
                return true;
            }

            string? featureRating = Feature.OfficialRating;
            string? trailerRating = trailer.OfficialRating;

            if (string.IsNullOrEmpty(featureRating) || string.IsNullOrEmpty(trailerRating) ||
                featureRating == "Unrated" || trailerRating == "Unrated")
            {
                return true;
            }

            string[] ratings = { "G", "PG", "PG-13", "R", "NC-17" };
            int featureIndex = Array.IndexOf(ratings, featureRating);
            int trailerIndex = Array.IndexOf(ratings, trailerRating);

            if (featureIndex == -1 || trailerIndex == -1)
            {
                return true;
            }

            return trailerIndex <= featureIndex;
        }

        /// <summary>
        /// Get all video items across the library that could serve as trailers.
        /// Excludes the current feature item.
        /// </summary>
        private List<BaseItem> GetTrailerCandidates()
        {
            var trailers = new List<BaseItem>();

            if (Plugin.LibraryManager == null)
            {
                return trailers;
            }

            // Query for all video content - movies and trailers
            var query = new InternalItemsQuery(User)
            {
                MediaTypes = new[] { MediaType.Video },
                IsVirtualItem = false,
                Recursive = true,
            };

            BaseItem[] items = Plugin.LibraryManager.GetItemList(query);

            foreach (BaseItem item in items)
            {
                // Skip the feature itself
                if (item.Id == Feature.Id)
                {
                    continue;
                }

                // Only include Movies and Trailers as candidates
                if (item is not Movie && item is not Trailer)
                {
                    continue;
                }

                if (!CheckRating(item))
                {
                    continue;
                }

                trailers.Add(item);
            }

            return trailers;
        }

        private List<BaseItem> FilterByYear(List<BaseItem> trailers)
        {
            if (!Feature.ProductionYear.HasValue)
            {
                return trailers;
            }

            return trailers.Where(x => x.ProductionYear == Feature.ProductionYear).ToList();
        }

        private List<BaseItem> FilterByDecade(List<BaseItem> trailers)
        {
            if (!Feature.ProductionYear.HasValue)
            {
                return trailers;
            }

            int decade = (Feature.ProductionYear.Value / 10) * 10;
            return trailers.Where(x => x.ProductionYear.HasValue &&
                (x.ProductionYear.Value / 10) * 10 == decade).ToList();
        }

        private List<BaseItem> FilterByGenre(List<BaseItem> trailers)
        {
            List<string> featureGenres = Feature.Genres.ToList();
            return trailers.Where(x => x.Genres.Any(g => featureGenres.Contains(g))).ToList();
        }

        private List<BaseItem> FilterByRecentlyAdded(List<BaseItem> trailers)
        {
            DateTime cutoff = DateTime.Now.AddDays(-30);
            return trailers.Where(x => x.DateCreated >= cutoff)
                .OrderByDescending(x => x.DateCreated)
                .ToList();
        }

        private List<BaseItem> FilterByMoreLikeThis(List<BaseItem> trailers)
        {
            List<string> featureGenres = Feature.Genres.ToList();
            List<string> featureTags = Feature.Tags.ToList();

            return trailers
                .Select(t => new
                {
                    Trailer = t,
                    Score = t.Genres.Count(g => featureGenres.Contains(g)) * 2 +
                            t.Tags.Count(tag => featureTags.Contains(tag))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Trailer)
                .ToList();
        }

        private List<BaseItem> FilterByUnplayed(List<BaseItem> trailers)
        {
            return trailers.Where(x => !x.IsPlayed(User)).ToList();
        }

        public List<BaseItem> SelectTrailers(int count)
        {
            var selectedTrailers = new List<BaseItem>();
            List<BaseItem> allTrailers = GetTrailerCandidates();

            if (allTrailers.Count == 0)
            {
                Logger.LogDebug("CinemaModeTV: No trailer candidates found.");
                return selectedTrailers;
            }

            Logger.LogDebug("CinemaModeTV: Found {Count} trailer candidates.", allTrailers.Count);

            foreach (TrailerSelectionConfig config in TrailerSelections)
            {
                if (selectedTrailers.Count >= count)
                {
                    break;
                }

                var candidates = new List<BaseItem>();

                if (config.Year)
                {
                    candidates = FilterByYear(allTrailers);
                }
                else if (config.Decade)
                {
                    candidates = FilterByDecade(allTrailers);
                }
                else if (config.Genre)
                {
                    candidates = FilterByGenre(allTrailers);
                }
                else if (config.RecentlyAdded)
                {
                    candidates = FilterByRecentlyAdded(allTrailers);
                }
                else if (config.MoreLikeThis)
                {
                    candidates = FilterByMoreLikeThis(allTrailers);
                }
                else if (config.Unplayed)
                {
                    candidates = FilterByUnplayed(allTrailers);
                }

                // Remove already selected trailers
                candidates = candidates.Where(c => !selectedTrailers.Any(s => s.Id == c.Id)).ToList();

                if (candidates.Count > 0)
                {
                    if (ConsumeMode)
                    {
                        selectedTrailers.Add(candidates[RNG.Next(candidates.Count)]);
                    }
                    else
                    {
                        int toAdd = Math.Min(count - selectedTrailers.Count, candidates.Count);
                        var shuffled = candidates.OrderBy(_ => RNG.Next()).ToList();
                        selectedTrailers.AddRange(shuffled.Take(toAdd));
                    }
                }
            }

            // Fill remaining slots with random trailers
            while (selectedTrailers.Count < count)
            {
                var remaining = allTrailers.Where(t => !selectedTrailers.Any(s => s.Id == t.Id)).ToList();
                if (remaining.Count == 0)
                {
                    break;
                }

                selectedTrailers.Add(remaining[RNG.Next(remaining.Count)]);
            }

            Logger.LogDebug("CinemaModeTV: Selected {Count} trailers.", selectedTrailers.Count);
            return selectedTrailers.Take(count).ToList();
        }
    }

    /// <summary>
    /// Manages intro selection for Cinema Mode.
    /// </summary>
    public class IntroManager
    {
        private readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntroManager"/> class.
        /// </summary>
        public IntroManager(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Get intros for the given item.
        /// </summary>
        /// <param name="item">The item being played.</param>
        /// <param name="user">The user playing the item.</param>
        /// <param name="isEpisode">Whether the item is a TV episode.</param>
        /// <returns>Enumerable of intro info.</returns>
        public IEnumerable<IntroInfo> Get(BaseItem item, User user, bool isEpisode)
        {
            var intros = new List<IntroInfo>();
            var config = Plugin.Instance?.PluginConfiguration;

            if (config == null)
            {
                Logger.LogWarning("CinemaModeTV: Plugin configuration is null.");
                return intros;
            }

            // Trailer Pre-Roll
            var trailerPreRollSelector = new PreRollSelector(PreRollType.TrailerPreRoll, item, user, config, Logger);
            BaseItem? trailerPreRoll = trailerPreRollSelector.SelectPreRoll();
            if (trailerPreRoll != null)
            {
                intros.Add(new IntroInfo { ItemId = trailerPreRoll.Id, Path = trailerPreRoll.Path });
            }

            // Trailers - use the correct count based on content type
            int trailerCount = isEpisode ? config.NumberOfTrailersForTV : config.NumberOfTrailers;
            if (trailerCount > 0)
            {
                var trailerSelector = new TrailerSelector(item, user, config, Logger);
                List<BaseItem> trailers = trailerSelector.SelectTrailers(trailerCount);
                foreach (BaseItem trailer in trailers)
                {
                    intros.Add(new IntroInfo { ItemId = trailer.Id, Path = trailer.Path });
                }
            }

            // Feature Pre-Roll
            var featurePreRollSelector = new PreRollSelector(PreRollType.FeaturePreRoll, item, user, config, Logger);
            BaseItem? featurePreRoll = featurePreRollSelector.SelectPreRoll();
            if (featurePreRoll != null)
            {
                intros.Add(new IntroInfo { ItemId = featurePreRoll.Id, Path = featurePreRoll.Path });
            }

            Logger.LogInformation("CinemaModeTV: Returning {Count} intros for '{Name}'", intros.Count, item.Name);
            return intros;
        }
    }
}
