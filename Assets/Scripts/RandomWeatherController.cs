using DigitalRuby.WeatherMaker;
using UnityEngine;

// Picks a new random weather state on a fixed real-time interval — Ben's
// ask (2026-08-14): "let's add some random weather... change every 5 real
// minutes." World-level system, not player-specific (unlike
// PlayerWeatherEffects, which reacts to whatever this sets).
//
// Deliberately simple: sets WeatherMakerPrecipitationManagerScript
// .Instance.Precipitation/.PrecipitationIntensity directly rather than
// touching Weather Maker's own WeatherZone/Profile system — the manager
// already smoothly tweens between precipitation types on its own
// (PrecipitationChangeDuration, a few seconds by default), so this script
// only needs to pick a target and hand it off.
public class RandomWeatherController : MonoBehaviour
{
    [SerializeField] private float changeIntervalSeconds = 300f;

    // None included in the pool so "clear" is a real possible outcome, not
    // just something that happens by default before the first change.
    private static readonly WeatherMakerPrecipitationType[] WeatherPool =
    {
        WeatherMakerPrecipitationType.None,
        WeatherMakerPrecipitationType.Rain,
        WeatherMakerPrecipitationType.Snow,
        WeatherMakerPrecipitationType.Sleet,
        WeatherMakerPrecipitationType.Hail,
    };

    [SerializeField] private float minIntensity = 0.3f;
    [SerializeField] private float maxIntensity = 0.8f;

    private float secondsUntilNextChange;

    private void Update()
    {
        if (!WeatherMakerPrecipitationManagerScript.HasInstance()) return;

        secondsUntilNextChange -= Time.deltaTime;
        if (secondsUntilNextChange > 0f) return;

        secondsUntilNextChange = changeIntervalSeconds;

        var precip = WeatherMakerPrecipitationManagerScript.Instance;
        var next = WeatherPool[Random.Range(0, WeatherPool.Length)];

        precip.Precipitation = next;
        precip.PrecipitationIntensity = next == WeatherMakerPrecipitationType.None
            ? 0f
            : Random.Range(minIntensity, maxIntensity);
    }
}
