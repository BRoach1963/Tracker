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

        private ImageSource? _profileImage;

        private ObservableCollection<EnumWrapper<RoleEnum>> _roles = new ();
        private ObservableCollection<EnumWrapper<EngineeringSpecialtyEnum>> _specialties = new();
        private ObservableCollection<EnumWrapper<SkillLevelEnum>> _levels = new();

        private EnumWrapper<RoleEnum>? _selectedRole;
        private EnumWrapper<EngineeringSpecialtyEnum>? _selectedSpecialty;
        private EnumWrapper<SkillLevelEnum>? _selectedLevel;

        private Dictionary<string, object> _changedProperties = new();

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
                ProfileImage = ImageHelper.GetImageSourceFromByteArrayAsync(_data.ProfileImage).Result;
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

        #endregion

        #region Public Properties

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
            await TrackerDataManager.Instance.UpdateTeamMember(_data);
            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
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

        private bool CanExecuteAddTeamMember(object? obj)
        {
            if (string.IsNullOrEmpty(FirstName)) return false;
            if (string.IsNullOrEmpty(LastName)) return false;
            if (string.IsNullOrEmpty(Email)) return false;

            return true;
        }

        private async void AddTeamMemberExecuted(object? parameter)
        {
            await TrackerDataManager.Instance.AddTeamMember(_data);
            if (parameter is BaseWindow window)
            {
                DialogManager.Instance.CloseDialog(window);
            }
        }

        #endregion

    }
}
