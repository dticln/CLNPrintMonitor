﻿using CLNPrintMonitor.Model;
using CLNPrintMonitor.Properties;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CLNPrintMonitor.Controller
{
    public partial class Main : Form
    {

        private ObservableCollection<Printer> printers;
        
        public Main()
        {
            InitializeComponent();
            this.printers = new ObservableCollection<Printer>();
            this.printers.CollectionChanged += new NotifyCollectionChangedEventHandler(CollectionChangedMethod);
            this.lvwMain.LargeImageList = new ImageList();
            this.lvwMain.LargeImageList.ImageSize = new Size(150, 150);
            Image[] range = {
                (Image)Resources.ResourceManager.GetObject("ink0"),
                (Image)Resources.ResourceManager.GetObject("ink30"),
                (Image)Resources.ResourceManager.GetObject("ink60"),
                (Image)Resources.ResourceManager.GetObject("ink90"),
                (Image)Resources.ResourceManager.GetObject("ink100"),
                (Image)Resources.ResourceManager.GetObject("offline"),
                (Image)Resources.ResourceManager.GetObject("error")
            };
            this.lvwMain.LargeImageList.Images.AddRange(range);
        }

        /// <summary>
        /// Button click action
        /// Include a new printer in the printers list
        /// </summary>
        /// <see cref="printers"/>
        /// <see cref="Printer"/>
        /// <param name="sender">Clicked button</param>
        /// <param name="e">Click event arg</param>
        private void BtnAddPrinterClickAsync(object sender, EventArgs e)
        {
            String strIp = tbxIpPrinter.Text;
            String name = tbxNamePrinter.Text;
            IPAddress ipv4;
            if (strIp != String.Empty && 
                name != String.Empty && 
                IPAddress.TryParse(strIp, out ipv4))
            {
                new Task(async () =>
                {
                    Printer printer = new Printer(name, ipv4);
                    this.printers.Add(printer);
                    if(await printer.GetInformationFromDevice())
                    {
                        InvokeUpdateItem(printer);
                    }
                }).Start();
            } else
            {
                MessageBox.Show("O endereço IP deve seguir os padrões de formatação.", "Endereço IP inválido", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            tbxIpPrinter.Text = "";
            tbxNamePrinter.Text = "";
        }

        /// <summary>
        /// Handler for CollectionChanged in ObservableCollection in printers list
        /// </summary>
        /// <see cref="printers"/>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionChangedMethod(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Printer printer = e.NewItems[0] as Printer;
                    ListViewItem item = new ListViewItem();
                    item.Text = printer.Name;
                    item.SubItems.Add(printer.Address.ToString());
                    item.ImageIndex = (int)printer.Status;
                    InvokeAddItems(item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }
        
        /// <summary>
        /// Invoke in UIThread the lvwMain.Items.Add() method
        /// </summary>
        /// <param name="item">Add new item in printers list</param>
        private void InvokeAddItems(ListViewItem item)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { this.InvokeAddItems(item); });
                return;
            }
            lvwMain.Items.Add(item);
        }

        /// <summary>
        /// Change an item Text in UIThread,
        /// searching for the target item by IPV4 address in printer object
        /// </summary>
        /// <param name="printer">New printer informations</param>
        private void InvokeUpdateItem(Printer printer)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { this.InvokeUpdateItem(printer); });
                return;
            }
            for (int i = 0; i < this.lvwMain.Items.Count; i++)
            {
                ListViewItem item = this.lvwMain.Items[i];
                if (item.SubItems[1].Text == printer.Address.ToString())
                {
                    item.ImageIndex = (int)printer.Status;
                }
            }
        }

        /// <summary>
        /// Change all items Text in UIThread
        /// using a printers list
        /// </summary>
        private void InvokeUpdateItems()
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate { this.InvokeUpdateItems(); });
                return;
            }
            for (int i = 0; i < this.lvwMain.Items.Count; i++)
            {
                ListViewItem item = this.lvwMain.Items[i];
                for (int j = 0; j < this.printers.Count; j++)
                {
                    Printer current = this.printers[j];
                    if (item.SubItems[1].Text == current.Address.ToString())
                    {
                        item.ImageIndex = (int)current.Status;
                        Console.WriteLine("Impressora " + current.Address.ToString() + " está sendo atualizada");
                    }
                }
            }
        }
        
        /// <summary>
        /// Every 30 seconds execute printer status update
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TmrRefreshTick(object sender, EventArgs e)
        {
            new Task(async () =>
            {
                
                for (int i = 0; i < this.printers.Count; i++)
                {
                    bool response = await this.printers[i].GetInformationFromDevice();
                }
                InvokeUpdateItems();
            }).Start();
        }

        private void TsmExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void NewPrinterForm(object sender, EventArgs e)
        {
            ListView list = sender as ListView;
            ListViewItem clicked = list.SelectedItems[0];
            Printer printer = null;
            for (int i = 0; i < printers.Count; i++)
            {
                if(printers[i].Address.ToString() == clicked.SubItems[1].Text)
                {
                    printer = printers[i];
                }
            }
            if(printer != null)
            {
                PrinterController form = new PrinterController(printer);
                form.Show();
            }
        }
    }
}