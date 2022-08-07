using System;
using Xunit;

namespace RyzenTuner
{
    public class CommonUtilsTest
    {
        [Fact]
        public void TestIsNight()
        {
            var u = new CommonUtils();

            // 测试符合要求的情况
            DateTime[] trueArr =
            {
                DateTime.Parse("2022/08/07 23:59:01"),
                DateTime.Parse("2022/08/07 00:00:00"),
                DateTime.Parse("2022/08/07 01:00:00"),
                DateTime.Parse("2022/08/07 02:00:00"),
                DateTime.Parse("2022/08/07 05:00:00"),
                DateTime.Parse("2022/08/07 06:00:00"),
                DateTime.Parse("2022/08/07 06:59:59"),
            };

            foreach (var item in trueArr)
            {
                Assert.True(u.IsNight(item));
            }
            
            // 测试不符合要求的情况
            DateTime[] falseArr =
            {
                DateTime.Parse("2022/08/07 07:00:00"),
                DateTime.Parse("2022/08/07 08:00:00"),
                DateTime.Parse("2022/08/07 09:00:00"),
                DateTime.Parse("2022/08/07 10:00:00"),
                DateTime.Parse("2022/08/07 15:00:00"),
                DateTime.Parse("2022/08/07 18:00:00"),
                DateTime.Parse("2022/08/07 20:00:00"),
                DateTime.Parse("2022/08/07 22:00:00"),
                DateTime.Parse("2022/08/07 23:59:00"),
            };

            foreach (var item in falseArr)
            {
                Assert.False(u.IsNight(item));
            }
        }
    }
}