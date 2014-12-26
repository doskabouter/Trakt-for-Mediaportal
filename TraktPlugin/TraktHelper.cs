﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Profile;
using TraktPlugin.GUI;
using TraktPlugin.TraktAPI.DataStructures;
using TraktPlugin.TraktAPI.Extensions;
using TraktPlugin.TraktHandlers;

namespace TraktPlugin
{
    public enum SkinThemeType
    {
        File,
        Image
    }

    public class TraktHelper
    {
        #region Skin Helpers

        public static string GetCurrrentSkinTheme()
        {
            if (GUIThemeManager.CurrentThemeIsDefault)
                return null;

            return GUIThemeManager.CurrentTheme;
        }

        public static string GetThemedSkinFile(SkinThemeType type, string filename)
        {
            string originalFile = string.Empty;
            string themedFile = string.Empty;

            if (type == SkinThemeType.Image)
                originalFile = GUIGraphicsContext.Skin + "\\Media\\" + filename;
            else
                originalFile = GUIGraphicsContext.Skin + "\\" + filename;

            string currentTheme = GetCurrrentSkinTheme();

            if (string.IsNullOrEmpty(currentTheme))
                return originalFile;

            if (type == SkinThemeType.Image)
                themedFile = GUIGraphicsContext.Skin + "\\Themes\\" + currentTheme + "\\Media\\" + filename;
            else
                themedFile = GUIGraphicsContext.Skin + "\\Themes\\" + currentTheme + "\\" + filename;

            // if the theme does not contain file return original
            if (!File.Exists(themedFile))
                return originalFile;

            return themedFile;
        }

        #endregion

        #region Plugin Helpers

        public static bool IsPluginEnabled(string name)
        {
            using (Settings xmlreader = new MPSettings())
            {
                return xmlreader.GetValueAsBool("plugins", name, false);
            }
        }

