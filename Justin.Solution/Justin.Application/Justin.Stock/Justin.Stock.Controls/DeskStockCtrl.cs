﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Justin.FrameWork.WinForm.Extensions;
using Justin.FrameWork.WinForm.Helper;
using Justin.Log;
using Justin.Stock.Controls.Entities;
using Justin.Stock.Service.Entities;
using Justin.Stock.Service.Models;

namespace Justin.Stock.Controls
{
    delegate void FormInvoke(FormInvokArgument argument);

    /// <summary>
    /// 总汇总显示代理
    /// </summary>
    /// <param name="message"></param>
    public delegate void DisplaySumProfitAndWarnMsgInContainerDelegate(string message);

    public partial class DeskStockCtrl : UserControl
    {
        public DeskStockCtrl()
        {
            InitializeComponent();
        }

        #region 只打开一次的Form

        MyStock myStock = new MyStock();

        #endregion

        private void DeskStockCtrl_Load(object sender, EventArgs e)
        {
            StockService.AddEvent(Display);
            StockService.AddEvent(ShowWarn);
            //StockService.Start();
        }

        #region 桌面显示和通知功能

        private void Display(object sender, StockEventArgs e)
        {

            IEnumerable<StockInfo> stockList = e.Stocks;
            try
            {
                #region format
                //简单通知
                //string notifyFormat = "{0}:{1}{2}%{6}%";
                string tipsFormat =
@"{0} 涨:{9}% 换:{14}% 盈:{11}
现价：{3} 成本：{13} 股:{12} 
最高：{4} 最低：{5}
今开:{1} 昨收：{2} 
成交量/额:{6}手/{7}万元
买：          卖：
{15}
{16}
时间：{8}
当前：{10}";

                string fiveDealFormat =
@"{0}:{1}   {2}:{3}";

                #endregion

                #region 桌面控件初始化

                TableLayoutPanel tableLayoutPanel1 = new TableLayoutPanel();
                tableLayoutPanel1.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(tableLayoutPanel1, true, null);

                tableLayoutPanel1.SuspendLayout();
                var rowStyle = new RowStyle(SizeType.Absolute, 9);

                for (int j = 0; j < 50; j++)
                {
                    tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
                }
                tableLayoutPanel1.ColumnCount = 1;
                tableLayoutPanel1.RowCount = stockList.Count();
                tableLayoutPanel1.Height = tableLayoutPanel1.RowCount * 10;
                tableLayoutPanel1.Dock = DockStyle.Fill;
                List<Control> controls = new List<Control>();
                foreach (Control item in tableLayoutPanel1.Controls)
                {
                    controls.Add(item);
                }
                foreach (var item in controls)
                {
                    item.Dispose();
                }
                tableLayoutPanel1.Controls.Clear();

                #endregion

                int rowIndex = 0;
                //StringBuilder stocksNotifyMessageBuilder = new StringBuilder();
                ToolTip tip = new ToolTip();
                //只显示指定的股票    并且先按Order+BuyCount排序
                var stocks = stockList.Where(row => row.ShowInFolatWindow).OrderByDescending(row => row.Order).ThenByDescending(row => row.BuyCount);
                foreach (var rtStock in stocks)
                {
                    Label stockLabel = this.GetNewlabel(rtStock.Order == -1);

                    if (rtStock.Order == 0 || rtStock.Order == -1)
                    {
                        stockLabel.Click += new EventHandler(stockLabel_Click);
                    }
                    #region 股票桌面信息

                    stockLabel.Tag = rtStock.Code;

                    //当前价格
                    decimal priceNow = Math.Round(rtStock.PriceNow, 2);
                    if (priceNow > 1000)
                    {
                        priceNow = Math.Round(priceNow, 1);
                    }
                    //总盈亏
                    stockLabel.Text = string.Format(Constants.Setting.DeskDisplayFormat
                                              , rtStock.SpellingInShort.PadLeft(4, ' ')                                             //简称
                                              , priceNow.ToString().PadLeft(6, ' ')                                                 //当前价格
                                              , (rtStock.SurgedRange.ToString() + "%").PadLeft(7, ' ')                              //当日涨幅
                                              , (rtStock.CurrentProfit.ToString()).PadLeft(6, ' ')                               //当前盈亏
                                             , Math.Round(rtStock.SumProfit, 0).ToString().PadLeft(6, ' ')                                              //总盈亏
                                              , (Math.Round(rtStock.SumProfitPercent * 100, 2).ToString() + "%").PadLeft(8, ' ')      //总盈亏比例
                                             , Math.Round(rtStock.BuyPrice, 2).ToString().PadLeft(6, ' ')                           //成本价
                                             , rtStock.BuyCount.ToString().PadLeft(5, ' ')                                          //股数
                                             , (rtStock.TurnOver.ToString() + "%").PadLeft(7, ' ')                                  //换手率
                                             , rtStock.MarketValue.ToString().PadLeft(6, ' ')                                       //当前市值
                                             , Math.Round(rtStock.SumCost, 0).ToString().PadLeft(6, ' ')

                                              );
                    //string stockNotifyMessage = string.Format(notifyFormat
                    //                         , rtStock.SpellingInShort
                    //                         , Math.Round(rtStock.PriceNow, 2).ToString().PadLeft(7, ' ')
                    //                         , rtStock.SurgedRange.ToString().PadLeft(6, ' ')
                    //                        , rtStock.ProfitOrLoss.ToString().PadLeft(5, ' ')
                    //                        , rtStock.BuyPrice.ToString().PadLeft(7, ' ')
                    //                        , rtStock.BuyCount.ToString().PadLeft(5, ' ')
                    //                        , rtStock.TurnOver.ToString().PadLeft(6, ' ')
                    //                         );
                    //stocksNotifyMessageBuilder.Append(stockNotifyMessage).AppendLine();
                    #endregion

                    #region 股票提示信息

                    string fiveDeal = new StringBuilder()
                        .AppendFormat(fiveDealFormat, rtStock.Buy1Price.ToString().PadLeft(5, ' '), rtStock.Buy1Count.ToString().PadLeft(6, ' '), rtStock.Sell1Price.ToString().PadLeft(5, ' '), rtStock.Sell1Count.ToString().PadLeft(6, ' ')).AppendLine()
                        .AppendFormat(fiveDealFormat, rtStock.Buy2Price.ToString().PadLeft(5, ' '), rtStock.Buy2Count.ToString().PadLeft(6, ' '), rtStock.Sell2Price.ToString().PadLeft(5, ' '), rtStock.Sell2Count.ToString().PadLeft(6, ' ')).AppendLine()
                        .AppendFormat(fiveDealFormat, rtStock.Buy3Price.ToString().PadLeft(5, ' '), rtStock.Buy3Count.ToString().PadLeft(6, ' '), rtStock.Sell3Price.ToString().PadLeft(5, ' '), rtStock.Sell3Count.ToString().PadLeft(6, ' ')).AppendLine()
                        .AppendFormat(fiveDealFormat, rtStock.Buy4Price.ToString().PadLeft(5, ' '), rtStock.Buy4Count.ToString().PadLeft(6, ' '), rtStock.Sell4Price.ToString().PadLeft(5, ' '), rtStock.Sell4Count.ToString().PadLeft(6, ' ')).AppendLine()
                        .AppendFormat(fiveDealFormat, rtStock.Buy5Price.ToString().PadLeft(5, ' '), rtStock.Buy5Count.ToString().PadLeft(6, ' '), rtStock.Sell5Price.ToString().PadLeft(5, ' '), rtStock.Sell5Count.ToString().PadLeft(6, ' '))
                        .ToString();
                    string stockTips = string.Format(tipsFormat
                        , rtStock.Name
                         , rtStock.PriceTodayStart
                         , rtStock.PriceYesterdayEnd
                         , rtStock.PriceNow
                         , rtStock.PriceTodayHigh
                         , rtStock.PriceTodayLow
                         , rtStock.DealsStockAmt
                         , rtStock.DealsMoney
                         , rtStock.DateTime
                         , rtStock.SurgedRange
                         , rtStock.Now
                         , rtStock.CurrentProfit
                         , rtStock.BuyCount
                         , rtStock.BuyPrice
                         , rtStock.TurnOver
                         , fiveDeal
                         , rtStock.Code
                        );

                    tip.SetToolTip(stockLabel, stockTips);

                    #endregion

                    tableLayoutPanel1.Controls.Add(stockLabel, 0, rowIndex);
                    rowIndex++;
                }
                Label columnNamesLabel = GetNewlabel();
                columnNamesLabel.Text = string.Format(Constants.Setting.DeskDisplayFormat
                                               , "Name".PadLeft(4, ' ')                                                     //简称
                                               , "Now¥".PadLeft(6, ' ')                                                 //当前价格
                                               , "↓↑%".PadLeft(6, ' ')                                                 //当日涨幅
                                               , "PF".PadLeft(6, ' ')                                                   //当前盈亏        
                                              , "∑PF".PadLeft(6, ' ')                                                   //总盈亏
                                               , "∑PF%".PadLeft(8, ' ')                                               //总盈亏比例
                                              , "Cost¥".PadLeft(6, ' ')                                                   //成本价
                                              , "*".PadLeft(5, ' ')                                                     //股数
                                              , "Turn%".PadLeft(7, ' ')                                                   //换手率
                                              , "Mkt¥".PadLeft(6, ' ')                                                  //当前市值
                                              , "∑Cost¥".PadLeft(6, ' ')                                                  //总成本
                                               );
                tip.SetToolTip(columnNamesLabel, string.Format(Constants.Setting.DeskDisplayFormat
                                               , "Name:简称" + Environment.NewLine                                               //简称
                                               , "Now¥:当前价格" + Environment.NewLine                                         //当前价格
                                               , "↓↑%:当日涨幅" + Environment.NewLine                                         //当日涨幅
                                               , "PF:当前盈亏" + Environment.NewLine                                         //当前盈亏        
                                              , "∑PF:总盈亏" + Environment.NewLine                                          //总盈亏
                                               , "∑PF%:总盈亏比例" + Environment.NewLine                                         //总盈亏比例
                                              , "Cost¥:成本价" + Environment.NewLine                                          //成本价
                                              , "*:股数" + Environment.NewLine                                          //股数
                                              , "Turn%:换手率" + Environment.NewLine                                            //换手率
                                              , "Mkt¥:当前市值" + Environment.NewLine                                              //当前市值
                                              , "∑Cost¥:总成本" + Environment.NewLine                                               //总成本
                                              ));
                tableLayoutPanel1.Controls.Add(columnNamesLabel, 0, rowIndex);

                tableLayoutPanel1.ResumeLayout();

                #region 总盈亏信息

                //总成本=所有股票总成本+当前余额
                decimal sumCost = stockList.Sum(row => row.SumCost) + Constants.Setting.Balance;//成本
                //总市值=所有股票当前市值+当前余额
                decimal sumCurrentPrice = stockList.Sum(row => row.MarketValue) + Constants.Setting.Balance;//市值

                decimal sumProfitOrLoss = stockList.Sum(row => row.SumProfit);

                tableLayoutPanel1.Tag = string.Format("M:{0}/C:{1} P:{2}/∑P:{3}", (int)sumCurrentPrice, (int)sumCost, (int)(stocks.Sum(row => row.CurrentProfit)), (int)sumProfitOrLoss);

                #endregion

                #region 实时显示股票警告信息到桌面标题

                string warnMsg = PrepareWarnMessage(stockList);
                FormInvokArgument argument = new FormInvokArgument()
                {
                    tableLayoutPanel1 = tableLayoutPanel1,
                    Message = warnMsg,
                };

                if (this.InvokeRequired == true)
                {
                    this.Invoke(new FormInvoke(ShowStockInDesk), argument);
                }
                else
                {
                    ShowStockInDesk(argument);
                }

                #endregion

            }
            catch (Exception ex)
            {
                JLog.Write(LogMode.Error, ex);
            }

        }

