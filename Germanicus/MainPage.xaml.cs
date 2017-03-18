﻿//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using Germanicus.AppDialogs;
using Germanicus.UserDetection;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Germanicus
{
    /// <summary>
    /// The main FamilyNotes page and controller class that the user interacts with.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Disabled;

            // Initialize the data model for the application, consisting of a list of people (the family),
            // a list of notes (which are assigned to a person), and the app's settings.
            App app = (App)Application.Current;
            FamilyModel = app.Model;
            AppSettings = app.AppSettings;

            // Initially assume we are unfiltered - all notes are shown.
            CurrentlyFiltered = false;

            // Update greeting that appears at the top of the screen e.g. "Good morning"
            UpdateGreeting(String.Empty);

            // Create default notes if first launch
            if (AppSettings.LaunchedPreviously != true)
                CreateDefaultNotes();

        }

        /// <summary>
        /// This method is called when the app first appears, but it also checks if it has been launched
        /// using Cortana's voice commands, and takes appropriate action if this is the case.
        /// </summary>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            _presence = new UserPresence(_dispatcher, _unfilteredName);

            _pageParameters = e.Parameter as VoiceCommandObjects.VoiceCommand;
            if (_pageParameters != null)
            {
                switch (_pageParameters.VoiceCommandName)
                {
                    case "addNewNote":
                        _activeNote = CreateNote(App.EVERYONE);
                        break;
                    case "addNewNoteForPerson":
                        _activeNote = CreateNote(_pageParameters.NoteOwner);
                        break;
                    default:
                        break;
                }
            }

            //Perform initialization for facial detection.
            if (AppSettings.FaceApiKey != "")
            {
                await FacialSimilarity.TrainDetectionAsync();
            }

            // Perform initialization for speech recognition.
            _speechManager = new SpeechManager(FamilyModel);
            _speechManager.PhraseRecognized += speechManager_PhraseRecognized;
            _speechManager.StateChanged += speechManager_StateChanged;
            await _speechManager.StartContinuousRecognition();
        }

        #endregion

        #region Public properties

        public Note FocusedNote
        {
            get
            {
                return taskPanel.FocusedNote;
            }
        }

        #endregion

        #region Public methods

        public StickyNote CreateNote(Person person)
        {
            // Create the new note
            StickyNote newNote = new StickyNote(person);
            FamilyModel.StickyNotes.Add(newNote);
            return newNote;
        }

        public StickyNote CreateNote(string nameTag)
        {
            // Create the new note
            StickyNote newNote = new StickyNote(nameTag);
            newNote.NoteIsFor = FamilyModel.PersonFromName(nameTag);
            FamilyModel.StickyNotes.Add(newNote);
            return newNote;
        }

        public void Public_ShowNotesForPerson(string nameTag)
        {
            Person selectedPerson = FamilyModel.PersonFromName(nameTag);
            taskPanel.FilterNotes(selectedPerson);

            // Determine whether or not we are currently filtering
            if (selectedPerson.FriendlyName == _unfilteredName)
            {
                CurrentlyFiltered = false;
            }
            else
            {
                CurrentlyFiltered = true;
            }
        }

        public void Public_AddNewPerson()
        {
            AddNewPersonDialog(null); // Null means there is no existing person. But is this even called?!
        }

        #endregion

        #region Implementation

        private Model FamilyModel { get; set; }

        private Settings AppSettings { get; set; }

        private bool CurrentlyFiltered
        {
            get
            {
                return _currentlyFiltered;
            }
            set
            {
                _currentlyFiltered = value;

                if (_presence != null)
                {
                    _presence._currentlyFiltered = value;
                    if (FaceDetectionEnabledIcon.Visibility == Visibility.Visible)
                    {
                        if (value)
                        {
                            ImageWarningBox.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ImageWarningBox.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Display a friendly greeting at the top of the screen.
        /// </summary>
        /// <param name="name">Name of user for the salutation</param>
        private async void UpdateGreeting(string name)
        {
            var now = DateTime.Now;
            var greeting =
                now.Hour < 12 ? "Good morning" :
                now.Hour < 18 ? "Good afternoon" :
                /* otherwise */ "Good night";
            var person = (string.IsNullOrEmpty(name) || name == App.EVERYONE) ? "!" : $", {name}!";
            TextGreeting.Text = $"{greeting}{person}";

            if (!string.IsNullOrEmpty(name) && (name != App.EVERYONE))
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {

                    var SpeakGreeting = $"{greeting} {name}";

                    var notes = taskPanel.CountNotes(FamilyModel.PersonFromName(name));

                    if (notes > 0)
                    {
                        if (notes == 1)
                            SpeakGreeting += ",there is a note for you.";
                        else
                            SpeakGreeting += $",there are {notes} notes for you.";
                    }

                    await this._speechManager.SpeakAsync(
                        SpeakGreeting,
                         this._media);
                });
            }
        }



        private async Task<bool> FocusedNoteAssigned()
        {
            bool focusedNote = false;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                focusedNote = FocusedNote != null ? true : false;
            });

            return focusedNote;
        }

        private void UserFilterFromDetection(object sender, UserPresence.UserIdentifiedEventArgs e)
        {
            this.Public_ShowNotesForPerson(e.User);
            UpdateGreeting(e.User);
        }

        #endregion

        #region User Interactions

        /// <summary>
        /// When the New Note button is tapped, the people picker popup promptly appears,
        /// and this allow notes to be created for specific people.
        /// </summary>
        private async void AddNewPersonDialog(Person currentPerson)
        {
            var dialog = new AddPersonContentDialog();

            dialog.ProvideExistingPerson(currentPerson);
            await dialog.ShowAsync();
            Person newPerson = dialog.AddedPerson;

            // If there is a valid person to add, add them
            if (newPerson != null)
            {
                // Get or create a directory for the user (we do this regardless of whether or not there is a profile picture)
                StorageFolder userFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(("Users\\" + newPerson.FriendlyName), CreationCollisionOption.OpenIfExists);

                // See if we have a profile photo
                if (dialog.TemporaryFile != null)
                {
                    // Save off the profile photo and delete the temporary file
                    await dialog.TemporaryFile.CopyAsync(userFolder, "ProfilePhoto.jpg", NameCollisionOption.ReplaceExisting);
                    await dialog.TemporaryFile.DeleteAsync();

                    // Update the profile picture for the person
                    newPerson.IsProfileImage = true;
                    newPerson.ImageFileName = userFolder.Path + "\\ProfilePhoto.jpg";

                    if (AppSettings.FaceApiKey != "")
                    {
                        await FacialSimilarity.AddTrainingImageAsync(newPerson.FriendlyName, new Uri($"ms-appdata:///local/Users/{newPerson.FriendlyName}/ProfilePhoto.jpg"));
                    }
                }
                // Add the user if it is new now that changes have been made
                if (currentPerson == null)
                {
                    await FamilyModel.AddPersonAsync(newPerson);
                }
                // Otherwise we had a user, so update the current one
                else
                {
                    //await FamilyModel.UpdatePersonImageAsync(newPerson);
                    Person personToUpdate = FamilyModel.PersonFromName(currentPerson.FriendlyName);
                    if (personToUpdate != null)
                    {
                        personToUpdate.IsProfileImage = true;
                        personToUpdate.ImageFileName = userFolder.Path + "\\ProfilePhoto.jpg";
                    }
                }
            }
        }

        private void AddPersonTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add null if there is no existing person
            AddNewPersonDialog(null);
        }

        private async void FaceDetectionButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Inform the user if we do not have a Microsoft face service key and then exit without doing anything
            if (AppSettings.FaceApiKey == "")
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog("You need a Microsoft Face API service key, which you define in settings, to use facial recognition.");
                await messageDialog.ShowAsync();
                return;
            }

            if (SetCameraButton.IsEnabled)
            {
                // Make sure the user accepts privacy implications.
                var dialog = new WarningDialog();
                await dialog.ShowAsync();
                if (dialog.WarningAccept == false)
                {
                    return;
                }
            }

            bool result = await _presence.EnableFaceDetection();
            if (result)
            {
                _presence.FilterOnFace += UserFilterFromDetection;
                CountBox.Visibility = Visibility.Visible;
                if (!_currentlyFiltered)
                {
                    ImageWarningBox.Visibility = Visibility.Visible;
                }
            }
            else
            {
                _presence.FilterOnFace -= UserFilterFromDetection;
                CountBox.Visibility = Visibility.Collapsed;
                ImageWarningBox.Visibility = Visibility.Collapsed;
            }

            // Update the face detection icon depending on whether the effect exists or not
            FaceDetectionDisabledIcon.Visibility = (result != true) ? Visibility.Visible : Visibility.Collapsed;
            FaceDetectionEnabledIcon.Visibility = (result == true) ? Visibility.Visible : Visibility.Collapsed;
            SetCameraButton.IsEnabled = (result != true);
        }

        /// <summary>
        /// Sets the camera object to use, in case you don't want to use the default
        /// </summary>
        private async void SetCameraDevice(object sender, TappedRoutedEventArgs e)
        {
            // Create a DevicePicker
            var devicePicker = new DevicePicker();

            // Set that we are looking for video capture devices
            devicePicker.Filter.SupportedDeviceClasses.Add(DeviceClass.VideoCapture);

            // Calculate the position to show the picker (right below the buttons)
            GeneralTransform transform = SetCameraButton.TransformToVisual(null);
            Point point = transform.TransformPoint(new Point());
            Rect rect = new Rect(point, new Point(point.X + SetCameraButton.ActualWidth, point.Y + SetCameraButton.ActualHeight));

            // Use the device picker to pick the device
            DeviceInformation deviceInfo = await devicePicker.PickSingleDeviceAsync(rect);
            if (null != deviceInfo)
            {
                _presence.CameraDeviceId = deviceInfo.Id;
                _presence.IsDefaultCapture = false;
            }
        }

        private void TidyNotes(object sender, TappedRoutedEventArgs e)
        {
            taskPanel.TidyNotes();
        }

        /// <summary>
        /// Show the notes for the selected person
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FamilyList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Link the XAML object tapped with the PERSON in the FAMILY collection.
            var listView = sender as ListView;
            var selectedPerson = listView.SelectedItem as Person;
            taskPanel.FilterNotes(selectedPerson); // show the notes that apply to this person

            // Determine whether or not we are currently filtering
            CurrentlyFiltered = selectedPerson.FriendlyName != _unfilteredName;

            // Update greeting that appears at the top of the screen e.g. "Good morning"
            UpdateGreeting(selectedPerson.FriendlyName);
        }


        private void FamilyList_RightTap(object sender, RightTappedRoutedEventArgs e)
        {
            FamilyListMethod(e.OriginalSource as Image);
        }



        /// <summary>
        /// When the user taps-and-holds on a person, a flyout menu appears. The menu options allow
        /// for a note to be quickly created for that person, or the person can be deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FamilyList_Holding(object sender, HoldingRoutedEventArgs e)
        {
            // Touch-and-hold is a useful shortcut to allow a note to be created for a specific person
            if (e.HoldingState != Windows.UI.Input.HoldingState.Started)
            {
                // Holding events are called multiple times per touch-and-hold event,
                // so the condition above makes sure that we only act on the one event
                // we need.
                return;
            }

            FamilyListMethod(e.OriginalSource as Image);

        }

        private void FamilyListMethod(Image selectedImage)
        {
            //  Image selectedImage = e.OriginalSource as Image;

            // To find which person the user is touching, we look at the owner of the image. However,
            // sometimes the selected Image is null, because the user's finger isn't exactly in the right 
            // place, and is (for example) touching the text under the image. We return when this happens.

            if (selectedImage == null)
            {
                return;
            }

            Person selectedPerson = selectedImage.DataContext as Person;

            var menu = new MenuFlyout();

            // We use our subclass of MenuFlyoutItem to store the selected person
            // and so pass it to the menu option handlers.

            var option1 = new MenuFlyoutItem() { Text = "Create note for " + selectedPerson.FriendlyName };
            var option2 = new MenuFlyoutItem() { Text = "Add/replace photo for " + selectedPerson.FriendlyName };
            var option3 = new MenuFlyoutItem() { Text = "Delete " + selectedPerson.FriendlyName };

            option1.Tag = selectedPerson;
            option2.Tag = selectedPerson;
            option3.Tag = selectedPerson;

            option1.Click += menuFlyoutOptionCreateNote;
            option2.Click += menuFlyoutOptionAddPhotoToPerson;
            option3.Click += menuFlyoutOptionDeletePerson;

            menu.Items.Add(option1);

            if (selectedPerson.FriendlyName != App.EVERYONE)
            {
                menu.Items.Add(option2);
                menu.Items.Add(option3);
            }

            menu.ShowAt(selectedImage, new Point(60, 0));
        }

        private void menuFlyoutOptionCreateNote(object sender, RoutedEventArgs e)
        {
            Person selectedPerson = (Person)(sender as MenuFlyoutItem).Tag;
            this.CreateNote(selectedPerson);
        }

        private async void menuFlyoutOptionDeletePerson(object sender, RoutedEventArgs e)
        {
            Person selectedPerson = (Person)(sender as MenuFlyoutItem).Tag;
            await this.FamilyModel.DeletePersonAsync(selectedPerson.FriendlyName);
        }

        private void menuFlyoutOptionAddPhotoToPerson(object sender, RoutedEventArgs e)
        {
            Person selectedPerson = (Person)(sender as MenuFlyoutItem).Tag;

            AddNewPersonDialog(selectedPerson);

            // Do the things to make the add person

        }

        /// <summary>
        /// Display the settings popup since the user has tapped the Settings button.
        /// </summary>
        private void SettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!SettingsPopup.IsOpen)
            {
                rootPopupBorder.Width = 346;
                rootPopupBorder.Height = ActualHeight;
                SettingsPopup.HorizontalOffset = Window.Current.Bounds.Width - rootPopupBorder.Width;
                SettingsPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// Delete all app data, including users (execept 'Everyone')
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteAllButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var dialog = new DeleteConfirmationDialog();
            await dialog.ShowAsync();
            if (dialog.DeleteData == false)
            {
                return;
            }

            // Delete FamilyNotesData and ink.isf
            IsolatedStorageFile appLocalData = IsolatedStorageFile.GetUserStoreForApplication();
            if (appLocalData.FileExists(App.NOTES_MODEL_FILE))
            {
                appLocalData.DeleteFile(App.NOTES_MODEL_FILE);
            }

            if (appLocalData.FileExists(App.NOTES_INK_FILE))
            {
                appLocalData.DeleteFile(App.NOTES_INK_FILE);
            }

            foreach (string name in FamilyModel.Family.Select(person => person.FriendlyName).ToList())
            {
                bool success = await FamilyModel.DeletePersonAsync(name);
                Debug.Assert(success == true, "Unable to delete person");
            }
        }

        #endregion

        #region Speech Handling

        private async void speechManager_StateChanged(object sender, StateChangedEventArgs e)
        {

            if (e.IsSessionState && !e.SessionCompletedSuccessfully && e.SessionTimedOut)
            {
                Debug.WriteLine("Timeout exceeded, resetting RecognitionMode to CommandPhrases");
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await _speechManager.SetRecognitionMode(SpeechRecognitionMode.CommandPhrases);
                });
            }
        }

        /// <summary>
        /// Handles the <see cref="SpeechManager.PhraseRecognized"/> event.
        /// </summary>
        /// <param name="sender">the <see cref="SpeechManager"/> that raised the event.</param>
        /// <param name="e">The event data.</param>
        private async void speechManager_PhraseRecognized(object sender, PhraseRecognizedEventArgs e)
        {
            Person person = e.PhraseTargetPerson;
            string phrase = e.PhraseText;
            CommandVerb verb = e.Verb;

            string msg = String.Format("Heard phrase: {0}", phrase);
            Debug.WriteLine(msg);

            switch (verb)
            {
                case CommandVerb.Dictation:
                    {
                        // The phrase came from dictation, so transition speech recognition
                        // to listen for command phrases.
                        await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            FocusedNote.NoteBusinessObject.NoteText = phrase;
                            await _speechManager.SetRecognitionMode(SpeechRecognitionMode.CommandPhrases);
                        });

                        break;
                    }
                case CommandVerb.Create:
                    {
                        // A command for creating a note was recognized.
                        await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            _activeNote = CreateNote(person);
                            _activeNote.NoteText = "Dictate your note here!";
                            await _speechManager.SpeakAsync("Dictate your note", _media);
                            await _speechManager.SetRecognitionMode(SpeechRecognitionMode.Dictation);
                        });

                        break;
                    }
                case CommandVerb.Read:
                    {
                        // The command for reading a note was recognized.
                        bool focusedNoteAssigned = await FocusedNoteAssigned();
                        if (focusedNoteAssigned)
                        {
                            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                await _speechManager.SpeakAsync(
                                    FocusedNote.NoteBusinessObject.NoteText,
                                    _media);
                            });
                        }

                        break;
                    }
                case CommandVerb.Edit:
                    {
                        // The command for editing a note was recognized.
                        bool focusedNoteAssigned = await FocusedNoteAssigned();
                        if (focusedNoteAssigned)
                        {
                            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                await _speechManager.SpeakAsync("Dictate your note", _media);
                                await _speechManager.SetRecognitionMode(SpeechRecognitionMode.Dictation);
                            });
                        }

                        break;
                    }
                case CommandVerb.Delete:
                    {
                        // The command for deleting a note was recognized.
                        bool focusedNoteAssigned = await FocusedNoteAssigned();
                        if (focusedNoteAssigned)
                        {
                            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                FocusedNote.OnDeleteNoteEvent();
                                await _speechManager.SpeakAsync("Note deleted", _media);
                            });
                        }

                        break;
                    }
                case CommandVerb.Show:
                    {
                        Debug.WriteLine("SpeechManager.PhraseRecognized handler: Show TBD");
                        break;
                    }
                case CommandVerb.Help:
                    {
                        // A command for spoken help was recognized.
                        await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            await _speechManager.SpeakAsync(_helpString, _media);
                        });

                        break;
                    }
                default:
                    {
                        Debug.WriteLine("SpeechManager.PhraseRecognized handler: Couldn't determine phrase intent");
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles transitions into and out of dictation mode 
        /// for the focused <see cref="Note"/> control.
        /// </summary>
        /// <param name="noteControl">The <see cref="Note"/> control.</param>
        /// <param name="e">The event data.</param>
        private async void taskPanel_NoteInputModeChanged(object sender, InputModeChangedEventArgs e)
        {
            // Transition out of dictation mode. 
            if (_speechManager.RecognitionMode ==
                SpeechRecognitionMode.Dictation &&
                e.NewInputMode != NoteInputMode.Dictation)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await _speechManager.SetRecognitionMode(SpeechRecognitionMode.CommandPhrases);
                });
            }
            // Transition to dictation mode. 
            else if (_speechManager.RecognitionMode !=
                SpeechRecognitionMode.Dictation &&
                e.NewInputMode == NoteInputMode.Dictation)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await _speechManager.SpeakAsync("Dictate your note", _media);
                    await _speechManager.SetRecognitionMode(SpeechRecognitionMode.Dictation);
                });
            }
        }

        #endregion


        public void CreateDefaultNotes()
        {
            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "6/6\n\nYou can also use voice commands, such as 'Add new note' Try 'What can I say?' for help.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "5/6\n\nFilter notes by tapping the relevant user's button on the left, or by turning on the camera if using face recognition.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "4/6\n\nIf you want to use the face recognition feature: obtain a Face API Key, add it to the app settings, and then enable the feature.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "3/6\n\nNow you can add new notes by pressing the 'New note' button and selecting 'Everyone' or another user.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "2/6\n\nTo get started, you should add some users by tapping on the 'New person' button at the bottom of the screen.";

            this._activeNote = CreateNote(App.EVERYONE);
            this._activeNote.NoteText = "1/6\n\nWelcome to Family Notes!\nOnce you have read these sample notes, you can delete them by tapping on the little X in the top left.";
        }

        #region Private fields

        private StickyNote _activeNote;
        private static string _unfilteredName = App.EVERYONE;
        private UserPresence _presence;
        private const string _detectionString = "Detected faces : ";
        private CoreDispatcher _dispatcher;
        private SpeechManager _speechManager;
        private VoiceCommandObjects.VoiceCommand _pageParameters;
        private bool _currentlyFiltered;
        private const string _helpString = "You can say: add note for person, or, create note to person, or, new note to person. For the active note, you can say, edit note, read note, and delete note.";

        #endregion

        private void selection_changed(object sender, SelectionChangedEventArgs e)
        {
            //

        }

        private void NewNote_Tapped(object sender, TappedRoutedEventArgs e)
        {
            /*
            if (!peoplePopup.IsOpen)
            {
                int width = (FamilyModel.Family.Count()) * 110;
                rootPopupBorder2.Width = 16 + width;
                rootPopupBorder2.Height = 128;
                peoplePopup.HorizontalOffset = 0;
                peoplePopup.VerticalOffset = ActualHeight - 208;
                peoplePopup.IsOpen = true;
            }

            _activeNote = CreateNote("1");
            _activeNote.NotePlaceholderText = "Type your note here.";
            _activeNote.NoteText = "Someting";
            _activeNote.WhoNoteIsFor = "Homework";
            */

            _activeNote = CreateNote("1");
            string content10 = act_content.Text.ToString();
            _activeNote.NoteText = content10;
            noteContent.Visibility = Visibility.Collapsed;
            //string name1 = type_to_choose.SelectedItem.ToString();
            //notename(name1);
        }

        /// <summary>
        /// The people picker popup appears after tapping New Note, and this is the event
        /// that is given the person selected by the user. It then creates a note for that person. 
        /// </summary>
        private void PeoplePicker_Tapped(object sender, TappedRoutedEventArgs e)
        {
            peoplePopup.IsOpen = false;
            Person person = (sender as ListView).SelectedItem as Person;
            _activeNote = CreateNote("Homework");
            _activeNote.NotePlaceholderText = "Type your note here.";
            _activeNote.NoteText = "Someting";
        }

       

        private void content_changed(object sender, TextChangedEventArgs e)
        {
            content10.Text = act_content.Text;
        }

        private void closecontfill(object sender, TappedRoutedEventArgs e)
        {
            noteContent.Visibility = Visibility.Collapsed;
        }

        private void NewNote_show(object sender, TappedRoutedEventArgs e)
        {
            noteContent.Visibility = Visibility.Visible;
        }

        private void date_changed(object sender, DatePickerValueChangedEventArgs e)
        {
            duedate10.Text = "To be done by " + dtpicker.Date.Day + "/" + dtpicker.Date.Month + "/" + dtpicker.Date.Year;
        }
    }

}
