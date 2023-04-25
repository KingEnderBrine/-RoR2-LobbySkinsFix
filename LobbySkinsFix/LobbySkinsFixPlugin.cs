using BepInEx;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using RoR2.SurvivorMannequins;
using System.Reflection;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion(LobbySkinsFix.LobbySkinsFixPlugin.Version)]
namespace LobbySkinsFix
{
    [BepInPlugin(GUID, Name, Version)]
    public class LobbySkinsFixPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.KingEnderBrine.LobbySkinsFix";
        public const string Name = "Lobby Skins Fix";
        public const string Version = "1.2.1";

        private static readonly MethodInfo onNetworkUserLoadoutChanged = typeof(SurvivorMannequinSlotController).GetMethod(nameof(SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance), BindingFlags.NonPublic | BindingFlags.Instance);

        private void Awake()
        {
            HookEndpointManager.Modify(onNetworkUserLoadoutChanged, (ILContext.Manipulator)ReverseSkin.RevertSkinIL);
        }

        private void Destroy()
        {
            HookEndpointManager.Unmodify(onNetworkUserLoadoutChanged, (ILContext.Manipulator)ReverseSkin.RevertSkinIL);
        }
    }
}