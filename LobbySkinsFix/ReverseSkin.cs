using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LobbySkinsFix
{
    public class ReverseSkin : MonoBehaviour
    {
        private readonly List<CharacterModel.RendererInfo> baseRendererInfos = new List<CharacterModel.RendererInfo>();
        private readonly List<SkinDef.GameObjectActivationTemplate> gameObjectActivationTemplates = new List<SkinDef.GameObjectActivationTemplate>();
        private readonly List<SkinDef.MeshReplacementTemplate> meshReplacementTemplates = new List<SkinDef.MeshReplacementTemplate>();

        private void Initialize(GameObject modelObject, SkinDef skinDef)
        {
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
                var rendererTransform = modelObject.transform.Find(meshReplacement.path);
                if (!rendererTransform)
                {
                    continue;
                }

                var renderer = rendererTransform.GetComponent<Renderer>();

                Mesh mesh;
                switch (renderer)
                {
                    case MeshRenderer _:
                        mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        break;
                    case SkinnedMeshRenderer skinnedMeshRenderer:
                        mesh = skinnedMeshRenderer.sharedMesh;
                        break;
                    default:
                        continue;
                }

                meshReplacementTemplates.Add(new SkinDef.MeshReplacementTemplate
                {
                    path = meshReplacement.path,
                    mesh = mesh
                });
            }
        }

        private void Apply(GameObject modelObject)
        {
            var transform = modelObject.transform;
            modelObject.GetComponent<CharacterModel>().baseRendererInfos = baseRendererInfos.ToArray();

            foreach (var objectActivation in gameObjectActivationTemplates)
            {
                var gameActivationTransform = transform.Find(objectActivation.path);
                if (gameActivationTransform)
                {
                    gameActivationTransform.gameObject.SetActive(objectActivation.shouldActivate);
                }
            }
            foreach (var meshReplacement in meshReplacementTemplates)
            {
                var rendererTransform = transform.Find(meshReplacement.path);
                if (!rendererTransform)
                {
                    continue;
                }

                var component = rendererTransform.GetComponent<Renderer>();
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

        internal static void RevertSkinIL(ILContext il)
        {
            var c = new ILCursor(il);

            var skinDefIndex = -1;

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(out skinDefIndex),
                x => x.MatchLdloc(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchCallOrCallvirt<SkinDef>(nameof(SkinDef.Apply)));

            c.Index--;
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldloc, skinDefIndex);
            c.Emit(OpCodes.Call, typeof(ReverseSkin).GetMethod(nameof(RevertSkin), BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static void RevertSkin(GameObject modelObject, SkinDef skinDef)
        {
            var previousReverseSkin = modelObject.GetComponent<ReverseSkin>();
            if (previousReverseSkin)
            {
                previousReverseSkin.Apply(modelObject);
                Destroy(previousReverseSkin);
            }

            var reverseSkin = modelObject.AddComponent<ReverseSkin>();
            reverseSkin.Initialize(modelObject, skinDef);
        }
    }
}