        public static bool IsOnlineVideosAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "OnlineVideos.MediaPortal1.dll")) && (IsPluginEnabled("Online Videos") || IsPluginEnabled("OnlineVideos"));
            }
        }

        public static bool IsMovingPicturesAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MovingPictures.dll")) && IsPluginEnabled("Moving Pictures");
            }
        }

        public static bool IsMPTVSeriesAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MP-TVSeries.dll")) && IsPluginEnabled("MP-TV Series");
            }
        }

        public static bool IsMyFilmsAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MyFilms.dll")) && IsPluginEnabled("MyFilms");
            }
        }

        public static bool IsMyAnimeAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "Anime2.dll")) && IsPluginEnabled("My Anime");
            }
        }

        public static bool IsMpNZBAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "mpNZB.dll")) && IsPluginEnabled("mpNZB");
            }
        }

        public static bool IsMyTorrentsAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "MyTorrents.dll")) && IsPluginEnabled("MyTorrents");
            }
        }

        public static bool IsTrailersAvailableAndEnabled
        {
            get
            {
                return File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "Trailers.dll")) && IsPluginEnabled("Trailers");
            }
        }

        #endregion

        #region Public API Helpers

        #region Movie Watchlist

        public static void AddMovieToWatchList(TraktMovie movie, bool updatePluginFilters)
        {
            AddMovieToWatchList(movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Tmdb, movie.Ids.Trakt, updatePluginFilters);
        }

        public static void AddMovieToWatchList(string title, string year)
        {
            AddMovieToWatchList(title, year, null);
        }

        public static void AddMovieToWatchList(string title, string year, string imdbid)
        {
            AddMovieToWatchList(title, year, imdbid, false);
        }

        public static void AddMovieToWatchList(string title, string year, bool updatePluginFilters)
        {
            AddMovieToWatchList(title, year, null, updatePluginFilters);
        }

        public static void AddMovieToWatchList(string title, string year, string imdbid, bool updatePluginFilters)
        {
            AddMovieToWatchList(title, year.ToNullableInt32(), imdbid, null, updatePluginFilters);
        }

        public static void AddMovieToWatchList(string title, int? year, string imdbid, int? tmdbid, bool updatePluginFilters)
        {
            AddMovieToWatchList(title, year, imdbid, tmdbid, null, updatePluginFilters);
        }

        public static void AddMovieToWatchList(string title, int? year, string imdbid, int? tmdbid, int? traktid, bool updatePluginFilters)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var movie = new TraktMovie
            {
                Ids = new TraktMovieId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid
                },
                Title = title,
                Year = year
            };
            
            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddMovieToWatchlist(objSyncData as TraktMovie);
                if (response == null) return;

                if (updatePluginFilters && IsMovingPicturesAvailableAndEnabled)
                {
                    // update categories & filters menu in MovingPictures
                    MovingPictures.AddMovieCriteriaToWatchlistNode(imdbid);
                }
                GUI.GUIWatchListMovies.ClearCache(TraktSettings.Username);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(movie);

            TraktCache.AddMovieToWatchlist(movie);
        }

        public static void RemoveMovieFromWatchList(TraktMovie movie, bool updateMovingPicturesFilters)
        {
            RemoveMovieFromWatchList(movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Tmdb, movie.Ids.Trakt, updateMovingPicturesFilters);
        }

        public static void RemoveMovieFromWatchList(string title, int? year, string imdbid, int? tmdbid, bool updateMovingPicturesFilters)
        {
            RemoveMovieFromWatchList(title, year, imdbid, null, null, updateMovingPicturesFilters);

            // clear gui watchlist cache if removing from external source
            // we already handle internal removal for self
            GUI.GUIWatchListMovies.ClearCache(TraktSettings.Username);
        }

        public static void RemoveMovieFromWatchList(string title, int? year, string imdbid, int? tmdbid, int? traktid, bool updatePluginFilters)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var movie = new TraktMovie
            {
                Ids = new TraktMovieId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveMovieFromWatchlist(objSyncData as TraktMovie);
                if (response == null) return;

                if (updatePluginFilters && IsMovingPicturesAvailableAndEnabled)
                {
                    // update categories & filters menu in MovingPictures              
                    MovingPictures.RemoveMovieCriteriaFromWatchlistNode(imdbid);
                }
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(movie);

            TraktCache.RemoveMovieFromWatchlist(movie);
        }

        #endregion

        #region Show WatchList

        public static void AddShowToWatchList(TraktShow show)
        {
            AddShowToWatchList(show.Title, show.Year, show.Ids.Tvdb, show.Ids.Imdb, show.Ids.Tmdb, show.Ids.Trakt);
        }

        public static void AddShowToWatchList(string title, string year, string tvdbid)
        {
            AddShowToWatchList(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, null, null);
        }

        public static void AddShowToWatchList(string title, int? year, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var show = new TraktShow
            {
                Ids = new TraktShowId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid,
                    Tvdb = tvdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddShowToWatchlist(objSyncData as TraktShow);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(show);

            TraktCache.AddShowToWatchlist(show);
        }

        public static void RemoveShowFromWatchList(TraktShow show)
        {
            RemoveShowFromWatchList(show.Title, show.Year, show.Ids.Tvdb, show.Ids.Imdb, show.Ids.Tmdb, show.Ids.Trakt);
        }

        public static void RemoveShowFromWatchList(string title, string year, string tvdbid)
        {
            RemoveShowFromWatchList(title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, null, null);
        }

        public static void RemoveShowFromWatchList(string title, int? year, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var show = new TraktShow
            {
                Ids = new TraktShowId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid,
                    Tvdb = tvdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveShowFromWatchlist(objSyncData as TraktShow);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(show);

            TraktCache.RemoveShowFromWatchlist(show);
        }

        #endregion

        #region Episode WatchList

        public static void AddEpisodeToWatchList(TraktEpisode episode)
        {
            AddEpisodeToWatchList(episode.Title, episode.Season, episode.Number, episode.Ids.Tvdb, episode.Ids.Imdb, episode.Ids.Tmdb, episode.Ids.Trakt);
        }

        public static void AddEpisodeToWatchList(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var episode = new TraktEpisode
            {
                Ids = new TraktEpisodeId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid,
                    Tvdb = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddEpisodeToWatchlist(objSyncData as TraktEpisode);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(episode);
        }

        public static void RemoveEpisodeFromWatchList(TraktEpisode episode)
        {
            RemoveEpisodeFromWatchList(episode.Title, episode.Season, episode.Number, episode.Ids.Tvdb, episode.Ids.Imdb, episode.Ids.Tmdb, episode.Ids.Trakt);
        }

        public static void RemoveEpisodeFromWatchList(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var episode = new TraktEpisode
            {
                Ids = new TraktEpisodeId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid,
                    Tvdb = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveEpisodeFromWatchlist(objSyncData as TraktEpisode);
            })
            {
                IsBackground = true,
                Name = "Watchlist"
            };

            syncThread.Start(episode);

            TraktCache.RemoveEpisodeFromWatchlist(episode);
        }

        #endregion

        #region Add/Remove Movie in List

        public static void AddRemoveMovieInUserList(TraktMovie movie, bool remove)
        {
            AddRemoveMovieInUserList(TraktSettings.Username, movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Tmdb, movie.Ids.Trakt, remove);
        }

        public static void AddRemoveMovieInUserList(string title, string year, string imdbid, bool remove)
        {
            AddRemoveMovieInUserList(TraktSettings.Username, title, year.ToNullableInt32(), imdbid, null, null, remove);
        }

        public static void AddRemoveMovieInUserList(string username, string title, int? year, string imdbid, int? tmdbid, int? traktid, bool remove)
        {
            if (!GUICommon.CheckLogin(false)) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var customlists = result as IEnumerable<TraktListDetail>;

                    // get slug of lists selected
                    List<int> slugs = TraktLists.GetUserListSelections(customlists.ToList());
                    if (slugs == null || slugs.Count == 0) return;

                    // add the movie to add/remove to a new sync list
                    var items = new TraktSyncAll
                    {
                        Movies = new List<TraktMovie>
                        { 
                            new TraktMovie
                            {
                                Ids = new TraktMovieId
                                {
                                    Trakt = traktid,
                                    Imdb = imdbid,
                                    Tmdb = tmdbid
                                },
                                Title = title,
                                Year = year
                            }
                        }
                    };
                    
                    AddRemoveItemInList(slugs, items, remove);
                }
            }, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Show in List

        public static void AddRemoveShowInUserList(TraktShowSummary show, bool remove)
        {
            AddRemoveShowInUserList(TraktSettings.Username, show.Title, show.Year, show.Ids.Tvdb, show.Ids.Imdb, show.Ids.Tmdb, show.Ids.Trakt, remove);
        }

        public static void AddRemoveShowInUserList(string title, string year, string tvdbid, bool remove)
        {
            AddRemoveShowInUserList(TraktSettings.Username, title, year.ToNullableInt32(), tvdbid.ToNullableInt32(), null, null, null, remove);
        }

        public static void AddRemoveShowInUserList(string username, string title, int? year, int? tvdbid, string imdbid, int? tmdbid, int? traktid, bool remove)
        {
            if (!GUICommon.CheckLogin(false)) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var customlists = result as IEnumerable<TraktListDetail>;

                    // get slug of lists selected
                    List<int> slugs = TraktLists.GetUserListSelections(customlists.ToList());
                    if (slugs == null || slugs.Count == 0) return;

                    // add the movie to add/remove to a new sync list
                    var items = new TraktSyncAll
                    {
                        Shows = new List<TraktShow>
                        { 
                            new TraktShow
                            {
                                Ids = new TraktShowId
                                {
                                    Trakt = traktid,
                                    Imdb = imdbid,
                                    Tmdb = tmdbid,
                                    Tvdb = tvdbid
                                },
                                Title = title,
                                Year = year
                            }
                        }
                    };

                    AddRemoveItemInList(slugs, items, remove);
                }
            }, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Season in List

        internal static void AddRemoveSeasonInUserList(TraktSeason season, bool remove)
        {
            if (!GUICommon.CheckLogin(false)) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(TraktSettings.Username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var customlists = result as IEnumerable<TraktListDetail>;

                    // get slug of lists selected
                    List<int> slugs = TraktLists.GetUserListSelections(customlists.ToList());
                    if (slugs == null || slugs.Count == 0) return;

                    // add the movie to add/remove to a new sync list
                    var items = new TraktSyncAll
                    {
                        Seasons = new List<TraktSeason>
                        { 
                            new TraktSeason
                            {
                                Ids = new TraktSeasonId { Trakt = season.Ids.Trakt }
                            }
                        }
                    };

                    AddRemoveItemInList(slugs, items, remove);
                }
            }, Translation.GettingLists, true);
        }

        #endregion

        #region Add/Remove Episode in List
        
        internal static void AddRemoveEpisodeInUserList(TraktEpisode episode, bool remove)
        {
            if (!GUICommon.CheckLogin(false)) return;

            GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback(() =>
            {
                return TraktLists.GetListsForUser(TraktSettings.Username);
            },
            delegate(bool success, object result)
            {
                if (success)
                {
                    var customlists = result as IEnumerable<TraktListDetail>;

                    // get slug of lists selected
                    List<int> slugs = TraktLists.GetUserListSelections(customlists.ToList());
                    if (slugs == null || slugs.Count == 0) return;

                    // add the movie to add/remove to a new sync list
                    var items = new TraktSyncAll
                    {
                        Episodes = new List<TraktEpisode>
                        {
                            new TraktEpisode
                            {
                                Ids = episode.Ids
                            }
                        }
                    };

                    AddRemoveItemInList(slugs, items, remove);
                }
            }, Translation.GettingLists, true);
        }

        #endregion

        #region Related Movies

        public static void ShowRelatedMovies(TraktMovie movie)
        {
            ShowRelatedMovies(movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Tmdb, movie.Ids.Trakt);
        }

        public static void ShowRelatedMovies(string title, string year, string imdbid)
        {
            ShowRelatedMovies(title, year.ToNullableInt32(), imdbid, null, null);
        }

        public static void ShowRelatedMovies(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Related.Movies.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }
           
            RelatedMovie relatedMovie = new RelatedMovie
            {
                ImdbId = imdbid,
                TmdbId = tmdbid,
                TraktId = traktid,
                Title = title,
                Year = year
            };
            GUIRelatedMovies.relatedMovie = relatedMovie;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedMovies);
        }

        #endregion

        #region Related Shows

        public static void ShowRelatedShows(TraktShowSummary show)
        {
            ShowRelatedShows(show.Title, show.Ids.Tvdb, show.Ids.Tmdb, show.Ids.Trakt);
        }

        public static void ShowRelatedShows(string title, string tvdbid)
        {
            ShowRelatedShows(title, tvdbid.ToNullableInt32(), null, null);
        }

        public static void ShowRelatedShows(string title, int? tvdbid, int? tmdbid, int? traktid)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Related.Shows.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            RelatedShow relatedShow = new RelatedShow
            {
                TvdbId = tvdbid,
                TmdbId = tmdbid,
                TraktId = traktid,
                Title = title                
            };
            GUIRelatedShows.relatedShow = relatedShow;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.RelatedShows);
        }

        #endregion

        #region Movie Shouts

        public static void ShowMovieShouts(TraktMovieSummary movie)
        {
            ShowMovieShouts(movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Trakt, movie.IsWatched(), movie.Images.Fanart.LocalImageFilename(ArtworkType.MovieFanart), TraktSettings.DownloadFullSizeFanart ? movie.Images.Fanart.FullSize : movie.Images.Fanart.MediumSize);
        }

        public static void ShowMovieShouts(string imdbid, string title, string year, string fanart)
        {
            ShowMovieShouts(title, year, imdbid, fanart, null);
        }

        public static void ShowMovieShouts(string title, string year, string imdbid, bool isWatched, string fanart)
        {
            ShowMovieShouts(title, year.ToNullableInt32(), imdbid, null, false, fanart, null);
        }

        public static void ShowMovieShouts(string title, string imdbid, string year, string fanart, string onlineFanart = null)
        {
            ShowMovieShouts(title, year.ToNullableInt32(), imdbid, null, false, fanart, onlineFanart);
        }

        public static void ShowMovieShouts(string title, int? year, string imdbid, int? traktid, bool isWatched, string fanart, string onlineFanart)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            MovieShout movieInfo = new MovieShout
            {
                ImdbId = imdbid,
                Title = title,
                Year = year
            };
            GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.movie;
            GUIShouts.MovieInfo = movieInfo;
            GUIShouts.Fanart = fanart;
            GUIShouts.OnlineFanart = onlineFanart;
            GUIShouts.IsWatched = isWatched;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
        }

        #endregion

        #region Show Shouts

        public static void ShowTVShowShouts(TraktShowSummary show)
        {
            ShowTVShowShouts(show.Title, show.Ids.Tvdb, show.Ids.Trakt, show.IsWatched(), show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart), TraktSettings.DownloadFullSizeFanart ? show.Images.Fanart.FullSize : show.Images.Fanart.MediumSize);
        }

        public static void ShowTVShowShouts(string title, int? tvdbid, int? traktid, bool isWatched, string fanart, string onlineFanart = null)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            ShowShout seriesInfo = new ShowShout
            {
                TvdbId = tvdbid,
                TraktId = traktid,
                Title = title,
            };
            GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.show;
            GUIShouts.ShowInfo = seriesInfo;
            GUIShouts.Fanart = fanart;
            GUIShouts.IsWatched = isWatched;
            GUIShouts.OnlineFanart = onlineFanart;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
        }

        #endregion

        #region Episode Shouts

        public static void ShowEpisodeShouts(TraktShowSummary show, TraktEpisodeSummary episode)
        {
            ShowEpisodeShouts(show.Title, show.Ids.Tvdb, show.Ids.Trakt, episode.Season, episode.Number, episode.IsWatched(show), show.Images.Fanart.LocalImageFilename(ArtworkType.ShowFanart), TraktSettings.DownloadFullSizeFanart ? show.Images.Fanart.FullSize : show.Images.Fanart.MediumSize);
        }

        public static void ShowEpisodeShouts(string title, string tvdbid, string season, string episode, bool isWatched, string fanart, string onlineFanart = null)
        {
            ShowEpisodeShouts(title, tvdbid.ToNullableInt32(), null, season.ToInt(), episode.ToInt(), isWatched, fanart, onlineFanart);
        }

        public static void ShowEpisodeShouts(string title, int? tvdbid, int? traktid, int season, int episode, bool isWatched, string fanart, string onlineFanart = null)
        {
            if (!File.Exists(GUIGraphicsContext.Skin + @"\Trakt.Shouts.xml"))
            {
                // let user know they need to update skin or harass skin author
                GUIUtils.ShowOKDialog(GUIUtils.PluginName(), Translation.SkinOutOfDate);
                return;
            }

            EpisodeShout episodeInfo = new EpisodeShout
            {
                TvdbId = tvdbid,
                TraktId = traktid,
                Title = title,
                SeasonIdx = season,
                EpisodeIdx = episode
            };

            GUIShouts.ShoutType = GUIShouts.ShoutTypeEnum.episode;
            GUIShouts.EpisodeInfo = episodeInfo;
            GUIShouts.Fanart = fanart;
            GUIShouts.OnlineFanart = onlineFanart;
            GUIShouts.IsWatched = isWatched;
            GUIWindowManager.ActivateWindow((int)TraktGUIWindows.Shouts);
        }

        #endregion

        #region Movie Watched History

        public static void AddMovieToWatchHistory(TraktMovie movie)
        {
            AddMovieToWatchHistory(movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Tmdb, movie.Ids.Trakt);
        }

        public static void AddMovieToWatchHistory(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            var movie = new TraktSyncMovieWatched
            {
                Ids = new TraktMovieId { Imdb = imdbid, Tmdb = tmdbid, Trakt = traktid },
                Title = title,
                Year = year,
                WatchedAt = DateTime.UtcNow.ToISO8601()
            };

            var syncThread = new Thread((objMovie) =>
            {
                TraktAPI.TraktAPI.AddMovieToWatchedHistory(objMovie as TraktSyncMovieWatched);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            syncThread.Start(movie);

            TraktCache.AddMovieToWatchHistory(movie);
        }

        public static void RemoveMovieFromWatchHistory(TraktMovie movie)
        {
            RemoveMovieFromWatchHistory(movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Tmdb, movie.Ids.Trakt);
        }

        public static void RemoveMovieFromWatchHistory(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            var movie = new TraktMovie
            {
                Ids = new TraktMovieId { Imdb = imdbid, Tmdb = tmdbid, Trakt = traktid },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objMovie) =>
            {
                var syncData = objMovie as TraktMovie;
                TraktAPI.TraktAPI.RemoveMovieFromWatchedHistory(syncData);
            })
            {
                IsBackground = true,
                Name = "MarkUnWatched"
            };

            syncThread.Start(movie);

            TraktCache.RemoveMovieFromWatchHistory(movie);
        }

        #endregion

        #region Episode Watched History

        public static void AddEpisodeToWatchHistory(TraktEpisode episode)
        {
            AddEpisodeToWatchHistory(episode.Title, episode.Season, episode.Number, episode.Ids.Tvdb, episode.Ids.Imdb, episode.Ids.Tmdb, episode.Ids.Trakt);
        }

        public static void AddEpisodeToWatchHistory(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            var episode = new TraktSyncEpisodeWatched
            {
                Ids = new TraktEpisodeId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid,
                    Tvdb = tvdbid
                },
                Title = title,
                Season = season,
                Number = number,
                WatchedAt = DateTime.UtcNow.ToISO8601()
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddEpisodeToWatchedHistory(objSyncData as TraktSyncEpisodeWatched);
            })
            {
                IsBackground = true,
                Name = "MarkWatched"
            };

            syncThread.Start(episode);
        }

        public static void RemoveEpisodeFromWatchHistory(TraktEpisode episode)
        {
            RemoveEpisodeFromWatchHistory(episode.Title, episode.Season, episode.Number, episode.Ids.Tvdb, episode.Ids.Imdb, episode.Ids.Tmdb, episode.Ids.Trakt);
        }

        public static void RemoveEpisodeFromWatchHistory(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            var episode = new TraktEpisode
            {
                Ids = new TraktEpisodeId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid,
                    Tvdb = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveEpisodeFromWatchedHistory(objSyncData as TraktEpisode);
            })
            {
                IsBackground = true,
                Name = "MarkUnWatched"
            };

            syncThread.Start(episode);
        }

        #endregion

        #region Movie Collection

        public static void AddMovieToCollection(TraktMovie movie)
        {
            AddMovieToCollection(movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Tmdb, movie.Ids.Trakt);
        }

        public static void AddMovieToCollection(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var movie = new TraktSyncMovieCollected
            {
                Ids = new TraktMovieId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddMovieToCollection(objSyncData as TraktSyncMovieCollected);
            })
            {
                IsBackground = true,
                Name = "AddCollection"
            };

            syncThread.Start(movie);

            TraktCache.AddMovieToCollection(movie);
        }

        public static void RemoveMovieFromCollection(TraktMovie movie)
        {
            RemoveMovieFromLibrary(movie.Title, movie.Year, movie.Ids.Imdb, movie.Ids.Tmdb, movie.Ids.Trakt);
        }

        public static void RemoveMovieFromLibrary(string title, int? year, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var movie = new TraktMovie
            {
                Ids = new TraktMovieId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid
                },
                Title = title,
                Year = year
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveMovieFromCollection(objSyncData as TraktMovie);
            })
            {
                IsBackground = true,
                Name = "RemoveCollection"
            };

            syncThread.Start(movie);

            TraktCache.RemoveMovieFromCollection(movie);
        }

        #endregion

        #region Episode Collection

        public static void AddEpisodeToCollection(TraktEpisode episode)
        {
            AddEpisodeToCollection(episode.Title, episode.Season, episode.Number, episode.Ids.Tvdb, episode.Ids.Imdb, episode.Ids.Tmdb, episode.Ids.Trakt);
        }

        public static void AddEpisodeToCollection(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var episode = new TraktSyncEpisodeCollected
            {
                Ids = new TraktEpisodeId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid,
                    Tvdb = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.AddEpisodeToCollection(objSyncData as TraktSyncEpisodeCollected);
            })
            {
                IsBackground = true,
                Name = "AddCollection"
            };

            syncThread.Start(episode);
        }

        public static void RemoveEpisodeFromCollection(TraktEpisode episode)
        {
            RemoveEpisodeFromCollection(episode.Title, episode.Season, episode.Number, episode.Ids.Tvdb, episode.Ids.Imdb, episode.Ids.Tmdb, episode.Ids.Trakt);
        }

        public static void RemoveEpisodeFromCollection(string title, int season, int number, int? tvdbid, string imdbid, int? tmdbid, int? traktid)
        {
            if (!GUICommon.CheckLogin(false)) return;

            var episode = new TraktEpisode
            {
                Ids = new TraktEpisodeId
                {
                    Trakt = traktid,
                    Imdb = imdbid,
                    Tmdb = tmdbid,
                    Tvdb = tvdbid
                },
                Title = title,
                Season = season,
                Number = number
            };

            var syncThread = new Thread((objSyncData) =>
            {
                var response = TraktAPI.TraktAPI.RemoveEpisodeFromCollection(objSyncData as TraktEpisode);
            })
            {
                IsBackground = true,
                Name = "RemoveCollection"
            };

            syncThread.Start(episode);
        }

        #endregion

        #endregion

        #region Internal Helpers

        #region Add / Remove List Items

        internal static void AddRemoveItemInList(int listId, TraktSyncAll items, bool remove)
        {
            AddRemoveItemInList(new List<int> { listId }, items, remove);
        }

        internal static void AddRemoveItemInList(List<int> listIds, TraktSyncAll items, bool remove)
        {
            var listThread = new Thread((o) =>
            {
                foreach (int listId in listIds)
                {
                    TraktSyncResponse response = null;
                    if (!remove)
                    {
                        response = TraktAPI.TraktAPI.AddItemsToList(TraktSettings.Username, listId.ToString(), items);
                    }
                    else
                    {
                        response = TraktAPI.TraktAPI.RemoveItemsFromList(TraktSettings.Username, listId.ToString(), items);
                    }

                    if (response != null)
                    {
                        // clear current items in any lists
                        // list items will be refreshed online if we try to request them
                        TraktLists.ClearItemsInList(TraktSettings.Username, listId);

                        // update MovingPictures Categories and Filters menu
                        if (items.Movies != null && items.Movies.Count > 0 && IsMovingPicturesAvailableAndEnabled)
                        {
                            // we need the name of the list so get list from slug first
                            var userLists = TraktLists.GetListsForUser(TraktSettings.Username);
                            if (userLists == null) continue;

                            // get the list
                            var userList = userLists.FirstOrDefault(l => l.Ids.Trakt == listId);
                            if (userList == null) continue;
                            
                            if (remove)
                            {
                                MovingPictures.RemoveMovieCriteriaFromCustomlistNode(userList.Name, items.Movies.First().Ids.Imdb);
                            }
                            else
                            {
                                MovingPictures.AddMovieCriteriaToCustomlistNode(userList.Name, items.Movies.First().Ids.Imdb);
                            }
                        }
                    }
                }
            })
            {
                Name = remove ? "RemoveList" : "AddList",
                IsBackground = true
            };

            listThread.Start();
        }

        #endregion

        #endregion
    }
}
