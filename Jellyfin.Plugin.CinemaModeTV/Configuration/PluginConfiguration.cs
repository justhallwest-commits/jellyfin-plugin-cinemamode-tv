using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CinemaModeTV.Configuration
{
    /// <summary>
    /// Pre-roll selection rule configuration.
    /// </summary>
    public class PreRollSelectionConfig
    {
        /// <summary>Gets or sets a value indicating whether to match by name.</summary>
        public bool Name { get; set; }

        /// <summary>Gets or sets a value indicating whether to match by year.</summary>
        public bool Year { get; set; }

        /// <summary>Gets or sets a value indicating whether to match by decade.</summary>
        public bool Decade { get; set; }

        /// <summary>Gets or sets a value indicating whether to match by seasonal tag.</summary>
        public bool Seasonal { get; set; }

        /// <summary>Gets or sets a value indicating whether to match by genre.</summary>
        public bool Genre { get; set; }

        /// <summary>Gets or sets a value indicating whether to match by studio.</summary>
        public bool Studios { get; set; }

        /// <summary>Gets or sets a value indicating whether all tags must match.</summary>
        public bool AllTags { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreRollSelectionConfig"/> class.
        /// </summary>
        public PreRollSelectionConfig()
        {
            Name = false;
            Year = false;
            Decade = false;
            Seasonal = false;
            Genre = false;
            Studios = false;
            AllTags = false;
        }
    }

    /// <summary>
    /// Trailer selection rule configuration.
    /// </summary>
    public class TrailerSelectionConfig
    {
        /// <summary>Gets or sets a value indicating whether to filter by year.</summary>
        public bool Year { get; set; }

        /// <summary>Gets or sets a value indicating whether to filter by decade.</summary>
        public bool Decade { get; set; }

        /// <summary>Gets or sets a value indicating whether to filter by genre.</summary>
        public bool Genre { get; set; }

        /// <summary>Gets or sets a value indicating whether to filter by recently added.</summary>
        public bool RecentlyAdded { get; set; }

        /// <summary>Gets or sets a value indicating whether to filter by similarity.</summary>
        public bool MoreLikeThis { get; set; }

        /// <summary>Gets or sets a value indicating whether to filter by unplayed status.</summary>
        public bool Unplayed { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrailerSelectionConfig"/> class.
        /// </summary>
        public TrailerSelectionConfig()
        {
            Year = false;
            Decade = false;
            Genre = false;
            RecentlyAdded = false;
            MoreLikeThis = false;
            Unplayed = false;
        }
    }

    /// <summary>
    /// Seasonal tag definition for time-based pre-roll matching.
    /// </summary>
    public class SeasonalTagDefinition
    {
        /// <summary>Gets or sets the tag name.</summary>
        public string Tag { get; set; }

        /// <summary>Gets or sets the start date (MM/DD).</summary>
        public string Start { get; set; }

        /// <summary>Gets or sets the end date (MM/DD).</summary>
        public string End { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonalTagDefinition"/> class.
        /// </summary>
        public SeasonalTagDefinition()
        {
            Tag = string.Empty;
            Start = string.Empty;
            End = string.Empty;
        }
    }

    /// <summary>
    /// Plugin configuration for Cinema Mode TV.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>Gets or sets the trailer pre-roll library name.</summary>
        public string TrailerPreRollsLibrary { get; set; }

        /// <summary>Gets or sets the feature pre-roll library name.</summary>
        public string FeaturePreRollsLibrary { get; set; }

        /// <summary>Gets or sets the feature pre-roll selection rules.</summary>
        public List<PreRollSelectionConfig> FeaturePreRollsSelections { get; set; }

        /// <summary>Gets or sets the trailer pre-roll selection rules.</summary>
        public List<PreRollSelectionConfig> TrailerPreRollsSelections { get; set; }

        /// <summary>Gets or sets a value indicating whether to enforce rating limits on trailer pre-rolls.</summary>
        public bool TrailerPreRollsRatingLimit { get; set; }

        /// <summary>Gets or sets a value indicating whether to enforce rating limits on feature pre-rolls.</summary>
        public bool FeaturePreRollsRatingLimit { get; set; }

        /// <summary>Gets or sets a value indicating whether to ignore out-of-season trailer pre-rolls.</summary>
        public bool TrailerPreRollsIgnoreOutOfSeason { get; set; }

        /// <summary>Gets or sets a value indicating whether to ignore out-of-season feature pre-rolls.</summary>
        public bool FeaturePreRollsIgnoreOutOfSeason { get; set; }

        /// <summary>Gets or sets the seasonal tag definitions.</summary>
        public List<SeasonalTagDefinition> SeasonalTagDefinitions { get; set; }

        /// <summary>Gets or sets the trailer selection rules.</summary>
        public List<TrailerSelectionConfig> TrailerSelectionRules { get; set; }

        /// <summary>Gets or sets a value indicating whether to enforce rating limits on trailers.</summary>
        public bool EnforceRatingLimitTrailers { get; set; }

        /// <summary>Gets or sets the number of trailers for movies.</summary>
        public int NumberOfTrailers { get; set; }

        /// <summary>Gets or sets a value indicating whether consume mode is on.</summary>
        public bool TrailerConsumeMode { get; set; }

        /// <summary>Gets or sets a value indicating whether Cinema Mode is enabled for TV shows.</summary>
        public bool EnableForTVShows { get; set; }

        /// <summary>Gets or sets a value indicating whether Cinema Mode is enabled for movies.</summary>
        public bool EnableForMovies { get; set; }

        /// <summary>Gets or sets the number of trailers for TV shows.</summary>
        public int NumberOfTrailersForTV { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            TrailerPreRollsLibrary = "-";
            FeaturePreRollsLibrary = "-";
            TrailerPreRollsSelections = new List<PreRollSelectionConfig>();
            FeaturePreRollsSelections = new List<PreRollSelectionConfig>();
            TrailerPreRollsRatingLimit = true;
            FeaturePreRollsRatingLimit = true;
            TrailerPreRollsIgnoreOutOfSeason = true;
            FeaturePreRollsIgnoreOutOfSeason = true;
            SeasonalTagDefinitions = new List<SeasonalTagDefinition>();
            TrailerSelectionRules = new List<TrailerSelectionConfig>();
            NumberOfTrailers = 2;
            EnforceRatingLimitTrailers = true;
            TrailerConsumeMode = false;
            EnableForTVShows = true;
            EnableForMovies = true;
            NumberOfTrailersForTV = 1;
        }
    }
}
