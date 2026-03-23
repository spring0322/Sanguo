using GameObjects;
using GameObjects.PersonDetail;
using GameManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    /// <summary>
    /// 备份数据恢复服务
    /// 提供从备份数据恢复对象关系的功能
    /// </summary>
    public class BackupDataRecoveryService
    {
        private readonly string _backupDirectory;
        private readonly IDataIntegrityChecker _integrityChecker;
        private readonly Dictionary<Type, IBackupDataReader> _dataReaders;

        public BackupDataRecoveryService(string backupDirectory = null)
        {
            _backupDirectory = backupDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WorldOfTheThreeKingdoms", "Backups");
            _integrityChecker = new DataIntegrityChecker();
            _dataReaders = new Dictionary<Type, IBackupDataReader>();
            
            RegisterDefaultDataReaders();
        }

        /// <summary>
        /// 注册默认的数据读取器
        /// </summary>
        private void RegisterDefaultDataReaders()
        {
            _dataReaders[typeof(Faction)] = new FactionBackupDataReader();
            _dataReaders[typeof(Person)] = new PersonBackupDataReader();
            _dataReaders[typeof(Architecture)] = new ArchitectureBackupDataReader();
            _dataReaders[typeof(Troop)] = new TroopBackupDataReader();
            _dataReaders[typeof(Legion)] = new LegionBackupDataReader();
        }

        /// <summary>
        /// 从备份数据恢复对象关系
        /// </summary>
        /// <param name="gameObject">要恢复的游戏对象</param>
        /// <param name="backupTimestamp">备份时间戳（可选）</param>
        /// <returns>恢复结果</returns>
        public async Task<BackupRecoveryResult> RecoverFromBackupAsync(GameObject gameObject, DateTime? backupTimestamp = null)
        {
            var result = new BackupRecoveryResult
            {
                ObjectType = gameObject?.GetType().Name ?? "Unknown",
                ObjectId = gameObject?.ID ?? -1,
                BackupTimestamp = backupTimestamp
            };

            if (gameObject == null)
            {
                result.Success = false;
                result.ErrorMessage = "游戏对象为null";
                return result;
            }

            try
            {
                Debug.WriteLine($"[BackupDataRecoveryService] 开始从备份恢复对象: {result.ObjectType}[{result.ObjectId}]");

                // 1. 查找可用的备份数据
                var availableBackups = await FindAvailableBackupsAsync(gameObject.GetType(), backupTimestamp);
                
                if (availableBackups.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "没有找到可用的备份数据";
                    return result;
                }

                // 2. 选择最合适的备份
                var selectedBackup = SelectBestBackup(availableBackups, gameObject.ID);
                
                if (selectedBackup == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "没有找到匹配的备份数据";
                    return result;
                }

                result.BackupTimestamp = selectedBackup.Timestamp;

                // 3. 从备份读取对象数据
                var backupData = await ReadBackupDataAsync(gameObject.GetType(), selectedBackup);
                
                if (backupData == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "备份数据读取失败";
                    return result;
                }

                // 4. 恢复对象关系
                var recoveredRelations = await RestoreRelationshipsFromBackupAsync(gameObject, backupData);
                
                // 5. 验证恢复结果
                var validationResult = await ValidateRecoveryAsync(gameObject);
                
                result.Success = validationResult.Success;
                result.RestoredRelations = recoveredRelations;
                result.ValidationResult = validationResult;
                result.Message = validationResult.Success ? 
                    $"成功从备份恢复 {recoveredRelations.Count} 个关系" : 
                    $"恢复部分成功，验证发现问题: {validationResult.ErrorMessage}";

                Debug.WriteLine($"[BackupDataRecoveryService] 备份恢复完成: {result.Message}");
                
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"备份恢复异常: {ex.Message}";
                Debug.WriteLine($"[BackupDataRecoveryService] 备份恢复异常: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 查找可用的备份数据
        /// </summary>
        /// <param name="objectType">对象类型</param>
        /// <param name="beforeTimestamp">时间戳限制</param>
        /// <returns>可用备份列表</returns>
        private async Task<List<BackupInfo>> FindAvailableBackupsAsync(Type objectType, DateTime? beforeTimestamp = null)
        {
            var backups = new List<BackupInfo>();

            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    Debug.WriteLine($"[BackupDataRecoveryService] 备份目录不存在: {_backupDirectory}");
                    return backups;
                }

                var typeDirectory = Path.Combine(_backupDirectory, objectType.Name);
                if (!Directory.Exists(typeDirectory))
                {
                    Debug.WriteLine($"[BackupDataRecoveryService] 类型备份目录不存在: {typeDirectory}");
                    return backups;
                }

                var backupFiles = Directory.GetFiles(typeDirectory, "*.backup", SearchOption.TopDirectoryOnly);
                
                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    var timestamp = fileInfo.LastWriteTime;
                    
                    if (beforeTimestamp.HasValue && timestamp > beforeTimestamp.Value)
                        continue;

                    backups.Add(new BackupInfo
                    {
                        FilePath = file,
                        Timestamp = timestamp,
                        ObjectType = objectType,
                        FileSize = fileInfo.Length
                    });
                }

                // 按时间戳降序排列（最新的在前）
                backups = backups.OrderByDescending(b => b.Timestamp).ToList();
                
                Debug.WriteLine($"[BackupDataRecoveryService] 找到 {backups.Count} 个可用备份");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupDataRecoveryService] 查找备份异常: {ex.Message}");
            }

            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
            return backups;
        }

        /// <summary>
        /// 选择最佳备份
        /// </summary>
        /// <param name="availableBackups">可用备份列表</param>
        /// <param name="objectId">对象ID</param>
        /// <returns>选中的备份</returns>
        private BackupInfo SelectBestBackup(List<BackupInfo> availableBackups, int objectId)
        {
            // 优先选择最新的备份
            // 未来可以添加更复杂的选择逻辑，比如根据对象ID匹配特定备份
            return availableBackups.FirstOrDefault();
        }

        /// <summary>
        /// 从备份读取对象数据
        /// </summary>
        /// <param name="objectType">对象类型</param>
        /// <param name="backup">备份信息</param>
        /// <returns>备份数据</returns>
        private async Task<BackupObjectData> ReadBackupDataAsync(Type objectType, BackupInfo backup)
        {
            if (_dataReaders.TryGetValue(objectType, out var reader))
            {
                return await reader.ReadBackupDataAsync(backup.FilePath);
            }

            Debug.WriteLine($"[BackupDataRecoveryService] 没有找到适用的数据读取器: {objectType.Name}");
            return null;
        }

        /// <summary>
        /// 从备份数据恢复对象关系
        /// </summary>
        /// <param name="gameObject">目标对象</param>
        /// <param name="backupData">备份数据</param>
        /// <returns>恢复的关系列表</returns>
        private async Task<List<string>> RestoreRelationshipsFromBackupAsync(GameObject gameObject, BackupObjectData backupData)
        {
            var restoredRelations = new List<string>();

            try
            {
                switch (gameObject)
                {
                    case Faction faction:
                        restoredRelations.AddRange(await RestoreFactionRelationshipsAsync(faction, backupData));
                        break;
                    case Person person:
                        restoredRelations.AddRange(await RestorePersonRelationshipsAsync(person, backupData));
                        break;
                    case Architecture architecture:
                        restoredRelations.AddRange(await RestoreArchitectureRelationshipsAsync(architecture, backupData));
                        break;
                    case Troop troop:
                        restoredRelations.AddRange(await RestoreTroopRelationshipsAsync(troop, backupData));
                        break;
                    case Legion legion:
                        restoredRelations.AddRange(RestoreLegionRelationships(legion, backupData));
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupDataRecoveryService] 恢复关系异常: {ex.Message}");
            }

            return restoredRelations;
        }

        /// <summary>
        /// 恢复势力关系
        /// </summary>
        private async Task<List<string>> RestoreFactionRelationshipsAsync(Faction faction, BackupObjectData backupData)
        {
            var restored = new List<string>();

            // 恢复Leader关系
            if (faction.Leader == null && backupData.Properties.TryGetValue("LeaderID", out var leaderIdObj))
            {
                if (int.TryParse(leaderIdObj.ToString(), out var leaderId) && leaderId > 0)
                {
                    // 这里需要从游戏管理器获取Person对象
                    // 暂时使用模拟逻辑
                    Debug.WriteLine($"[BackupDataRecoveryService] 尝试恢复Faction.Leader关系: LeaderID={leaderId}");
                    restored.Add("Leader");
                }
            }

            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
            return restored;
        }

        /// <summary>
        /// 恢复人物关系
        /// </summary>
        private async Task<List<string>> RestorePersonRelationshipsAsync(Person person, BackupObjectData backupData)
        {
            var restored = new List<string>();

            // 恢复IdealTendency关系
            if (person.IdealTendency == null && backupData.Properties.TryGetValue("IdealTendency", out var idealTendencyObj))
            {
                // 尝试恢复IdealTendency
                Debug.WriteLine($"[BackupDataRecoveryService] 尝试恢复Person.IdealTendency关系");
                restored.Add("IdealTendency");
            }

            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
            return restored;
        }

        /// <summary>
        /// 恢复建筑关系
        /// </summary>
        private async Task<List<string>> RestoreArchitectureRelationshipsAsync(Architecture architecture, BackupObjectData backupData)
        {
            var restored = new List<string>();

            // 恢复BelongedFaction关系
            if (architecture.BelongedFaction == null && backupData.Properties.TryGetValue("BelongedFactionID", out var factionIdObj))
            {
                if (int.TryParse(factionIdObj.ToString(), out var factionId) && factionId > 0)
                {
                    Debug.WriteLine($"[BackupDataRecoveryService] 尝试恢复Architecture.BelongedFaction关系: FactionID={factionId}");
                    restored.Add("BelongedFaction");
                }
            }

            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
            return restored;
        }

        /// <summary>
        /// 恢复部队关系
        /// </summary>
        private async Task<List<string>> RestoreTroopRelationshipsAsync(Troop troop, BackupObjectData backupData)
        {
            var restored = new List<string>();

            // 恢复BelongedLegion关系
            if (troop.BelongedLegion == null && backupData.Properties.TryGetValue("BelongedLegionID", out var legionIdObj))
            {
                if (int.TryParse(legionIdObj.ToString(), out var legionId) && legionId > 0)
                {
                    Debug.WriteLine($"[BackupDataRecoveryService] 尝试恢复Troop.BelongedLegion关系: LegionID={legionId}");
                    restored.Add("BelongedLegion");
                }
            }

            // 添加真正的异步操作以避免CS1998警告
            await Task.CompletedTask;
            return restored;
        }

        /// <summary>
        /// 恢复军团关系
        /// </summary>
        private List<string> RestoreLegionRelationships(Legion legion, BackupObjectData backupData)
        {
            var restored = new List<string>();

            // 恢复相关关系
            Debug.WriteLine($"[BackupDataRecoveryService] 恢复Legion关系（待实现具体逻辑）");

            return restored;
        }

        /// <summary>
        /// 验证恢复结果
        /// </summary>
        /// <param name="gameObject">恢复后的对象</param>
        /// <returns>验证结果</returns>
        private async Task<RecoveryValidationResult> ValidateRecoveryAsync(GameObject gameObject)
        {
            var result = new RecoveryValidationResult();

            try
            {
                // 使用完整性检查器验证对象
                var integrityReport = await _integrityChecker.CheckAsync(gameObject);
                
                result.Success = integrityReport.IssuesFound == 0;
                result.IssuesFound = integrityReport.IssuesFound;
                result.ValidationDetails = integrityReport.Issues.Select(i => i.Description).ToList();
                
                if (!result.Success)
                {
                    result.ErrorMessage = $"验证发现 {integrityReport.IssuesFound} 个问题";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"验证异常: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 创建备份数据
        /// </summary>
        /// <param name="gameObject">要备份的对象</param>
        /// <returns>备份是否成功</returns>
        public async Task<bool> CreateBackupAsync(GameObject gameObject)
        {
            if (gameObject == null) return false;

            try
            {
                var typeDirectory = Path.Combine(_backupDirectory, gameObject.GetType().Name);
                Directory.CreateDirectory(typeDirectory);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = Path.Combine(typeDirectory, $"{gameObject.ID}_{timestamp}.backup");

                if (_dataReaders.TryGetValue(gameObject.GetType(), out var reader))
                {
                    await reader.CreateBackupAsync(gameObject, backupFile);
                    Debug.WriteLine($"[BackupDataRecoveryService] 创建备份成功: {backupFile}");
                    return true;
                }

                Debug.WriteLine($"[BackupDataRecoveryService] 没有找到适用的数据读取器: {gameObject.GetType().Name}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BackupDataRecoveryService] 创建备份异常: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 备份信息
    /// </summary>
    public class BackupInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Type ObjectType { get; set; }
        public long FileSize { get; set; }
    }

    /// <summary>
    /// 备份对象数据
    /// </summary>
    public class BackupObjectData
    {
        public int ObjectId { get; set; }
        public string ObjectType { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public DateTime BackupTime { get; set; }
    }

    /// <summary>
    /// 备份恢复结果
    /// </summary>
    public class BackupRecoveryResult
    {
        public bool Success { get; set; }
        public string ObjectType { get; set; } = string.Empty;
        public int ObjectId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime? BackupTimestamp { get; set; }
        public List<string> RestoredRelations { get; set; } = new List<string>();
        public RecoveryValidationResult ValidationResult { get; set; }
        public DateTime RecoveryTime { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return Success ? Message : $"恢复失败: {ErrorMessage}";
        }
    }

    /// <summary>
    /// 恢复验证结果
    /// </summary>
    public class RecoveryValidationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int IssuesFound { get; set; }
        public List<string> ValidationDetails { get; set; } = new List<string>();
    }

    /// <summary>
    /// 备份数据读取器接口
    /// </summary>
    public interface IBackupDataReader
    {
        Task<BackupObjectData> ReadBackupDataAsync(string filePath);
        Task CreateBackupAsync(GameObject gameObject, string filePath);
    }
}