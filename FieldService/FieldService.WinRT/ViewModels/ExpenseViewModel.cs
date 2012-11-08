﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieldService.Data;
using FieldService.WinRT.Utilities;
using FieldService.Utilities;
using FieldService.WinRT.Views;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Xamarin.Media;

namespace FieldService.WinRT.ViewModels {
    public class ExpenseViewModel : FieldService.ViewModels.ExpenseViewModel {
        DelegateCommand addExpenseCommand, saveExpenseCommand, deleteExpenseCommand, cancelExpenseCommand, addImageCommand;
        Expense selectedExpense;
        ExpenseCategory [] expenseTypes = new ExpenseCategory [] { ExpenseCategory.Gas, ExpenseCategory.Food, ExpenseCategory.Supplies, ExpenseCategory.Other };
        string expenseCost = string.Empty;
        Popup addExpensePopUp;
        MediaPicker picker;
        AssignmentViewModel assignmentViewModel;

        public ExpenseViewModel ()
        {
            picker = new MediaPicker ();

            assignmentViewModel = ServiceContainer.Resolve<AssignmentViewModel>();

            addExpenseCommand = new DelegateCommand (obj => {
                var expense = obj as Expense;
                if (expense != null)
                    SelectedExpense = expense;
                else
                    SelectedExpense = new Expense ();
                addExpensePopUp = new Popup ();
                addExpensePopUp.Height = Window.Current.Bounds.Height;
                addExpensePopUp.Width = Constants.PopUpWidth;
                AddExpenseFlyoutPanel flyoutpanel = new AddExpenseFlyoutPanel ();
                flyoutpanel.Width = addExpensePopUp.Width;
                flyoutpanel.Height = addExpensePopUp.Height;
                addExpensePopUp.Child = flyoutpanel;
                addExpensePopUp.SetValue (Canvas.LeftProperty, Window.Current.Bounds.Width - Constants.PopUpWidth);
                addExpensePopUp.SetValue (Canvas.TopProperty, 0);
                addExpensePopUp.IsOpen = true;
            });

            saveExpenseCommand = new DelegateCommand (async _ => {
                selectedExpense.Cost = ExpenseCost.ToDecimal ();
                selectedExpense.Assignment = assignmentViewModel.SelectedAssignment.ID;
                await SaveExpense (assignmentViewModel.SelectedAssignment, selectedExpense);
                await LoadExpenses (assignmentViewModel.SelectedAssignment);
                addExpensePopUp.IsOpen = false;
            });

            deleteExpenseCommand = new DelegateCommand (async _ => {
                await DeleteExpense (assignmentViewModel.SelectedAssignment, selectedExpense);
                await LoadExpenses (assignmentViewModel.SelectedAssignment);
                addExpensePopUp.IsOpen = false;
            });

            cancelExpenseCommand = new DelegateCommand (_ => {
                addExpensePopUp.IsOpen = false;
            });

            addImageCommand = new DelegateCommand (async _ => {
                bool cameraCommand = false, imageCommand = false;
                var dialog = new MessageDialog ("Take picture with your built in camera or select one from your photo library.", "Add Image");
                if (picker.IsCameraAvailable) {
                    dialog.Commands.Add (new UICommand ("Camera", new UICommandInvokedHandler (q => cameraCommand = true)));
                }
                dialog.Commands.Add (new UICommand ("Library", new UICommandInvokedHandler (q => imageCommand = true)));

                await dialog.ShowAsync ();

                if (cameraCommand) {
                    StoreCameraMediaOptions options = new StoreCameraMediaOptions {
                        Directory = "FieldService",
                        Name = "FieldService.jpg",
                    };
                    try {
                        var mediaFile = await picker.TakePhotoAsync (options);
                        
                        await mediaFile.GetStream ().LoadBytes ().ContinueWith (t => {
                            SelectedExpense.Photo = t.Result;
                        });
                        OnPropertyChanged ("SelectedExpense");
                    } catch (Exception exc) {
                        Debug.WriteLine (exc.Message);
                        //this could happen if they cancel, etc.
                    }
                } else if (imageCommand) {
                    try {
                        var mediaFile = await picker.PickPhotoAsync ();

                        await mediaFile.GetStream ().LoadBytes ().ContinueWith (t => {
                            SelectedExpense.Photo = t.Result;
                        });
                        OnPropertyChanged ("SelectedExpense");
                    } catch (Exception exc) {
                        Debug.WriteLine (exc.Message);
                        //this could happen if they cancel, etc.
                    }
                }
            });
        }

        /// <summary>
        /// Command to show add expense flyout
        /// </summary>
        public DelegateCommand AddExpenseCommand
        {
            get { return addExpenseCommand; }
        }

        /// <summary>
        /// Command to delete expense 
        /// </summary>
        public DelegateCommand DeleteExpenseCommand
        {
            get { return deleteExpenseCommand; }
        }

        /// <summary>
        /// Command to save expense 
        /// </summary>
        public DelegateCommand SaveExpenseCommand
        {
            get { return saveExpenseCommand; }
        }

        /// <summary>
        /// Command to hide add expense flyout
        /// </summary>
        public DelegateCommand CancelExpenseCommand
        {
            get { return cancelExpenseCommand; }
        }

        /// <summary>
        /// Command to get an image for the expense
        /// </summary>
        public DelegateCommand AddImageCommand
        {
            get { return addImageCommand; }
        }

        /// <summary>
        /// list of top 5 labor hour items
        /// </summary>
        public IEnumerable<Expense> TopExpenses
        {
            get
            {
                if (Expenses == null)
                    return null;

                return Expenses.Take (5);
            }
        }

        public Expense SelectedExpense
        {
            get { return selectedExpense; }
            set
            {
                selectedExpense = value;
                OnPropertyChanged ("SelectedExpense");
                if (value != null)
                    ExpenseCost = value.Cost.ToString ("0.00");
                else
                    ExpenseCost = string.Empty;
            }
        }

        public ExpenseCategory [] ExpenseTypes
        {
            get { return expenseTypes; }
        }

        public string ExpenseCost
        {
            get { return expenseCost; }
            set { expenseCost = value; OnPropertyChanged ("ExpenseCost"); }
        }

        protected override void OnPropertyChanged (string propertyName)
        {
            base.OnPropertyChanged (propertyName);

            //Make sure property changed is raised for new properties
            if (propertyName == "Expenses") {
                OnPropertyChanged ("TopExpenses");
            }
        }
    }
}