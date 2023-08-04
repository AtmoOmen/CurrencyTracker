using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CurrencyTracker.Manager;

public class Transctions
{
    public Configuration Configuration { get; set; } = null!;
    // 临时存放单一交易记录处
    private List<TransactionsConvetor> singleTransactionList = new List<TransactionsConvetor>();

    // 定义委托
    public delegate void CurrencyTransactionHandler(DateTime Timestamp, string CurrencyType, long Amount, long Change);

    // 添加交易记录
    public async void AppendTransaction(DateTime Timestamp, string currencyType, long Amount, long Change)
    {
        var singleTransaction = new TransactionsConvetor
        {
            TimeStamp = Timestamp,
            CurrencyType = currencyType,
            Amount = Amount,
            Change = Change
        };
        singleTransactionList.Add(singleTransaction);

        CharacterInfo CharacterInfo = Configuration.CurrentActiveCharacter[0];
        string PlayerDataFolder = CharacterInfo.PlayerDataFolder;

        string filePath = Path.Join(PlayerDataFolder, $"{currencyType}.txt");

        await singleTransaction.AppendTransactionToFileAsync(filePath, singleTransactionList);
        singleTransactionList.Clear();
    }
}
