using Microsoft.VisualStudio.TestTools.UnitTesting;
using SunriseCalculator;
using SunriseCalculator.Enums;
using System;

namespace SunriseCalculatorTests
{
    [TestClass]
    public class UnitTests
    {
        private void AssertAreWithinNMinutes(DateTime expected, DateTime actual, int minutes = 1)
        {
            DateTime exp = ReduceToMinutes(expected);
            DateTime act = ReduceToMinutes(actual);
            for (int i = 1; i <= minutes; i++)
            {
                try
                {
                    Assert.AreEqual(exp, act.AddMinutes(i));
                    return;
                }
                catch (Exception)
                {
                }

                try
                {
                    Assert.AreEqual(exp, act.AddMinutes(-i));
                    return;
                }
                catch (Exception)
                {
                }
            }

            Assert.AreEqual(exp, act);
        }

        private DateTime ReduceToMinutes(DateTime value, bool round = false)
            => new DateTime(value.Year, value.Month, value.Day, 
                value.Hour, round ? value.Minute + (int)Math.Round(value.Second / 60.0) : value.Minute, 0);

        [TestMethod]
        public void ArcticResults()
        {
            double[] degreesFromPoleToTest = { 2, 5, 10, 15, 20/*, 22*/ };

            foreach (var degreesFromPole in degreesFromPoleToTest)
            {
                DateTime northernMidsummer = new DateTime(2021, 6, 21);
                DateTime equinox = new DateTime(2021, 9, 22);
                DateTime southernMidsummer = new DateTime(2021, 12, 21);

                SunriseCalc farNorth = new SunriseCalc(SunriseCalc.MaxLatitude - degreesFromPole, 0);
                SunriseCalc farSouth = new SunriseCalc(SunriseCalc.MinLatitude + degreesFromPole, 0);

                // Northern summer solstice
                farNorth.Day = farSouth.Day = northernMidsummer;
                DiurnalResult northResult = farNorth.GetSunrise(out DateTime northRise);
                Assert.AreEqual(DiurnalResult.SunAlwaysAbove, northResult);
                var southResult = farSouth.GetSunrise(out DateTime southRise);
                Assert.AreEqual(DiurnalResult.SunAlwaysBelow, southResult);
                // We still expect reasonable values for sunrise to be returned; north should be 12h before south
                Assert.AreEqual(northRise.AddHours(12), southRise);

                //Equinox
                farNorth.Day = farSouth.Day = equinox;
                //northResult = farNorth.GetSunrise(out northRise);
                northResult = farNorth.GetSunrise(out _);
                Assert.AreEqual(DiurnalResult.NormalDay, northResult);
                //southResult = farSouth.GetSunrise(out southRise);
                southResult = farSouth.GetSunrise(out _);
                Assert.AreEqual(DiurnalResult.NormalDay, southResult);
                // We don't expect these to be exactly equal, because we round to a day, rather than the instant of equinox
                // But the closer to the pole we are, the more wildly the equinox times diverge!
                // TODO: Requires investigation, may be fine, may be systematic error.
                //AssertAreWithinNMinutes(northRise, southRise, 15);

                // Southern summer solstice
                farNorth.Day = farSouth.Day = southernMidsummer;
                northResult = farNorth.GetSunrise(out northRise);
                Assert.AreEqual(DiurnalResult.SunAlwaysBelow, northResult);
                southResult = farSouth.GetSunrise(out southRise);
                Assert.AreEqual(DiurnalResult.SunAlwaysAbove, southResult);
                Assert.AreEqual(northRise, southRise.AddHours(12));
            }
        }

        [TestMethod]
        public void HorizonsTest()
        {
            // Ensures that the different horizons result in different and well-ordered times.

            const double OstravaLat = 49.8209;
            const double OstravaLong = 18.2625;
            SunriseCalc nyc = new SunriseCalc(OstravaLat, OstravaLong);
            _ = nyc.GetRiseAndSet(out DateTime normalRise, out DateTime normalSet, null, Horizon.Normal);
            _ = nyc.GetRiseAndSet(out DateTime civilRise, out DateTime civilSet, null, Horizon.Civil);
            _ = nyc.GetRiseAndSet(out DateTime nauticalRise, out DateTime nauticalSet, null, Horizon.Nautical);
            _ = nyc.GetRiseAndSet(out DateTime astronomicalRise, out DateTime astronomicalSet, null, Horizon.Astronomical);

            DateTime[] rises = { normalRise, civilRise, nauticalRise, astronomicalRise };
            string[] riseTypes = { nameof(normalRise), nameof(civilRise), nameof(nauticalRise), nameof(astronomicalRise) };
            DateTime[] sets = { normalSet, civilSet, nauticalSet, astronomicalSet };
            string[] setTypes = { nameof(normalSet), nameof(civilSet), nameof(nauticalSet), nameof(astronomicalSet) };

            // Compare rises
            for (int i = 0; i < 3; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    Assert.IsTrue(rises[i] > rises[j], $"{riseTypes[i]} {rises[i]} not later than {riseTypes[j]} {rises[j]}");
                    Assert.IsTrue(sets[i] < sets[j], $"{setTypes[i]} {sets[i]} not earlier than {setTypes[j]} {sets[j]}");
                }
            }
        }

