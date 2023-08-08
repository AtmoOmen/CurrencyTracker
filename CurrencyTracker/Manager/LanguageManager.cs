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
                        { "ConfigLabel", "记录筛选排序选项:"},
                        { "ReverseSort", "倒序排序    " },
                        { "ClusterByTime", "按时间聚类:" },
                        { "ClusterInterval", "小时" },
                        { "ClusterByTimeHelp1", "当前设置: 以 " },
                        { "ClusterByTimeHelp2", "小时 为间隔显示数据" },
                        { "FilterEnabled", "收支筛选    " },
                        { "FilterLabel", "仅显示收支" },
                        { "Greater", "大于" },
                        { "Less", "小于" },
                        { "FilterValueLabel", "的记录" },
                        { "TrackInDuty", "记录副本内数据" },
                        { "TrackInDutyHelp", "部分副本、特殊场景探索中会给予货币奖励\n插件默认当玩家在副本、特殊场景探索中时暂停记录，等待玩家退出它们时再计算期间货币的总增减\n你可以通过开启本项目关闭此功能\n\n注: 这可能会使插件记录大量小额数据，使得记录量大幅增加" },
                        { "TransactionsPerPage", "每页显示记录数:" },
                        { "ExportCsv", "导出当前记录为CSV文件" },
                        { "FileRenameLabel", "请输入文件名: (按Enter键确认)" },
                        { "FileRenameLabel1", "文件名: " },
                        { "FileRenameLabel2", "当前时间" },
                        { "FileRenameHelp", "注1: 为避免错误，请勿输入过长的文件名\n注2: 导出的数据将会应用当前所启用的一切排序筛选条件(不包含分页)" },
                        { "ExportCsvMessage", "导出CSV文件错误: 当前未选中任何货币类型" },
                        { "ExportCsvMessage1", "导出CSV文件错误: 当前货币下无收支记录" },
                        { "ExportCsvMessage2", "时间,货币数,收支" },
                        { "ExportCsvMessage3", "已导出CSV文件至: " },
                        { "Page", " 页" },
                        { "PreviousPage", "上一页" },
                        { "NextPage", "下一页" },
                        { "Di", "第 " },
                        { "Gong", "共 " },
                        { "Column", "时间" },
                        { "Column1", "货币数" },
                        { "Column2", "收支" }
                    }
                },

                // English
                {
                    "English", new Dictionary<string, string>
                    {
                        { "ConfigLabel", "Transactions Sort/Filiter Options:"},
                        { "ReverseSort", "Reverse Sort    " },
                        { "ClusterByTime", "Cluster By Time:" },
                        { "ClusterInterval", "Hours" },
                        { "ClusterByTimeHelp1", "Setting: Display transactions in " },
                        { "ClusterByTimeHelp2", "h interval" },
                        { "FilterEnabled", "Filter By Change    " },
                        { "FilterLabel", "Only Display Transactions" },
                        { "Greater", "Above" },
                        { "Less", "Below" },
                        { "FilterValueLabel", " in Change" },
                        { "TrackInDuty", "Track in Duty" },
                        { "TrackInDutyHelp", "By Default, the plugin will pause recording when players are in Duty or Field Entry\nand waits until players exit those before calculating the total Changes in currency during that period.\nYou can disable this behavior by enabling this option.\n\nNote: This may result in recording a large number of transactions with small changes, significantly increasing the amount of transactions" },
                        { "TransactionsPerPage", "Transactions Per Page:" },
                        { "ExportCsv", " Export to a .CSV File " },
                        { "FileRenameLabel", "Enter a File Name: (Press Enter to Confirm)" },
                        { "FileRenameLabel1", "Filename: " },
                        { "FileRenameLabel2", "CurrentTime" },
                        { "FileRenameHelp", "Note 1: To avoid errors, please do not enter a file name that is \"too long\"\nNote 2: The exported transactions will be subject to all the sorting and filtering conditions that are currently enabled (excluding paging)." },
                        { "ExportCsvMessage", "ERROR: No currency type currently selected." },
                        { "ExportCsvMessage1", "ERROR: No transactions under current selected currency." },
                        { "ExportCsvMessage2", "Time,Amount,Change" },
                        { "ExportCsvMessage3", "A .csv file has been exported to: " },
                        { "Page", "" },
                        { "PreviousPage", "Previous" },
                        { "NextPage", "Next" },
                        { "Di", "" },
                        { "Gong", "" },
                        { "Column", "Time" },
                        { "Column1", "Amount" },
                        { "Column2", "Change" }
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

