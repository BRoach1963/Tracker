using System.Globalization;
using System.Windows.Media;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Helpers;

namespace Tracker.DataWrappers
{
    public class TeamMemberWrapper : BaseDataWrapper
    {
        #region Fields

        private TeamMember _data;
        private EnumWrapper<RoleEnum> _role;
        private EnumWrapper<SkillLevelEnum> _level;
        private EnumWrapper<EngineeringSpecialtyEnum> _speciality;
        private ImageSource? _profileImage;

        #endregion

        #region Ctor

        public TeamMemberWrapper(TeamMember? data = null)
        {
            _data = data ?? new TeamMember();
            _role = new EnumWrapper<RoleEnum>(_data.Role);
            _level = new EnumWrapper<SkillLevelEnum>(_data.SkillLevel);
            _speciality = new EnumWrapper<EngineeringSpecialtyEnum>(_data.Specialty);
            LoadProfileImageIfAvailable(_data.ProfileImage);
        }

        #endregion

        #region Public Properties

        public TeamMember Data => _data;

        public EnumWrapper<RoleEnum>? Role => _role;

        public EnumWrapper<SkillLevelEnum>? Level => _level;

        public EnumWrapper<EngineeringSpecialtyEnum>? Speciality => _speciality;

        public string? JobTitle
        {
            get => _data.JobTitle;
            set
            {
                _data.JobTitle = value;
                RaisePropertyChanged();
            }
        }

        public string? Nickname
        {
            get => _data.Nickname;
            set
            {
                _data.Nickname = value;
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

        public string? Email
        {
            get => _data.Email;
            set
            {
                _data.Email = value;
                RaisePropertyChanged();
            }
        }

        public string? Phone
        {
            get => _data.Phone;
            set
            {
                _data.Phone = value;
                RaisePropertyChanged();
            }
        }

        public string HireDateDisplay
        {
            get => _data.HireDate == null ? "MM/DD/YYYY" : _data.HireDate.Value.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParseExact(value, "MM/dd/yyyy", null, DateTimeStyles.None, out DateTime date))
                {
                    _data.HireDate = date;
                }
                RaisePropertyChanged();
            }
        }

        public string BirthdayDisplay
        {
            get => _data.Birthday == null ? "MM/DD" : _data.Birthday.Value.ToString("MM/dd");
            set
            {
                if (DateTime.TryParseExact(value, "MM/dd", null, DateTimeStyles.None, out DateTime date))
                {
                    _data.Birthday = date;
                }
                RaisePropertyChanged();
            }
        }

        public string TerminationDateDisplay
        {
            get => _data.TerminationDate == null ? "MM/DD/YYYY" : _data.TerminationDate.Value.ToString("MM/dd/yyyy");
            set
            {
                if (DateTime.TryParseExact(value, "MM/dd/yyyy", null, DateTimeStyles.None, out DateTime date))
                {
                    _data.TerminationDate = date;
                }
                RaisePropertyChanged();
            }
        }

        public DateTime? Birthday
        {
            get => _data.Birthday;
            set
            {
                _data.Birthday = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(BirthdayDisplay));
            }
        }

        public DateTime? HireDate
        {
            get => _data.HireDate;
            set
            {
                _data.HireDate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HireDateDisplay));
            }
        }

        public DateTime? TerminationDate
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

        public string? LinkedInUrl
        {
            get => _data.LinkedInUrl;
            set
            {
                _data.LinkedInUrl = value;
                RaisePropertyChanged();
            }
        }

        public string? FacebookProfile
        {
            get => _data.FacebookProfile;
            set
            {
                _data.FacebookProfile = value;
                RaisePropertyChanged();
            }
        }

        public string? InstagramProfile
        {
            get => _data.InstagramProfile;
            set
            {
                _data.InstagramProfile = value;
                RaisePropertyChanged();
            }
        }

        public string? XProfile
        {
            get => _data.XProfile;
            set
            {
                _data.XProfile = value;
                RaisePropertyChanged();
            }
        }

        public string? Department
        {
            get => _data.Department;
            set
            {
                _data.Department = value;
                RaisePropertyChanged();
            }
        }

        public string? Location
        {
            get => _data.Location;
            set
            {
                _data.Location = value;
                RaisePropertyChanged();
            }
        }

        public string? Bio
        {
            get => _data.Bio;
            set
            {
                _data.Bio = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Private Methods

        private async void LoadProfileImageIfAvailable(byte[] dataProfileImage)
        {
            _profileImage = await ImageHelper.GetImageSourceFromByteArrayAsync(_data.ProfileImage);
            RaisePropertyChanged(nameof(ProfileImage));
        }

        #endregion
    }
}
