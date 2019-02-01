﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RemotePC8801
{
    /// <summary>
    /// PageSector.xaml の相互作用ロジック
    /// </summary>
    public partial class PageSector : Page
    {
        public PageSector()
        {
            InitializeComponent();
        }

        private const string Sixteen = "0123456789ABCDEF";
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 16; i++)
            {
                var tb = new TextBlock(new Run(Sixteen[i].ToString()));
                Grid.SetColumn(tb, i + 1);
                Grid.SetRow(tb, 0);
                MainGrid.Children.Add(tb);
                var tb2 = new TextBlock(new Run(Sixteen[i].ToString()));
                Grid.SetColumn(tb2, 0);
                Grid.SetRow(tb2, i+1);
                MainGrid.Children.Add(tb2);
            }

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var tb = new TextBox();
                    tb.Text = "XX";
                    tb.IsReadOnly = true;
                    tb.Name = $"table{Sixteen[x]}{Sixteen[y]}";
                    Grid.SetColumn(tb, x + 1);
                    Grid.SetRow(tb, y + 1);
                    MainGrid.Children.Add(tb);
                }
            }
        }
    }
}