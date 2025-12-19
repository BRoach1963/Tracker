using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Win32;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using Tracker.Command;
using Tracker.Common;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Helpers;
using Tracker.Managers;
using Tracker.Controls;
using Tracker.Database;
using Tracker.Eventing;
using Tracker.Eventing.Messages;

namespace Tracker.ViewModels.DialogViewModels
{
    public class TeamMemberViewModel : BaseDialogViewModel
    {
        #region Fields 

        private TeamMember _data;
        private bool _inEditMode; 

        private ICommand? _toggleEditModeCommand;
        private ICommand? _chooseProfilePicCommand;
        private ICommand? _launchLinkedInProfileCommand;
        private ICommand? _updateTeamMemberCommand;
        private ICommand? _addTeamMemberCommand;

        // Feedback commands
        private ICommand? _addFeedbackCommand;
        private ICommand? _editFeedbackCommand;
        private ICommand? _deleteFeedbackCommand;

        // Goal commands
        private ICommand? _addGoalCommand;
        private ICommand? _editGoalCommand;
        private ICommand? _deleteGoalCommand;

        // Meeting command
        private ICommand? _viewMeetingDetailsCommand;

        // Quick Message command
        private ICommand? _sendMessageCommand;

        private ImageSource? _profileImage;

        private ObservableCollection<EnumWrapper<RoleEnum>> _roles = new ();
        private ObservableCollection<EnumWrapper<EngineeringSpecialtyEnum>> _specialties = new();
        private ObservableCollection<EnumWrapper<SkillLevelEnum>> _levels = new();

        private EnumWrapper<RoleEnum>? _selectedRole;
        private EnumWrapper<EngineeringSpecialtyEnum>? _selectedSpecialty;
        private EnumWrapper<SkillLevelEnum>? _selectedLevel;

        private Dictionary<string, object> _changedProperties = new();

        // Meeting history
        private ObservableCollection<OneOnOne> _meetings = new();
        private OneOnOne? _selectedMeeting;

        // Feedback history
        private ObservableCollection<Feedback> _feedbacks = new();
        private Feedback? _selectedFeedback;

        // Individual goals
        private ObservableCollection<IndividualGoal> _goals = new();
        private IndividualGoal? _selectedGoal;

        #endregion

        #region Ctor

