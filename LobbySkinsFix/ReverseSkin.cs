using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LobbySkinsFix
{
    public class ReverseSkin : MonoBehaviour
    {
        private readonly List<CharacterModel.RendererInfo> baseRendererInfos = new List<CharacterModel.RendererInfo>();
        private readonly List<SkinDef.MeshReplacementTemplate> meshReplacementTemplates = new List<SkinDef.MeshReplacementTemplate>();

        private IEnumerator Initialize(GameObject modelObject, SkinDef skinDef)
        {
            var bakeEnumerator = skinDef.BakeAsync();
            while (bakeEnumerator.MoveNext())
            {
                yield return bakeEnumerator.Current;
            }

            var runtimeSkin = skinDef.runtimeSkin;

            baseRendererInfos.AddRange(modelObject.GetComponent<CharacterModel>().baseRendererInfos);
            foreach (var meshReplacement in runtimeSkin.meshReplacementTemplates)
            {
                var rendererTransform = modelObject.transform.Find(meshReplacement.transformPath);
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
                    transformPath = meshReplacement.transformPath,
                    meshReference = new AssetOrDirectReference<Mesh> { directRef = mesh }
                });
            }
        }

        private void Apply(GameObject modelObject)
        {
            var transform = modelObject.transform;
            modelObject.GetComponent<CharacterModel>().baseRendererInfos = baseRendererInfos.ToArray();

            foreach (var meshReplacement in meshReplacementTemplates)
            {
                var rendererTransform = transform.Find(meshReplacement.transformPath);
                if (!rendererTransform)
                {
                    continue;
                }

                var component = rendererTransform.GetComponent<Renderer>();
                switch (component)
                {
                    case MeshRenderer _:
                        component.GetComponent<MeshFilter>().sharedMesh = meshReplacement.meshReference.directRef;
                        break;
                    case SkinnedMeshRenderer skinnedMeshRenderer:
                        skinnedMeshRenderer.sharedMesh = meshReplacement.meshReference.directRef;
                        break;
                }
            }
        }

        internal static void RevertSkinIL(ILContext il)
        {
            var c = new ILCursor(il);

            var skinIndex = -1;

            if (!c.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdloc(out skinIndex),
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt<ModelSkinController>(nameof(ModelSkinController.ApplySkinAsync))))
            {
                LobbySkinsFixPlugin.InstanceLogger.LogError($"Failed to apply {nameof(RevertSkinIL)} hook");
                return;
            }

            c.Emit(OpCodes.Dup);
            c.Index += 3;
            c.Emit(OpCodes.Ldloc, skinIndex);
            c.Emit(OpCodes.Call, typeof(ReverseSkin).GetMethod(nameof(RevertSkin), BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static IEnumerator RevertSkin(ModelSkinController modelSkinController, IEnumerator applySkinAsync, int skinIndex)
        {
            if ((uint)skinIndex >= modelSkinController.skins.Length)
            {
                skinIndex = 0;
            }

            if (skinIndex != modelSkinController.currentSkinIndex && modelSkinController.skins.Length != 0)
            {
                var previousReverseSkin = modelSkinController.GetComponent<ReverseSkin>();
                if (previousReverseSkin)
                {
                    previousReverseSkin.Apply(modelSkinController.gameObject);
                    Destroy(previousReverseSkin);
                }

                var reverseSkin = modelSkinController.gameObject.AddComponent<ReverseSkin>();
                var init = reverseSkin.Initialize(modelSkinController.gameObject, modelSkinController.skins[skinIndex]);
                while (init.MoveNext())
                {
                    yield return init.Current;
                }
            }

            while (applySkinAsync.MoveNext())
            {
                yield return applySkinAsync.Current;
            }
        }
    }
}
