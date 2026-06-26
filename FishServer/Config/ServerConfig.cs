using System.Text.Json.Serialization;

namespace FishServer.Config
{
    /// <summary>
    /// 服务器配置类
    /// 包含服务器启动所需的各项参数
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// 监听IP地址，默认 0.0.0.0 表示监听所有网卡
        /// </summary>
        public string IP { get; set; } = "0.0.0.0";

        /// <summary>
        /// 监听端口，默认 8888
        /// </summary>
        public int Port { get; set; } = 8888;

        /// <summary>
        /// 最大连接数，0 表示不限制
        /// </summary>
        public int MaxConnections { get; set; } = 1000;

        /// <summary>
        /// 数据存储根路径（相对于工作目录）
        /// </summary>
        public string DataPath { get; set; } = "data";

        /// <summary>
        /// 玩家数据文件名
        /// </summary>
        public string PlayersFileName { get; set; } = "players.json";

        /// <summary>
        /// 数据自动保存间隔（秒），0 表示不自动保存
        /// </summary>
        public int AutoSaveIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 接收缓冲区大小（字节）
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 8192;

        /// <summary>
        /// 获取玩家数据文件完整路径
        /// </summary>
        public string GetPlayersFilePath()
        {
            return Path.Combine(DataPath, PlayersFileName);
        }

        /// <summary>
        /// 默认配置实例
        /// </summary>
        public static ServerConfig Default => new ServerConfig();
    }
}
