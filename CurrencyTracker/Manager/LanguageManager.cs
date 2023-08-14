using System;
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
        private string currentLanguage;

        public LanguageManager()
        {
            languageResources = new Dictionary<string, Dictionary<string, string>>
            {
                // 简体中文
                {
                    "ChineseSimplified", new Dictionary<string, string>
                    {
                        { "ConfigLabel", "筛选排序选项:"},
                        { "ConfigLabel1", "记录选项:" },
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
                        { "ExportCsv", "导出当前记录为CSV文件" },
                        { "FileRenameLabel", "请输入文件名: (按Enter键确认)" },
                        { "FileRenameLabel1", "文件名: " },
                        { "FileRenameLabel2", "当前时间" },
                        { "FileRenameHelp", "注1: 为避免错误，请勿输入过长的文件名\n注2: 导出的数据将会应用当前所启用的一切排序筛选条件(不包含分页)" },
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
                    }
                },

                // English
                {
                    "English", new Dictionary<string, string>
                    {
                        { "ConfigLabel", "Filiter Options:"},
                        { "ConfigLabel1", "Record Options:" },
                        { "ReverseSort", "Reverse Sort  " },
                        { "ClusterByTime", "Cluster By Time:" },
                        { "ClusterInterval", "Hours" },
                        { "ClusterByTimeHelp1", "Current Setting: Display transactions in " },
                        { "ClusterByTimeHelp2", "h interval" },
                        { "ChangeFilterEnabled", "Filter By Change    " },
                        { "ChangeFilterLabel", "Only Display Transactions" },
                        { "Greater", "Above" },
                        { "Less", "Below" },
                        { "ChangeFilterValueLabel", " in Change" },
                        { "FilterByTime", "Filter By Time" },
                        { "TimeFilterLabel", "Only Display Transactions Between" },
                        { "TimeFilterLabel1", " And " },
                        { "TimeFilterLabel2", "" },
                        { "Year", "Y" },
                        { "Month", "M" },
                        { "Day", "D" },
                        { "TrackInDuty", "Track in Duty" },
                        { "TrackInDutyHelp", "By Default, the plugin will pause recording when players are in Duty or Field Entry\nand waits until players exit those before calculating the total Changes in currency during that period.\nYou can disable this behavior by enabling this option.\n\nNote: This may result in recording a large number of transactions with small changes, significantly increasing the amount of transactions" },
                        { "MinimumRecordValue", "Minimum Record Value In Duty:" },
                        { "MinimumRecordValueHelp", "Current setting: Only record when the absolute value of change is ≥ " },
                        { "MinimumRecordValueHelp1", "\nNote: Set to 0 to disable this feature / Only effective in duty / May lead to inaccurate location recording" },
                        { "TransactionsPerPage", "Transactions Per Page:" },
                        { "ExportCsv", " Export to a .CSV File " },
                        { "FileRenameLabel", "Enter a File Name: (Press Enter to Confirm)" },
                        { "FileRenameLabel1", "Filename: " },
                        { "FileRenameLabel2", "CurrentTime" },
                        { "FileRenameHelp", "Note 1: To avoid errors, please do not enter a file name that is \"too long\"\nNote 2: The exported transactions will be subject to all the sorting and filtering conditions that are currently enabled (excluding paging)." },
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
            return key; // 返回原始文本（如果未找到对应翻译）
        }
    }
}

