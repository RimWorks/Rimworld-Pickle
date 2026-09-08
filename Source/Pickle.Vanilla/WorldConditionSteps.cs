using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Weather, the calendar and temperature, which gate a lot of seasonal content.</summary>
[PickleSteps]
public class WorldConditionSteps {
  /// <summary>Asserts the map's current weather matches a def.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="weatherDefName">The weather def expected.</param>
  [Then("the weather is {string}")]
  public void AssertWeather(PickleContext ctx, string weatherDefName) {
    Map map = MapLookup.RequireMap(ctx);
    WeatherDef def = DefLookup.Require<WeatherDef>(weatherDefName);

    ctx.Assert(
        map.weatherManager.curWeather == def,
        $"the weather should be '{weatherDefName}'; {DescribeWeather(map)}");
  }

  /// <summary>Transitions the map to a weather def and waits for it to take effect.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="weatherDefName">The weather def to transition to.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I set the weather to {string}")]
  public async Task SetWeather(PickleContext ctx, string weatherDefName) {
    Map map = MapLookup.RequireMap(ctx);
    WeatherDef def = DefLookup.Require<WeatherDef>(weatherDefName);

    map.weatherManager.TransitionTo(def);

    await ctx.AssertEventually(
        () => map.weatherManager.curWeather == def,
        () => $"the weather never became '{weatherDefName}'; {DescribeWeather(map)}");
  }

  /// <summary>Asserts the map's current season.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="seasonName">The expected season's name.</param>
  [Then("the season is {word}")]
  public void AssertSeason(PickleContext ctx, string seasonName) {
    Map map = MapLookup.RequireMap(ctx);
    Season wanted = RequireSeason(seasonName);

    ctx.Assert(
        GenLocalDate.Season(map) == wanted,
        $"the season should be {seasonName}; it is {GenLocalDate.Season(map)}. {DescribeDate(map)}");
  }

  /// <summary>Jumps the map's calendar forward, day by day, to the first day that falls in a season.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="seasonName">The season to jump to.</param>
  // This moves the calendar and nothing else. No plant grows, no food rots and no pawn gets
  // hungry, because a season is 900000 ticks and the runner cannot tick that far.
  [When("I set the season to {word}")]
  public void SetSeason(PickleContext ctx, string seasonName) {
    Map map = MapLookup.RequireMap(ctx);
    Season wanted = RequireSeason(seasonName);
    int start = Find.TickManager.TicksGame;

    HashSet<Season> seen = [];
    for (int day = 0; day <= GenDate.DaysPerYear; day++) {
      Find.TickManager.DebugSetTicksGame(start + (day * GenDate.TicksPerDay));
      Season now = GenLocalDate.Season(map);
      if (now == wanted) {
        return;
      }

      seen.Add(now);
    }

    Find.TickManager.DebugSetTicksGame(start);
    ctx.Require(
        false,
        $"this map never sees {seasonName} in a whole year. an equatorial tile has one season " +
        $"all year. it sees: {string.Join(", ", seen)}");
  }

  /// <summary>Asserts the map's current hour of day.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="hour">The expected hour, 0 to 23.</param>
  [Then("the hour is {int}")]
  public void AssertHour(PickleContext ctx, int hour) {
    Map map = MapLookup.RequireMap(ctx);

    ctx.Assert(
        GenLocalDate.HourOfDay(map) == hour,
        $"the hour should be {hour}; it is {GenLocalDate.HourOfDay(map)}. {DescribeDate(map)}");
  }

  /// <summary>Jumps the map's calendar forward to the next occurrence of an hour.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="hour">The hour to jump to, 0 to 23.</param>
  // The same calendar jump the season step makes, so nothing ages here either.
  [When("I set the hour to {int}")]
  public void SetHour(PickleContext ctx, int hour) {
    Map map = MapLookup.RequireMap(ctx);
    ctx.Require(hour is >= 0 and <= 23, $"the hour must be 0 to 23; got {hour}");

    int ahead = (((hour - GenLocalDate.HourOfDay(map)) % 24) + 24) % 24;
    Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + (ahead * GenDate.TicksPerHour));

