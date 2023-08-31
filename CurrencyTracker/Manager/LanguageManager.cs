using System.Collections.Generic;

namespace CurrencyTracker.Manger
{
    // Language Names in Game:
    // Japanese
    // English
    // German
    // French
    // ChineseSimplified
    public class LanguageManager
    {
        private Dictionary<string, Dictionary<string, string>> languageResources;
        private string? currentLanguage;

        public static readonly Dictionary<string, string> LanguageNames = new Dictionary<string, string>
        {
            { "Japanese", "日本語" },
            { "English", "English" },
            { "German", "Deutsch" },
            { "French", "français" },
            { "ChineseSimplified", "简体中文" },
            { "Korean", "한국어" }
        };

        public LanguageManager()
        {
            languageResources = new Dictionary<string, Dictionary<string, string>>
            {
                // 简体中文
                {
                    "ChineseSimplified", new Dictionary<string, string>
                    {
                        { "ConfigLabel", "筛选排序选项"},
                        { "ConfigLabel1", "记录选项" },
                        { "ConfigLabel2", "其他" },
                        { "ConfigLabelHelp", "点击以展开/收回" },
                        { "ReverseSort", "倒序排序 " },
                        { "ClusterByTime", "按时间聚类:" },
                        { "ClusterInterval", "小时" },
                        { "ClusterByTimeHelp1", "当前设置: 以 " },
                        { "ClusterByTimeHelp2", "小时 为间隔显示数据" },
                        { "ChangeFilterEnabled", "收支筛选 " },
                        { "ChangeFilterLabel", "仅显示收支" },
                        { "Greater", "大于" },
                        { "Less", "小于" },
                        { "ChangeFilterValueLabel", "的记录" },
                        { "FilterByTime", "时间筛选" },
                        { "TimeFilterLabel", "仅显示" },
                        { "TimeFilterLabel1", " 至 " },
                        { "TimeFilterLabel2", "期间的记录" },
                        { "Year", "年" },
                        { "Month", "月" },
                        { "Day", "日" },
                        { "TrackInDuty", "记录副本内数据" },
                        { "TrackInDutyHelp", "部分副本、特殊场景探索中会给予货币奖励\n插件默认当玩家在副本、特殊场景探索中时暂停记录，等待玩家退出它们时再计算期间货币的总增减，无法记录副本内货币变化的地点\n你可以通过开启本项目关闭此功能\n\n注: 这可能会使插件记录大量小额数据，使得记录量大幅增加，但可以正常记录副本内货币变化的地点" },
                        { "MinimumRecordValue", "副本内最小记录值:" },
                        { "MinimumRecordValueHelp", "当前设置: 当货币收支的绝对值≥ " },
                        { "MinimumRecordValueHelp1", " 时，才记录一次数据\n注: 设置为 0 以关闭本项/仅在副本内生效/可能造成不准确的地点记录" },
                        { "TransactionsPerPage", "单页记录数:" },
                        { "ExportCsv", "导出为CSV" },
                        { "FileRenameLabel", "请输入文件名: (按Enter键确认)" },
                        { "FileRenameLabel1", "文件名: " },
                        { "FileRenameLabel2", "当前时间" },
                        { "FileRenameHelp", "注1: 为避免错误，请勿输入过长的文件名\n注2: 导出的数据将会应用当前所启用的一切排序筛选条件(不包含分页)" },
                        { "FileRenameHelp1", "如果留空，文件名则为: " },
                        { "ExportCsvMessage", "导出CSV文件错误: 当前未选中任何货币类型" },
                        { "ExportCsvMessage1", "导出CSV文件错误: 当前货币下无收支记录" },
                        { "ExportCsvMessage2", "时间,货币数,收支,地点" },
                        { "ExportCsvMessage3", "已导出CSV文件至: " },
                        { "Page", " 页" },
                        { "PreviousPage", "上一页" },
                        { "NextPage", "下一页" },
                        { "Di", "第 " },
                        { "Gong", "共 " },
                        { "Column", "时间" },
                        { "Column1", "货币数" },
                        { "Column2", "收支" },
                        { "Column3", "地点" },
                        { "UnknownLocation", "未知区域" },
                        { "CustomCurrencyLabel", "自定义货币追踪" },
                        { "CustomCurrencyHelp", "注:\n1.插件预设的19种货币不可更改\n2.你可以选择追踪物品，但请注意，在插件看来，即便增减为1也是增减，\n这可能导致大量收支为1的记录出现\n3.请尽量避免因为好奇而添加已经废弃的物品/货币，插件可能因此出现意料之外的错误\n3.删除货币并不会删除已有的数据文件，如有需要请自行删除" },
                        { "CustomCurrencyLabel1", "自定义货币追踪" },
                        { "CustomCurrencyLabel2", "当前已选择:" },
                        { "CustomCurrencyLabel3", "请选择..." },
                        { "CustomCurrencyLabel4", "搜索框" },
                        { "CustomCurrencyHelp1", "添加失败: 货币已存在" },
                        { "CustomCurrencyHelp2", "删除失败: 货币不存在" },
                        { "Add", "添加" },
                        { "Delete", "删除" },
                        { "OpenDataFolder", "打开数据文件夹" },
                        { "MergeTransactionsLabel", "合并记录" },
                        { "MergeTransactionsLabel1", "临界值" },
                        { "MergeTransactionsHelp", "请输入临界值" },
                        { "MergeTransactionsHelp1", "合并了 " },
                        { "MergeTransactionsHelp2", " 条记录。" },
                        { "MergeTransactionsHelp3", "合并当前选中货币下，地点相同、彼此相邻且收支 小于 临界值的记录\n" },
                        { "MergeTransactionsHelp4", "合并后记录时间、总数取被合并记录中最新一条的数值，收支则为彼此相加\n\n" },
                        { "TransactionsHelp", "未找到符合条件的记录。" },
                        { "TransactionsHelp1", "当前未选择任何货币类型。" },
                        { "TransactionsHelp2", "注: 操作无法回退，如有需要，请做好数据备份" },
                        { "ClearExTransactionsLabel", "清除异常记录"},
                        { "ClearExTransactionsHelp", "尝试清除当前选中货币下的所有异常记录\n"},
                        { "ClearExTransactionsHelp1", "当前判断标准: 收支等于总金额的记录 / 收支为 0 的记录\n\n"},
                        { "ClearExTransactionsHelp2", "清除了 " },
                        { "ClearExTransactionsHelp3", " 条异常记录。" },
                        { "Confirm", "确认" },
                        { "UnknownCurrency", "未知的货币类型"},
                        { "TrackModeLabel", "记录模式" },
                        { "TrackModeLabel1", "计时器模式" },
                        { "TrackModeLabel2", "消息模式" },
                        { "TrackModeLabel3", "间隔 (ms)" },
                        { "TrackModeHelp", "计时器模式:\n每隔 " },
                        { "TrackModeHelp1", " 毫秒，检查一次所有货币的变化情况" },
                        { "TrackModeHelp2", "消息模式:\n每当接收到新消息时，检查一次所有货币的变化情况\n(此模式下可能会得到更加精准的记录)" },
                        { "TrackModeHelp3", "过低的触发间隔会带来更高的性能损耗 (不明显)"}
                    }
                },

                // English
                {
                    "English", new Dictionary<string, string>
                    {
                        { "ConfigLabel", "Filiter Options"},
                        { "ConfigLabel1", "Record Options" },
                        { "ConfigLabel2", "Others" },
                        { "ConfigLabelHelp", "Click to expand/collaspe" },
                        { "ReverseSort", "Inverse Sort  " },
                        { "ClusterByTime", "Cluster By Time:" },
                        { "ClusterInterval", "Hours" },
                        { "ClusterByTimeHelp1", "Current Setting: Display transactions in " },
                        { "ClusterByTimeHelp2", "h interval" },
                        { "ChangeFilterEnabled", "Filter By Change    " },
                        { "ChangeFilterLabel", "Only Display Records" },
                        { "Greater", "Above" },
                        { "Less", "Below" },
                        { "ChangeFilterValueLabel", " in Change" },
                        { "FilterByTime", "Filter By Time" },
                        { "TimeFilterLabel", "Only Display Records Between" },
                        { "TimeFilterLabel1", " And " },
                        { "TimeFilterLabel2", "" },
                        { "Year", "Y" },
                        { "Month", "M" },
                        { "Day", "D" },
                        { "TrackInDuty", "Track in Duty" },
                        { "TrackInDutyHelp", "By Default, the plugin will pause recording when players are in Duty or Field Entry\nand waits until players exit those before calculating the total Changes in currency during that period.\nYou can disable this behavior by enabling this option.\n\nNote: This may result in recording a large number of transactions with small changes, significantly increasing the amount of transactions" },
                        { "MinimumRecordValue", "Min Record Value:" },
                        { "MinimumRecordValueHelp", "Current setting: Only record when the absolute value of change is ≥ " },
                        { "MinimumRecordValueHelp1", "\n\nNote: Set to 0 to disable this feature / Only effective in duty / May lead to inaccurate location recording" },
                        { "TransactionsPerPage", "Records Per Page:" },
                        { "ExportCsv", "Export .CSV" },
                        { "FileRenameLabel", "Enter a File Name: (Press Enter to Confirm)" },
                        { "FileRenameLabel1", "Filename: " },
                        { "FileRenameLabel2", "CurrentTime" },
                        { "FileRenameHelp", "Note 1: To avoid errors, please do not enter a file name that is \"too long\"\nNote 2: The exported transactions will be subject to all the sorting and filtering conditions that are currently enabled (excluding paging)." },
                        { "FileRenameHelp1", "If left blank, the file name will be: " },
                        { "ExportCsvMessage", "ERROR: No currency type currently selected." },
                        { "ExportCsvMessage1", "ERROR: No transactions under current selected currency." },
                        { "ExportCsvMessage2", "Time,Amount,Change,Location" },
                        { "ExportCsvMessage3", "A .csv file has been exported to: " },
                        { "Page", "" },
                        { "PreviousPage", "Previous" },
                        { "NextPage", "Next" },
                        { "Di", "" },
                        { "Gong", "" },
                        { "Column", "Time" },
                        { "Column1", "Amount" },
                        { "Column2", "Change" },
                        { "Column3", "Location" },
                        { "UnknownLocation", "Unknown" },
                        { "CustomCurrencyLabel", "Custom Tracker" },
                        { "CustomCurrencyHelp", "Note:\n1. The 19 currencies preset in the plugin cannot be changed\n2. You can choose to track items, but please be aware that in the plugin's view, even if the change is 1, it is still a change,\n which may result in a large number of records with changes of 1 appearing\n3. Please try to avoid adding items/currencies that have been deprecated out of curiosity, the plugin may make unexpected errors as a result\n3. Deleting currencies does not delete the existing data files, please delete them yourself if necessary." },
                        { "CustomCurrencyLabel1", "Custom Currency Tracker" },
                        { "CustomCurrencyLabel2", "Currently Selected:" },
                        { "CustomCurrencyLabel3", "Select a currency..." },
                        { "CustomCurrencyLabel4", "Search Filter" },
                        { "CustomCurrencyHelp1", "Fail to Add: The Currency You Selected Has Alreadey Existed" },
                        { "CustomCurrencyHelp2", "Fail to Delete: The Currency You Selected Doesn't Existed" },
                        { "Add", "Add " },
                        { "Delete", "Delete " },
                        { "OpenDataFolder", "Open Data Folder" },
                        { "MergeTransactionsLabel", "Merge Records" },
                        { "MergeTransactionsLabel1", "Threshold" },
                        { "MergeTransactionsHelp", "Please enter a threshold" },
                        { "MergeTransactionsHelp1", "Merged " },
                        { "MergeTransactionsHelp2", " records." },
                        { "MergeTransactionsHelp3", "Merge transactions with the same location name, adjacent locations, and change below the threshold in the currently selected currency\n" },
                        { "MergeTransactionsHelp4", "After the merger, the transactions' Time and Amount are taken from the latest value in the merged transactions, and the Change are the values obtained by adding them together\n\n" },
                        { "TransactionsHelp", "No matching transactions found." },
                        { "TransactionsHelp1", "No currency being selected now." },
                        { "TransactionsHelp2", "Note: The operation cannot be rolled back. If necessary, please make a backup" },
                        { "ClearExTransactionsLabel", "Clear Ex Records"},
                        { "ClearExTransactionsHelp", "Attempt to clear all exception transactions under the currently selected currency\n"},
                        { "ClearExTransactionsHelp1", "Current judgment criteria: Transactions with Change equal to the Amount /with Change equal to 0\n\n"},
                        { "ClearExTransactionsHelp2", "Clear " },
                        { "ClearExTransactionsHelp3", " exceptional transactions." },
                        { "Confirm", "Confirm" },
                        { "UnknownCurrency", "Unknown Currency"},
                        { "TrackModeLabel", "Track Mode" },
                        { "TrackModeLabel1", "Timer Mode" },
                        { "TrackModeLabel2", "Chat Mode" },
                        { "TrackModeLabel3", "Interval (ms)" },
                        { "TrackModeHelp", "Timer Mode:\nCheck all currency changes every " },
                        { "TrackModeHelp1", " milliseconds" },
                        { "TrackModeHelp2", "Chat Mode:\nCheck all currency changes whenever a new chat message is received\n(May record more accurate transactions data)" },
                        { "TrackModeHelp3", "Too low trigger interval may result in higher performance overhead (not significant)"}
                    }
                }
            };
        }

        public void LoadLanguage(string languageName)
        {
            if (languageResources.ContainsKey(languageName))
            {
                currentLanguage = languageName;
            }
        }

        public List<string> AvailableLanguage()
        {
            var availablelangs = new List<string>();
            foreach (var language in LanguageNames.Keys)
            {
                if (languageResources.ContainsKey(language))
                {
                    var languagename = LanguageNames[language];
                    availablelangs.Add(languagename);
                }
            }
            return availablelangs;
        }

        public string GetText(string key)
        {
            if (currentLanguage != null && languageResources.ContainsKey(currentLanguage.ToString()))
            {
                var languageDict = languageResources[currentLanguage.ToString()];
                if (languageDict.ContainsKey(key))
                {
                    return languageDict[key];
                }
            }
            return key;
        }
    }
}