        public TeamMemberViewModel(Action? callback, TeamMember data, bool edit = false) : base(callback)
        {
            _inEditMode = edit;
            _data = data;
            if (!_inEditMode)
            {
                _data.BirthDay = DateTime.Now;
                _data.HireDate = DateTime.Now;
            } 
         
            if (_data.ProfileImage.Length > 0)
            {
                // Use default image initially, then load async to avoid blocking
                ProfileImage = ImageHelper.LoadDefaultProfileImage();
                _ = LoadProfileImageAsync(_data.ProfileImage);
            }
            else
            {
                //No Data - use default Image
                ProfileImage = ImageHelper.LoadDefaultProfileImage();
            }

            SetLists();
            SelectedLevel = _levels.FirstOrDefault(x => x.EnumValue == _data.SkillLevel);
            SelectedRole = _roles.FirstOrDefault(x => x.EnumValue == _data.Role);
            SelectedSpeciality = _specialties.FirstOrDefault(x => x.EnumValue == _data.Specialty);
            
            // Load data for existing team members
            if (_inEditMode && _data.Id > 0)
            {
                LoadMeetingHistory();
                LoadFeedbackHistory();
                LoadGoals();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _roles.Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Commands

        public ICommand ToggleEditModeCommand => _toggleEditModeCommand ??= new TrackerCommand(ToggleEditModeExecuted);

        public ICommand ChooseProfilePictureCommand =>
            _chooseProfilePicCommand ??= new TrackerCommand(ExecuteChooseProfilePicture);

        public ICommand LaunchLinkedInUrlCommand => _launchLinkedInProfileCommand ??=
            new TrackerCommand(LaunchLinkedInProfileExecuted, CanLaunchLinkedInProfile);

        public ICommand UpdateTeamMemberCommand => _updateTeamMemberCommand ??=
            new TrackerCommand(UpdateTeamMemberExecuted, CanExecuteUpdateTeamMember);

        public ICommand AddTeamMemberCommand => _addTeamMemberCommand ??=
            new TrackerCommand(AddTeamMemberExecuted, CanExecuteAddTeamMember);

        // Feedback Commands
        public ICommand AddFeedbackCommand => _addFeedbackCommand ??=
            new TrackerCommand(AddFeedbackExecuted);

        public ICommand EditFeedbackCommand => _editFeedbackCommand ??=
            new TrackerCommand(EditFeedbackExecuted, _ => SelectedFeedback != null);

        public ICommand DeleteFeedbackCommand => _deleteFeedbackCommand ??=
            new TrackerCommand(DeleteFeedbackExecuted, _ => SelectedFeedback != null);

        // Goal Commands
        public ICommand AddGoalCommand => _addGoalCommand ??=
            new TrackerCommand(AddGoalExecuted);

        public ICommand EditGoalCommand => _editGoalCommand ??=
            new TrackerCommand(EditGoalExecuted, _ => SelectedGoal != null);

        public ICommand DeleteGoalCommand => _deleteGoalCommand ??=
            new TrackerCommand(DeleteGoalExecuted, _ => SelectedGoal != null);

        // Meeting Command
        public ICommand ViewMeetingDetailsCommand => _viewMeetingDetailsCommand ??=
            new TrackerCommand(ViewMeetingDetailsExecuted);

        // Quick Message Command
        public ICommand SendMessageCommand => _sendMessageCommand ??=
            new TrackerCommand(SendMessageExecuted, CanSendMessage);

        public bool CanShowMessaging => Services.Microsoft365.MicrosoftGraphAuthService.Instance.IsAuthenticated;

        private bool CanSendMessage(object? obj) => !string.IsNullOrEmpty(_data?.Email) && 
            Services.Microsoft365.MicrosoftGraphAuthService.Instance.IsAuthenticated;

        private void SendMessageExecuted(object? parameter)
        {
            if (_data == null) return;
            Views.Dialogs.QuickMessageDialog.ShowDialog(_data);
        }

        #endregion

        #region Public Properties

        // Meeting History Properties
        public ObservableCollection<OneOnOne> Meetings => _meetings;
        
        public OneOnOne? SelectedMeeting
        {
            get => _selectedMeeting;
            set
            {
                _selectedMeeting = value;
                RaisePropertyChanged();
            }
        }
        
        public int MeetingCount => _meetings.Count;
        public bool HasMeetings => _meetings.Count > 0;
        public bool HasNoMeetings => _meetings.Count == 0;

        // Feedback History Properties
        public ObservableCollection<Feedback> Feedbacks => _feedbacks;
        
        public Feedback? SelectedFeedback
        {
            get => _selectedFeedback;
            set
            {
                _selectedFeedback = value;
                RaisePropertyChanged();
            }
        }
        
        public int FeedbackCount => _feedbacks.Count;
        public bool HasFeedback => _feedbacks.Count > 0;
        public bool HasNoFeedback => _feedbacks.Count == 0;

        // Individual Goals Properties
        public ObservableCollection<IndividualGoal> Goals => _goals;
        
        public IndividualGoal? SelectedGoal
        {
            get => _selectedGoal;
            set
            {
                _selectedGoal = value;
                RaisePropertyChanged();
            }
        }
        
        public int GoalCount => _goals.Count;
        public int ActiveGoalCount => _goals.Count(g => g.Status == GoalStatus.InProgress);
        public bool HasGoals => _goals.Count > 0;
        public bool HasNoGoals => _goals.Count == 0;

        public ObservableCollection<EnumWrapper<RoleEnum>> Roles => _roles;

        public ObservableCollection<EnumWrapper<EngineeringSpecialtyEnum>> Specialties => _specialties;

        public ObservableCollection<EnumWrapper<SkillLevelEnum>> Levels => _levels;

        public EnumWrapper<RoleEnum>? SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                RaisePropertyChanged();
                if(value != null) UpdateChangedValues(TrackerConstants.TeamMemberRole, value.EnumValue);
            }
        }

