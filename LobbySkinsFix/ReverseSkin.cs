using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LobbySkinsFix
{
    public class ReverseSkin
    {
        private static readonly Dictionary<GameObject, ReverseSkin> reverseSkins = new Dictionary<GameObject, ReverseSkin>();

        private readonly List<CharacterModel.RendererInfo> baseRendererInfos = new List<CharacterModel.RendererInfo>();
        private readonly List<SkinDef.GameObjectActivationTemplate> gameObjectActivationTemplates = new List<SkinDef.GameObjectActivationTemplate>();
        private readonly List<SkinDef.MeshReplacementTemplate> meshReplacementTemplates = new List<SkinDef.MeshReplacementTemplate>();

        private readonly GameObject modelObject;

        private ReverseSkin(GameObject modelObject, SkinDef skinDef)
        {
            this.modelObject = modelObject;
            skinDef.Bake();
            var runtimeSkin = skinDef.runtimeSkin;

            baseRendererInfos.AddRange(modelObject.GetComponent<CharacterModel>().baseRendererInfos);
            foreach (var objectActivation in runtimeSkin.gameObjectActivationTemplates)
            {
                gameObjectActivationTemplates.Add(new SkinDef.GameObjectActivationTemplate
                {
                    path = objectActivation.path,
                    shouldActivate = !objectActivation.shouldActivate
                });
            }
            foreach (var meshReplacement in runtimeSkin.meshReplacementTemplates)
            {
                var renderer = modelObject.transform.Find(meshReplacement.path).GetComponent<Renderer>();
                
                Mesh mesh = null;
                switch (renderer)
                {
                    case MeshRenderer _:
                        mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        break;
                    case SkinnedMeshRenderer skinnedMeshRenderer:
                        mesh = skinnedMeshRenderer.sharedMesh;
                        break;
                }

                meshReplacementTemplates.Add(new SkinDef.MeshReplacementTemplate
                {
                    path = meshReplacement.path,
                    mesh = mesh
                });
            }
        }

        private void Apply()
        {
            var transform = modelObject.transform;
            modelObject.GetComponent<CharacterModel>().baseRendererInfos = baseRendererInfos.ToArray();

            foreach (var objectActivation in gameObjectActivationTemplates)
            {
                transform.Find(objectActivation.path).gameObject.SetActive(objectActivation.shouldActivate);
            }
            foreach (var meshReplacement in meshReplacementTemplates)
            {
                Renderer component = transform.Find(meshReplacement.path).GetComponent<Renderer>();
                switch (component)
                {
                    case MeshRenderer _:
                        component.GetComponent<MeshFilter>().sharedMesh = meshReplacement.mesh;
                        break;
                    case SkinnedMeshRenderer skinnedMeshRenderer:
                        skinnedMeshRenderer.sharedMesh = meshReplacement.mesh;
                        break;
                }
            }
        }

        //RoR2.UI.CharacterSelectController.OnNetworkUserLoadoutChanged()
        internal static void RevertSkinIL(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(5),
                x => x.MatchLdloc(6),
                x => x.MatchCallOrCallvirt(out _));

            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldloc, 5);
            c.Emit(OpCodes.Call, typeof(ReverseSkin).GetMethod(nameof(RevertSkin), BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static void RevertSkin(GameObject modelObject, SkinDef skinDef)
        {
            if (reverseSkins.TryGetValue(modelObject, out var reverseSkin))
            {
                reverseSkin.Apply();
            }
            reverseSkins[modelObject] = new ReverseSkin(modelObject, skinDef);

            VerifyDictionary();
        }

        private static void VerifyDictionary()
        {
            foreach (var emptyModelObject in reverseSkins.Keys.Where(el => !el).ToList())
            {
                reverseSkins.Remove(emptyModelObject);
            }
        }
    }
}
