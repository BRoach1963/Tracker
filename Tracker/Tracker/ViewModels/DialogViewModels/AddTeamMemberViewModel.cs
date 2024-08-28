using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Win32;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.DataWrappers;
using Tracker.Helpers;

namespace Tracker.ViewModels.DialogViewModels
{
    public class AddTeamMemberViewModel : BaseDialogViewModel
    {
        #region Fields 

        private TeamMember _data;
        private bool _inEditMode; 

        private ICommand? _toggleEditModeCommand;
        private ICommand? _chooseProfilePicCommand;
        private ICommand? _launchLinkedInProfileCommand;

        private ImageSource? _profileImage;

        private ObservableCollection<EnumWrapper<RoleEnum>> _roles = new ();
        private ObservableCollection<EnumWrapper<EngineeringSpecialtyEnum>> _specialties = new();
        private ObservableCollection<EnumWrapper<SkillLevelEnum>> _levels = new();

        private EnumWrapper<RoleEnum>? _selectedRole;
        private EnumWrapper<EngineeringSpecialtyEnum>? _selectedSpecialty;
        private EnumWrapper<SkillLevelEnum>? _selectedLevel;

        #endregion

        #region Ctor

        public AddTeamMemberViewModel(Action? callback, TeamMember data, bool edit = true) : base(callback)
        {
            _inEditMode = edit;
            _data = data;
            _data.BirthDay = DateTime.Now;
            _data.HireDate = DateTime.Now;
            if (_data.ProfileImage.Length > 0)
            {
                //No Data - use default Image
                ProfileImage = ImageHelper.LoadDefaultProfileImage();
            }
            else
            {
                 
            }

            SetLists();
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
            }
        }
        public EnumWrapper<SkillLevelEnum>? SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                _selectedLevel = value;
                RaisePropertyChanged();
            }
        }

        public EnumWrapper<EngineeringSpecialtyEnum>? SelectedSpeciality
        {
            get => _selectedSpecialty;
            set
            {
                _selectedSpecialty = value;
                RaisePropertyChanged();
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
            }
        }
        public string NickName
        {
            get => _data.NickName;
            set
            {
                _data.NickName = value;
                RaisePropertyChanged();
            }
        }

        public string FirstName
        {
            get => _data.FirstName;
            set
            {
                _data.FirstName = value;
                RaisePropertyChanged();
            }
        }

        public string LastName
        {
            get => _data.LastName;
            set
            {
                _data.LastName = value;
                RaisePropertyChanged();
            }
        }

        public string Email
        {
            get => _data.Email;
            set
            {
                _data.Email = value;
                RaisePropertyChanged();
            }
        }

        public string CellPhone
        {
            get => _data.CellPhone;
            set
            {
                _data.CellPhone = value;
                RaisePropertyChanged();
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
            }
        }

        public string LinkedInProfile
        {
            get => _data.LinkedInProfile;
            set
            {
                _data.LinkedInProfile = value;
                RaisePropertyChanged();
            }
        }

        public string FacebookProfile
        {
            get => _data.FacebookProfile;
            set
            {
                _data.FacebookProfile = value;
                RaisePropertyChanged();
            }
        }

        public string InstagramProfile
        {
            get => _data.InstagramProfile;
            set
            {
                _data.InstagramProfile = value;
                RaisePropertyChanged();
            }
        }

        public string XProfile
        {
            get => _data.XProfile;
            set
            {
                _data.XProfile = value;
                RaisePropertyChanged();
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

        #endregion

    }
}
