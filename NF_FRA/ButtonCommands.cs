﻿using System;
using System.Windows.Forms;
using System.Windows.Input;
using Cursor = System.Windows.Forms.Cursor;
using Cursors = System.Windows.Forms.Cursors;

namespace NF_FRA
{
    public class ButtonCommands
    {
        public class SettingCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            private MainWindowViewModel vm;
            public SettingCommand(MainWindowViewModel viewModel)
            {
                vm = viewModel;
            }

            public bool CanExecute(object parameter) { return true; }

            public void Execute(object parameter)
            {
                Cursor cursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                if (vm.MinFreq >= 0.00001 && vm.MinFreq <= 15000000 &&
                   vm.MaxFreq >= 0.00001 && vm.MaxFreq <= 15000000 &&
                   vm.MinFreq < vm.MaxFreq &&
                   vm.Points >= 3 && vm.Points <= 20000 &&
                   vm.Accumulation >= 1 && vm.Accumulation <= 9999 &&
                   vm.Gain >= 0 && vm.Gain <= 10)
                {
                    try
                    {
                        if (vm.fra51615.IsOpen)
                        {
                            if (!vm.ca5351.IsOpen || (vm.ca5351.IsOpen && vm.Gain >= 3 && vm.Gain <= 10))
                            {
                                vm.MinFreq = vm.fra51615.setMinFreq(vm.MinFreq);
                                vm.MaxFreq = vm.fra51615.setMaxFreq(vm.MaxFreq);
                                vm.Points = vm.fra51615.setPoints(vm.Points);
                                vm.Accumulation = vm.fra51615.setAccumulation(vm.Accumulation);

                                if (vm.ca5351.IsOpen)
                                {
                                    int gain;
                                    if ((gain = (int)Math.Log10(vm.fra51615.setGain((long)Math.Pow(10, vm.Gain)))) == vm.ca5351.setGain(vm.Gain))
                                        vm.Gain = gain;
                                    else
                                        MessageBox.Show("接続を確認してください。", "エラー");
                                }
                                else
                                    vm.Gain = (int)Math.Log10(vm.fra51615.setGain((long)Math.Pow(10, vm.Gain)));
                            }
                            else
                            {
                                MessageBox.Show("値が不正です。", "エラー");
                            }
                        }
                        else
                            MessageBox.Show("接続を確認してください。", "エラー");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "エラー");
                    }
                }
                else
                {
                    MessageBox.Show("値が不正です。", "エラー");
                }
                Cursor.Current = cursor;
            }
        }
        public class SelectFolderCommand : ICommand
        {
            public event EventHandler CanExecuteChanged;
            private MainWindowViewModel vm;
            public SelectFolderCommand(MainWindowViewModel viewModel)
            {
                vm = viewModel;
            }

            public bool CanExecute(object parameter) { return true; }

            public void Execute(object parameter)
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    DialogResult result = dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        vm.SavePath = dialog.SelectedPath;
                }
            }
        }
    }
}
