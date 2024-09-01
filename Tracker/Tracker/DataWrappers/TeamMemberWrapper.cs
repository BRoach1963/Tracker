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

        public EnumWrapper<RoleEnum>? Role => _role;

        public EnumWrapper<SkillLevelEnum>? Level => _level;

        public EnumWrapper<EngineeringSpecialtyEnum>? Speciality => _speciality;

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

        private async void LoadProfileImageIfAvailable(byte[] dataProfileImage)
        {
            _profileImage = await ImageHelper.GetImageSourceFromByteArrayAsync(_data.ProfileImage);
        }


        #endregion
    }
}
