using BepInEx;
using R2API.Utils;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace LobbySkinsFix
{
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.KingEnderBrine.LobbySkinFix", "Lobby skin fix", "1.0.0")]
    public class LobbySkinFixPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            IL.RoR2.UI.CharacterSelectController.OnNetworkUserLoadoutChanged += ReverseSkin.RevertSkinIL;
        }

        private void Destroy()
        {
            IL.RoR2.UI.CharacterSelectController.OnNetworkUserLoadoutChanged -= ReverseSkin.RevertSkinIL;
        }
    }
}