        void stockLabel_Click(object sender, EventArgs e)
        {
            var form = this.FindForm();
            if (form is AutoAnchorForm)
            {
                var autoHideForm = form as AutoAnchorForm;
                autoHideToolStripMenuItem.Checked = !autoHideToolStripMenuItem.Checked;
                autoHideForm.EnableAutoAnchor = autoHideToolStripMenuItem.Checked;
            };
        }
        private void ShowStockInDesk(FormInvokArgument argument)
        {
            this.DoubleBuffered = true;
            this.SuspendLayout();
            TableLayoutPanel tableLayoutPanel1 = argument.tableLayoutPanel1;

            #region 重绘控件

            List<Control> controls = new List<Control>();
            foreach (Control item in this.Controls)
            {
                controls.Add(item);
            }
            foreach (var item in controls)
            {
                item.Dispose();
            }
            this.Controls.Clear();

            this.Controls.Add(tableLayoutPanel1);

            #endregion
            this.ResumeLayout();

            #region 抛出股票总盈亏汇总和警告信息给容器，以便显示到标题上

            string message = string.Format("{0}  {1}", tableLayoutPanel1.Tag.ToString(), argument.Message);
            this.Text = message;
            if (DisplaySumProfitAndWarnMsgInContainerEvent != null)
            {
                DisplaySumProfitAndWarnMsgInContainerEvent(message);
            }

            #endregion
        }

