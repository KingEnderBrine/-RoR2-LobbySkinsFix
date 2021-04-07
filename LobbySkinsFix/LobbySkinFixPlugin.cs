using BepInEx;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: R2API.Utils.ManualNetworkRegistration]
[assembly: EnigmaticThunder.Util.ManualNetworkRegistration]
namespace LobbySkinsFix
{
    [BepInPlugin("com.KingEnderBrine.LobbySkinFix", "Lobby skin fix", "1.1.1")]
    public class LobbySkinFixPlugin : BaseUnityPlugin
    {
        private static readonly MethodInfo onNetworkUserLoadoutChanged = typeof(RoR2.UI.CharacterSelectController).GetMethod(nameof(RoR2.UI.CharacterSelectController.OnNetworkUserLoadoutChanged), BindingFlags.NonPublic | BindingFlags.Instance);
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