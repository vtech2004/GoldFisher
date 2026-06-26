using System.Collections.Concurrent;
using System.Text.Json;
using FishServer.Config;
using FishServer.Models;

namespace FishServer.Storage
{
    /// <summary>
    /// 文件存储管理器
    /// 负责玩家数据的JSON序列化持久化
    /// 存储路径：FishServer/data/players.json
    /// </summary>
    public class FileStorage
    {
        private readonly ServerConfig _config;
        private readonly object _fileLock = new object();
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// 存储的数据结构（与文件内容对应）
        /// </summary>
        [Serializable]
        private class StorageData
        {
            /// <summary>所有玩家数据：PlayerId -> PlayerData</summary>
            public Dictionary<string, PlayerData> Players { get; set; } = new Dictionary<string, PlayerData>();
        }

        public FileStorage(ServerConfig config)
        {
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
            };
        }

        /// <summary>
        /// 从文件加载所有玩家数据
        /// 如果文件不存在或读取失败，返回空字典
        /// </summary>
        public ConcurrentDictionary<string, PlayerData> Load()
        {
            var result = new ConcurrentDictionary<string, PlayerData>();
            string filePath = _config.GetPlayersFilePath();

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"[FileStorage] 数据文件不存在，将创建新文件: {filePath}");
                    return result;
                }

                string json;
                lock (_fileLock)
                {
                    json = File.ReadAllText(filePath);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("[FileStorage] 数据文件为空，使用空数据。");
                    return result;
                }

                var data = JsonSerializer.Deserialize<StorageData>(json, _jsonOptions);
                if (data?.Players != null)
                {
                    foreach (var kv in data.Players)
                    {
                        result[kv.Key] = kv.Value;
                    }
                    Console.WriteLine($"[FileStorage] 成功加载 {result.Count} 个玩家数据。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileStorage] 加载数据失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 保存所有玩家数据到文件
        /// </summary>
        public void Save(ConcurrentDictionary<string, PlayerData> players)
        {
            string filePath = _config.GetPlayersFilePath();
            try
            {
                // 确保目录存在
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var storageData = new StorageData
                {
                    Players = new Dictionary<string, PlayerData>(players)
                };

                string json = JsonSerializer.Serialize(storageData, _jsonOptions);

                lock (_fileLock)
                {
                    // 先写入临时文件再替换，避免写入过程中崩溃导致数据损坏
                    string tempFile = filePath + ".tmp";
                    File.WriteAllText(tempFile, json);
                    if (File.Exists(filePath))
                    {
                        File.Replace(tempFile, filePath, null);
                    }
                    else
                    {
                        File.Move(tempFile, filePath);
                    }
                }

                Console.WriteLine($"[FileStorage] 成功保存 {players.Count} 个玩家数据到 {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileStorage] 保存数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动定时自动保存任务
        /// </summary>
        /// <param name="players">玩家数据字典</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task StartAutoSaveAsync(ConcurrentDictionary<string, PlayerData> players, CancellationToken cancellationToken)
        {
            int interval = _config.AutoSaveIntervalSeconds;
            if (interval <= 0)
            {
                Console.WriteLine("[FileStorage] 自动保存已禁用（间隔<=0）。");
                return;
            }

            Console.WriteLine($"[FileStorage] 自动保存已启动，间隔 {interval} 秒。");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(interval * 1000, cancellationToken);
                    Save(players);
                }
            }
            catch (TaskCanceledException)
            {
                // 正常关闭
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileStorage] 自动保存任务异常: {ex.Message}");
            }
        }
    }
}
