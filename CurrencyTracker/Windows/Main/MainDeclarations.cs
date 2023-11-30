namespace CurrencyTracker.Windows;

public partial class Main
{
    // 当前货币相关值 Current Currency Related Values
    internal uint selectedCurrencyID = 0;
    internal int selectedOptionIndex = -1;
    internal Dictionary<uint, List<bool>>? selectedStates = new();
    internal Dictionary<uint, List<TransactionsConvertor>>? selectedTransactions = new();
    internal List<TransactionsConvertor> currentTypeTransactions = new();
    internal List<TransactionsConvertor> lastTransactions = new();

    // 顶栏显示情况 Top Columns Visibility
    private bool showRecordOptions = true;
    private bool showOthers = true;

    // 筛选器开关 Filters Switch
    private bool isClusteredByTime = false;
    private bool isChangeFilterEnabled = false;
    private bool isTimeFilterEnabled = false;
    private bool isLocationFilterEnabled = false;
    private bool isNoteFilterEnabled = false;

    // 筛选器值 Filters Values
    private int clusterHour = 0;
    private DateTime filterStartDate = DateTime.Now;
    private DateTime filterEndDate = DateTime.Now;
    private bool startDateEnable;
    private bool endDateEnable;
    private int filterMode;
    private int filterValue = 0;
    private bool isChangeColoring = false;
    private Vector4 positiveChangeColor = new(1.0f, 0.0f, 0.0f, 1.0f);
    private Vector4 negativeChangeColor = new(0.0f, 1.0f, 0.0f, 1.0f);
    private string? searchLocationName = string.Empty;
    private string? searchNoteContent = string.Empty;

    // 自定义货币追踪相关值 CCT Related Values
    private static string searchFilterCCT = string.Empty;
    private uint currencyIDCCT = uint.MaxValue;
    private const int itemsPerPageCCT = 10;
    private int currentItemPageCCT = 0;
    private uint itemCountsCCT = 0;
    private static Dictionary<uint, string>? ItemNames; // 原生拉取的值 Original Pull
    private static List<string>? itemNamesCCT;
    private static readonly string[] filterNamesForCCT = new string[]
    {
        // 过期物品 Dated items
        "†", "过期", "Dated", "Ex-" ,
        // 腰带类物品 Belt
        "腰带", "ベルト", "Gürtel", "gürtel", "Ceinture"
    };

    // 数据显示相关值 Data Display Related Values
    private int currentPage;
    private int transactionsPerPage = 20;
    private int visibleStartIndex;
    private int visibleEndIndex;

    // 数据处理相关值 Data Handler Related Values
    private string fileName = string.Empty;
    private bool isOnMergingTT = false;
    private int mergeThreshold;
    private bool isOnEdit = false;
    private string? editedLocationName = string.Empty;
    private string editedNoteContent = string.Empty;
    private string editedCurrencyName = string.Empty;

    // 界面控制相关值 UI Control Related Values
    private readonly bool selectTimeDeco = false; // Always False
    private readonly Timer searchTimer = new(100);
    private readonly Timer searchTimerCCT = new(100);
    private float windowWidth;
    private int childWidthOffset = 0;
    private static readonly Dictionary<string, int> columnWidths = new()
    {
        {"Time", 150},
        {"Amount", 130},
        {"Change", 100},
        {"Location", 100},
        {"Note", 150},
        {"Checkbox", 30}
    };
    private static readonly Dictionary<string, ImGuiTableColumnFlags> columnFlags = new()
    {
        {"Order", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize},
        {"Checkbox", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize}
    };
    private static readonly Dictionary<string, System.Action> ColumnHeaderActions = new()
    {
        {"Order", () => Plugin.Instance.Main.OrderColumnHeaderUI()},
        {"Time", () => Plugin.Instance.Main.TimeColumnHeaderUI()},
        {"Amount", () => Plugin.Instance.Main.AmountColumnHeaderUI()},
        {"Change", () => Plugin.Instance.Main.ChangeColumnHeaderUI()},
        {"Location", () => Plugin.Instance.Main.LocationColumnHeaderUI()},
        {"Note", () => Plugin.Instance.Main.NoteColumnHeaderUI()},
        {"Checkbox", () => Plugin.Instance.Main.CheckboxColumnHeaderUI()}
    };
    private static readonly Dictionary<string, Action<int, bool, TransactionsConvertor>> ColumnCellActions = new()
    {
        {"Order", Plugin.Instance.Main.OrderColumnCellUI},
        {"Time", Plugin.Instance.Main.TimeColumnCellUI},
        {"Amount", Plugin.Instance.Main.AmountColumnCellUI},
        {"Change", Plugin.Instance.Main.ChangeColumnCellUI},
        {"Location", Plugin.Instance.Main.LocationColumnCellUI},
        {"Note", Plugin.Instance.Main.NoteColumnCellUI},
        {"Checkbox", Plugin.Instance.Main.CheckboxColumnCellUI}
    };

    private readonly Configuration? C = Plugin.Instance.Configuration;
    private readonly Plugin? P = Plugin.Instance;
}