        private string PrepareWarnMessage(IEnumerable<StockInfo> StockList)
        {
            if (StockList == null)
            {
                return "";
            }
            string strMsg = "";
            string strUpOrDown = "";
            foreach (var stock in StockList)
            {
                bool showWarnFlag = false;
                if (stock.PriceNow != 0 & stock.Warn)
                {
                    if (stock.WarnPrice_Max != 0 && stock.PriceNow > stock.WarnPrice_Max)
                    {
                        showWarnFlag = true;
                        strUpOrDown = "↑";
                    }
                    if (stock.WarnPrice_Min != 0 && stock.PriceNow < stock.WarnPrice_Min)
                    {
                        strUpOrDown = "↓";
                        showWarnFlag = true;
                    }
                    if (stock.WarnPercent_Max != 0 && stock.SurgedRange > stock.WarnPercent_Max)
                    {
                        strUpOrDown = "↑";
                        showWarnFlag = true;
                    }
                    if (stock.WarnPercent_Min != 0 && stock.SurgedRange < stock.WarnPercent_Min)
                    {
                        strUpOrDown = "↓";
                        showWarnFlag = true;
                    }
                    if (showWarnFlag)
                        strMsg += string.Format("{0}{1}", stock.SpellingInShort, strUpOrDown);
                }
            }

            return strMsg;
        }
        private void ShowWarn(object sender, StockEventArgs e)
        {
            IEnumerable<StockInfo> stockList = e.Stocks;
            if (Constants.Setting.ShowWarn)
            {
                FormInvokArgument argument = new FormInvokArgument() { Message = PrepareWarnMessage(stockList) };
                if (this.InvokeRequired == true)
                {
                    this.Invoke(new FormInvoke(DisplayWarn), argument);
                }
                else
                {
                    DisplayWarn(argument);
                }
            }

        }
        private void DisplayWarn(FormInvokArgument arg)
        {
            if (arg.Message.Length > 0)
            {
                NotifyHelper.Show(arg.Message);
                //this.notifyIcon1.ShowBalloonTip(1, "", msg, ToolTipIcon.Info);
            }
        }

