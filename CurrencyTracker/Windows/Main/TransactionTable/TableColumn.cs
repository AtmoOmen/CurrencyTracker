using System.Collections.Generic;
using CurrencyTracker.Infos;
using CurrencyTracker.Manager;
using ImGuiNET;

namespace CurrencyTracker.Windows;

public abstract class TableColumn
{
    public bool IsVisible => Service.Config.ColumnsVisibility[GetType().Name.Replace("Column", "")];
    public virtual ImGuiTableColumnFlags ColumnFlags { get; protected set; } = ImGuiTableColumnFlags.None;
    public virtual float ColumnWidthOrWeight { get; protected set; } = 150;

    protected static uint SelectedCurrencyID => Main.SelectedCurrencyID;
    protected static List<DisplayTransaction> CurrentTransactions => Main.currentTransactions;
    protected static TransactionFileCategory CurrentView => Main.currentView;
    protected static ulong CurrentViewID => Main.currentViewID;

    public virtual void Header() { }

    public virtual void Cell(int i, DisplayTransaction transaction) { }

    protected static void RefreshTable() => Main.RefreshTransactionsView();
}
