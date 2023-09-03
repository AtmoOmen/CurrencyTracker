[TOC]



## ChangeLog

| Date       | Version | Update Description                                           |
| ---------- | ------- | ------------------------------------------------------------ |
| 2023/09/03 | 1.1.2.3 | - Expand Minimum Record Value In Duty feature to Minimum Record Value which allows to set the currency tracking behavior of In Duty and Out Of Duty seperately |
| 2023/09/02 | 1.1.2.2 | - Add One-Way Merge function to Merge Records feature, and original merging logic will be Two-Way Merge function.<br>- Add two buttons that allow to jump to the first page and last page of transactions in chart<br>- Now Records Per Page data will be saved to configuration. |
| 2023/09/01 | 1.1.2.1 | Changed the logic in the Track Mode section of the code, which should now significantly improve the performance of Chat Mode. |
| 2023/08/31 | 1.1.2.0 | - Modified the UI layout and interactive logic of the main interface.<br/>  - Added an "Others" category for options, relocating the previous functions "Export .CSV," "Open Data Folder," and "Languages" to this category.<br/>  - Enhanced the interactive logic for option categories, allowing users to expand/collapse all options within a category by clicking the category label.<br/>  - Revised the height calculation logic for list boxes and sub-windows displaying tables, enabling them to automatically adjust height while maintaining alignment as the window height changes.<br/><br/>- Added a feature called "Track Mode," located under the "Record Options" category.<br/>  - "Timer Mode" (existing) - Users can now customize the time interval for triggering the timer.<br/>  - "Chat Mode" (new) - Automatically checks the changes in all currencies whenever new in-game messages are received, potentially leading to more accurate records under this mode.<br/><br/>- Fixed a bug where the same custom currency was repeatedly added to the list box after re-enabling the plugin under certain circumstances.<br/><br/>- (English only) Modified certain inaccuracies in the English localization text. |
| 2023/08/29 | 1.1.1.4 | - Modified the conditional logic of certain code segments.<br>- Fixed a bug in Merge Transactions function that generated incorrect records in certain scenarios.<br>- Added fuzzy query functionality to the command. |
| 2023/08/28 | 1.1.1.3 | - Fix bugs that might cause game crashes.<br/>- Add a new command to open the main interface of the plugin with specific currency shown. |
| 2023/08/27 | 1.1.1.2 | - Add a new command "/ct" to open the main interface of the plugin.<br/>- Modified the way the data is displayed in the main interface of the plugin, so that filters can now be applied properly.<br/> |
| 2023/08/23 | 1.1.1.1 | - Further optimises the performance of the plugin, especially with very large amounts of data |
| 2023/08/22 | 1.1.1.0 | - Modified the way transactions are displayed, which now effectively reduces the performance consumption caused by large amounts of data<br/><br/>- Added a function to merge transactions of the same location by threshold value<br/><br/>- Added a function to clear exceptional transactions |
| 2023/08/15 | 1.1.0.0 | - Added a new field Location to transaction records, allowing you to track the location where the transaction occurred (Note: Data accuracy may vary / Existing data will be marked as unknown).<br/><br/>- Implemented comprehensive multilingual support at the code level.<br/><br/>- Added a time filtering feature, enabling you to display transactions within a specified time range.<br/><br/>- Introduced a minimum recording threshold for in-duty records, reducing the occurrence of numerous small-value data entries when in-duty data tracking is enabled.<br/><br/>- Added a custom currency tracking feature, allowing you to use the Currency Tracker to monitor changes in any in-game item or currency.<br/><br/>- Included an option to open the data folder, allowing easy access to the folder where the transaction data for the current character is stored across different operating systems. |
| 2023/08/08 | 1.0.1.0 | Add Mutiple Languages Support(English)                       |
| 2023/08/07 | 1.0.0.1 | Add login detection                                          |
| 2023/08/07 | 1.0.0.0 | Official version release                                     |

## 更新日志