        #endregion

        #region 右键菜单

        Label stockLabel;
        private void deskMenu_Opening(object sender, CancelEventArgs e)
        {
            Control sourceControl = (sender as ContextMenuStrip).SourceControl;
            if (sourceControl is Label)
            {
                stockLabel = sourceControl as Label;
            }
            Form form = this.FindForm();
            if (form != null)
                this.topMostToolStripMenuItem.Checked = form.TopMost;

            var tempAutoHideForm = this.FindForm();
            if (tempAutoHideForm is AutoAnchorForm)
            {
                var autoHideForm = tempAutoHideForm as AutoAnchorForm;
                autoHideToolStripMenuItem.Checked = autoHideForm.EnableAutoAnchor;
            }
        }

        private void timeSheetMenuItem_Click(object sender, EventArgs e)
        {
            if (stockLabel == null)
                return;
            this.ShowChart(stockLabel.Tag.ToString(), ChartType.TimeSheet);
        }
        private void DayKMenuItem_Click(object sender, EventArgs e)
        {
            if (stockLabel == null)
                return;
            this.ShowChart(stockLabel.Tag.ToString(), ChartType.KOfDay);
        }
        private void WeekKMenuItem_Click(object sender, EventArgs e)
        {
            if (stockLabel == null)
                return;
            this.ShowChart(stockLabel.Tag.ToString(), ChartType.KOfWeek);
        }
        private void MonthKMenuItem_Click(object sender, EventArgs e)
        {
            if (stockLabel == null)
                return;
            this.ShowChart(stockLabel.Tag.ToString(), ChartType.KOfMonth);
        }
        private void monitorStockMenuItem_Click(object sender, EventArgs e)
        {
            myStock.Show(0);
        }
        private void personalStocksMenuItem_Click(object sender, EventArgs e)
        {
            myStock.Show(1);
        }
        private void systemSettingMenuItem_Click(object sender, EventArgs e)
        {
            myStock.Show(3);
        }

