﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Interfaces;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine.Stages.Downloading
{
    internal sealed class DownloadManager : IDownloadManager
    {
        private readonly IDownloader _defaultDownloader;
        private readonly IPluginManager _pluginManager;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public DownloadManager(IPluginManager pluginManager, IDownloader defaultDownloader)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            _defaultDownloader = defaultDownloader ?? throw new ArgumentNullException(nameof(defaultDownloader));
        }

        public async Task Download(List<CrawledUrl> crawledUrls, string downloadDirectory)
        {
            for (int i = 0; i < crawledUrls.Count; i++)
            {
                CrawledUrl entry = crawledUrls[i];

                if (!UrlChecker.IsValidUrl(entry.Url))
                {
                    _logger.Error($"[{entry.PostId}] Invalid or blacklisted external entry of type {entry.UrlType}: {entry.Url}");
                    continue;
                }

                _logger.Info($"Downloading {i + 1}/{crawledUrls.Count}: {entry.Url}");

                _logger.Debug($"{entry.Url} is {entry.UrlType}");

                IDownloader downloader = await _pluginManager.GetDownloader(entry.Url) ?? _defaultDownloader;

                try
                {
                    await downloader.Download(entry, downloadDirectory);
                }
                catch (DownloadException ex)
                {
                    string logMessage = $"Download error: {ex.Message}";
                    if (ex.InnerException != null)
                        logMessage += $". Inner Exception: {ex.InnerException}";
                    _logger.Error(logMessage);
                }
            }
        }
    }
}
