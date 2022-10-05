using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;

namespace Starvation;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Starvation : BaseUnityPlugin
{
	private const string ModName = "Starvation";
	private const string ModVersion = "1.0.1";
	private const string ModGUID = "org.bepinex.plugins.starvation";
	private static int tickCount = 0;

	private enum Toggle
	{
		On = 1,
		Off = 0
	}

	private static ConfigEntry<Toggle> serverConfigLocked = null!;
	private static ConfigEntry<int> tickFrequency = null!;
	private static ConfigEntry<float> damagePerTick = null!;

	private static readonly ConfigSync configSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = "1.0.0" };

	private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
	{
		ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

		SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
		syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

		return configEntry;
	}

	private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

	public void Awake()
	{
		serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
		configSync.AddLockingConfigEntry(serverConfigLocked);
		tickFrequency = config("1 - General", "Damage frequency in seconds", 15, "Time in seconds between every damage tick, if no food is active.");
		damagePerTick = config("1 - General", "Damage per tick", 1f, "Damage taken per tick, if no food is active.");

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);
	}

	[HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
	private class DamagePlayer
	{
		private static void Prefix(Player __instance, float dt, bool forceUpdate)
		{
			if (__instance.m_foodUpdateTimer + dt >= 1.0 && !forceUpdate)
			{
				if (__instance.m_foods.Count == 0)
				{
					++tickCount;
					if (tickCount >= tickFrequency.Value)
					{
						__instance.ApplyDamage(new HitData { m_damage = new HitData.DamageTypes { m_damage = damagePerTick.Value }, m_point = __instance.GetCenterPoint() }, true, true);
						tickCount = 0;
					}
				}
				else
				{
					tickCount = 0;
				}
			}
		}
	}
}
