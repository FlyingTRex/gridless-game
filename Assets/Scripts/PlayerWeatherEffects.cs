using DigitalRuby.WeatherMaker;
using UnityEngine;

// Bridges Weather Maker's live precipitation state to
// PlayerVitals.bodyTemperature (WEATHER_MAKER_PLANNING.md decision 5 —
// the actual gameplay payoff behind MVP2 item 5, not just visuals).
// Standing in rain/snow/sleet/hail now actively cools the player, using
// the same WarmNear mechanism a lit Campfire already uses to warm them —
// WarmNear's own MoveTowards-based implementation is symmetric, so a
// colder target cools instead of warms with no separate method needed.
// No shelter/indoor detection yet (Weather Maker's NullZone system could
// gate this later) — first-pass, whole-area effect only, same scope this
// project's other "ship the real useful slice" systems started with.
[RequireComponent(typeof(PlayerVitals))]
public class PlayerWeatherEffects : MonoBehaviour
{
    // Cooling target temperature at full (1.0) intensity for each
    // precipitation type, and how fast bodyTemperature moves toward it
    // per second at that same full intensity (scales down linearly with
    // actual intensity below 1.0). First-pass numbers, same "tune by
    // playtesting" status as every other balance value in this project.
    // Snow/hail read colder than rain/sleet.
    [SerializeField] private float rainColdTarget = 20f;
    [SerializeField] private float sleetColdTarget = 15f;
    [SerializeField] private float snowColdTarget = 10f;
    [SerializeField] private float hailColdTarget = 10f;
    [SerializeField] private float maxCoolingRatePerSecond = 3f;

    private PlayerVitals vitals;

    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
    }

    private void Update()
    {
        if (!WeatherMakerPrecipitationManagerScript.HasInstance()) return;

        var precip = WeatherMakerPrecipitationManagerScript.Instance;

        // Whichever precipitation type is currently most intense drives
        // the cooling target — same "strongest wins" convention the
        // manager's own wetness calculation already uses
        // (Mathf.Max(RainIntensity, SleetIntensity)), extended to all
        // four falling-particle types.
        float intensity = precip.RainIntensity;
        float coldTarget = rainColdTarget;

        if (precip.SleetIntensity > intensity) { intensity = precip.SleetIntensity; coldTarget = sleetColdTarget; }
        if (precip.SnowIntensity > intensity) { intensity = precip.SnowIntensity; coldTarget = snowColdTarget; }
        if (precip.HailIntensity > intensity) { intensity = precip.HailIntensity; coldTarget = hailColdTarget; }

        if (intensity <= 0f) return;

        vitals.WarmNear(coldTarget, maxCoolingRatePerSecond * intensity);
    }
}
