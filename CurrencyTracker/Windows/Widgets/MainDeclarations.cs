using CurrencyTracker.Manager;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CurrencyTracker.Windows;

public partial class Main
{
    // 计时器触发间隔 Timer Trigger Interval
    private int timerInterval = 500;

    // 记录模式: 0为计时器模式, 1为聊天记录模式 Record Mode: 0 for Timer Mode, 1 for Chat Mode
    private int recordMode = 0;

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

    // 副本内记录开关 Duty Tracking Switch
    private bool isTrackedinDuty;

    // 收支筛选开关 Income/Expense Filter Switch
    private bool isChangeFilterEnabled;

    // 时间筛选开关 Time Filter Switch
    private bool isTimeFilterEnabled;

    // 地点筛选开关 Location Filter Switch
    private bool isLocationFilterEnabled;

    // 地点筛选名称 Locatio Filter Key
    private string? searchLocationName = string.Empty;

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

    // 最小值 Min Value to Make a new record
    private int inDutyMinTrackValue;

    private int outDutyMinTrackValue;

    // 修改后地点名 Location Name after Editing
    private string? editedLocationName = string.Empty;

    // 编辑页面开启状态 Edit Popup
    private bool isOnEdit = false;

    // 工具栏合并页面开启状态 Merging Popup in Table Tools
    private bool isOnMergingTT = false;

    // 收支染色开启状态 Change Text Coloring
    private bool isChangeColoring = false;

    // 收支染色 Change Text Colors
    private Vector4 positiveChangeColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
    private Vector4 negativeChangeColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

    // 每页显示的项数和页数 Items in Custom Currency Paging Function
    private int itemsPerPage = 10;
    private int currentItemPage = 0;

    // 筛选时间段的起始 Filtering Time Period
    private DateTime filterStartDate = DateTime.Now;
    private DateTime filterEndDate = DateTime.Now;
    private bool startDateEnable;
    private bool endDateEnable;
    // 这个 bool 永远为 false，仅为填充用，无实际作用
    // This bool will always be false, for method filling purposes only, no actual effect
    private bool selectTimeDeco = false;

    internal Dictionary<string, List<bool>>? selectedStates = new Dictionary<string, List<bool>>();
    internal Dictionary<string, List<TransactionsConvertor>>? selectedTransactions = new Dictionary<string, List<TransactionsConvertor>>();
    private Transactions transactions = new Transactions();
    private TransactionsConvertor transactionsConvertor = new TransactionsConvertor();
    private CurrencyInfo? currencyInfo = null!;
    private static LanguageManager? Lang;
    private List<string> permanentCurrencyName = new List<string>();
    internal List<string> options = new List<string>();
    internal List<string>? ordedOptions = new List<string>();
    internal List<string>? hiddenOptions = new List<string>();
    internal List<TransactionsConvertor> currentTypeTransactions = new List<TransactionsConvertor>();
    internal List<TransactionsConvertor> lastTransactions = new List<TransactionsConvertor>();
    internal long[]? LinePlotData;

    private System.Timers.Timer searchTimer = new System.Timers.Timer(100); // 500毫秒延迟

    

}
