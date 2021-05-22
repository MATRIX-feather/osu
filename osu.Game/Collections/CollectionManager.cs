// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.IO.Legacy;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Collections
{
    /// <summary>
    /// Handles user-defined collections of beatmaps.
    /// </summary>
    /// <remarks>
    /// This is currently reading and writing from the osu-stable file format. This is a temporary arrangement until we refactor the
    /// database backing the game. Going forward writing should be done in a similar way to other model stores.
    /// </remarks>
    public class CollectionManager : Component
    {
        /// <summary>
        /// Database version in stable-compatible YYYYMMDD format.
        /// </summary>
        private const int database_version = 30000000;

        private const string database_name = "collection.db";

        public readonly BindableList<BeatmapCollection> Collections = new BindableList<BeatmapCollection>();

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; }

        private readonly Storage storage;

        public CollectionManager(Storage storage)
        {
            this.storage = storage;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Collections.CollectionChanged += collectionsChanged;

            if (storage.Exists(database_name))
            {
                using (var stream = storage.GetStream(database_name))
                    importCollections(readCollections(stream));
            }
        }

        private void collectionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var c in e.NewItems.Cast<BeatmapCollection>())
                        c.Changed += backgroundSave;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var c in e.OldItems.Cast<BeatmapCollection>())
                        c.Changed -= backgroundSave;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var c in e.OldItems.Cast<BeatmapCollection>())
                        c.Changed -= backgroundSave;

                    foreach (var c in e.NewItems.Cast<BeatmapCollection>())
                        c.Changed += backgroundSave;
                    break;
            }

            backgroundSave();
        }

        /// <summary>
        /// Set an endpoint for notifications to be posted to.
        /// </summary>
        public Action<Notification> PostNotification { protected get; set; }

        /// <summary>
        /// This is a temporary method and will likely be replaced by a full-fledged (and more correctly placed) migration process in the future.
        /// </summary>
        public Task ImportFromStableAsync(StableStorage stableStorage)
        {
            if (!stableStorage.Exists(database_name))
            {
                // This handles situations like when the user does not have a collections.db file
                Logger.Log($"No {database_name} available in osu!stable installation", LoggingTarget.Information, LogLevel.Error);
                return Task.CompletedTask;
            }

            return Task.Run(async () =>
            {
                using (var stream = stableStorage.GetStream(database_name))
                    await Import(stream).ConfigureAwait(false);
            });
        }

        public async Task Import(Stream stream)
        {
            var notification = new ProgressNotification
            {
                State = ProgressNotificationState.Active,
                Text = "正在导入收藏夹..."
            };

            PostNotification?.Invoke(notification);

            var collections = readCollections(stream, notification);
            await importCollections(collections).ConfigureAwait(false);

            notification.CompletionText = $"已导入 {collections.Count} 个收藏夹";
            notification.State = ProgressNotificationState.Completed;
        }

        private Task importCollections(List<BeatmapCollection> newCollections)
        {
            var tcs = new TaskCompletionSource<bool>();

            Schedule(() =>
            {
                try
                {
                    foreach (var newCol in newCollections)
                    {
                        var existing = Collections.FirstOrDefault(c => c.Name.Value == newCol.Name.Value);
                        if (existing == null)
                            Collections.Add(existing = new BeatmapCollection { Name = { Value = newCol.Name.Value } });

                        foreach (var newBeatmap in newCol.Beatmaps)
                        {
                            if (!existing.Beatmaps.Contains(newBeatmap))
                                existing.Beatmaps.Add(newBeatmap);
                        }
                    }

                    tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to import collection.");
                    tcs.SetException(e);
                }
            });

            return tcs.Task;
        }

        private List<BeatmapCollection> readCollections(Stream stream, ProgressNotification notification = null)
        {
            if (notification != null)
            {
                notification.Text = "Reading collections...";
                notification.Progress = 0;
            }

            var result = new List<BeatmapCollection>();

            try
            {
                using (var sr = new SerializationReader(stream))
                {
                    sr.ReadInt32(); // Version

                    int collectionCount = sr.ReadInt32();
                    result.Capacity = collectionCount;

                    for (int i = 0; i < collectionCount; i++)
                    {
                        if (notification?.CancellationToken.IsCancellationRequested == true)
                            return result;

                        var collection = new BeatmapCollection { Name = { Value = sr.ReadString() } };
                        int mapCount = sr.ReadInt32();

                        for (int j = 0; j < mapCount; j++)
                        {
                            if (notification?.CancellationToken.IsCancellationRequested == true)
                                return result;

                            string checksum = sr.ReadString();

                            var beatmap = beatmaps.QueryBeatmap(b => b.MD5Hash == checksum);
                            if (beatmap != null)
                                collection.Beatmaps.Add(beatmap);
                        }

                        if (notification != null)
                        {
                            notification.Text = $"Imported {i + 1} of {collectionCount} collections";
                            notification.Progress = (float)(i + 1) / collectionCount;
                        }

                        result.Add(collection);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to read collection database.");
            }

            return result;
        }

        public void DeleteAll()
        {
            Collections.Clear();
            PostNotification?.Invoke(new ProgressCompletionNotification { Text = "已删除所有收藏夹!" });
        }

        private readonly object saveLock = new object();
        private int lastSave;
        private int saveFailures;

        /// <summary>
        /// Perform a save with debounce.
        /// </summary>
        private void backgroundSave()
        {
            var current = Interlocked.Increment(ref lastSave);
            Task.Delay(100).ContinueWith(task =>
            {
                if (current != lastSave)
                    return;

                if (!save())
                    backgroundSave();
            });
        }

        private bool save()
        {
            lock (saveLock)
            {
                Interlocked.Increment(ref lastSave);

                try
                {
                    // This is NOT thread-safe!!

                    using (var sw = new SerializationWriter(storage.GetStream(database_name, FileAccess.Write)))
                    {
                        sw.Write(database_version);
                        sw.Write(Collections.Count);

                        foreach (var c in Collections)
                        {
                            sw.Write(c.Name.Value);
                            sw.Write(c.Beatmaps.Count);

                            foreach (var b in c.Beatmaps)
                                sw.Write(b.MD5Hash);
                        }
                    }

                    if (saveFailures < 10)
                        saveFailures = 0;
                    return true;
                }
                catch (Exception e)
                {
                    // Since this code is not thread-safe, we may run into random exceptions (such as collection enumeration or out of range indexing).
                    // Failures are thus only alerted if they exceed a threshold (once) to indicate "actual" errors having occurred.
                    if (++saveFailures == 10)
                        Logger.Error(e, "Failed to save collection database!");
                }

                return false;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            save();
        }
    }
}
