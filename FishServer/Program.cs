using FishServer.Config;
using System.Text.Json;

namespace FishServer
{
    /// <summary>
    /// 程序入口
    /// </summary>
    class Program
    {
        /// <summary>
        /// 服务器配置文件路径（相对于工作目录）
        /// </summary>
        private const string ConfigFileName = "serverconfig.json";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            // 打印启动横幅
            PrintBanner();

            // 读取配置
            ServerConfig config = LoadConfig();
            Console.WriteLine($"[Program] 配置加载完成: IP={config.IP}, Port={config.Port}, MaxConnections={config.MaxConnections}");
            Console.WriteLine($"[Program] 数据路径: {Path.GetFullPath(config.DataPath)}");

            // 创建并启动服务器
            var server = new GameServer(config);

            // 处理Ctrl+C优雅关闭
            using var cts = new CancellationTokenSource();
            var exitEvent = new ManualResetEventSlim(false);

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n[Program] 收到关闭信号 (Ctrl+C)，正在优雅关闭...");
                exitEvent.Set();
            };

            // 在后台启动服务器
            var serverTask = Task.Run(async () =>
            {
                try
                {
                    await server.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Program] 服务器运行异常: {ex.Message}");
                }
            });

            // 等待关闭信号
            exitEvent.Wait();

            // 优雅关闭
            await server.StopAsync();

            // 等待服务器任务结束
            try
            {
                await serverTask;
            }
            catch { }

            Console.WriteLine("[Program] 服务器已退出。按任意键关闭...");
        }

        /// <summary>
        /// 加载服务器配置
        /// 优先从配置文件加载，不存在则使用默认配置并保存一份
        /// </summary>
        private static ServerConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFileName))
                {
                    string json = File.ReadAllText(ConfigFileName);
                    var config = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json, _jsonOptions);
                    if (config != null)
                    {
                        Console.WriteLine($"[Program] 从 {ConfigFileName} 加载配置。");
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] 加载配置文件失败，使用默认配置: {ex.Message}");
            }

            // 使用默认配置并保存
            var defaultConfig = ServerConfig.Default;
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(defaultConfig, _jsonOptions);
                File.WriteAllText(ConfigFileName, json);
                Console.WriteLine($"[Program] 已生成默认配置文件: {ConfigFileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] 保存默认配置文件失败: {ex.Message}");
            }
            return defaultConfig;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// 打印启动横幅
        /// </summary>
        private static void PrintBanner()
        {
            Console.WriteLine("============================================");
            Console.WriteLine("    GoldFisher 钓鱼游戏服务器");
            Console.WriteLine("    (黄金矿工玩法换皮)");
            Console.WriteLine("============================================");
            Console.WriteLine();
        }
    }
}