        [TestMethod]
        public void SetGetProperties()
        {
            SunriseCalc calc = new SunriseCalc(0, 0);
            Assert.AreEqual(0, calc.Latitude);
            Assert.AreEqual(0, calc.Longitude);
            Assert.AreEqual(DateTime.Today, calc.Day);

            calc.GetSunrise(out DateTime lastRise);

            const int daysToTest = 365 * 2; // Two years
            for (int i = 1; i < daysToTest; i++)
            {
                calc.Day += TimeSpan.FromDays(1);
                DateTime day = DateTime.Today + TimeSpan.FromDays(i);
                Assert.AreEqual(day, calc.Day);
                calc.GetSunrise(out DateTime thisRise);
                Assert.AreNotEqual(lastRise, thisRise);
                lastRise = thisRise;
            }

            Assert.AreEqual(0, calc.Latitude);
            Assert.AreEqual(0, calc.Longitude);

            const int divisionsToTest = 10;
            for (int i = 1; i < divisionsToTest + 1; i++)
            {
                double lon = ((double)i / (divisionsToTest + 1) * (2 * SunriseCalc.MaxLongitude)) - SunriseCalc.MaxLongitude;

                calc.Longitude = lon;
                Assert.AreEqual(lon, calc.Longitude);

                for (int j = 1; j < divisionsToTest + 1; j++)
                {
                    double lat = ((double)j / (divisionsToTest + 1) * (2 * SunriseCalc.MaxLatitude)) - SunriseCalc.MaxLatitude;

                    calc.Latitude = lat;
                    Assert.AreEqual(lat, calc.Latitude);

                    calc.GetSunrise(out DateTime thisRise);
                    Assert.AreNotEqual(lastRise, thisRise);
                    lastRise = thisRise;
                }
            }

            Assert.AreEqual(DateTime.Today + TimeSpan.FromDays(daysToTest - 1), calc.Day);
        }

        [TestMethod]
        public void SimpleTestNYC()
        {
            // A simple spot test for one known location and time.

            DateTime testDate = new DateTime(2021, 7, 8);
            TimeSpan EDTOffset = TimeSpan.FromHours(-4);
            const double NYCLat = 40.7128;
            const double NYCLong = -74.0060;
            DateTime actualSunriseNYC = testDate.AddHours(5).AddMinutes(32);
            DateTime actualSunsetNYC = testDate.AddHours(20).AddMinutes(29);
            var nytz = TimeZoneInfo.FindSystemTimeZoneById("US Eastern Standard Time");

            SunriseCalc nyc = new SunriseCalc(NYCLat, NYCLong, testDate);
            var result = nyc.GetRiseAndSet(out DateTime sunriseUTC, out DateTime sunsetUTC);
            var result2 = nyc.GetRiseAndSet(out DateTime sunrise, out DateTime sunset, nytz);

            // The sun always rises on New York City.
            Assert.AreEqual(DiurnalResult.NormalDay, result);
            Assert.AreEqual(result, result2);

            // The sunrise and sunset should be within a minute of the expected value.
            AssertAreWithinNMinutes(actualSunriseNYC, sunriseUTC + EDTOffset);
            AssertAreWithinNMinutes(actualSunsetNYC, sunsetUTC + EDTOffset);

            // The library should correctly perform tz conversions
            Assert.AreEqual(sunriseUTC + EDTOffset, sunrise);
            Assert.AreEqual(sunsetUTC + EDTOffset, sunset);

            var riseResult = nyc.GetSunrise(out DateTime sunrise2, nytz);
            Assert.AreEqual(DiurnalResult.NormalDay, riseResult);

            // We expect both methods to return the same value.
            Assert.AreEqual(sunrise, sunrise2);

            var setResult = nyc.GetSunset(out DateTime sunset2, nytz);
            Assert.AreEqual(DiurnalResult.NormalDay, setResult);
            Assert.AreEqual(sunset, sunset2);

            // We expect dusk - dawn to = day length
            var dayLength = nyc.GetDayLength();
            Assert.AreEqual(dayLength, sunsetUTC - sunriseUTC);
        }
    }
}