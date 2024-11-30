namespace InventoryManagementMAUI.Services
{
    public class DatabaseBackupService
    {
        private readonly string _databasePath;
        private readonly string _backupDirectory;
        private readonly DatabaseService _databaseService;

        public DatabaseBackupService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "inventory.db");
            _backupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "InventoryBackups");

            if (!Directory.Exists(_backupDirectory))
                Directory.CreateDirectory(_backupDirectory);
        }

        public async Task<string> CreateBackup()
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(_backupDirectory, $"inventory_backup_{timestamp}.db");

                await _databaseService.CloseConnection();

                File.Copy(_databasePath, backupPath, true);

                await _databaseService.ReopenConnection();

                return backupPath;
            }
            catch (Exception ex)
            {
                await _databaseService.ReopenConnection();
                throw new Exception($"Error creating backup: {ex.Message}");
            }
        }

        public async Task RestoreFromBackup(string backupPath)
        {
            try
            {
                await _databaseService.CloseConnection();

                string tempBackup = _databasePath + ".temp";
                if (File.Exists(_databasePath))
                    File.Copy(_databasePath, tempBackup, true);

                try
                {
                    File.Copy(backupPath, _databasePath, true);
                }
                catch
                {
                    if (File.Exists(tempBackup))
                        File.Copy(tempBackup, _databasePath, true);
                    throw;
                }
                finally
                {
                    if (File.Exists(tempBackup))
                        File.Delete(tempBackup);
                    await _databaseService.ReopenConnection();
                }
            }
            catch (Exception ex)
            {
                await _databaseService.ReopenConnection();
                throw new Exception($"Error restoring backup: {ex.Message}");
            }
        }
    }
}