| 日期          | 版本号      | 更新日志                   |
|---------------|-------------|----------------------------|
| 2023年9月3日 | 1.1.2.3 | - 将 副本内最小记录值 功能扩展为 最小记录值 功能，该功能允许分别为每种货币设置单独的、副本内外的 最小记录值 逻辑 |
| 2023年9月2日 | 1.1.2.2 | - 为合并记录功能新增了单项合并功能，原有的合并逻辑会变为双向合并功能<br>- 在表格页面新增了两个按钮，用于跳转至记录的首页和尾页<br>- 现在 单页记录数 数据会被持久化存储至配置文件中 |
| 2023年9月1日 | 1.1.2.1 | - 修改了“记录模式”部分的代码逻辑，现在应该能看到“消息模式”下插件明显的性能提升 |
| 2023年8月31日 | 1.1.2.0 | \- 修改了主界面的 UI 布局和交互逻辑  - 新增了选项类别"其他"，原有的“导出文件”、“打开数据文件夹”和“语言切换”功能移动至该类别下<br>  - 新增了对选项类别的交互逻辑，现在该逻辑允许用户通过单击该选项类别的标签以展开/折叠该类别下的所有选项<br>  - 修改了列表框和显示表格的子窗体的高度计算逻辑，现在它们能在保持高度对齐的情况随着窗口大小自动调节高度<br> - 新增了功能“记录模式”，位于“记录选项”选项类别下<br>  - “计时器模式”(原) - 现在允许用户自定义计时器触发的时间间隔<br>  - “消息模式”(新) - 每当游戏内接收到新消息时，自动检查当前所有货币的变化情况，该模式下的记录可能会更加准确<br> - 修复了“在某些情况下，重新启用插件后，列表框内被重复添加相同的自定义货币”的BUG<br> - (仅限英语)修改了英语本地化文本中的一些不太准确的表达 |
| 2023年8月29日 | 1.1.1.4 | - 更改了部分代码片段的判断逻辑<br>- 修复了"合并记录"功能在某些情况下生成的错误收支记录<br>- 为插件命令新增了模糊查询功能 |
| 2023年8月28日 | 1.1.1.3 | - 修复了一些可能导致游戏崩溃的 BUG<br>- 新增了一个用于打开指定货币界面的指令 |
| 2023年8月27日 | 1.1.1.2 | - 增加一个新命令"/ct"，用于开启插件主界面<br/>- 修改了插件主界面数据显示方式，现在可以正常地应用筛选器 |
| 2023年8月23日 | 1.1.1.1 | 进一步优化了插件的性能表现 —— 尤其是在超大量数据下 |
| 2023年8月22日 | 1.1.1.0 | - 修改了记录的显示方式，现在可以有效降低数据量过大时造成的性能消耗<br/><br/>- 新增了按临界值合并相同地点记录的功能<br/><br/>- 新增了移除异常记录的功能 |
| 2023年8月15日 | 1.1.0.0 | - 为收支记录增加了新项目 地点，现在可以记录收支记录产生时所在的地点了(可能出现不准确的数据 / 以前的数据将会被标记为未知地点)<br/><br/>- 从代码层面增加了完整的多语言支持<br/><br/>- 增加了按时间筛选功能，现在可以仅显示指定时间段的收支记录了<br/><br/>- 增加了副本内最小记录值功能，现在可以一定程度上避免开启副本内数据记录后大量小额数据出现了<br/>- 增加了自定义货币追踪功能，现在你可以使用 Currency Tracker 追踪任意游戏内物品/货币的变化情况了<br/>- 增加了打开数据文件夹功能，现在支持多系统一键打开存储当前角色收支记录数据的文件夹了<br/> |
| 2023年8月8日  | 1.0.1.0     | 增加多语言支持(英语)        |
| 2023年8月7日  | 1.0.0.1     | 加上了登陆检测              |
| 2023年8月7日  | 1.0.0.0     | 正式版发布                 |