        private void ShowChart(string stockNo, ChartType chartType)
        {
            StockChart chart = new StockChart();
            chart.Show(stockNo, chartType);
        }

        #endregion

        #region 提供给外部注册事件用
        /// <summary>
        /// 注册显示盈亏总汇总和破线预警信息到容器标题
        /// </summary>
        public static DisplaySumProfitAndWarnMsgInContainerDelegate DisplaySumProfitAndWarnMsgInContainerEvent { get; set; }


        public void AddDisplayHandler()
        {
            StockService.AddEvent(Display);
        }
        public void RemoveDisplayHandler()
        {
            StockService.RemoveEvent(Display);
        }
        public void AddWarnHandler()
        {
            StockService.AddEvent(ShowWarn);
        }
        public void RemoveWarnHandler()
        {
            StockService.RemoveEvent(ShowWarn);
        }

        public void CloseChildrenForm()
        {
            this.myStock.Close(true);
        }

        #endregion

        private Label GetNewlabel(bool bold = false)
        {
            Label stockLabel = new Label();
            stockLabel.ContextMenuStrip = deskMenu;
            stockLabel.Width = 200;
            stockLabel.Font = new Font("Consolas", 8F, bold ? FontStyle.Bold : FontStyle.Regular);
            stockLabel.Dock = DockStyle.Fill;
            return stockLabel;
        }

        private void topMostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = this.FindForm();
            if (form != null)
                form.TopMost = !form.TopMost;
        }

        private void autoHideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = this.FindForm();
            if (form is AutoAnchorForm)
            {
                var autoHideForm = form as AutoAnchorForm;
                autoHideToolStripMenuItem.Checked = !autoHideToolStripMenuItem.Checked;
                autoHideForm.EnableAutoAnchor = autoHideToolStripMenuItem.Checked;
            }
        }


    }
}
