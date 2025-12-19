using FluentAssertions;
using System.Globalization;
using System.Windows;
using Tracker.Common.Converters;
using Tracker.Common.Enums;

namespace Tracker.Tests.Converters
{
    public class ConverterTests
    {
        #region BooleanToVisibilityConverter Tests

        [Fact]
        public void BooleanToVisibilityConverter_True_ReturnsVisible()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void BooleanToVisibilityConverter_False_ReturnsCollapsed()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Collapsed);
        }

        [Fact]
        public void BooleanToVisibilityConverter_NullValue_ReturnsCollapsed()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Collapsed);
        }

        #endregion

        #region InverseBooleanConverter Tests

        [Fact]
        public void InverseBooleanConverter_True_ReturnsFalse()
        {
            var converter = new InverseBooleanConverter();
            var result = converter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture);
            result.Should().Be(false);
        }

        [Fact]
        public void InverseBooleanConverter_False_ReturnsTrue()
        {
            var converter = new InverseBooleanConverter();
            var result = converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture);
            result.Should().Be(true);
        }

        [Fact]
        public void InverseBooleanConverter_ConvertBack_True_ReturnsFalse()
        {
            var converter = new InverseBooleanConverter();
            var result = converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);
            result.Should().Be(false);
        }

        #endregion

        #region NullToVisibilityConverter Tests

        [Fact]
        public void NullToVisibilityConverter_NotNull_ReturnsVisible()
        {
            var converter = new NullToVisibilityConverter();
            var result = converter.Convert("some value", typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void NullToVisibilityConverter_Null_ReturnsCollapsed()
        {
            var converter = new NullToVisibilityConverter();
            var result = converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Collapsed);
        }

        [Fact]
        public void NullToVisibilityConverter_EmptyString_ReturnsCollapsed()
        {
            var converter = new NullToVisibilityConverter();
            var result = converter.Convert("", typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Collapsed);
        }

        #endregion

        #region StringToVisibilityConverter Tests

        [Fact]
        public void StringToVisibilityConverter_NonEmptyString_ReturnsVisible()
        {
            var converter = new StringToVisibilityConverter();
            var result = converter.Convert("Hello", typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void StringToVisibilityConverter_EmptyString_ReturnsCollapsed()
        {
            var converter = new StringToVisibilityConverter();
            var result = converter.Convert("", typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Collapsed);
        }

        [Fact]
        public void StringToVisibilityConverter_Whitespace_ReturnsCollapsed()
        {
            var converter = new StringToVisibilityConverter();
            var result = converter.Convert("   ", typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Collapsed);
        }

        #endregion

        #region EnumToStringConverter Tests

        [Fact]
        public void EnumToStringConverter_ShouldConvertEnumToString()
        {
            var converter = new EnumToStringConverter();
            var result = converter.Convert(TaskPriorityEnum.High, typeof(string), null, CultureInfo.InvariantCulture);
            result.Should().Be("High");
        }

        [Fact]
        public void EnumToStringConverter_ShouldHandleNull()
        {
            var converter = new EnumToStringConverter();
            var result = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
            result.Should().Be(string.Empty);
        }

        #endregion

        #region BoolToStringConverter Tests

        [Fact]
        public void BoolToStringConverter_True_ReturnsYes()
        {
            var converter = new BoolToStringConverter();
            var result = converter.Convert(true, typeof(string), "Yes|No", CultureInfo.InvariantCulture);
            result.Should().Be("Yes");
        }

        [Fact]
        public void BoolToStringConverter_False_ReturnsNo()
        {
            var converter = new BoolToStringConverter();
            var result = converter.Convert(false, typeof(string), "Yes|No", CultureInfo.InvariantCulture);
            result.Should().Be("No");
        }

        [Fact]
        public void BoolToStringConverter_CustomStrings()
        {
            var converter = new BoolToStringConverter();
            var result = converter.Convert(true, typeof(string), "Active|Inactive", CultureInfo.InvariantCulture);
            result.Should().Be("Active");
        }

        #endregion

        #region DateTimeConverter Tests

        [Fact]
        public void RelativeDateConverter_Today_ReturnsToday()
        {
            var converter = new RelativeDateConverter();
            var result = converter.Convert(DateTime.Today, typeof(string), null, CultureInfo.InvariantCulture);
            result.Should().Be("Today");
        }

        [Fact]
        public void RelativeDateConverter_Tomorrow_ReturnsTomorrow()
        {
            var converter = new RelativeDateConverter();
            var result = converter.Convert(DateTime.Today.AddDays(1), typeof(string), null, CultureInfo.InvariantCulture);
            result.Should().Be("Tomorrow");
        }

        [Fact]
        public void RelativeDateConverter_Yesterday_ReturnsYesterday()
        {
            var converter = new RelativeDateConverter();
            var result = converter.Convert(DateTime.Today.AddDays(-1), typeof(string), null, CultureInfo.InvariantCulture);
            result.Should().Be("Yesterday");
        }

        #endregion

        #region CountToVisibilityConverter Tests

        [Fact]
        public void CountToVisibilityConverter_PositiveCount_ReturnsVisible()
        {
            var converter = new CountToVisibilityConverter();
            var result = converter.Convert(5, typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void CountToVisibilityConverter_Zero_ReturnsCollapsed()
        {
            var converter = new CountToVisibilityConverter();
            var result = converter.Convert(0, typeof(Visibility), null, CultureInfo.InvariantCulture);
            result.Should().Be(Visibility.Collapsed);
        }

        #endregion
    }
}
