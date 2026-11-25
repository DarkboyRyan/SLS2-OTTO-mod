using BaseLib.Utils.NodeFactories;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace OttoMod;

// Explicit construction avoids depending on PackedScene.Instantiate being
// intercepted by BaseLib on every platform and runtime configuration.
[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Create))]
internal static class OttoRestSitePatch
{
    private static readonly Action<NRestSiteCharacter, Player> SetPlayer =
        AccessTools.MethodDelegate<Action<NRestSiteCharacter, Player>>(
            AccessTools.PropertySetter(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter.Player)));

    private static readonly AccessTools.FieldRef<NRestSiteCharacter, int> CharacterIndex =
        AccessTools.FieldRefAccess<NRestSiteCharacter, int>("_characterIndex");

    [HarmonyPrefix]
    private static bool Create(Player player, int characterIndex, ref NRestSiteCharacter __result)
    {
        if (player.Character is not OttoCharacter otto)
            return true;

        var scene = NodeFactory<NRestSiteCharacter>.CreateFromScene(otto.CustomRestSiteAnimPath);
        SetPlayer(scene, player);
        CharacterIndex(scene) = characterIndex;
        __result = scene;
        return false;
    }
}