        public EnumWrapper<SkillLevelEnum>? SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                _selectedLevel = value;
                RaisePropertyChanged();
                if (value != null) UpdateChangedValues(TrackerConstants.TeamMemberSkill, value.EnumValue);
            }
        }

        public EnumWrapper<EngineeringSpecialtyEnum>? SelectedSpeciality
        {
            get => _selectedSpecialty;
            set
            {
                _selectedSpecialty = value;
                RaisePropertyChanged();
                if (value != null) UpdateChangedValues(TrackerConstants.TeamMemberSpeciality, value.EnumValue);
            }
        }
 

        public bool InEditMode
        {
            get => _inEditMode;
            set
            {
                _inEditMode = value;
                RaisePropertyChanged();
            }
        }

        public string JobTitle
        {
            get => _data.JobTitle;
            set
            {
                _data.JobTitle = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberJobTitle, value);
            }
        }
        public string NickName
        {
            get => _data.NickName;
            set
            {
                _data.NickName = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberNickname, value);
            }
        }

        public string FirstName
        {
            get => _data.FirstName;
            set
            {
                _data.FirstName = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberFirstName, value);
            }
        }

        public string LastName
        {
            get => _data.LastName;
            set
            {
                _data.LastName = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberLastName, value);
            }
        }

        public string Email
        {
            get => _data.Email;
            set
            {
                _data.Email = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberEmail, value);
            }
        }

        public string CellPhone
        {
            get => _data.CellPhone;
            set
            {
                _data.CellPhone = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberCell, value);
            }
        }

        public string HireDateDisplay
        {
            get => _data.HireDate == new DateTime(1900, 1, 1) ? "MM/DD/YYYY" : _data.HireDate.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParseExact(value, "MM/dd/yyyy", null, DateTimeStyles.None, out DateTime date))
                {
                    _data.HireDate = date;
                }
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberHireDate, _data.HireDate);
            }
        }

        public string BirthDayDisplay
        {
            get => _data.BirthDay == DateTime.MinValue ? "MM/DD" : _data.BirthDay.ToString("MM/dd");
            set
            {
                if (DateTime.TryParseExact(value, "MM/dd", null, DateTimeStyles.None, out DateTime date))
                {
                    _data.BirthDay = date;
                }
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberBirthday, _data.BirthDay);
            }
        }

        public string TerminationDateDisplay
        {
            get => _data.TerminationDate == new DateTime(1900, 1, 1) ? "MM/DD/YYYY" : _data.TerminationDate.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParseExact(value, "MM/dd/yyyy", null, DateTimeStyles.None, out DateTime date))
                {
                    _data.TerminationDate = date;
                }
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberTerminationDate, _data.TerminationDate);
            }
        }

        public DateTime BirthDay
        {
            get => _data.BirthDay;
            set
            {
                _data.BirthDay = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(BirthDayDisplay));
            }
        }

        public DateTime HireDate
        {
            get => _data.HireDate;
            set
            {
                _data.HireDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HireDateDisplay));
            }
        }

        public DateTime TerminationDate
        {
            get => _data.TerminationDate;
            set
            {
                _data.TerminationDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TerminationDateDisplay));
            }
        }

        public ImageSource? ProfileImage
        {
            get { return _profileImage; }
            set
            {
                _profileImage = value;
               RaisePropertyChanged(); 
            }
        }

        public bool IsActive
        {
            get => _data.IsActive;
            set
            {
                _data.IsActive = value;
                if (_data.IsActive == false)
                {
                    TerminationDate = DateTime.Now;
                }
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberIsActive, _data.IsActive);
            }
        }

        public string LinkedInProfile
        {
            get => _data.LinkedInProfile;
            set
            {
                _data.LinkedInProfile = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberLinkedInProfile, _data.LinkedInProfile);
            }
        }

        public string FacebookProfile
        {
            get => _data.FacebookProfile;
            set
            {
                _data.FacebookProfile = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberFacebookProfile, _data.FacebookProfile);
            }
        }

        public string InstagramProfile
        {
            get => _data.InstagramProfile;
            set
            {
                _data.InstagramProfile = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberInstaProfile, _data.InstagramProfile);
            }
        }

        public string XProfile
        {
            get => _data.XProfile;
            set
            {
                _data.XProfile = value;
                RaisePropertyChanged();
                UpdateChangedValues(TrackerConstants.TeamMemberXProfile, _data.XProfile);
            }
        }

        #endregion

        #region Private Methods

        private void ToggleEditModeExecuted(object? parameter)
        {
            var inEditMode = (bool)(parameter ?? false);
            InEditMode = inEditMode; 
        }

        private void ExecuteChooseProfilePicture(object? obj)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFileName = openFileDialog.FileName;
                // Load and display the image
                ProfileImage = ImageHelper.GetImageSourceFromFile(selectedFileName);
                // Store the image in the database
                _data.ProfileImage = ImageHelper.GetByteArrayFromFile(selectedFileName);
                UpdateChangedValues(TrackerConstants.TeamMemberProfileImage, _data.ProfileImage);
            }

        }

        private bool CanExecuteUpdateTeamMember(object? obj)
        {
            return _changedProperties.Count > 0;
        }

        private async void UpdateTeamMemberExecuted(object? parameter)
        {
            try
            {
                await TrackerDataManager.Instance.UpdateTeamMember(_data);
                NotificationManager.Instance.ShowSuccess("Team Member Updated", $"{FirstName} {LastName} has been updated.");
                DataMessenger.SendRefresh(DataChangeType.TeamMembers);
                
                if (parameter is BaseWindow window)
                {
                    DialogManager.Instance.CloseDialog(window);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Error", $"Failed to update team member: {ex.Message}");
            }
        }

        private bool CanLaunchLinkedInProfile(object? obj)
        {
            return !string.IsNullOrEmpty(_data.LinkedInProfile);
        }

        private void LaunchLinkedInProfileExecuted(object? obj)
        {
            if (string.IsNullOrEmpty(_data.LinkedInProfile)) return;
             
            Process.Start(FormatUrl(_data.LinkedInProfile));
        }

        private ProcessStartInfo FormatUrl(string url)
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            return new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
        }

        private void SetLists()
        {
            foreach (RoleEnum role in Enum.GetValues(typeof(RoleEnum)))
            {
                _roles.Add(new EnumWrapper<RoleEnum>(role));
            }

            foreach (EngineeringSpecialtyEnum speciality in Enum.GetValues(typeof(EngineeringSpecialtyEnum)))
            {
                _specialties.Add(new EnumWrapper<EngineeringSpecialtyEnum>(speciality));
            }

            foreach (SkillLevelEnum level in Enum.GetValues(typeof(SkillLevelEnum)))
            {
                _levels.Add(new EnumWrapper<SkillLevelEnum>(level));
            }
        }


        private void UpdateChangedValues(string key, object? value)
        {
            if (value == null)
            {
                _changedProperties.Remove(key);
            }
            else
            {
                _changedProperties[key] = value;
            }
        }

        private async Task LoadProfileImageAsync(byte[] imageData)
        {
            try
            {
                var image = await ImageHelper.GetImageSourceFromByteArrayAsync(imageData).ConfigureAwait(true);
                if (image != null)
                {
                    ProfileImage = image;
                    RaisePropertyChanged(nameof(ProfileImage));
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash - default image is already set
                System.Diagnostics.Debug.WriteLine($"Error loading profile image: {ex.Message}");
            }
        }

        private async void LoadMeetingHistory()
        {
            try
            {
                var meetings = await TrackerDbManager.Instance.GetMeetingsForTeamMemberAsync(_data.Id);
                _meetings.Clear();
                foreach (var meeting in meetings.OrderByDescending(m => m.Date))
                {
                    _meetings.Add(meeting);
                }
                RaisePropertyChanged(nameof(Meetings));
                RaisePropertyChanged(nameof(MeetingCount));
                RaisePropertyChanged(nameof(HasMeetings));
                RaisePropertyChanged(nameof(HasNoMeetings));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading meeting history: {ex.Message}");
            }
        }

        private async void LoadFeedbackHistory()
        {
            try
            {
                var feedbacks = await TrackerDbManager.Instance.GetFeedbackForTeamMemberAsync(_data.Id);
                _feedbacks.Clear();
                foreach (var feedback in feedbacks)
                {
                    _feedbacks.Add(feedback);
                }
                RaisePropertyChanged(nameof(Feedbacks));
                RaisePropertyChanged(nameof(FeedbackCount));
                RaisePropertyChanged(nameof(HasFeedback));
                RaisePropertyChanged(nameof(HasNoFeedback));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading feedback history: {ex.Message}");
            }
        }

        private async void LoadGoals()
        {
            try
            {
                var goals = await TrackerDbManager.Instance.GetGoalsForTeamMemberAsync(_data.Id);
                _goals.Clear();
                foreach (var goal in goals)
                {
                    _goals.Add(goal);
                }
                RaisePropertyChanged(nameof(Goals));
                RaisePropertyChanged(nameof(GoalCount));
                RaisePropertyChanged(nameof(ActiveGoalCount));
                RaisePropertyChanged(nameof(HasGoals));
                RaisePropertyChanged(nameof(HasNoGoals));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading goals: {ex.Message}");
            }
        }

        private bool CanExecuteAddTeamMember(object? obj)
        {
            if (string.IsNullOrEmpty(FirstName)) return false;
            if (string.IsNullOrEmpty(LastName)) return false;
            if (string.IsNullOrEmpty(Email)) return false;

            return true;
        }

        private async void AddTeamMemberExecuted(object? parameter)
        {
            try
            {
                await TrackerDataManager.Instance.AddTeamMember(_data);
                NotificationManager.Instance.ShowSuccess("Team Member Added", $"{FirstName} {LastName} has been added to your team.");
                DataMessenger.SendRefresh(DataChangeType.TeamMembers);
                
                if (parameter is BaseWindow window)
                {
                    DialogManager.Instance.CloseDialog(window);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Error", $"Failed to add team member: {ex.Message}");
            }
        }

        #region Feedback Methods

        private void AddFeedbackExecuted(object? parameter)
        {
            var vm = new FeedbackViewModel(OnFeedbackDialogClosed, null, _data.Id, false);
            var dialog = new Views.Dialogs.AddFeedbackDialog
            {
                DataContext = vm,
                Owner = System.Windows.Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        private void EditFeedbackExecuted(object? parameter)
        {
            if (SelectedFeedback == null) return;
            
            var vm = new FeedbackViewModel(OnFeedbackDialogClosed, SelectedFeedback, _data.Id, true);
            var dialog = new Views.Dialogs.AddFeedbackDialog
            {
                DataContext = vm,
                Title = "Edit Feedback",
                Owner = System.Windows.Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        private async void DeleteFeedbackExecuted(object? parameter)
        {
            if (SelectedFeedback == null) return;
            
            var result = MessageBoxHelper.Show(
                "Are you sure you want to delete this feedback?",
                "Delete Feedback");
                
            if (result == System.Windows.MessageBoxResult.OK)
            {
                var success = await TrackerDbManager.Instance!.DeleteFeedbackAsync(SelectedFeedback.Id);
                if (success)
                {
                    _feedbacks.Remove(SelectedFeedback);
                    NotificationManager.Instance.ShowSuccess("Deleted", "Feedback has been deleted.");
                    RaisePropertyChanged(nameof(FeedbackCount));
                    RaisePropertyChanged(nameof(HasFeedback));
                    RaisePropertyChanged(nameof(HasNoFeedback));
                }
            }
        }

        private void OnFeedbackDialogClosed()
        {
            LoadFeedbackHistory();
        }

        #endregion

        #region Goal Methods

        private void AddGoalExecuted(object? parameter)
        {
            var vm = new GoalViewModel(OnGoalDialogClosed, null, _data.Id, false);
            var dialog = new Views.Dialogs.AddGoalDialog
            {
                DataContext = vm,
                Owner = System.Windows.Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        private void EditGoalExecuted(object? parameter)
        {
            if (SelectedGoal == null) return;
            
            var vm = new GoalViewModel(OnGoalDialogClosed, SelectedGoal, _data.Id, true);
            var dialog = new Views.Dialogs.AddGoalDialog
            {
                DataContext = vm,
                Title = "Edit Goal",
                Owner = System.Windows.Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        private async void DeleteGoalExecuted(object? parameter)
        {
            if (SelectedGoal == null) return;
            
            var result = MessageBoxHelper.Show(
                $"Are you sure you want to delete the goal '{SelectedGoal.Title}'?",
                "Delete Goal");
                
            if (result == System.Windows.MessageBoxResult.OK)
            {
                var success = await TrackerDbManager.Instance!.DeleteGoalAsync(SelectedGoal.Id);
                if (success)
                {
                    _goals.Remove(SelectedGoal);
                    NotificationManager.Instance.ShowSuccess("Deleted", "Goal has been deleted.");
                    RaisePropertyChanged(nameof(GoalCount));
                    RaisePropertyChanged(nameof(ActiveGoalCount));
                    RaisePropertyChanged(nameof(HasGoals));
                    RaisePropertyChanged(nameof(HasNoGoals));
                }
            }
        }

        private void OnGoalDialogClosed()
        {
            LoadGoals();
        }

        #endregion

        #region Meeting Methods

        private void ViewMeetingDetailsExecuted(object? parameter)
        {
            if (parameter is OneOnOne meeting)
            {
                // Launch the 1:1 dialog in edit mode
                var vm = new OneOnOneViewModel(null, meeting, true);
                var dialog = new Views.Dialogs.AddOneOnOneDialog(vm)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                dialog.ShowDialog();
            }
        }

        #endregion

        #endregion

    }
}