    ctx.Assert(
        GenLocalDate.HourOfDay(map) == hour,
        $"the hour should be {hour} after the jump; it is {GenLocalDate.HourOfDay(map)}. {DescribeDate(map)}");
  }

  /// <summary>Asserts whether it is currently day or night on the map, by sun glow.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="partOfDay">Either <c>day</c>/<c>daytime</c> or <c>night</c>/<c>nighttime</c>.</param>
  [Then("it is {word}")]
  public void AssertDayOrNight(PickleContext ctx, string partOfDay) {
    Map map = MapLookup.RequireMap(ctx);
    bool wantsDay = RequireDayOrNight(partOfDay);
    float glow = GenCelestial.CurCelestialSunGlow(map);

    ctx.Assert(
        GenCelestial.IsDaytime(glow) == wantsDay,
        $"it should be {partOfDay.ToLowerInvariant()}; sun glow is {glow:F2}. {DescribeDate(map)}");
  }

  /// <summary>Asserts a cell's temperature is above a bound.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  /// <param name="bound">The lower bound, exclusive.</param>
  [Then("the temperature at \\({int}, {int}\\) is above {int}")]
  public void AssertCellTempAbove(PickleContext ctx, int x, int z, int bound) {
    AssertCellTemp(ctx, x, z, actual => actual > bound, $"should be above {bound}");
  }

  /// <summary>Asserts a cell's temperature is below a bound.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  /// <param name="bound">The upper bound, exclusive.</param>
  [Then("the temperature at \\({int}, {int}\\) is below {int}")]
  public void AssertCellTempBelow(PickleContext ctx, int x, int z, int bound) {
    AssertCellTemp(ctx, x, z, actual => actual < bound, $"should be below {bound}");
  }

  /// <summary>Asserts the map's outdoor temperature is above a bound.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="bound">The lower bound, exclusive.</param>
  [Then("the outdoor temperature is above {int}")]
  public void AssertOutdoorAbove(PickleContext ctx, int bound) {
    AssertOutdoorTemp(ctx, actual => actual > bound, $"should be above {bound}");
  }

  /// <summary>Asserts the map's outdoor temperature is below a bound.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="bound">The upper bound, exclusive.</param>
  [Then("the outdoor temperature is below {int}")]
  public void AssertOutdoorBelow(PickleContext ctx, int bound) {
    AssertOutdoorTemp(ctx, actual => actual < bound, $"should be below {bound}");
  }

  private static void AssertCellTemp(PickleContext ctx, int x, int z, Func<float, bool> holds, string wanted) {
    Map map = MapLookup.RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    float actual = GenTemperature.GetTemperatureForCell(cell, map);
    ctx.Assert(
        holds(actual),
        $"the temperature at ({x}, {z}) {wanted}; it is {actual:F1}C. " +
        $"outdoors is {map.mapTemperature.OutdoorTemp:F1}C, roofed={cell.Roofed(map)}");
  }

  private static void AssertOutdoorTemp(PickleContext ctx, Func<float, bool> holds, string wanted) {
    Map map = MapLookup.RequireMap(ctx);
    float actual = map.mapTemperature.OutdoorTemp;

    ctx.Assert(
        holds(actual),
        $"the outdoor temperature {wanted}; it is {actual:F1}C. " +
        $"the seasonal average is {map.mapTemperature.SeasonalTemp:F1}C. {DescribeDate(map)}");
  }

  private static Season RequireSeason(string seasonName) {
    if (Enum.TryParse(seasonName, ignoreCase: true, out Season season) && season != Season.Undefined) {
      return season;
    }

    string names = string.Join(", ", Enum.GetNames(typeof(Season)).Where(n => n != nameof(Season.Undefined)));
    throw new InvalidOperationException($"'{seasonName}' is not a season. try one of: {names}");
  }

  private static bool RequireDayOrNight(string partOfDay) {
    return partOfDay.ToLowerInvariant() switch {
      "day" or "daytime" => true,
      "night" or "nighttime" => false,
      _ => throw new InvalidOperationException($"'{partOfDay}' is not a part of day; say day or night"),
    };
  }

  // curWeather changes at once but the sky blends, so a mod reading the perceived weather sees
  // the old one for a while.
  private static string DescribeWeather(Map map) {
    return $"current={map.weatherManager.curWeather?.defName ?? "(none)"} " +
        $"perceived={map.weatherManager.CurWeatherPerceived?.defName ?? "(none)"} " +
        $"age={map.weatherManager.curWeatherAge} ticks";
  }

  private static string DescribeDate(Map map) {
    return $"season={GenLocalDate.Season(map)} hour={GenLocalDate.HourOfDay(map)} " +
        $"day={GenLocalDate.DayOfSeason(map) + 1} of the season, glow={GenCelestial.CurCelestialSunGlow(map):F2}";
  }
}
