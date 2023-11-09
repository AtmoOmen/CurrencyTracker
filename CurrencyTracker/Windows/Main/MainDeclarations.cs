using CurrencyTracker.Manager;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CurrencyTracker.Windows;

public partial class Main
{
    // 记录模式: 1为聊天记录模式 Record Mode: 1 for Chat Mode
    private int recordMode = 1;

    // 是否显示记录选项 If Show Record Options
    private bool showRecordOptions = true;

    // 是否显示其他 If Show Others
    private bool showOthers = true;

    // 时间聚类 Time Clustering
    private int clusterHour;

    // 时间聚类开关 Time Clustering Switch
    private bool isClusteredByTime;

    // 倒序排序开关 Reverse Sorting Switch
    internal bool isReversed;

    // 收支筛选开关 Income/Expense Filter Switch
    private bool isChangeFilterEnabled;

    // 时间筛选开关 Time Filter Switch
    private bool isTimeFilterEnabled;

    // 地点筛选开关 Location Filter Switch
    private bool isLocationFilterEnabled;

    // 备注筛选开关 Note Filter Switch
    private bool isNoteFilterEnabled;

    // 地点筛选名称 Location Filter Key
    private string? searchLocationName = string.Empty;

    // 备注筛选名称 Note Filter Key
    private string? searchNoteContent = string.Empty;

    // 筛选模式：0为大于，1为小于 Filtering Mode: 0 for Above, 1 for Below
    private int filterMode;

    // 用户指定的筛选值 User-Specified Filtering Value
    private int filterValue;

    // 每页显示的交易记录数 Number of Transaction Records Displayed Per Page
    private int transactionsPerPage = 20;

    // 当前页码 Current Page Number
    private int currentPage;

    // 自定义追踪物品ID Custom Tracked Currency ID
    private uint customCurrency = uint.MaxValue;

    // CSV文件名 CSV File Name
    private string fileName = string.Empty;

    // 默认选中的选项 Default Selected Option
    internal int selectedOptionIndex = -1;

    // 选择的语言 Selected Language
    internal string playerLang = string.Empty;

    // 当前选中的货币名称 Currently Selected Currency Name
    internal string? selectedCurrencyName;

    // 搜索框值 Search Filter
    private static string searchFilter = string.Empty;

    // 合并的临界值 Merge Threshold
    private int mergeThreshold;

    // 当前页索引 Current Page Index
    private int visibleStartIndex;

    private int visibleEndIndex;

    // 修改后地点名 Location Name after Editing
    private string? editedLocationName = string.Empty;

    // 编辑页面开启状态 Edit Popup
    private bool isOnEdit = false;

    // 工具栏合并页面开启状态 Merging Popup in Table Tools
    private bool isOnMergingTT = false;

    // 收支染色开启状态 Change Text Coloring
    private bool isChangeColoring = false;

    // 收支染色 Change Text Colors
    private Vector4 positiveChangeColor = new(1.0f, 0.0f, 0.0f, 1.0f);

    private Vector4 negativeChangeColor = new(0.0f, 1.0f, 0.0f, 1.0f);

    // 每页显示的项数和页数 Items in Custom Currency Paging Function
    private readonly int itemsPerPage = 10;

    private int currentItemPage = 0;

    // 筛选时间段的起始 Filtering Time Period
    private DateTime filterStartDate = DateTime.Now;

    private DateTime filterEndDate = DateTime.Now;
    private bool startDateEnable;
    private bool endDateEnable;

    // 自定义货币追踪过滤物品用 Used to filter some outdate/abandoned items in custom tracker item list
    private static readonly string[] filterNamesForCCT = new string[]
    {
        // 过期物品 Dated items
        "†", "过期", "Dated", "Ex-" ,
        // 腰带类物品 Belt
        "腰带", "ベルト", "Gürtel", "gürtel", "Ceinture"
    };

    // 这个 bool 永远为 false，仅为填充用，无实际作用
    // This bool will always be false, for method filling purposes only, no actual effect
    private bool selectTimeDeco = false;

    // 导出文件的类型 Export Data File Type : 0 - .csv ; 1 - .md
    private int exportDataFileType = 0;

    // 编辑的备注内容 Edited Note Content
    private string editedNoteContent = string.Empty;

    // 是否显示表格地点列 Whether Show Location Column
    private bool isShowLocationColumn = true;

    // 是否显示表格备注列 Whether Show Note Column
    private bool isShowNoteColumn = true;

    // 是否显示序号列 Where Show Order Column
    private bool isShowOrderColumn = true;

    // 用于控制 UI 的刷新速度 Used to slow down UI refresh speed
    private readonly System.Timers.Timer searchTimer = new(100);

    // 是否为本回首次打开 Is First Time to Open Main Windown
    internal bool isFirstTime = true;

    // 修改后货币名 Edited Currency Name
    private string editedCurrencyName = string.Empty;

    // 自定义货币追踪物品名称 For Custom Currency Tracker
    private List<string> CCTItemNames = new();

    // 临时 Temp
    private bool isRecordContentName;

    private bool isRecordTeleportDes;
    private bool isRecordTeleport;
    private bool isTrackinDuty;
    private bool isWaitExComplete;
    private bool isRecordMGPSource;
    private bool isRecordTripleTriad;
    private bool isRecordQuestName;
    private bool isRecordTrade;
    private bool isRecordFate;
    private bool isRecordIsland;

    internal Dictionary<string, List<bool>>? selectedStates = new();
    internal Dictionary<string, List<TransactionsConvertor>>? selectedTransactions = new();
    internal List<string> options = new();
    internal List<string>? ordedOptions = new();
    internal List<string>? hiddenOptions = new();
    internal List<TransactionsConvertor> currentTypeTransactions = new();
    internal List<TransactionsConvertor> lastTransactions = new();
    internal long[]? LinePlotData;

    private Configuration? C = Plugin.Instance.Configuration;
    private Plugin? P = Plugin.Instance;
}
