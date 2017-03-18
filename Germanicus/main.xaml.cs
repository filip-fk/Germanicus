using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Germanicus
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class main : Page
    {
        public main()
        {
            this.InitializeComponent();
            ToDo.Visibility = Visibility.Visible;
        }

        private void opensplitview(object sender, RoutedEventArgs e)
        {
            spv.IsPaneOpen = !spv.IsPaneOpen;
        }

        private void svp_sc(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem lbi = ((sender as ListBox).SelectedItem as ListBoxItem);
            
            if (todo.IsSelected == true)
            {
                ToDo.Visibility = Visibility.Visible;
                wordg.Visibility = Visibility.Collapsed;
                dictionaryg.Visibility = Visibility.Collapsed;
                practiceg.Visibility = Visibility.Collapsed;
                quizg.Visibility = Visibility.Collapsed;
            }

            if (word.IsSelected == true)
            {
                ToDo.Visibility = Visibility.Collapsed;
                wordg.Visibility = Visibility.Visible;
                dictionaryg.Visibility = Visibility.Collapsed;
                practiceg.Visibility = Visibility.Collapsed;
                quizg.Visibility = Visibility.Collapsed;
            }

            if (dictionary.IsSelected == true)
            {
                ToDo.Visibility = Visibility.Collapsed;
                wordg.Visibility = Visibility.Collapsed;
                dictionaryg.Visibility = Visibility.Visible;
                practiceg.Visibility = Visibility.Collapsed;
                quizg.Visibility = Visibility.Collapsed;
            }

            if (practice.IsSelected == true)
            {
                ToDo.Visibility = Visibility.Collapsed;
                wordg.Visibility = Visibility.Collapsed;
                dictionaryg.Visibility = Visibility.Collapsed;
                practiceg.Visibility = Visibility.Visible;
                quizg.Visibility = Visibility.Collapsed;
            }

            if (quiz.IsSelected == true)
            {
                ToDo.Visibility = Visibility.Collapsed;
                wordg.Visibility = Visibility.Collapsed;
                dictionaryg.Visibility = Visibility.Collapsed;
                practiceg.Visibility = Visibility.Collapsed;
                quizg.Visibility = Visibility.Visible;
            }
        }

        private string[] selectionItems = new string[] { "Weather", "try 2", "random 3", "my 4" };

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var autoSuggestBox = (AutoSuggestBox)sender;
            var filtered = selectionItems.Where(p => p.StartsWith(autoSuggestBox.Text)).ToArray();
            autoSuggestBox.ItemsSource = filtered;
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                // User selected an item from the suggestion list, take an action on it here.
            }
            else
            {
                // Use args.QueryText to determine what to do.
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            /*
            if (sugg.Text == "Weather")
            {

            }
            */
        }

        private void signout_in(object sender, RoutedEventArgs e)
        {

        }
    }
}
