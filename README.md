# Currency Tracker

Currency Tracker is a plugin designed to help you track the income and expenses of various in-game currencies (such as Gil, Seal, Tomestone, etc.). It also provides features for custom queries and exporting data files.

![Currency Tracker2](https://raw.githubusercontent.com/AtmoOmen/CurrencyTracker/master/Assets/img2.png)

The plugin supports multiple languages, now supports English and Simplified Chinese.

The inspiration for this plugin came from Youri's FFXIVMoneyTracker ([GitHub Link](https://github.com/yschuurmans/FFXIVMoneyTracker)). The initial support for multiple languages and key plugin logic draws from Lharz's CurrencyAlert ([GitHub Link](https://github.com/Lharz/xiv-currency-alert)). Special thanks to KamiLib ([GitHub Link](https://github.com/MidoriKami/KamiLib)).

## ChangeLog

| Date       | Version | Update Description                                           |
| ---------- | ------- | ------------------------------------------------------------ |
| 2023/08/15 | 1.1.0.0 | Changelog:<br/>- Added a new field Location to transaction records, allowing you to track the location where the transaction occurred (Note: Data accuracy may vary / Existing data will be marked as unknown).<br/><br/>- Implemented comprehensive multilingual support at the code level.<br/><br/>- Added a time filtering feature, enabling you to display transactions within a specified time range.<br/><br/>- Introduced a minimum recording threshold for in-duty records, reducing the occurrence of numerous small-value data entries when in-duty data tracking is enabled.<br/><br/>- Added a custom currency tracking feature, allowing you to use the Currency Tracker to monitor changes in any in-game item or currency.<br/><br/>- Included an option to open the data folder, allowing easy access to the folder where the transaction data for the current character is stored across different operating systems. |
| 2023/08/08 | 1.0.1.0 | Add Mutiple Languages Support(English)                       |
| 2023/08/07 | 1.0.0.1 | Add login detection                                          |
| 2023/08/07 | 1.0.0.0 | Official version release                                     |

# Currency Tracker

Currency Tracker 是一个用于记录你游戏内各项货币（金币、军票、神典石等）收入与支出情况，并提供自定义查询、导出数据文件的插件。

![Currency Tracker1](https://raw.githubusercontent.com/AtmoOmen/CurrencyTracker/master/Assets/img1.png)

支持多语言，目前支持简体中文和英语。

插件灵感来源于 Youri 的 [FFXIVMoneyTracker](https://github.com/yschuurmans/FFXIVMoneyTracker)，对多语言的初步支持和重要的插件逻辑来源于 Lharz 的 [CurrencyAlert](https://github.com/Lharz/xiv-currency-alert)，同时感谢 [KamiLib](https://github.com/MidoriKami/KamiLib)。

## 更新日志

| 日期          | 版本号      | 更新日志                   |
|---------------|-------------|----------------------------|
| 2023年8月15日 | 1.1.0.0 | 更新日志:<br/>- 为收支记录增加了新项目 地点，现在可以记录收支记录产生时所在的地点了(可能出现不准确的数据 / 以前的数据将会被标记为未知地点)<br/><br/>- 从代码层面增加了完整的多语言支持<br/><br/>- 增加了按时间筛选功能，现在可以仅显示指定时间段的收支记录了<br/><br/>- 增加了副本内最小记录值功能，现在可以一定程度上避免开启副本内数据记录后大量小额数据出现了<br/>- 增加了自定义货币追踪功能，现在你可以使用 Currency Tracker 追踪任意游戏内物品/货币的变化情况了<br/>- 增加了打开数据文件夹功能，现在支持多系统一键打开存储当前角色收支记录数据的文件夹了<br/> |
| 2023年8月8日  | 1.0.1.0     | 增加多语言支持(英语)        |
| 2023年8月7日  | 1.0.0.1     | 加上了登陆检测              |
| 2023年8月7日  | 1.0.0.0     | 正式版发布                 |
